using CyhAstro.Core.Entities;
using CyhAstro.Infrastructure.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CyhAstro.Admin.Pages.Admin.Scans;

public class IndexModel : PageModel
{
    private readonly ISqlConnectionFactory _db;

    public IndexModel(ISqlConnectionFactory db)
    {
        _db = db;
    }

    public IEnumerable<ScanHistory> Scans { get; set; } = Enumerable.Empty<ScanHistory>();

    public async Task OnGetAsync()
    {
        using var conn = _db.CreateConnection();
        Scans = await conn.QueryAsync<ScanHistory>(
            "SELECT TOP 100 * FROM tbl_ScanHistory ORDER BY ScannedAt DESC");
    }
}
