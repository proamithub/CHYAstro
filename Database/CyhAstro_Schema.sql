-- ============================================================
-- CyhAstro Database Schema
-- SQL Server 2019+ / SQL Server Express
-- All tables, indexes and stored procedures
-- ============================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'CyhAstroDB')
BEGIN
    CREATE DATABASE CyhAstroDB;
END
GO

USE CyhAstroDB;
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
        Gender          NVARCHAR(10)        NOT NULL DEFAULT 'Other',   -- Male, Female, Other
        DateOfBirth     DATE                NOT NULL,
        TimeOfBirth     TIME                NULL,
        BirthCity       NVARCHAR(150)       NULL,
        BirthLatitude   DECIMAL(9,6)        NULL,
        BirthLongitude  DECIMAL(9,6)        NULL,
        ProfilePhotoUrl NVARCHAR(500)       NULL,
        Role            NVARCHAR(20)        NOT NULL DEFAULT 'User',    -- User, Admin
        IsActive        BIT                 NOT NULL DEFAULT 1,
        IsBlocked       BIT                 NOT NULL DEFAULT 0,
        BlockReason     NVARCHAR(300)       NULL,
        DeviceToken     NVARCHAR(500)       NULL,       -- Push notification
        LastLoginAt     DATETIME2           NULL,
        CreatedAt       DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt       DATETIME2           NOT NULL DEFAULT GETUTCDATE()
    );
    CREATE UNIQUE INDEX UX_Users_Email ON tbl_Users(Email) WHERE Email IS NOT NULL;
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
        Plan                NVARCHAR(20)        NOT NULL DEFAULT 'Free', -- Free, Weekly, Monthly, Yearly, Lifetime
        StartDate           DATETIME2           NULL,
        ExpiryDate          DATETIME2           NULL,
        IsActive            AS (CASE WHEN Plan = 'Free' THEN CAST(1 AS BIT)
                                     WHEN ExpiryDate > GETUTCDATE() THEN CAST(1 AS BIT)
                                     ELSE CAST(0 AS BIT) END) PERSISTED,
        PalmScansRemaining  INT                 NOT NULL DEFAULT 1,     -- Free: 1, Paid: 999
        FaceScansRemaining  INT                 NOT NULL DEFAULT 0,     -- Free: 0, Paid: 999
        ChatQuestionsToday  INT                 NOT NULL DEFAULT 3,
        QuotaResetDate      DATE                NOT NULL DEFAULT CAST(GETUTCDATE() AS DATE),
        PaymentGateway      NVARCHAR(50)        NULL,  -- Razorpay, GooglePlay, AppleStore, Manual
        GatewayTxnId        NVARCHAR(300)       NULL,
        CreatedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE()
    );
    CREATE UNIQUE INDEX UX_Sub_UserId ON tbl_Subscriptions(UserId);
END
GO

