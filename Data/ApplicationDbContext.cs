using System.Globalization;
using System.Diagnostics;
using System.Text;
using Microsoft.Data.SqlClient;
using gift_of_the_givers.Models;

namespace gift_of_the_givers.Data;

public class ApplicationDbContext
{
    private const string SchemaScriptRelativePath = @"Database\Schema.sql";
    private const string DefaultDatabaseName = "GiftOfTheGivers";
    private readonly string _configuredConnectionString;
    private string _connectionString;

    public ApplicationDbContext(IConfiguration configuration)
    {
        _configuredConnectionString = configuration.GetConnectionString("DefaultConnection")
            ?? BuildSqlConnectionString("(localdb)\\MSSQLLocalDB", DefaultDatabaseName);
        _connectionString = _configuredConnectionString;
    }

    // Creates a fresh SQL connection for each operation so the prototype stays simple and stateless.
    private SqlConnection CreateConnection() => new(_connectionString);

    // Builds a reusable local SQL connection string for the common developer instances we want the app to try automatically.
    private static string BuildSqlConnectionString(string dataSource, string databaseName)
    {
        return $"Server={dataSource};Database={databaseName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
    }

    // Builds an administrative connection string that points at master so the app can create the target database if it is missing.
    private SqlConnection CreateMasterConnection()
    {
        var builder = new SqlConnectionStringBuilder(_connectionString)
        {
            InitialCatalog = "master"
        };

        return new SqlConnection(builder.ConnectionString);
    }

    // Loads and runs the schema script on startup so a new database can be prepared without an EF migration step.
    public async Task InitializeAsync()
    {
        _connectionString = await ResolveConnectionStringAsync();
        await EnsureDatabaseAsync();

        await using var connection = CreateConnection();
        await connection.OpenAsync();
        await ExecuteSchemaScriptAsync(connection);
    }

    // Tries the configured connection string first, then the common local SQL instances, so the app can self-select whichever server is actually installed.
    private async Task<string> ResolveConnectionStringAsync()
    {
        var databaseName = GetDatabaseNameOrDefault(_configuredConnectionString);
        var candidates = new[]
        {
            _configuredConnectionString,
            BuildSqlConnectionString("(localdb)\\MSSQLLocalDB", databaseName),
            BuildSqlConnectionString(@".\SQLEXPRESS", databaseName),
            BuildSqlConnectionString(".", databaseName),
            BuildSqlConnectionString("localhost", databaseName)
        };

        foreach (var candidate in candidates)
        {
            if (await CanOpenMasterAsync(candidate))
            {
                return candidate;
            }

            if (IsLocalDbConnectionString(candidate) && await TryStartLocalDbAsync(candidate) && await CanOpenMasterAsync(candidate))
            {
                return candidate;
            }
        }

        return _configuredConnectionString;
    }

    // Makes the server side ready by optionally starting LocalDB, creating the database when needed, and verifying the app connection can open.
    private async Task EnsureDatabaseAsync()
    {
        var databaseName = GetDatabaseNameOrDefault(_connectionString);
        await using var connection = CreateMasterConnection();
        await connection.OpenAsync();

        if (!await DatabaseExistsAsync(connection, databaseName))
        {
            await ExecuteNonQueryAsync(connection, $"CREATE DATABASE [{EscapeSqlIdentifier(databaseName)}];");
        }
    }

    // Checks whether the server portion of the connection string is reachable by opening the master database instead of the app database.
    private static async Task<bool> CanOpenMasterAsync(string connectionString)
    {
        try
        {
            await using var connection = CreateMasterConnection(connectionString);
            await connection.OpenAsync();
            return true;
        }
        catch (SqlException)
        {
            return false;
        }
    }

    // Returns the database name from the configured connection string so we can create it when the instance is healthy but the database itself is missing.
    private static string GetDatabaseNameOrDefault(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        return string.IsNullOrWhiteSpace(builder.InitialCatalog) ? DefaultDatabaseName : builder.InitialCatalog;
    }

    // Escapes a database or object name for dynamic SQL so create statements stay safe even when names contain closing brackets.
    private static string EscapeSqlIdentifier(string identifier)
    {
        return identifier.Replace("]", "]]");
    }

