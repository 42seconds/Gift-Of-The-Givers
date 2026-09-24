IF OBJECT_ID(N'dbo.AspNetRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetRoles (
        Id nvarchar(450) NOT NULL,
        Name nvarchar(256) NULL,
        NormalizedName nvarchar(256) NULL,
        ConcurrencyStamp nvarchar(max) NULL,
        CONSTRAINT PK_AspNetRoles PRIMARY KEY (Id)
    );
END
GO

IF OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUsers (
        Id nvarchar(450) NOT NULL,
        UserName nvarchar(256) NULL,
        NormalizedUserName nvarchar(256) NULL,
        Email nvarchar(256) NULL,
        NormalizedEmail nvarchar(256) NULL,
        EmailConfirmed bit NOT NULL CONSTRAINT DF_AspNetUsers_EmailConfirmed DEFAULT(0),
        PasswordHash nvarchar(max) NULL,
        SecurityStamp nvarchar(max) NULL,
        ConcurrencyStamp nvarchar(max) NULL,
        FirstName nvarchar(100) NOT NULL,
        LastName nvarchar(100) NOT NULL,
        CreatedUtc datetime2 NOT NULL CONSTRAINT DF_AspNetUsers_CreatedUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT PK_AspNetUsers PRIMARY KEY (Id)
    );
END
GO

IF OBJECT_ID(N'dbo.AspNetUserRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserRoles (
        UserId nvarchar(450) NOT NULL,
        RoleId nvarchar(450) NOT NULL,
        CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId),
        CONSTRAINT FK_AspNetUserRoles_AspNetUsers FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE,
        CONSTRAINT FK_AspNetUserRoles_AspNetRoles FOREIGN KEY (RoleId) REFERENCES dbo.AspNetRoles(Id) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID(N'dbo.Donations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Donations (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Donations PRIMARY KEY,
        DonorName nvarchar(100) NOT NULL,
        DonorEmail nvarchar(max) NULL,
        IsAnonymous bit NOT NULL,
        UserId nvarchar(450) NULL,
        Amount decimal(18,2) NOT NULL,
        Currency int NOT NULL,
        Frequency int NOT NULL,
        DonationDate datetime2 NOT NULL,
        CONSTRAINT FK_Donations_AspNetUsers FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id)
    );
END
GO

IF OBJECT_ID(N'dbo.VolunteerSignups', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.VolunteerSignups (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_VolunteerSignups PRIMARY KEY,
        FullName nvarchar(100) NOT NULL,
        Email nvarchar(max) NOT NULL,
        PhoneNumber nvarchar(max) NULL,
        Skills nvarchar(300) NOT NULL,
        Availability nvarchar(max) NOT NULL,
        SubmittedOn datetime2 NOT NULL
    );
END
GO

IF OBJECT_ID(N'dbo.ProjectUpdates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProjectUpdates (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProjectUpdates PRIMARY KEY,
        Title nvarchar(150) NOT NULL,
        Description nvarchar(2000) NOT NULL,
        PostedByUserId nvarchar(450) NOT NULL,
        PostedByName nvarchar(max) NULL,
        PostedOn datetime2 NOT NULL,
        CONSTRAINT FK_ProjectUpdates_AspNetUsers FOREIGN KEY (PostedByUserId) REFERENCES dbo.AspNetUsers(Id)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AspNetUsers_NormalizedEmail' AND object_id = OBJECT_ID('dbo.AspNetUsers'))
BEGIN
    CREATE INDEX IX_AspNetUsers_NormalizedEmail ON dbo.AspNetUsers(NormalizedEmail);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AspNetUsers_NormalizedUserName' AND object_id = OBJECT_ID('dbo.AspNetUsers'))
BEGIN
    CREATE UNIQUE INDEX IX_AspNetUsers_NormalizedUserName ON dbo.AspNetUsers(NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AspNetRoles_NormalizedName' AND object_id = OBJECT_ID('dbo.AspNetRoles'))
BEGIN
    CREATE UNIQUE INDEX IX_AspNetRoles_NormalizedName ON dbo.AspNetRoles(NormalizedName) WHERE NormalizedName IS NOT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Donations_DonationDate' AND object_id = OBJECT_ID('dbo.Donations'))
BEGIN
    CREATE INDEX IX_Donations_DonationDate ON dbo.Donations(DonationDate DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Donations_UserId' AND object_id = OBJECT_ID('dbo.Donations'))
BEGIN
    CREATE INDEX IX_Donations_UserId ON dbo.Donations(UserId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_VolunteerSignups_SubmittedOn' AND object_id = OBJECT_ID('dbo.VolunteerSignups'))
BEGIN
    CREATE INDEX IX_VolunteerSignups_SubmittedOn ON dbo.VolunteerSignups(SubmittedOn DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProjectUpdates_PostedOn' AND object_id = OBJECT_ID('dbo.ProjectUpdates'))
BEGIN
    CREATE INDEX IX_ProjectUpdates_PostedOn ON dbo.ProjectUpdates(PostedOn DESC);
END
GO
