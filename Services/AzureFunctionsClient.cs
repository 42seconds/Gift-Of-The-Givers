using System.Net.Http.Json;
using gift_of_the_givers.Models;

namespace gift_of_the_givers.Services;

// Settings bound from the "AzureFunctions" section of appsettings.json.
public class AzureFunctionsOptions
{
    // Local:  http://localhost:7071/api/
    // Azure:  https://<your-function-app>.azurewebsites.net/api/
    public string BaseUrl { get; set; } = "http://localhost:7071/api/";

    // Function key (Azure portal > Function App > App keys > default). Leave empty when running locally.
    public string? FunctionKey { get; set; }
}

// Certificate data returned by the GenerateTaxCertificate Azure Function.
public class FunctionTaxCertificate
{
    public string CertificateNumber { get; set; } = string.Empty;

    public string DonorName { get; set; } = string.Empty;

    public string TaxYear { get; set; } = string.Empty;

    public string Section18AReference { get; set; } = string.Empty;

    public DateTime IssuedOnUtc { get; set; }

    public string CertificateText { get; set; } = string.Empty;

    public string GeneratedBy { get; set; } = string.Empty;
}

// One entry of the Azure Table Storage project update log, as returned by the GetProjectUpdateLog function.
public class ProjectUpdateLogEntry
{
    public int ProjectUpdateId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string PostedByName { get; set; } = string.Empty;

    public DateTime LoggedOnUtc { get; set; }
}

// Typed HttpClient that connects the Razor Pages app to the GiftOfTheGivers.Functions project.
// Every call is best-effort: if the Function App is offline the web app keeps working and just reports it.
public class AzureFunctionsClient
{
    private readonly HttpClient _http;
    private readonly ILogger<AzureFunctionsClient> _logger;

    public AzureFunctionsClient(HttpClient http, ILogger<AzureFunctionsClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    // Sends a completed donation to the function and returns the dummy tax certificate it generates.
    public async Task<FunctionTaxCertificate?> GenerateTaxCertificateAsync(Donation donation)
    {
        var payload = new
        {
            DonationId = donation.Id,
            donation.DonorName,
            donation.DonorEmail,
            donation.IsAnonymous,
            donation.Amount,
            Currency = donation.Currency.ToString(),
            Frequency = donation.Frequency.ToString(),
            DonationDate = DateTime.SpecifyKind(donation.DonationDate, DateTimeKind.Utc)
        };

        try
        {
            using var response = await _http.PostAsJsonAsync("certificates", payload);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<FunctionTaxCertificate>();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            _logger.LogWarning(ex, "GenerateTaxCertificate function could not be reached for donation {DonationId}.", donation.Id);
            return null;
        }
    }

    // Sends a newly posted project update to the function, which logs it in Azure Table Storage.
    public async Task<bool> LogProjectUpdateAsync(ProjectUpdate update)
    {
        var payload = new
        {
            ProjectUpdateId = update.Id,
            update.Title,
            update.Description,
            update.PostedByUserId,
            update.PostedByName,
            PostedOnUtc = DateTime.SpecifyKind(update.PostedOn, DateTimeKind.Utc)
        };

        try
        {
            using var response = await _http.PostAsJsonAsync("project-updates", payload);
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "LogProjectUpdate function could not be reached for update {ProjectUpdateId}.", update.Id);
            return false;
        }
    }

    // Reads the newest entries back from the Azure Storage log so employees can see what was recorded.
    public async Task<IReadOnlyList<ProjectUpdateLogEntry>?> GetProjectUpdateLogAsync(int take)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<ProjectUpdateLogResponse>($"project-updates?take={take}");
            return result?.Entries ?? new List<ProjectUpdateLogEntry>();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            _logger.LogWarning(ex, "GetProjectUpdateLog function could not be reached.");
            return null;
        }
    }

    private sealed class ProjectUpdateLogResponse
    {
        public List<ProjectUpdateLogEntry> Entries { get; set; } = new();
    }
}
