using CyhAstro.Core.DTOs;
using CyhAstro.Core.Entities;
using CyhAstro.Core.Interfaces;
using CyhAstro.Infrastructure.Data;
using Dapper;
using System.Data;

namespace CyhAstro.Infrastructure.Repositories;

/// <summary>
/// Kundli repository — IKundliRepository implement karta hai.
/// Vedic Kundli results DB mein store aur retrieve karta hai.
/// </summary>
public class KundliRepository : IKundliRepository
{
    private readonly ISqlConnectionFactory _db;

    public KundliRepository(ISqlConnectionFactory db) => _db = db;

    public async Task SaveKundliAsync(KundliProfile profile)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "sp_KundliProfile_Save",
            new
            {
                profile.UserId,
                profile.LagnaRashi,
                profile.LagnaDegree,
                profile.MoonRashi,
                profile.SunRashi,
                profile.Nakshatra,
                profile.NakshatraPada,
                profile.MahadashaCurrent,
                MahadashaEndDate = profile.MahadashaEndDate?.ToDateTime(TimeOnly.MinValue),
                profile.IsManglik,
                profile.SadeSatiActive,
                profile.PlanetaryJson
            },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<KundliProfile?> GetKundliAsync(Guid userId)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<KundliProfile>(
            "SELECT * FROM tbl_KundliProfiles WHERE UserId = @UserId",
            new { UserId = userId });
    }
}
