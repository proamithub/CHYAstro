using CyhAstro.Core.DTOs;
using CyhAstro.Core.Entities;
using CyhAstro.Core.Interfaces;
using CyhAstro.Infrastructure.Data;
using Dapper;
using System.Data;

namespace CyhAstro.Infrastructure.Repositories;

/// <summary>
/// Admin repository — IAdminRepository implement karta hai.
/// Dashboard stats, user management, ledger aur settings sab yahan.
/// </summary>
public class AdminRepository : IAdminRepository
{
    private readonly ISqlConnectionFactory _db;

    public AdminRepository(ISqlConnectionFactory db) => _db = db;

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleAsync<DashboardStatsDto>(
            "sp_Admin_Dashboard",
            commandType: CommandType.StoredProcedure);
    }

    public async Task<PagedResult<AdminUserListItemDto>> GetUsersListAsync(
        string? search, string? planFilter, string? statusFilter, int page, int pageSize)
    {
        using var conn = _db.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@SearchQuery",  search);
        p.Add("@PlanFilter",   planFilter);
        p.Add("@StatusFilter", statusFilter);
        p.Add("@PageNumber",   page);
        p.Add("@PageSize",     pageSize);
        p.Add("@TotalCount",   dbType: DbType.Int32, direction: ParameterDirection.Output);

        var items = (await conn.QueryAsync<AdminUserListItemDto>(
            "sp_Admin_GetUsersList", p,
            commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<AdminUserListItemDto>
        {
            Items      = items,
            TotalCount = p.Get<int>("@TotalCount"),
            PageNumber  = page,
            PageSize   = pageSize
        };
    }

    public async Task<PagedResult<UserLedger>> GetLedgerAsync(
        Guid? userId, DateOnly? from, DateOnly? to, int page, int pageSize)
    {
        using var conn = _db.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@UserId",     userId);
        p.Add("@FromDate",   from?.ToDateTime(TimeOnly.MinValue));
        p.Add("@ToDate",     to?.ToDateTime(TimeOnly.MinValue));
        p.Add("@PageNumber", page);
        p.Add("@PageSize",   pageSize);
        p.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

        var items = (await conn.QueryAsync<UserLedger>(
            "sp_Admin_GetUserLedger", p,
            commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<UserLedger>
        {
            Items      = items,
            TotalCount = p.Get<int>("@TotalCount"),
            PageNumber  = page,
            PageSize   = pageSize
        };
    }

    /// <summary>
    /// ATOMIC: Block/Unblock/AddPlan/AddCredits + automatic ledger audit entry.
    /// ResultCode: 0 = Success, -1 = Error
    /// </summary>
    public async Task<int> AdjustUserAsync(
        Guid targetUserId, Guid adminUserId, string action,
        string? planName, int planDays, int creditsToAdd, string? remark)
    {
        using var conn = _db.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@TargetUserId",  targetUserId);
        p.Add("@AdminUserId",   adminUserId);
        p.Add("@Action",        action);
        p.Add("@PlanName",      planName);
        p.Add("@PlanDays",      planDays);
        p.Add("@CreditsToAdd",  creditsToAdd);
        p.Add("@AdminRemark",   remark);
        p.Add("@ResultCode",    dbType: DbType.Int32, direction: ParameterDirection.Output);

        await conn.ExecuteAsync(
            "sp_Admin_AdjustUser", p,
            commandType: CommandType.StoredProcedure);

        return p.Get<int>("@ResultCode");
    }

    public async Task<AppVersionCheckDto?> CheckAppVersionAsync(string platform, int currentVersionCode)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<AppVersionCheckDto>(
            "sp_AppVersion_Check",
            new { Platform = platform, CurrentCode = currentVersionCode },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<bool> UpdateSettingAsync(string key, string value, string updatedBy)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(
            "UPDATE tbl_AppSettings SET SettingValue = @Value, UpdatedAt = GETUTCDATE(), UpdatedBy = @UpdatedBy WHERE SettingKey = @Key",
            new { Key = key, Value = value, UpdatedBy = updatedBy });
        return rows > 0;
    }

    public async Task<Dictionary<string, string>> GetAllSettingsAsync()
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync("SELECT SettingKey, SettingValue FROM tbl_AppSettings ORDER BY SettingKey");
        return rows.ToDictionary(r => (string)r.SettingKey, r => (string)r.SettingValue);
    }
}
