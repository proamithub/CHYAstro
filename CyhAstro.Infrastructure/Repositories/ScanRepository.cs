using CyhAstro.Core.Entities;
using CyhAstro.Core.Interfaces;
using CyhAstro.Infrastructure.Data;
using Dapper;
using System.Data;

namespace CyhAstro.Infrastructure.Repositories;

/// <summary>
/// Scan repository — IScanRepository implement karta hai.
/// Palm aur Face scan ka save, credit deduction aur sync yahan hota hai.
/// </summary>
public class ScanRepository : IScanRepository
{
    private readonly ISqlConnectionFactory _db;

    public ScanRepository(ISqlConnectionFactory db) => _db = db;

    /// <summary>
    /// ATOMIC TRANSACTION:
    /// 1. Credit available hai? Check.
    /// 2. Credit deduct karo.
    /// 3. Scan record save karo.
    /// 4. Ledger entry karo.
    /// Ya toh sab hoga, ya rollback.
    /// </summary>
    public async Task<(int ResultCode, Guid ScanId, int CreditsLeft)> SaveScanAsync(
        Guid userId, string scanType, string? handSide,
        string imagePath, string? thumbnailPath,
        string? aiRawResponse, string? summaryText)
    {
        using var conn = _db.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@UserId",        userId);
        p.Add("@ScanType",      scanType);
        p.Add("@HandSide",      handSide);
        p.Add("@ImagePath",     imagePath);
        p.Add("@ThumbnailPath", thumbnailPath);
        p.Add("@AiRawResponse", aiRawResponse);
        p.Add("@SummaryText",   summaryText);
        p.Add("@ScanId",        dbType: DbType.Guid,   direction: ParameterDirection.Output);
        p.Add("@ResultCode",    dbType: DbType.Int32,  direction: ParameterDirection.Output);
        p.Add("@CreditsLeft",   dbType: DbType.Int32,  direction: ParameterDirection.Output);

        await conn.ExecuteAsync(
            "sp_Scan_Save", p,
            commandType: CommandType.StoredProcedure);

        return (p.Get<int>("@ResultCode"), p.Get<Guid>("@ScanId"), p.Get<int>("@CreditsLeft"));
    }

    /// <summary>Offline mode: sirf image path save, credit baad mein deduct</summary>
    public async Task<Guid> SaveOfflineScanAsync(
        Guid userId, string scanType, string? handSide, string imagePath, DateTime scannedAt)
    {
        using var conn = _db.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@UserId",    userId);
        p.Add("@ScanType",  scanType);
        p.Add("@HandSide",  handSide);
        p.Add("@ImagePath", imagePath);
        p.Add("@ScannedAt", scannedAt);
        p.Add("@ScanId",    dbType: DbType.Guid, direction: ParameterDirection.Output);

        await conn.ExecuteAsync(
            "sp_Scan_SaveOffline", p,
            commandType: CommandType.StoredProcedure);

        return p.Get<Guid>("@ScanId");
    }

    /// <summary>Gemini ka result aane ke baad — SyncStatus = Synced</summary>
    public async Task UpdateAiResultAsync(Guid scanId, string aiRawResponse, string summaryText)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "sp_Scan_UpdateAiResult",
            new { ScanId = scanId, AiRawResponse = aiRawResponse, SummaryText = summaryText },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<IEnumerable<ScanHistory>> GetUserScansAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<ScanHistory>(
            @"SELECT * FROM tbl_ScanHistory
              WHERE UserId = @UserId
              ORDER BY ScannedAt DESC
              OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY",
            new { UserId = userId, Offset = (page - 1) * pageSize, PageSize = pageSize });
    }

    public async Task<IEnumerable<ScanHistory>> GetPendingScansAsync(Guid userId)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<ScanHistory>(
            @"SELECT * FROM tbl_ScanHistory
              WHERE UserId = @UserId AND SyncStatus = 'Pending'
              ORDER BY ScannedAt ASC",
            new { UserId = userId });
    }
}
