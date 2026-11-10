using CyhAstro.Core.DTOs;
using CyhAstro.Core.Entities;
using CyhAstro.Core.Interfaces;
using CyhAstro.Infrastructure.Data;
using Dapper;
using System.Data;

namespace CyhAstro.Infrastructure.Repositories;

/// <summary>
/// Subscription repository — ISubscriptionRepository implement karta hai.
/// Plan upgrade (ATOMIC) aur quota check yahan hota hai.
/// </summary>
public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly ISqlConnectionFactory _db;

    public SubscriptionRepository(ISqlConnectionFactory db) => _db = db;

    /// <summary>
    /// ATOMIC TRANSACTION:
    /// 1. Plan update karo (tbl_Subscriptions)
    /// 2. Ledger entry karo (tbl_UserLedger)
    /// Agar koi bhi step fail hua → ROLLBACK, koi bhi change nahi bachega
    /// </summary>
    public async Task<(int ResultCode, DateTime? NewExpiryDate)> UpgradePlanAsync(SubscriptionUpgradeRequest req)
    {
        using var conn = _db.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@UserId",         req.UserId);
        p.Add("@NewPlan",        req.NewPlan);
        p.Add("@AmountPaidINR",  req.AmountPaidINR);
        p.Add("@PaymentGateway", req.PaymentGateway);
        p.Add("@GatewayTxnId",   req.GatewayTxnId);
        p.Add("@ResultCode",     dbType: DbType.Int32,    direction: ParameterDirection.Output);
        p.Add("@NewExpiryDate",  dbType: DbType.DateTime2, direction: ParameterDirection.Output);

        await conn.ExecuteAsync(
            "sp_Subscription_Upgrade", p,
            commandType: CommandType.StoredProcedure);

        return (p.Get<int>("@ResultCode"), p.Get<DateTime?>("@NewExpiryDate"));
    }

    public async Task<Subscription?> GetSubscriptionAsync(Guid userId)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Subscription>(
            "SELECT * FROM tbl_Subscriptions WHERE UserId = @UserId",
            new { UserId = userId });
    }

    public async Task<Dictionary<string, string>> GetAppSettingsAsync()
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync("SELECT SettingKey, SettingValue FROM tbl_AppSettings");
        return rows.ToDictionary(r => (string)r.SettingKey, r => (string)r.SettingValue);
    }
}