-- ============================================================
-- TABLE 3: tbl_UserLedger (Financial & Credit Khata)
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_UserLedger')
BEGIN
    CREATE TABLE tbl_UserLedger (
        LedgerId            UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID() PRIMARY KEY,
        UserId              UNIQUEIDENTIFIER    NOT NULL REFERENCES tbl_Users(UserId),
        TransactionType     NVARCHAR(50)        NOT NULL,
        -- SubscriptionPurchase, ScanDeduction, CreditTopUp, Refund, AdminAdjustment, DailyReset
        AmountINR           DECIMAL(10,2)       NOT NULL DEFAULT 0.00,
        CreditChange        INT                 NOT NULL DEFAULT 0,     -- +5 add, -1 deduct
        BalanceAfter        INT                 NOT NULL DEFAULT 0,     -- Total credits after this txn
        Description         NVARCHAR(500)       NOT NULL,
        Status              NVARCHAR(20)        NOT NULL DEFAULT 'Success', -- Pending, Success, Failed
        PaymentGateway      NVARCHAR(50)        NULL,
        GatewayTxnId        NVARCHAR(300)       NULL,
        AdminRemark         NVARCHAR(300)       NULL,
        CreatedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE()
    );
    CREATE INDEX IX_Ledger_UserId ON tbl_UserLedger(UserId);
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
        LagnaRashi          NVARCHAR(50)        NULL,   -- e.g. Mesh (Aries)
        LagnaDegree         DECIMAL(8,4)        NULL,
        MoonRashi           NVARCHAR(50)        NULL,   -- Chandra Rashi
        SunRashi            NVARCHAR(50)        NULL,   -- Surya Rashi
        Nakshatra           NVARCHAR(80)        NULL,   -- e.g. Rohini
        NakshatraPada       INT                 NULL,   -- 1 to 4
        MahadashaCurrent    NVARCHAR(50)        NULL,
        MahadashaEndDate    DATE                NULL,
        IsManglik           BIT                 NOT NULL DEFAULT 0,
        SadeSatiActive      BIT                 NOT NULL DEFAULT 0,
        PlanetaryJson       NVARCHAR(MAX)       NULL,   -- All 9 Grahas positions JSON
        AyanamsaUsed        NVARCHAR(30)        NULL DEFAULT 'Lahiri',
        CreatedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE()
    );
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
        ScanType            NVARCHAR(20)        NOT NULL,   -- Palm, Face
        HandSide            NVARCHAR(10)        NULL,       -- Left, Right (Palm ke liye)
        ImagePath           NVARCHAR(500)       NOT NULL,   -- Server upload path
        ThumbnailPath       NVARCHAR(500)       NULL,
        AiRawResponse       NVARCHAR(MAX)       NULL,       -- Gemini ka full JSON response
        SummaryText         NVARCHAR(MAX)       NULL,       -- User ko dikhne wala result
        IsFullReportUnlocked BIT               NOT NULL DEFAULT 0,
        SyncStatus          NVARCHAR(20)        NOT NULL DEFAULT 'Synced', -- Pending, Synced, Failed
        ScannedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
        SyncedAt            DATETIME2           NULL
    );
    CREATE INDEX IX_Scan_UserId ON tbl_ScanHistory(UserId);
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
        Platform        NVARCHAR(20)        NOT NULL,   -- Android, iOS, Windows
        VersionCode     INT                 NOT NULL,   -- 2, 3, 4...
        VersionName     NVARCHAR(20)        NOT NULL,   -- 1.0.1, 1.2.0
        DownloadUrl     NVARCHAR(500)       NULL,       -- Direct APK URL
        PlayStoreUrl    NVARCHAR(500)       NULL,
        ReleaseNotes    NVARCHAR(1000)      NULL,
        IsMandatory     BIT                 NOT NULL DEFAULT 0,
        IsActive        BIT                 NOT NULL DEFAULT 1,
        ReleasedAt      DATETIME2           NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

-- ============================================================
-- TABLE 7: tbl_AppSettings (Admin se configure hoga)
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

    -- Default Settings Insert
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
-- ============================================================
-- STORED PROCEDURES
-- ============================================================
-- ============================================================


-- ============================================================
-- SP 1: sp_User_RegisterOrLogin
-- ============================================================
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
    @ResultCode     INT             OUTPUT,  -- 0=Success, 1=Already Exists, -1=Error
    @UserId         UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Check if user already exists
        IF EXISTS (SELECT 1 FROM tbl_Users WHERE Email = @Email AND @Email IS NOT NULL)
           OR EXISTS (SELECT 1 FROM tbl_Users WHERE PhoneNumber = @PhoneNumber AND @PhoneNumber IS NOT NULL)
        BEGIN
            -- Login: return existing user
            SELECT @UserId = UserId FROM tbl_Users
            WHERE (Email = @Email AND @Email IS NOT NULL)
               OR (PhoneNumber = @PhoneNumber AND @PhoneNumber IS NOT NULL);

            -- Update device token and last login
            UPDATE tbl_Users SET DeviceToken = @DeviceToken, LastLoginAt = GETUTCDATE()
            WHERE UserId = @UserId;

            SET @ResultCode = 1; -- Existing user
            COMMIT TRANSACTION;
            RETURN;
        END

        -- New User Registration
        SET @UserId = NEWID();

        -- Get free scan settings
        DECLARE @FreeScans INT = (SELECT CAST(SettingValue AS INT) FROM tbl_AppSettings WHERE SettingKey = 'FreeUserPalmScans');

        INSERT INTO tbl_Users (UserId, FullName, Email, PhoneNumber, PasswordHash, Gender, DateOfBirth, TimeOfBirth, BirthCity, BirthLatitude, BirthLongitude, DeviceToken, LastLoginAt)
        VALUES (@UserId, @FullName, @Email, @PhoneNumber, @PasswordHash, @Gender, @DateOfBirth, @TimeOfBirth, @BirthCity, @BirthLatitude, @BirthLongitude, @DeviceToken, GETUTCDATE());

        -- Create default Free subscription + initial quota
        INSERT INTO tbl_Subscriptions (UserId, Plan, PalmScansRemaining, FaceScansRemaining)
        VALUES (@UserId, 'Free', @FreeScans, 0);

        -- Log first ledger entry: Welcome credits
        INSERT INTO tbl_UserLedger (UserId, TransactionType, AmountINR, CreditChange, BalanceAfter, Description, Status)
        VALUES (@UserId, 'WelcomeCredit', 0, @FreeScans, @FreeScans, 'Welcome! ' + CAST(@FreeScans AS NVARCHAR) + ' free palm scan credited.', 'Success');

        SET @ResultCode = 0; -- New user registered
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        SET @ResultCode = -1;
        THROW;
    END CATCH
