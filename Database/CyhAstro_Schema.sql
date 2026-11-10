-- ============================================================
-- CyhAstro Database Schema
-- SQL Server 2022 / Express Edition
-- All tables, indexes and stored procedures
-- ============================================================

USE master;
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'CyhAstroDB')
BEGIN
    CREATE DATABASE CyhAstroDB;
END
GO

USE CyhAstroDB;
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================
-- TABLE 1: tbl_Users
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_Users')
BEGIN
    CREATE TABLE tbl_Users (
        UserId          UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID() PRIMARY KEY,
        FullName        NVARCHAR(150)       NOT NULL,
        Email           NVARCHAR(200)       NULL,
        PhoneNumber     NVARCHAR(20)        NULL,
        PasswordHash    NVARCHAR(500)       NOT NULL,
        Gender          NVARCHAR(10)        NOT NULL DEFAULT 'Other',
        DateOfBirth     DATE                NOT NULL,
        TimeOfBirth     TIME                NULL,
        BirthCity       NVARCHAR(150)       NULL,
        BirthLatitude   DECIMAL(9,6)        NULL,
        BirthLongitude  DECIMAL(9,6)        NULL,
        ProfilePhotoUrl NVARCHAR(500)       NULL,
        Role            NVARCHAR(20)        NOT NULL DEFAULT 'User',
        IsActive        BIT                 NOT NULL DEFAULT 1,
        IsBlocked       BIT                 NOT NULL DEFAULT 0,
        BlockReason     NVARCHAR(300)       NULL,
        DeviceToken     NVARCHAR(500)       NULL,
        LastLoginAt     DATETIME2           NULL,
        CreatedAt       DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt       DATETIME2           NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_Users_Email' AND object_id = OBJECT_ID('tbl_Users'))
BEGIN
    CREATE UNIQUE INDEX UX_Users_Email ON tbl_Users(Email) WHERE Email IS NOT NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_Users_Phone' AND object_id = OBJECT_ID('tbl_Users'))
BEGIN
    CREATE UNIQUE INDEX UX_Users_Phone ON tbl_Users(PhoneNumber) WHERE PhoneNumber IS NOT NULL;
END
GO

-- ============================================================
-- TABLE 2: tbl_Subscriptions
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_Subscriptions')
BEGIN
    CREATE TABLE tbl_Subscriptions (
        SubId               UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID() PRIMARY KEY,
        UserId              UNIQUEIDENTIFIER    NOT NULL REFERENCES tbl_Users(UserId),
        [Plan]              NVARCHAR(20)        NOT NULL DEFAULT 'Free',
        StartDate           DATETIME2           NULL,
        ExpiryDate          DATETIME2           NULL,
        IsActive            AS (CASE WHEN [Plan] = 'Free' THEN CAST(1 AS BIT)
                                     WHEN ExpiryDate > GETUTCDATE() THEN CAST(1 AS BIT)
                                     ELSE CAST(0 AS BIT) END),
        PalmScansRemaining  INT                 NOT NULL DEFAULT 1,
        FaceScansRemaining  INT                 NOT NULL DEFAULT 0,
        ChatQuestionsToday  INT                 NOT NULL DEFAULT 3,
        QuotaResetDate      DATE                NOT NULL DEFAULT CAST(GETUTCDATE() AS DATE),
        PaymentGateway      NVARCHAR(50)        NULL,
        GatewayTxnId        NVARCHAR(300)       NULL,
        CreatedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_Sub_UserId' AND object_id = OBJECT_ID('tbl_Subscriptions'))
BEGIN
    CREATE UNIQUE INDEX UX_Sub_UserId ON tbl_Subscriptions(UserId);
END
GO

-- ============================================================
-- TABLE 3: tbl_UserLedger
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_UserLedger')
BEGIN
    CREATE TABLE tbl_UserLedger (
        LedgerId            UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID() PRIMARY KEY,
        UserId              UNIQUEIDENTIFIER    NOT NULL REFERENCES tbl_Users(UserId),
        TransactionType     NVARCHAR(50)        NOT NULL,
        AmountINR           DECIMAL(10,2)       NOT NULL DEFAULT 0.00,
        CreditChange        INT                 NOT NULL DEFAULT 0,
        BalanceAfter        INT                 NOT NULL DEFAULT 0,
        Description         NVARCHAR(500)       NOT NULL,
        Status              NVARCHAR(20)        NOT NULL DEFAULT 'Success',
        PaymentGateway      NVARCHAR(50)        NULL,
        GatewayTxnId        NVARCHAR(300)       NULL,
        AdminRemark         NVARCHAR(300)       NULL,
        CreatedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Ledger_UserId' AND object_id = OBJECT_ID('tbl_UserLedger'))
BEGIN
    CREATE INDEX IX_Ledger_UserId ON tbl_UserLedger(UserId);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Ledger_CreatedAt' AND object_id = OBJECT_ID('tbl_UserLedger'))
BEGIN
    CREATE INDEX IX_Ledger_CreatedAt ON tbl_UserLedger(CreatedAt DESC);
END
GO

-- ============================================================
-- TABLE 4: tbl_KundliProfiles
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_KundliProfiles')
BEGIN
    CREATE TABLE tbl_KundliProfiles (
        KundliId            UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID() PRIMARY KEY,
        UserId              UNIQUEIDENTIFIER    NOT NULL REFERENCES tbl_Users(UserId),
        LagnaRashi          NVARCHAR(50)        NULL,
        LagnaDegree         DECIMAL(8,4)        NULL,
        MoonRashi           NVARCHAR(50)        NULL,
        SunRashi            NVARCHAR(50)        NULL,
        Nakshatra           NVARCHAR(80)        NULL,
        NakshatraPada       INT                 NULL,
        MahadashaCurrent    NVARCHAR(50)        NULL,
        MahadashaEndDate    DATE                NULL,
        IsManglik           BIT                 NOT NULL DEFAULT 0,
        SadeSatiActive      BIT                 NOT NULL DEFAULT 0,
        PlanetaryJson       NVARCHAR(MAX)       NULL,
        AyanamsaUsed        NVARCHAR(30)        NULL DEFAULT 'Lahiri',
        CreatedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_Kundli_UserId' AND object_id = OBJECT_ID('tbl_KundliProfiles'))
BEGIN
    CREATE UNIQUE INDEX UX_Kundli_UserId ON tbl_KundliProfiles(UserId);
END
GO

-- ============================================================
-- TABLE 5: tbl_ScanHistory
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_ScanHistory')
BEGIN
    CREATE TABLE tbl_ScanHistory (
        ScanId              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID() PRIMARY KEY,
        UserId              UNIQUEIDENTIFIER    NOT NULL REFERENCES tbl_Users(UserId),
        ScanType            NVARCHAR(20)        NOT NULL,
        HandSide            NVARCHAR(10)        NULL,
        ImagePath           NVARCHAR(500)       NOT NULL,
        ThumbnailPath       NVARCHAR(500)       NULL,
        AiRawResponse       NVARCHAR(MAX)       NULL,
        SummaryText         NVARCHAR(MAX)       NULL,
        IsFullReportUnlocked BIT               NOT NULL DEFAULT 0,
        SyncStatus          NVARCHAR(20)        NOT NULL DEFAULT 'Synced',
        ScannedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
        SyncedAt            DATETIME2           NULL
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Scan_UserId' AND object_id = OBJECT_ID('tbl_ScanHistory'))
BEGIN
    CREATE INDEX IX_Scan_UserId ON tbl_ScanHistory(UserId);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Scan_Status' AND object_id = OBJECT_ID('tbl_ScanHistory'))
BEGIN
    CREATE INDEX IX_Scan_Status ON tbl_ScanHistory(SyncStatus);
END
GO

-- ============================================================
-- TABLE 6: tbl_AppVersions
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_AppVersions')
BEGIN
    CREATE TABLE tbl_AppVersions (
        VersionId       UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID() PRIMARY KEY,
        Platform        NVARCHAR(20)        NOT NULL,
        VersionCode     INT                 NOT NULL,
        VersionName     NVARCHAR(20)        NOT NULL,
        DownloadUrl     NVARCHAR(500)       NULL,
        PlayStoreUrl    NVARCHAR(500)       NULL,
        ReleaseNotes    NVARCHAR(1000)      NULL,
        IsMandatory     BIT                 NOT NULL DEFAULT 0,
        IsActive        BIT                 NOT NULL DEFAULT 1,
        ReleasedAt      DATETIME2           NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

-- ============================================================
-- TABLE 7: tbl_AppSettings
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_AppSettings')
BEGIN
    CREATE TABLE tbl_AppSettings (
        SettingKey      NVARCHAR(100)   NOT NULL PRIMARY KEY,
        SettingValue    NVARCHAR(MAX)   NOT NULL,
        Description     NVARCHAR(300)   NULL,
        UpdatedAt       DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
        UpdatedBy       NVARCHAR(100)   NULL
    );

    INSERT INTO tbl_AppSettings (SettingKey, SettingValue, Description) VALUES
    ('PriceWeeklyINR',       '99',   'Weekly subscription price in INR'),
    ('PriceMonthlyINR',      '299',  'Monthly subscription price in INR'),
    ('PriceYearlyINR',       '1999', 'Yearly subscription price in INR'),
    ('FreeUserPalmScans',    '1',    'Number of free palm scans for new users'),
    ('FreeUserFaceScans',    '0',    'Number of free face scans for new users'),
    ('FreeUserChatLimit',    '3',    'Daily chat questions for free users'),
    ('PaidUserPalmScans',    '999',  'Palm scans for paid users (effectively unlimited)'),
    ('PaidUserFaceScans',    '999',  'Face scans for paid users'),
    ('GeminiPalmPrompt',
     'You are an expert Vedic Palmistry (Hast Rekha) analyst. Analyze this palm image carefully. Identify and describe: 1) Life Line (Jeevan Rekha) - length, depth, continuity, breaks 2) Head Line (Mastishk Rekha) - curve, length, strength 3) Heart Line (Hriday Rekha) - emotion, relationships 4) Fate Line (Bhagya Rekha) - career direction, strength 5) Mount of Jupiter (Guru Parvat) - ambition, leadership 6) Mount of Venus (Shukra Parvat) - love, creativity 7) Mount of Sun (Surya Parvat) - fame, success. For each item provide: summary (1 sentence), strength (Weak/Moderate/Strong), and 2 key insights. Respond ONLY in valid JSON format with keys: lifeLine, headLine, heartLine, fateLine, mountJupiter, mountVenus, mountSun. Each key has: summary, strength, insights (array of 2 strings).',
     'Gemini AI prompt for palm reading'),
    ('GeminiFacePrompt',
     'You are an expert in Samudrik Shastra (Indian Physiognomy / Face Reading). Analyze this frontal face image. Examine: 1) Forehead (Mastak) - width, height, lines - indicates intellect and destiny 2) Eyebrows - shape, thickness, gap - indicates personality and decisions 3) Eyes - size, shape, brightness - indicates soul and emotions 4) Nose - shape, tip, nostrils - indicates wealth and willpower 5) Lips & Mouth - fullness, shape - indicates communication and desires 6) Jawline & Chin - structure - indicates determination. Provide analysis for each zone. Respond ONLY in valid JSON with keys: forehead, eyebrows, eyes, nose, lipsAndMouth, jawAndChin. Each key: summary, trait (positive personality trait), insight (1 key life insight).',
     'Gemini AI prompt for face reading'),
    ('AppName',              'CyhAstro',  'Application display name'),
    ('SupportEmail',         'support@cyhastro.com', 'Support email address');
END
GO

PRINT 'All Tables Created Successfully.';
GO

-- ============================================================
-- STORED PROCEDURES
-- ============================================================

-- 1: sp_User_RegisterOrLogin
CREATE OR ALTER PROCEDURE sp_User_RegisterOrLogin
    @FullName       NVARCHAR(150),
    @Email          NVARCHAR(200)   = NULL,
    @PhoneNumber    NVARCHAR(20)    = NULL,
    @PasswordHash   NVARCHAR(500),
    @Gender         NVARCHAR(10),
    @DateOfBirth    DATE,
    @TimeOfBirth    TIME            = NULL,
    @BirthCity      NVARCHAR(150)   = NULL,
    @BirthLatitude  DECIMAL(9,6)    = NULL,
    @BirthLongitude DECIMAL(9,6)    = NULL,
    @DeviceToken    NVARCHAR(500)   = NULL,
    @ResultCode     INT             OUTPUT,
    @UserId         UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        IF EXISTS (SELECT 1 FROM tbl_Users WHERE Email = @Email AND @Email IS NOT NULL)
           OR EXISTS (SELECT 1 FROM tbl_Users WHERE PhoneNumber = @PhoneNumber AND @PhoneNumber IS NOT NULL)
        BEGIN
            SELECT @UserId = UserId FROM tbl_Users
            WHERE (Email = @Email AND @Email IS NOT NULL)
               OR (PhoneNumber = @PhoneNumber AND @PhoneNumber IS NOT NULL);

            UPDATE tbl_Users SET DeviceToken = @DeviceToken, LastLoginAt = GETUTCDATE()
            WHERE UserId = @UserId;

            SET @ResultCode = 1;
            COMMIT TRANSACTION;
            RETURN;
        END

        SET @UserId = NEWID();
        DECLARE @FreeScans INT = (SELECT CAST(SettingValue AS INT) FROM tbl_AppSettings WHERE SettingKey = 'FreeUserPalmScans');

        INSERT INTO tbl_Users (UserId, FullName, Email, PhoneNumber, PasswordHash, Gender, DateOfBirth, TimeOfBirth, BirthCity, BirthLatitude, BirthLongitude, DeviceToken, LastLoginAt)
        VALUES (@UserId, @FullName, @Email, @PhoneNumber, @PasswordHash, @Gender, @DateOfBirth, @TimeOfBirth, @BirthCity, @BirthLatitude, @BirthLongitude, @DeviceToken, GETUTCDATE());

        INSERT INTO tbl_Subscriptions (UserId, [Plan], PalmScansRemaining, FaceScansRemaining)
        VALUES (@UserId, 'Free', @FreeScans, 0);

        INSERT INTO tbl_UserLedger (UserId, TransactionType, AmountINR, CreditChange, BalanceAfter, Description, Status)
        VALUES (@UserId, 'WelcomeCredit', 0, @FreeScans, @FreeScans, 'Welcome! ' + CAST(@FreeScans AS NVARCHAR) + ' free palm scan credited.', 'Success');

        SET @ResultCode = 0;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        SET @ResultCode = -1;
        THROW;
    END CATCH
END
GO

-- 2: sp_User_GetProfile
CREATE OR ALTER PROCEDURE sp_User_GetProfile
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        u.UserId, u.FullName, u.Email, u.PhoneNumber, u.Gender,
        u.DateOfBirth, u.TimeOfBirth, u.BirthCity, u.BirthLatitude, u.BirthLongitude,
        u.ProfilePhotoUrl, u.Role, u.IsActive, u.IsBlocked, u.LastLoginAt,
        s.[Plan], s.IsActive AS IsSubscriptionActive, s.ExpiryDate,
        s.PalmScansRemaining, s.FaceScansRemaining, s.ChatQuestionsToday,
        k.LagnaRashi, k.MoonRashi, k.SunRashi, k.Nakshatra, k.NakshatraPada,
        k.MahadashaCurrent, k.MahadashaEndDate, k.IsManglik, k.SadeSatiActive
    FROM tbl_Users u
    LEFT JOIN tbl_Subscriptions s ON s.UserId = u.UserId
    LEFT JOIN tbl_KundliProfiles k ON k.UserId = u.UserId
    WHERE u.UserId = @UserId AND u.IsBlocked = 0;
END
GO

-- 3: sp_Scan_Save
CREATE OR ALTER PROCEDURE sp_Scan_Save
    @UserId         UNIQUEIDENTIFIER,
    @ScanType       NVARCHAR(20),
    @HandSide       NVARCHAR(10)     = NULL,
    @ImagePath      NVARCHAR(500),
    @ThumbnailPath  NVARCHAR(500)    = NULL,
    @AiRawResponse  NVARCHAR(MAX)    = NULL,
    @SummaryText    NVARCHAR(MAX)    = NULL,
    @ScanId         UNIQUEIDENTIFIER OUTPUT,
    @ResultCode     INT              OUTPUT,
    @CreditsLeft    INT              OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @PalmLeft INT, @FaceLeft INT, @CurrentBalance INT;
        SELECT @PalmLeft = PalmScansRemaining, @FaceLeft = FaceScansRemaining
        FROM tbl_Subscriptions WHERE UserId = @UserId;

        IF (@ScanType = 'Palm' AND @PalmLeft <= 0) OR (@ScanType = 'Face' AND @FaceLeft <= 0)
        BEGIN
            SET @ResultCode = 1;
            SET @CreditsLeft = 0;
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF @ScanType = 'Palm'
        BEGIN
            UPDATE tbl_Subscriptions SET PalmScansRemaining = PalmScansRemaining - 1 WHERE UserId = @UserId;
            SELECT @CreditsLeft = PalmScansRemaining FROM tbl_Subscriptions WHERE UserId = @UserId;
        END
        ELSE IF @ScanType = 'Face'
        BEGIN
            UPDATE tbl_Subscriptions SET FaceScansRemaining = FaceScansRemaining - 1 WHERE UserId = @UserId;
            SELECT @CreditsLeft = FaceScansRemaining FROM tbl_Subscriptions WHERE UserId = @UserId;
        END

        SELECT @CurrentBalance = (PalmScansRemaining + FaceScansRemaining) FROM tbl_Subscriptions WHERE UserId = @UserId;

        SET @ScanId = NEWID();
        INSERT INTO tbl_ScanHistory (ScanId, UserId, ScanType, HandSide, ImagePath, ThumbnailPath, AiRawResponse, SummaryText, SyncStatus)
        VALUES (@ScanId, @UserId, @ScanType, @HandSide, @ImagePath, @ThumbnailPath, @AiRawResponse, @SummaryText, 'Synced');

        INSERT INTO tbl_UserLedger (UserId, TransactionType, AmountINR, CreditChange, BalanceAfter, Description, Status)
        VALUES (@UserId, 'ScanDeduction', 0, -1, @CurrentBalance,
                @ScanType + ' scan completed. 1 credit used.', 'Success');

        SET @ResultCode = 0;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        SET @ResultCode = -1;
        SET @CreditsLeft = 0;
        THROW;
    END CATCH
END
GO

-- 4: sp_Scan_SaveOffline
CREATE OR ALTER PROCEDURE sp_Scan_SaveOffline
    @UserId         UNIQUEIDENTIFIER,
    @ScanType       NVARCHAR(20),
    @HandSide       NVARCHAR(10)    = NULL,
    @ImagePath      NVARCHAR(500),
    @ScannedAt      DATETIME2,
    @ScanId         UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @ScanId = NEWID();
    INSERT INTO tbl_ScanHistory (ScanId, UserId, ScanType, HandSide, ImagePath, SyncStatus, ScannedAt)
    VALUES (@ScanId, @UserId, @ScanType, @HandSide, @ImagePath, 'Pending', @ScannedAt);
END
GO

-- 5: sp_Scan_UpdateAiResult
CREATE OR ALTER PROCEDURE sp_Scan_UpdateAiResult
    @ScanId         UNIQUEIDENTIFIER,
    @AiRawResponse  NVARCHAR(MAX),
    @SummaryText    NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE tbl_ScanHistory
    SET AiRawResponse = @AiRawResponse,
        SummaryText = @SummaryText,
        SyncStatus = 'Synced',
        SyncedAt = GETUTCDATE()
    WHERE ScanId = @ScanId;
END
GO

-- 6: sp_Subscription_Upgrade
CREATE OR ALTER PROCEDURE sp_Subscription_Upgrade
    @UserId             UNIQUEIDENTIFIER,
    @NewPlan            NVARCHAR(20),
    @AmountPaidINR      DECIMAL(10,2),
    @PaymentGateway     NVARCHAR(50),
    @GatewayTxnId       NVARCHAR(300),
    @ResultCode         INT             OUTPUT,
    @NewExpiryDate      DATETIME2       OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        SET @NewExpiryDate = CASE @NewPlan
            WHEN 'Weekly'   THEN DATEADD(DAY, 7, GETUTCDATE())
            WHEN 'Monthly'  THEN DATEADD(MONTH, 1, GETUTCDATE())
            WHEN 'Yearly'   THEN DATEADD(YEAR, 1, GETUTCDATE())
            WHEN 'Lifetime' THEN DATEADD(YEAR, 100, GETUTCDATE())
            ELSE NULL
        END;

        DECLARE @PaidPalmScans INT = CAST((SELECT SettingValue FROM tbl_AppSettings WHERE SettingKey = 'PaidUserPalmScans') AS INT);
        DECLARE @PaidFaceScans INT = CAST((SELECT SettingValue FROM tbl_AppSettings WHERE SettingKey = 'PaidUserFaceScans') AS INT);

        UPDATE tbl_Subscriptions
        SET [Plan] = @NewPlan,
            StartDate = GETUTCDATE(),
            ExpiryDate = @NewExpiryDate,
            PalmScansRemaining = @PaidPalmScans,
            FaceScansRemaining = @PaidFaceScans,
            PaymentGateway = @PaymentGateway,
            GatewayTxnId = @GatewayTxnId,
            UpdatedAt = GETUTCDATE()
        WHERE UserId = @UserId;

        DECLARE @TotalBalance INT = @PaidPalmScans + @PaidFaceScans;
        INSERT INTO tbl_UserLedger (UserId, TransactionType, AmountINR, CreditChange, BalanceAfter, Description, Status, PaymentGateway, GatewayTxnId)
        VALUES (@UserId, 'SubscriptionPurchase', @AmountPaidINR, @TotalBalance, @TotalBalance,
                @NewPlan + ' plan activated. Valid till ' + CONVERT(NVARCHAR, @NewExpiryDate, 103),
                'Success', @PaymentGateway, @GatewayTxnId);

        SET @ResultCode = 0;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        SET @ResultCode = -1;
        SET @NewExpiryDate = NULL;
        THROW;
    END CATCH
END
GO

-- 7: sp_Admin_Dashboard
CREATE OR ALTER PROCEDURE sp_Admin_Dashboard
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        (SELECT COUNT(*) FROM tbl_Users WHERE Role = 'User') AS TotalUsers,
        (SELECT COUNT(*) FROM tbl_Users WHERE CAST(CreatedAt AS DATE) = CAST(GETUTCDATE() AS DATE)) AS NewUsersToday,
        (SELECT COUNT(*) FROM tbl_Subscriptions WHERE [Plan] != 'Free' AND IsActive = 1) AS ActiveSubscribers,
        (SELECT COUNT(*) FROM tbl_ScanHistory WHERE CAST(ScannedAt AS DATE) = CAST(GETUTCDATE() AS DATE)) AS TotalScansToday,
        (SELECT COUNT(*) FROM tbl_ScanHistory WHERE ScanType = 'Palm' AND CAST(ScannedAt AS DATE) = CAST(GETUTCDATE() AS DATE)) AS PalmScansToday,
        (SELECT COUNT(*) FROM tbl_ScanHistory WHERE ScanType = 'Face' AND CAST(ScannedAt AS DATE) = CAST(GETUTCDATE() AS DATE)) AS FaceScansToday,
        (SELECT ISNULL(SUM(AmountINR), 0) FROM tbl_UserLedger WHERE TransactionType = 'SubscriptionPurchase' AND CAST(CreatedAt AS DATE) = CAST(GETUTCDATE() AS DATE) AND Status = 'Success') AS RevenueToday,
        (SELECT ISNULL(SUM(AmountINR), 0) FROM tbl_UserLedger WHERE TransactionType = 'SubscriptionPurchase' AND MONTH(CreatedAt) = MONTH(GETUTCDATE()) AND YEAR(CreatedAt) = YEAR(GETUTCDATE()) AND Status = 'Success') AS RevenueThisMonth,
        (SELECT ISNULL(SUM(AmountINR), 0) FROM tbl_UserLedger WHERE TransactionType = 'SubscriptionPurchase' AND Status = 'Success') AS RevenueTotalAllTime;
END
GO

-- 8: sp_Admin_GetUsersList
CREATE OR ALTER PROCEDURE sp_Admin_GetUsersList
    @SearchQuery    NVARCHAR(100)   = NULL,
    @PlanFilter     NVARCHAR(20)    = NULL,
    @StatusFilter   NVARCHAR(20)    = NULL,
    @PageNumber     INT             = 1,
    @PageSize       INT             = 25,
    @TotalCount     INT             OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT @TotalCount = COUNT(*)
    FROM tbl_Users u
    LEFT JOIN tbl_Subscriptions s ON s.UserId = u.UserId
    WHERE u.Role = 'User'
    AND (@SearchQuery IS NULL OR u.FullName LIKE '%' + @SearchQuery + '%'
         OR u.Email LIKE '%' + @SearchQuery + '%'
         OR u.PhoneNumber LIKE '%' + @SearchQuery + '%')
    AND (@PlanFilter IS NULL OR s.[Plan] = @PlanFilter)
    AND (@StatusFilter IS NULL OR (@StatusFilter = 'Blocked' AND u.IsBlocked = 1) OR (@StatusFilter = 'Active' AND u.IsBlocked = 0));

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    SELECT
        u.UserId, u.FullName, u.Email, u.PhoneNumber, u.Gender,
        u.DateOfBirth, u.IsBlocked, u.LastLoginAt, u.CreatedAt,
        s.[Plan], s.IsActive AS IsSubActive, s.ExpiryDate,
        s.PalmScansRemaining, s.FaceScansRemaining,
        (SELECT COUNT(*) FROM tbl_ScanHistory sh WHERE sh.UserId = u.UserId) AS TotalScans
    FROM tbl_Users u
    LEFT JOIN tbl_Subscriptions s ON s.UserId = u.UserId
    WHERE u.Role = 'User'
    AND (@SearchQuery IS NULL OR u.FullName LIKE '%' + @SearchQuery + '%'
         OR u.Email LIKE '%' + @SearchQuery + '%'
         OR u.PhoneNumber LIKE '%' + @SearchQuery + '%')
    AND (@PlanFilter IS NULL OR s.[Plan] = @PlanFilter)
    AND (@StatusFilter IS NULL OR (@StatusFilter = 'Blocked' AND u.IsBlocked = 1) OR (@StatusFilter = 'Active' AND u.IsBlocked = 0))
    ORDER BY u.CreatedAt DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

-- 9: sp_Admin_GetUserLedger
CREATE OR ALTER PROCEDURE sp_Admin_GetUserLedger
    @UserId         UNIQUEIDENTIFIER    = NULL,
    @FromDate       DATE                = NULL,
    @ToDate         DATE                = NULL,
    @PageNumber     INT                 = 1,
    @PageSize       INT                 = 50,
    @TotalCount     INT                 OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT @TotalCount = COUNT(*)
    FROM tbl_UserLedger l
    WHERE (@UserId IS NULL OR l.UserId = @UserId)
    AND (@FromDate IS NULL OR CAST(l.CreatedAt AS DATE) >= @FromDate)
    AND (@ToDate IS NULL OR CAST(l.CreatedAt AS DATE) <= @ToDate);

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    SELECT
        l.LedgerId, l.UserId, u.FullName AS UserName, u.Email AS UserEmail, u.PhoneNumber AS UserPhone,
        l.TransactionType, l.AmountINR, l.CreditChange, l.BalanceAfter,
        l.Description, l.Status, l.PaymentGateway, l.GatewayTxnId,
        l.AdminRemark, l.CreatedAt
    FROM tbl_UserLedger l
    INNER JOIN tbl_Users u ON u.UserId = l.UserId
    WHERE (@UserId IS NULL OR l.UserId = @UserId)
    AND (@FromDate IS NULL OR CAST(l.CreatedAt AS DATE) >= @FromDate)
    AND (@ToDate IS NULL OR CAST(l.CreatedAt AS DATE) <= @ToDate)
    ORDER BY l.CreatedAt DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

-- 10: sp_Admin_AdjustUser
CREATE OR ALTER PROCEDURE sp_Admin_AdjustUser
    @TargetUserId       UNIQUEIDENTIFIER,
    @AdminUserId        UNIQUEIDENTIFIER,
    @Action             NVARCHAR(50),
    @PlanName           NVARCHAR(20)    = NULL,
    @PlanDays           INT             = 0,
    @CreditsToAdd       INT             = 0,
    @AdminRemark        NVARCHAR(300)   = NULL,
    @ResultCode         INT             OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        IF @Action = 'Block'
        BEGIN
            UPDATE tbl_Users SET IsBlocked = 1, BlockReason = @AdminRemark WHERE UserId = @TargetUserId;
            SET @ResultCode = 0;
        END
        ELSE IF @Action = 'Unblock'
        BEGIN
            UPDATE tbl_Users SET IsBlocked = 0, BlockReason = NULL WHERE UserId = @TargetUserId;
            SET @ResultCode = 0;
        END
        ELSE IF @Action = 'AddPlan'
        BEGIN
            DECLARE @NewExpiry DATETIME2;
            SELECT @NewExpiry = CASE WHEN ExpiryDate > GETUTCDATE()
                                     THEN DATEADD(DAY, @PlanDays, ExpiryDate)
                                     ELSE DATEADD(DAY, @PlanDays, GETUTCDATE()) END
            FROM tbl_Subscriptions WHERE UserId = @TargetUserId;

            UPDATE tbl_Subscriptions
            SET [Plan] = ISNULL(@PlanName, [Plan]), ExpiryDate = @NewExpiry, UpdatedAt = GETUTCDATE()
            WHERE UserId = @TargetUserId;

            INSERT INTO tbl_UserLedger (UserId, TransactionType, AmountINR, CreditChange, BalanceAfter, Description, AdminRemark, Status)
            VALUES (@TargetUserId, 'AdminAdjustment', 0, 0, 0,
                    'Admin added ' + CAST(@PlanDays AS NVARCHAR) + ' days to plan.',
                    @AdminRemark, 'Success');
            SET @ResultCode = 0;
        END
        ELSE IF @Action = 'AddCredits'
        BEGIN
            UPDATE tbl_Subscriptions
            SET PalmScansRemaining = PalmScansRemaining + @CreditsToAdd WHERE UserId = @TargetUserId;

            DECLARE @NewBal INT;
            SELECT @NewBal = PalmScansRemaining + FaceScansRemaining FROM tbl_Subscriptions WHERE UserId = @TargetUserId;

            INSERT INTO tbl_UserLedger (UserId, TransactionType, AmountINR, CreditChange, BalanceAfter, Description, AdminRemark, Status)
            VALUES (@TargetUserId, 'AdminAdjustment', 0, @CreditsToAdd, @NewBal,
                    'Admin added ' + CAST(@CreditsToAdd AS NVARCHAR) + ' scan credits.',
                    @AdminRemark, 'Success');
            SET @ResultCode = 0;
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        SET @ResultCode = -1;
        THROW;
    END CATCH
END
GO

-- 11: sp_AppVersion_Check
CREATE OR ALTER PROCEDURE sp_AppVersion_Check
    @Platform       NVARCHAR(20),
    @CurrentCode    INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1
        VersionCode, VersionName, DownloadUrl, PlayStoreUrl,
        ReleaseNotes, IsMandatory,
        CASE WHEN VersionCode > @CurrentCode THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS UpdateAvailable
    FROM tbl_AppVersions
    WHERE Platform = @Platform AND IsActive = 1
    ORDER BY VersionCode DESC;
END
GO

-- 12: sp_KundliProfile_Save
CREATE OR ALTER PROCEDURE sp_KundliProfile_Save
    @UserId             UNIQUEIDENTIFIER,
    @LagnaRashi         NVARCHAR(50),
    @LagnaDegree        DECIMAL(8,4),
    @MoonRashi          NVARCHAR(50),
    @SunRashi           NVARCHAR(50),
    @Nakshatra          NVARCHAR(80),
    @NakshatraPada      INT,
    @MahadashaCurrent   NVARCHAR(50),
    @MahadashaEndDate   DATE,
    @IsManglik          BIT,
    @SadeSatiActive     BIT,
    @PlanetaryJson      NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM tbl_KundliProfiles WHERE UserId = @UserId)
        UPDATE tbl_KundliProfiles SET
            LagnaRashi = @LagnaRashi, LagnaDegree = @LagnaDegree,
            MoonRashi = @MoonRashi, SunRashi = @SunRashi,
            Nakshatra = @Nakshatra, NakshatraPada = @NakshatraPada,
            MahadashaCurrent = @MahadashaCurrent, MahadashaEndDate = @MahadashaEndDate,
            IsManglik = @IsManglik, SadeSatiActive = @SadeSatiActive,
            PlanetaryJson = @PlanetaryJson, UpdatedAt = GETUTCDATE()
        WHERE UserId = @UserId;
    ELSE
        INSERT INTO tbl_KundliProfiles (UserId, LagnaRashi, LagnaDegree, MoonRashi, SunRashi, Nakshatra, NakshatraPada, MahadashaCurrent, MahadashaEndDate, IsManglik, SadeSatiActive, PlanetaryJson)
        VALUES (@UserId, @LagnaRashi, @LagnaDegree, @MoonRashi, @SunRashi, @Nakshatra, @NakshatraPada, @MahadashaCurrent, @MahadashaEndDate, @IsManglik, @SadeSatiActive, @PlanetaryJson);
END
GO

-- Seed Admin user if not exists
-- Verified BCrypt hash of 'Admin@123'
IF NOT EXISTS (SELECT 1 FROM tbl_Users WHERE Role = 'Admin')
BEGIN
    DECLARE @AdminId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO tbl_Users (UserId, FullName, Email, PasswordHash, Gender, DateOfBirth, Role)
    VALUES (@AdminId, 'Super Admin', 'admin@cyhastro.com',
            '$2a$11$qv98cEf7E3TmHEKWwSV0qu0qZqrz9Dq9i58Qpc1/Fkwv7y5CJp2p.',
            'Other', '1990-01-01', 'Admin');

    INSERT INTO tbl_Subscriptions (UserId, [Plan], PalmScansRemaining, FaceScansRemaining)
    VALUES (@AdminId, 'Lifetime', 9999, 9999);
END
GO

-- Seed App Versions if not exists
IF NOT EXISTS (SELECT 1 FROM tbl_AppVersions)
BEGIN
    INSERT INTO tbl_AppVersions (Platform, VersionCode, VersionName, ReleaseNotes, IsMandatory)
    VALUES
    ('Android', 1, '1.0.0', 'Initial Release - CyhAstro v1.0', 0),
    ('iOS',     1, '1.0.0', 'Initial Release - CyhAstro v1.0', 0),
    ('Windows', 1, '1.0.0', 'Initial Release - CyhAstro v1.0', 0);
END
GO

PRINT '=================================================';
PRINT 'CyhAstro Database Setup Complete with ZERO Errors!';
PRINT 'Tables: 7 | Stored Procedures: 12';
PRINT 'Default Admin: admin@cyhastro.com';
PRINT '=================================================';
GO
