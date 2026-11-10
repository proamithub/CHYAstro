// ============================================================
// CyhAstro.Core — Repository Interfaces
// Infrastructure layer inhe implement karega
// API aur Admin sirf ye interfaces use karenge (not concrete classes)
// ============================================================

using CyhAstro.Core.DTOs;
using CyhAstro.Core.Entities;

namespace CyhAstro.Core.Interfaces;

// ─────────────────────────────────────────────
// 1. USER REPOSITORY
// ─────────────────────────────────────────────
public interface IUserRepository
{
    /// <summary>Naya user register ya existing user login. ResultCode: 0=New, 1=Existing, -1=Error</summary>
    Task<(int ResultCode, Guid? UserId)> RegisterOrLoginAsync(RegisterRequest req);

    /// <summary>User ka poora profile + subscription + kundli snapshot</summary>
    Task<UserProfileDto?> GetProfileAsync(Guid userId);

    /// <summary>Device token update (push notifications ke liye)</summary>
    Task UpdateDeviceTokenAsync(Guid userId, string token);
}

// ─────────────────────────────────────────────
// 2. SCAN REPOSITORY
// ─────────────────────────────────────────────
public interface IScanRepository
{
    /// <summary>
    /// ATOMIC: Scan save karo + 1 credit deduct karo.
    /// ResultCode: 0=OK, 1=No Credits, -1=DB Error
    /// </summary>
    Task<(int ResultCode, Guid ScanId, int CreditsLeft)> SaveScanAsync(
        Guid userId, string scanType, string? handSide,
        string imagePath, string? thumbnailPath,
        string? aiRawResponse, string? summaryText);

    /// <summary>Offline scan ka sirf record save karo (credit baad mein deduct hoga)</summary>
    Task<Guid> SaveOfflineScanAsync(Guid userId, string scanType, string? handSide, string imagePath, DateTime scannedAt);

    /// <summary>Gemini se result aane ke baad update karo aur Synced mark karo</summary>
    Task UpdateAiResultAsync(Guid scanId, string aiRawResponse, string summaryText);

    /// <summary>User ke sabhi scans (paginated)</summary>
    Task<IEnumerable<ScanHistory>> GetUserScansAsync(Guid userId, int page = 1, int pageSize = 20);

    /// <summary>Pending (offline) scans jo server pe sync hone hain</summary>
    Task<IEnumerable<ScanHistory>> GetPendingScansAsync(Guid userId);
}

// ─────────────────────────────────────────────
// 3. KUNDLI REPOSITORY
// ─────────────────────────────────────────────
public interface IKundliRepository
{
    /// <summary>Kundli result save ya update karo (upsert)</summary>
    Task SaveKundliAsync(KundliProfile profile);

    /// <summary>User ki Kundli lo</summary>
    Task<KundliProfile?> GetKundliAsync(Guid userId);
}

// ─────────────────────────────────────────────
// 4. SUBSCRIPTION REPOSITORY
// ─────────────────────────────────────────────
public interface ISubscriptionRepository
{
    /// <summary>ATOMIC: Plan upgrade + Ledger entry — ek hi SP call mein</summary>
    Task<(int ResultCode, DateTime? NewExpiryDate)> UpgradePlanAsync(SubscriptionUpgradeRequest req);

    /// <summary>User ka current plan aur quota</summary>
    Task<Subscription?> GetSubscriptionAsync(Guid userId);

    /// <summary>App settings se plan prices lo</summary>
    Task<Dictionary<string, string>> GetAppSettingsAsync();
}

// ─────────────────────────────────────────────
// 5. ADMIN REPOSITORY
// ─────────────────────────────────────────────
public interface IAdminRepository
{
    /// <summary>Dashboard stats: Users, Revenue, Scans, Subscribers</summary>
    Task<DashboardStatsDto> GetDashboardStatsAsync();

    /// <summary>Users list with search, filter aur pagination</summary>
    Task<PagedResult<AdminUserListItemDto>> GetUsersListAsync(
        string? search, string? planFilter, string? statusFilter, int page, int pageSize);

    /// <summary>Poora ledger / khata with filters (sabhi users ya kisi ek user ka)</summary>
    Task<PagedResult<UserLedger>> GetLedgerAsync(
        Guid? userId, DateOnly? from, DateOnly? to, int page, int pageSize);

    /// <summary>ATOMIC: Block/Unblock/AddPlan/AddCredits + audit entry</summary>
    Task<int> AdjustUserAsync(
        Guid targetUserId, Guid adminUserId, string action,
        string? planName, int planDays, int creditsToAdd, string? remark);

    /// <summary>App version check karo (in-app update ke liye)</summary>
    Task<AppVersionCheckDto?> CheckAppVersionAsync(string platform, int currentVersionCode);

    /// <summary>App settings update karo (prices, prompts, quotas)</summary>
    Task<bool> UpdateSettingAsync(string key, string value, string updatedBy);

    /// <summary>Saari app settings lo</summary>
    Task<Dictionary<string, string>> GetAllSettingsAsync();
}

// ─────────────────────────────────────────────
// 6. APP VERSION REPOSITORY
// ─────────────────────────────────────────────
public interface IAppVersionRepository
{
    Task<AppVersionCheckDto?> CheckVersionAsync(string platform, int currentCode);
    Task<bool> AddVersionAsync(string platform, int versionCode, string versionName, string? downloadUrl, string? releaseNotes, bool isMandatory);
    Task<IEnumerable<AppVersionCheckDto>> GetAllVersionsAsync(string platform);
}