END
GO

-- ============================================================
-- SP 2: sp_User_GetProfile
-- ============================================================
CREATE OR ALTER PROCEDURE sp_User_GetProfile
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        u.UserId, u.FullName, u.Email, u.PhoneNumber, u.Gender,
        u.DateOfBirth, u.TimeOfBirth, u.BirthCity, u.BirthLatitude, u.BirthLongitude,
        u.ProfilePhotoUrl, u.Role, u.IsActive, u.IsBlocked, u.LastLoginAt,
        s.Plan, s.IsActive AS IsSubscriptionActive, s.ExpiryDate,
        s.PalmScansRemaining, s.FaceScansRemaining, s.ChatQuestionsToday,
        k.LagnaRashi, k.MoonRashi, k.SunRashi, k.Nakshatra, k.NakshatraPada,
        k.MahadashaCurrent, k.MahadashaEndDate, k.IsManglik, k.SadeSatiActive
    FROM tbl_Users u
    LEFT JOIN tbl_Subscriptions s ON s.UserId = u.UserId
    LEFT JOIN tbl_KundliProfiles k ON k.UserId = u.UserId
    WHERE u.UserId = @UserId AND u.IsBlocked = 0;
END
GO

-- ============================================================
-- SP 3: sp_Scan_Save (ATOMIC - Credit Deduct + Save)
-- ============================================================
CREATE OR ALTER PROCEDURE sp_Scan_Save
    @UserId         UNIQUEIDENTIFIER,
    @ScanType       NVARCHAR(20),    -- Palm, Face
    @HandSide       NVARCHAR(10)     = NULL,
    @ImagePath      NVARCHAR(500),
    @ThumbnailPath  NVARCHAR(500)    = NULL,
    @AiRawResponse  NVARCHAR(MAX)    = NULL,
    @SummaryText    NVARCHAR(MAX)    = NULL,
    @ScanId         UNIQUEIDENTIFIER OUTPUT,
    @ResultCode     INT              OUTPUT,  -- 0=OK, 1=No Credits, -1=Error
    @CreditsLeft    INT              OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Check credits
        DECLARE @PalmLeft INT, @FaceLeft INT, @CurrentBalance INT;
        SELECT @PalmLeft = PalmScansRemaining, @FaceLeft = FaceScansRemaining
        FROM tbl_Subscriptions WHERE UserId = @UserId;

        IF (@ScanType = 'Palm' AND @PalmLeft <= 0) OR (@ScanType = 'Face' AND @FaceLeft <= 0)
        BEGIN
            SET @ResultCode = 1; -- No Credits
            SET @CreditsLeft = 0;
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- Deduct credit
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

        -- Get current total balance for ledger
        SELECT @CurrentBalance = (PalmScansRemaining + FaceScansRemaining) FROM tbl_Subscriptions WHERE UserId = @UserId;

        -- Save scan record
        SET @ScanId = NEWID();
        INSERT INTO tbl_ScanHistory (ScanId, UserId, ScanType, HandSide, ImagePath, ThumbnailPath, AiRawResponse, SummaryText, SyncStatus)
        VALUES (@ScanId, @UserId, @ScanType, @HandSide, @ImagePath, @ThumbnailPath, @AiRawResponse, @SummaryText, 'Synced');

        -- Log ledger entry
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

-- ============================================================
-- SP 4: sp_Scan_SaveOffline (Mobile offline queue se)
-- ============================================================
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

-- ============================================================
-- SP 5: sp_Scan_UpdateAiResult (After Gemini processing)
-- ============================================================
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

