using CyhAstro.Core.DTOs;
using CyhAstro.Core.Interfaces;
using CyhAstro.Infrastructure.Data;
using CyhAstro.Infrastructure.Repositories;

namespace CyhAstro.Infrastructure.Repositories;

/// <summary>AppVersionRepository — IAppVersionRepository implement karta hai.</summary>
public class AppVersionRepository : IAppVersionRepository
{
    private readonly ISqlConnectionFactory _db;
    public AppVersionRepository(ISqlConnectionFactory db) => _db = db;

    public async Task<AppVersionCheckDto?> CheckVersionAsync(string platform, int currentCode)
    {
        using var conn = _db.CreateConnection();
        return await Dapper.SqlMapper.QuerySingleOrDefaultAsync<AppVersionCheckDto>(conn,
            "sp_AppVersion_Check",
            new { Platform = platform, CurrentCode = currentCode },
            commandType: System.Data.CommandType.StoredProcedure);
    }

    public async Task<bool> AddVersionAsync(string platform, int versionCode, string versionName,
        string? downloadUrl, string? releaseNotes, bool isMandatory)
    {
        using var conn = _db.CreateConnection();
        var rows = await Dapper.SqlMapper.ExecuteAsync(conn,
            @"INSERT INTO tbl_AppVersions (Platform, VersionCode, VersionName, DownloadUrl, ReleaseNotes, IsMandatory)
              VALUES (@Platform, @VersionCode, @VersionName, @DownloadUrl, @ReleaseNotes, @IsMandatory)",
            new { Platform = platform, VersionCode = versionCode, VersionName = versionName,
                  DownloadUrl = downloadUrl, ReleaseNotes = releaseNotes, IsMandatory = isMandatory });
        return rows > 0;
    }

    public async Task<IEnumerable<AppVersionCheckDto>> GetAllVersionsAsync(string platform)
    {
        using var conn = _db.CreateConnection();
        return await Dapper.SqlMapper.QueryAsync<AppVersionCheckDto>(conn,
            "SELECT VersionCode, VersionName, DownloadUrl, PlayStoreUrl, ReleaseNotes, IsMandatory, 0 AS UpdateAvailable FROM tbl_AppVersions WHERE Platform = @Platform AND IsActive = 1 ORDER BY VersionCode DESC",
            new { Platform = platform });
    }
}