    // Builds a master-connection probe for a specific connection string without mutating the actual app connection settings.
    private static SqlConnection CreateMasterConnection(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master"
        };

        return new SqlConnection(builder.ConnectionString);
    }

    // Checks the master catalog for the target database name.
    private static async Task<bool> DatabaseExistsAsync(SqlConnection connection, string databaseName)
    {
        await using var command = new SqlCommand("""
SELECT COUNT(1)
FROM sys.databases
WHERE name = @DatabaseName;
""", connection);
        command.Parameters.AddWithValue("@DatabaseName", databaseName);
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result, CultureInfo.InvariantCulture) > 0;
    }

    // Starts the default LocalDB instance when the application points at LocalDB but the instance has not been started yet.
    private static async Task<bool> TryStartLocalDbAsync(string connectionString)
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            var instanceName = GetLocalDbInstanceName(builder.DataSource);
            var startInfo = new ProcessStartInfo
            {
                FileName = "sqllocaldb",
                Arguments = $"start {instanceName}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return false;
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            return process.ExitCode == 0
                || output.Contains("already running", StringComparison.OrdinalIgnoreCase)
                || output.Contains("started", StringComparison.OrdinalIgnoreCase)
                || error.Contains("already running", StringComparison.OrdinalIgnoreCase)
                || error.Contains("started", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    // Detects whether the connection string is pointing at LocalDB so the bootstrapper knows when it can try to start the instance itself.
    private static bool IsLocalDbConnectionString(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        return builder.DataSource.Contains("localdb", StringComparison.OrdinalIgnoreCase);
    }

    // Extracts the LocalDB instance name from the data source, falling back to the default instance name when the connection string uses the shorthand format.
    private static string GetLocalDbInstanceName(string dataSource)
    {
        var separatorIndex = dataSource.LastIndexOf('\\');
        if (separatorIndex >= 0 && separatorIndex < dataSource.Length - 1)
        {
            return dataSource[(separatorIndex + 1)..];
        }

        return "MSSQLLocalDB";
    }

    // Ensures a role exists before user-role records are inserted.
    public async Task EnsureRoleAsync(string roleName)
    {
        if (await RoleExistsAsync(roleName))
        {
            return;
        }

        await using var connection = CreateConnection();
        await connection.OpenAsync();
        await ExecuteNonQueryAsync(connection, """
INSERT INTO dbo.AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
VALUES (@Id, @Name, @NormalizedName, @ConcurrencyStamp);
""",
            P("@Id", Guid.NewGuid().ToString("N")),
            P("@Name", roleName),
            P("@NormalizedName", roleName.ToUpperInvariant()),
            P("@ConcurrencyStamp", Guid.NewGuid().ToString("N")));
    }

    // Checks whether the normalized role name is already stored.
    public async Task<bool> RoleExistsAsync(string roleName)
    {
        return await ExecuteScalarAsync<int?>("""
SELECT 1
FROM dbo.AspNetRoles
WHERE NormalizedName = @NormalizedName;
""", P("@NormalizedName", roleName.ToUpperInvariant())) is not null;
    }

    // Returns the primary key for a role name so the join table can reference it.
    public async Task<string?> GetRoleIdAsync(string roleName)
    {
        return await ExecuteScalarAsync<string?>("""
SELECT TOP 1 Id
FROM dbo.AspNetRoles
WHERE NormalizedName = @NormalizedName;
""", P("@NormalizedName", roleName.ToUpperInvariant()));
    }

    // Adds a role link for the user only when that pair does not already exist.
    public async Task AddUserToRoleAsync(string userId, string roleName)
    {
        await EnsureRoleAsync(roleName);
        var roleId = await GetRoleIdAsync(roleName);
        if (roleId is null)
        {
            throw new InvalidOperationException($"Role '{roleName}' could not be resolved.");
        }

        await using var connection = CreateConnection();
        await connection.OpenAsync();
        await ExecuteNonQueryAsync(connection, """
IF NOT EXISTS (SELECT 1 FROM dbo.AspNetUserRoles WHERE UserId = @UserId AND RoleId = @RoleId)
BEGIN
    INSERT INTO dbo.AspNetUserRoles (UserId, RoleId)
    VALUES (@UserId, @RoleId);
END
""",
            P("@UserId", userId),
            P("@RoleId", roleId));
    }

    // Returns all role names associated with the supplied user identifier.
    public async Task<IReadOnlyList<string>> GetRolesForUserAsync(string userId)
    {
        return await QueryListAsync("""
SELECT r.Name
FROM dbo.AspNetUserRoles ur
INNER JOIN dbo.AspNetRoles r ON r.Id = ur.RoleId
WHERE ur.UserId = @UserId
ORDER BY r.Name;
""",
            reader => reader["Name"] as string ?? string.Empty,
            P("@UserId", userId));
    }

    // Looks up a user by email, which is the main login identifier in this prototype.
    public async Task<ApplicationUser?> FindUserByEmailAsync(string email)
    {
        return await QuerySingleOrDefaultAsync("""
SELECT TOP 1 *
FROM dbo.AspNetUsers
WHERE NormalizedEmail = @NormalizedEmail;
""",
            MapUser,
            P("@NormalizedEmail", email.Trim().ToUpperInvariant()));
    }

    // Looks up a user by primary key for the active authentication cookie.
    public async Task<ApplicationUser?> FindUserByIdAsync(string userId)
    {
        return await QuerySingleOrDefaultAsync("""
SELECT TOP 1 *
FROM dbo.AspNetUsers
WHERE Id = @Id;
""",
            MapUser,
            P("@Id", userId));
    }

    // Returns true when the email address is already taken.
    public async Task<bool> UserExistsByEmailAsync(string email)
    {
        return await FindUserByEmailAsync(email) != null;
    }

    // Inserts the split-name user record into SQL Server.
    public async Task CreateUserAsync(ApplicationUser user)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();
        await ExecuteNonQueryAsync(connection, """
INSERT INTO dbo.AspNetUsers (
    Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
    PasswordHash, SecurityStamp, ConcurrencyStamp, FirstName, LastName, CreatedUtc
)
VALUES (
    @Id, @UserName, @NormalizedUserName, @Email, @NormalizedEmail, @EmailConfirmed,
    @PasswordHash, @SecurityStamp, @ConcurrencyStamp, @FirstName, @LastName, @CreatedUtc
);
""",
            BuildUserParameters(user));
    }

    // Updates the user row in case a future page needs to change the stored account details.
    public async Task UpdateUserAsync(ApplicationUser user)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();
        await ExecuteNonQueryAsync(connection, """
UPDATE dbo.AspNetUsers
SET
    UserName = @UserName,
    NormalizedUserName = @NormalizedUserName,
    Email = @Email,
    NormalizedEmail = @NormalizedEmail,
    EmailConfirmed = @EmailConfirmed,
    PasswordHash = @PasswordHash,
    SecurityStamp = @SecurityStamp,
    ConcurrencyStamp = @ConcurrencyStamp,
    FirstName = @FirstName,
    LastName = @LastName,
    CreatedUtc = @CreatedUtc
WHERE Id = @Id;
""",
            BuildUserParameters(user));
    }

    // Saves a donation and returns the generated row identifier.
    public async Task<int> InsertDonationAsync(Donation donation)
    {
        var result = await ExecuteScalarAsync<int>("""
INSERT INTO dbo.Donations (
    DonorName, DonorEmail, IsAnonymous, UserId, Amount, Currency, Frequency, DonationDate
)
OUTPUT INSERTED.Id
VALUES (
    @DonorName, @DonorEmail, @IsAnonymous, @UserId, @Amount, @Currency, @Frequency, @DonationDate
);
""",
            BuildDonationParameters(donation));

        donation.Id = result;
        return donation.Id;
    }

    // Returns a donation by its identity value.
    public async Task<Donation?> GetDonationByIdAsync(int id)
    {
        return await QuerySingleOrDefaultAsync("""
SELECT TOP 1 *
FROM dbo.Donations
WHERE Id = @Id;
""",
            MapDonation,
            P("@Id", id));
    }

    // Returns the newest donations first for employee review.
    public async Task<IReadOnlyList<Donation>> GetRecentDonationsAsync(int take)
    {
        return await QueryListAsync("""
SELECT TOP (@Take) *
FROM dbo.Donations
ORDER BY DonationDate DESC;
""",
            MapDonation,
            P("@Take", take));
    }

    // Returns a user's donation history for summary cards.
    public async Task<IReadOnlyList<Donation>> GetRecentDonationsForUserAsync(string userId, int take)
    {
        return await QueryListAsync("""
SELECT TOP (@Take) *
FROM dbo.Donations
WHERE UserId = @UserId
ORDER BY DonationDate DESC;
""",
            MapDonation,
            P("@Take", take),
            P("@UserId", userId));
    }

    // Counts user donations so the prototype can show a simple total.
    public async Task<int> CountDonationsForUserAsync(string userId)
    {
        return await ExecuteScalarAsync<int>("""
SELECT COUNT(1)
FROM dbo.Donations
WHERE UserId = @UserId;
""",
            P("@UserId", userId));
    }

    // Sums user donations so the employee or account view can show the rough financial impact.
    public async Task<decimal> SumDonationsForUserAsync(string userId)
    {
        return await ExecuteScalarAsync<decimal?>("""
SELECT COALESCE(SUM(Amount), 0)
FROM dbo.Donations
WHERE UserId = @UserId;
""",
            P("@UserId", userId)) ?? 0m;
    }

    // Inserts a volunteer form submission.
    public async Task<int> InsertVolunteerSignupAsync(VolunteerSignup signup)
    {
        var result = await ExecuteScalarAsync<int>("""
INSERT INTO dbo.VolunteerSignups (
    FullName, Email, PhoneNumber, Skills, Availability, SubmittedOn
)
OUTPUT INSERTED.Id
VALUES (
    @FullName, @Email, @PhoneNumber, @Skills, @Availability, @SubmittedOn
);
""",
            BuildVolunteerParameters(signup));

        signup.Id = result;
        return signup.Id;
    }

    // Returns the newest volunteer sign-ups first.
    public async Task<IReadOnlyList<VolunteerSignup>> GetRecentVolunteerSignupsAsync(int take)
    {
        return await QueryListAsync("""
SELECT TOP (@Take) *
FROM dbo.VolunteerSignups
ORDER BY SubmittedOn DESC;
""",
            MapVolunteer,
            P("@Take", take));
    }

    // Counts all volunteer sign-ups in the database.
    public async Task<int> CountVolunteerSignupsAsync()
    {
        return await ExecuteScalarAsync<int>("""
SELECT COUNT(1)
FROM dbo.VolunteerSignups;
""");
    }

    // Inserts a quick employee update into the project updates table.
    public async Task<int> InsertProjectUpdateAsync(ProjectUpdate update)
    {
        var result = await ExecuteScalarAsync<int>("""
INSERT INTO dbo.ProjectUpdates (
    Title, Description, PostedByUserId, PostedByName, PostedOn
)
OUTPUT INSERTED.Id
VALUES (
    @Title, @Description, @PostedByUserId, @PostedByName, @PostedOn
);
""",
            BuildProjectUpdateParameters(update));

        update.Id = result;
        return update.Id;
    }

    // Loads every project update for the employee page.
    public async Task<IReadOnlyList<ProjectUpdate>> GetAllProjectUpdatesAsync()
    {
        return await QueryListAsync("""
SELECT *
FROM dbo.ProjectUpdates
ORDER BY PostedOn DESC;
""",
            MapProjectUpdate);
    }

    // Loads only the most recent project updates.
    public async Task<IReadOnlyList<ProjectUpdate>> GetLatestProjectUpdatesAsync(int take)
    {
        return await QueryListAsync("""
SELECT TOP (@Take) *
FROM dbo.ProjectUpdates
ORDER BY PostedOn DESC;
""",
            MapProjectUpdate,
            P("@Take", take));
    }

    // Counts project updates for the employee summary cards.
    public async Task<int> CountProjectUpdatesAsync()
    {
        return await ExecuteScalarAsync<int>("""
SELECT COUNT(1)
FROM dbo.ProjectUpdates;
""");
    }

    // Reads and executes the schema file so the app can stand up the database structure itself.
    private async Task ExecuteSchemaScriptAsync(SqlConnection connection)
    {
        var schemaPath = Path.Combine(AppContext.BaseDirectory, SchemaScriptRelativePath);
        if (!File.Exists(schemaPath))
        {
            throw new FileNotFoundException($"Schema file was not found at '{schemaPath}'.");
        }

        var script = await File.ReadAllTextAsync(schemaPath);
        foreach (var batch in SplitSqlBatches(script))
        {
            if (string.IsNullOrWhiteSpace(batch))
            {
                continue;
            }

            await ExecuteNonQueryAsync(connection, batch);
        }
    }

    // Splits the SQL script into executable batches using GO separators.
    private static IEnumerable<string> SplitSqlBatches(string script)
    {
        var builder = new StringBuilder();
        foreach (var line in script.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
        {
            if (line.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
            {
                yield return builder.ToString();
                builder.Clear();
                continue;
            }

            builder.AppendLine(line);
        }

        if (builder.Length > 0)
        {
            yield return builder.ToString();
        }
    }

    // Creates a SQL command and attaches parameters when they exist.
    private static SqlCommand CreateCommand(SqlConnection connection, string sql, params SqlParameter[] parameters)
    {
        var command = new SqlCommand(sql, connection);
        if (parameters.Length > 0)
        {
            command.Parameters.AddRange(parameters);
        }

        return command;
    }

    // Executes INSERT, UPDATE, or DDL statements.
    private static async Task ExecuteNonQueryAsync(SqlConnection connection, string sql, params SqlParameter[] parameters)
    {
        await using var command = CreateCommand(connection, sql, parameters);
        await command.ExecuteNonQueryAsync();
    }

    // Runs a scalar command and normalizes the result to the requested type.
    private async Task<T> ExecuteScalarAsync<T>(string sql, params SqlParameter[] parameters)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();
        await using var command = CreateCommand(connection, sql, parameters);
        var value = await command.ExecuteScalarAsync();
        return ConvertScalar<T>(value);
    }

    // Reads a single row into a reference type or returns null when the query is empty.
    private async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, Func<SqlDataReader, T> map, params SqlParameter[] parameters)
        where T : class
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();
        await using var command = CreateCommand(connection, sql, parameters);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? map(reader) : null;
    }

    // Reads a whole result set into a plain list for Razor Pages to render.
    private async Task<IReadOnlyList<T>> QueryListAsync<T>(string sql, Func<SqlDataReader, T> map, params SqlParameter[] parameters)
    {
        var items = new List<T>();
        await using var connection = CreateConnection();
        await connection.OpenAsync();
        await using var command = CreateCommand(connection, sql, parameters);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(map(reader));
        }

        return items;
    }

    // Converts boxed SQL values into the requested return type while handling nulls safely.
    private static T ConvertScalar<T>(object? value)
    {
        if (value is null || value is DBNull)
        {
            return default!;
        }

        if (value is T typedValue)
        {
            return typedValue;
        }

        var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        return (T)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }

    // Packages user values into SQL parameters in one place so insert and update stay in sync.
    private static SqlParameter[] BuildUserParameters(ApplicationUser user)
    {
        return new[]
        {
            P("@Id", user.Id),
            P("@UserName", user.UserName),
            P("@NormalizedUserName", user.NormalizedUserName),
            P("@Email", user.Email),
            P("@NormalizedEmail", user.NormalizedEmail),
            P("@EmailConfirmed", user.EmailConfirmed),
            P("@PasswordHash", user.PasswordHash),
            P("@SecurityStamp", user.SecurityStamp),
            P("@ConcurrencyStamp", user.ConcurrencyStamp),
            P("@FirstName", user.FirstName),
            P("@LastName", user.LastName),
            P("@CreatedUtc", user.CreatedUtc)
        };
    }

    // Packages donation values into SQL parameters in one place.
    private static SqlParameter[] BuildDonationParameters(Donation donation)
    {
        return new[]
        {
            P("@DonorName", donation.DonorName),
            P("@DonorEmail", donation.DonorEmail),
            P("@IsAnonymous", donation.IsAnonymous),
            P("@UserId", donation.UserId),
            P("@Amount", donation.Amount),
            P("@Currency", (int)donation.Currency),
            P("@Frequency", (int)donation.Frequency),
            P("@DonationDate", donation.DonationDate)
        };
    }

    // Packages volunteer values into SQL parameters in one place.
    private static SqlParameter[] BuildVolunteerParameters(VolunteerSignup signup)
    {
        return new[]
        {
            P("@FullName", signup.FullName),
            P("@Email", signup.Email),
            P("@PhoneNumber", signup.PhoneNumber),
            P("@Skills", signup.Skills),
            P("@Availability", signup.Availability),
            P("@SubmittedOn", signup.SubmittedOn)
        };
    }

    // Packages project update values into SQL parameters in one place.
    private static SqlParameter[] BuildProjectUpdateParameters(ProjectUpdate update)
    {
        return new[]
        {
            P("@Title", update.Title),
            P("@Description", update.Description),
            P("@PostedByUserId", update.PostedByUserId),
            P("@PostedByName", update.PostedByName),
            P("@PostedOn", update.PostedOn)
        };
    }

    // Creates a single nullable SQL parameter.
    private static SqlParameter P(string name, object? value) => new(name, value ?? DBNull.Value);

    // Maps SQL users back into the split-name model used by the app.
    private static ApplicationUser MapUser(SqlDataReader reader)
    {
        return new ApplicationUser
        {
            Id = reader["Id"].ToString() ?? string.Empty,
            UserName = reader["UserName"] as string,
            NormalizedUserName = reader["NormalizedUserName"] as string,
            Email = reader["Email"] as string,
            NormalizedEmail = reader["NormalizedEmail"] as string,
            PasswordHash = reader["PasswordHash"] as string,
            SecurityStamp = reader["SecurityStamp"] as string,
            ConcurrencyStamp = reader["ConcurrencyStamp"] as string,
            FirstName = reader["FirstName"] as string ?? string.Empty,
            LastName = reader["LastName"] as string ?? string.Empty,
            EmailConfirmed = reader["EmailConfirmed"] is bool emailConfirmed && emailConfirmed,
            CreatedUtc = reader["CreatedUtc"] == DBNull.Value ? DateTime.UtcNow : Convert.ToDateTime(reader["CreatedUtc"])
        };
    }

    // Maps SQL donations into the lightweight donation model.
    private static Donation MapDonation(SqlDataReader reader)
    {
        return new Donation
        {
            Id = Convert.ToInt32(reader["Id"]),
            DonorName = reader["DonorName"] as string ?? string.Empty,
            DonorEmail = reader["DonorEmail"] as string,
            IsAnonymous = reader["IsAnonymous"] is bool isAnonymous && isAnonymous,
            UserId = reader["UserId"] as string,
            Amount = reader["Amount"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["Amount"]),
            Currency = (Currency)Convert.ToInt32(reader["Currency"]),
            Frequency = (DonationFrequency)Convert.ToInt32(reader["Frequency"]),
            DonationDate = reader["DonationDate"] == DBNull.Value ? DateTime.UtcNow : Convert.ToDateTime(reader["DonationDate"])
        };
    }

    // Maps SQL volunteer records into the volunteer sign-up model.
    private static VolunteerSignup MapVolunteer(SqlDataReader reader)
    {
        return new VolunteerSignup
        {
            Id = Convert.ToInt32(reader["Id"]),
            FullName = reader["FullName"] as string ?? string.Empty,
            Email = reader["Email"] as string ?? string.Empty,
            PhoneNumber = reader["PhoneNumber"] as string,
            Skills = reader["Skills"] as string ?? string.Empty,
            Availability = reader["Availability"] as string ?? string.Empty,
            SubmittedOn = reader["SubmittedOn"] == DBNull.Value ? DateTime.UtcNow : Convert.ToDateTime(reader["SubmittedOn"])
        };
    }

    // Maps SQL project updates into the employee dashboard model.
    private static ProjectUpdate MapProjectUpdate(SqlDataReader reader)
    {
        return new ProjectUpdate
        {
            Id = Convert.ToInt32(reader["Id"]),
            Title = reader["Title"] as string ?? string.Empty,
            Description = reader["Description"] as string ?? string.Empty,
            PostedByUserId = reader["PostedByUserId"] as string ?? string.Empty,
            PostedByName = reader["PostedByName"] as string,
            PostedOn = reader["PostedOn"] == DBNull.Value ? DateTime.UtcNow : Convert.ToDateTime(reader["PostedOn"])
        };
    }
}