-- ============================================================
-- SP 6: sp_Subscription_Upgrade (ATOMIC - Plan + Ledger)
-- ============================================================
CREATE OR ALTER PROCEDURE sp_Subscription_Upgrade
    @UserId             UNIQUEIDENTIFIER,
    @NewPlan            NVARCHAR(20),    -- Weekly, Monthly, Yearly, Lifetime
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

        -- Calculate expiry
        SET @NewExpiryDate = CASE @NewPlan
            WHEN 'Weekly'   THEN DATEADD(DAY, 7, GETUTCDATE())
            WHEN 'Monthly'  THEN DATEADD(MONTH, 1, GETUTCDATE())
            WHEN 'Yearly'   THEN DATEADD(YEAR, 1, GETUTCDATE())
            WHEN 'Lifetime' THEN DATEADD(YEAR, 100, GETUTCDATE())
            ELSE NULL
        END;

        DECLARE @PaidPalmScans INT = CAST((SELECT SettingValue FROM tbl_AppSettings WHERE SettingKey = 'PaidUserPalmScans') AS INT);
        DECLARE @PaidFaceScans INT = CAST((SELECT SettingValue FROM tbl_AppSettings WHERE SettingKey = 'PaidUserFaceScans') AS INT);

        -- Update subscription (STEP 1)
        UPDATE tbl_Subscriptions
        SET Plan = @NewPlan,
            StartDate = GETUTCDATE(),
            ExpiryDate = @NewExpiryDate,
            PalmScansRemaining = @PaidPalmScans,
            FaceScansRemaining = @PaidFaceScans,
            PaymentGateway = @PaymentGateway,
            GatewayTxnId = @GatewayTxnId,
            UpdatedAt = GETUTCDATE()
        WHERE UserId = @UserId;

        -- Log ledger entry (STEP 2)
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

-- ============================================================
-- SP 7: sp_Admin_Dashboard
-- ============================================================
CREATE OR ALTER PROCEDURE sp_Admin_Dashboard
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        (SELECT COUNT(*) FROM tbl_Users WHERE Role = 'User') AS TotalUsers,
        (SELECT COUNT(*) FROM tbl_Users WHERE CAST(CreatedAt AS DATE) = CAST(GETUTCDATE() AS DATE)) AS NewUsersToday,
        (SELECT COUNT(*) FROM tbl_Subscriptions WHERE Plan != 'Free' AND IsActive = 1) AS ActiveSubscribers,
        (SELECT COUNT(*) FROM tbl_ScanHistory WHERE CAST(ScannedAt AS DATE) = CAST(GETUTCDATE() AS DATE)) AS TotalScansToday,
        (SELECT COUNT(*) FROM tbl_ScanHistory WHERE ScanType = 'Palm' AND CAST(ScannedAt AS DATE) = CAST(GETUTCDATE() AS DATE)) AS PalmScansToday,
        (SELECT COUNT(*) FROM tbl_ScanHistory WHERE ScanType = 'Face' AND CAST(ScannedAt AS DATE) = CAST(GETUTCDATE() AS DATE)) AS FaceScansToday,
        (SELECT ISNULL(SUM(AmountINR), 0) FROM tbl_UserLedger WHERE TransactionType = 'SubscriptionPurchase' AND CAST(CreatedAt AS DATE) = CAST(GETUTCDATE() AS DATE) AND Status = 'Success') AS RevenueToday,
        (SELECT ISNULL(SUM(AmountINR), 0) FROM tbl_UserLedger WHERE TransactionType = 'SubscriptionPurchase' AND MONTH(CreatedAt) = MONTH(GETUTCDATE()) AND YEAR(CreatedAt) = YEAR(GETUTCDATE()) AND Status = 'Success') AS RevenueThisMonth,
        (SELECT ISNULL(SUM(AmountINR), 0) FROM tbl_UserLedger WHERE TransactionType = 'SubscriptionPurchase' AND Status = 'Success') AS RevenueTotalAllTime;
END
GO

-- ============================================================
-- SP 8: sp_Admin_GetUsersList
-- ============================================================
CREATE OR ALTER PROCEDURE sp_Admin_GetUsersList
    @SearchQuery    NVARCHAR(100)   = NULL,
    @PlanFilter     NVARCHAR(20)    = NULL,  -- Free, Weekly, Monthly, Yearly, Lifetime
    @StatusFilter   NVARCHAR(20)    = NULL,  -- Active, Blocked
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
    AND (@PlanFilter IS NULL OR s.Plan = @PlanFilter)
    AND (@StatusFilter IS NULL OR (@StatusFilter = 'Blocked' AND u.IsBlocked = 1) OR (@StatusFilter = 'Active' AND u.IsBlocked = 0));

    SELECT
        u.UserId, u.FullName, u.Email, u.PhoneNumber, u.Gender,
        u.DateOfBirth, u.IsBlocked, u.LastLoginAt, u.CreatedAt,
        s.Plan, s.IsActive AS IsSubActive, s.ExpiryDate,
        s.PalmScansRemaining, s.FaceScansRemaining,
        (SELECT COUNT(*) FROM tbl_ScanHistory sh WHERE sh.UserId = u.UserId) AS TotalScans
    FROM tbl_Users u
    LEFT JOIN tbl_Subscriptions s ON s.UserId = u.UserId
    WHERE u.Role = 'User'
    AND (@SearchQuery IS NULL OR u.FullName LIKE '%' + @SearchQuery + '%'
         OR u.Email LIKE '%' + @SearchQuery + '%'
         OR u.PhoneNumber LIKE '%' + @SearchQuery + '%')
    AND (@PlanFilter IS NULL OR s.Plan = @PlanFilter)
    AND (@StatusFilter IS NULL OR (@StatusFilter = 'Blocked' AND u.IsBlocked = 1) OR (@StatusFilter = 'Active' AND u.IsBlocked = 0))
    ORDER BY u.CreatedAt DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

-- ============================================================
-- SP 9: sp_Admin_GetUserLedger
-- ============================================================
CREATE OR ALTER PROCEDURE sp_Admin_GetUserLedger
    @UserId         UNIQUEIDENTIFIER    = NULL,  -- NULL = all users
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

    SELECT
        l.LedgerId, l.UserId, u.FullName, u.Email, u.PhoneNumber,
        l.TransactionType, l.AmountINR, l.CreditChange, l.BalanceAfter,
        l.Description, l.Status, l.PaymentGateway, l.GatewayTxnId,
        l.AdminRemark, l.CreatedAt
    FROM tbl_UserLedger l
    INNER JOIN tbl_Users u ON u.UserId = l.UserId
    WHERE (@UserId IS NULL OR l.UserId = @UserId)
    AND (@FromDate IS NULL OR CAST(l.CreatedAt AS DATE) >= @FromDate)
    AND (@ToDate IS NULL OR CAST(l.CreatedAt AS DATE) <= @ToDate)
    ORDER BY l.CreatedAt DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

-- ============================================================
-- SP 10: sp_Admin_AdjustUser (Manual: Block/Plan/Credits)
-- ============================================================
CREATE OR ALTER PROCEDURE sp_Admin_AdjustUser
    @TargetUserId       UNIQUEIDENTIFIER,
    @AdminUserId        UNIQUEIDENTIFIER,
    @Action             NVARCHAR(50),    -- Block, Unblock, AddPlan, AddCredits
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
            SET Plan = ISNULL(@PlanName, Plan), ExpiryDate = @NewExpiry, UpdatedAt = GETUTCDATE()
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

-- ============================================================
-- SP 11: sp_AppVersion_Check
-- ============================================================
CREATE OR ALTER PROCEDURE sp_AppVersion_Check
    @Platform       NVARCHAR(20),    -- Android, iOS, Windows
    @CurrentCode    INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1
        VersionCode, VersionName, DownloadUrl, PlayStoreUrl,
        ReleaseNotes, IsMandatory,
        CASE WHEN VersionCode > @CurrentCode THEN 1 ELSE 0 END AS UpdateAvailable
    FROM tbl_AppVersions
    WHERE Platform = @Platform AND IsActive = 1
    ORDER BY VersionCode DESC;
END
GO

-- ============================================================
-- SP 12: sp_KundliProfile_Save
-- ============================================================
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

-- ============================================================
-- Initial Admin User (Change password after first login!)
-- Password: Admin@123 (bcrypt hash placeholder)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM tbl_Users WHERE Role = 'Admin')
BEGIN
    DECLARE @AdminId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO tbl_Users (UserId, FullName, Email, PasswordHash, Gender, DateOfBirth, Role)
    VALUES (@AdminId, 'Super Admin', 'admin@cyhastro.com',
            'REPLACE_WITH_BCRYPT_HASH_OF_Admin@123',
            'Other', '1990-01-01', 'Admin');

    INSERT INTO tbl_Subscriptions (UserId, Plan, PalmScansRemaining, FaceScansRemaining)
    VALUES (@AdminId, 'Lifetime', 9999, 9999);
END
GO

-- Initial App Version
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
PRINT 'CyhAstro Database Setup Complete!';
PRINT 'Tables: 7 | Stored Procedures: 12';
PRINT 'Default Admin: admin@cyhastro.com';
PRINT 'IMPORTANT: Update admin password hash before production!';
PRINT '=================================================';
GO
