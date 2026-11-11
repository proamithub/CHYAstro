using CyhAstro.Core.Entities;
using CyhAstro.Core.Interfaces;
using CyhAstro.Infrastructure.Data;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CyhAstro.Admin.Pages.Admin.Scans;

public class IndexModel : PageModel
{
    private readonly ISqlConnectionFactory _db;
    private readonly IScanRepository _scanRepo;
    private readonly IWebHostEnvironment _env;

    public IndexModel(ISqlConnectionFactory db, IScanRepository scanRepo, IWebHostEnvironment env)
    {
        _db = db;
        _scanRepo = scanRepo;
        _env = env;
    }

    public IEnumerable<ScanHistory> Scans { get; set; } = Enumerable.Empty<ScanHistory>();

    public async Task OnGetAsync()
    {
        using var conn = _db.CreateConnection();
        Scans = await conn.QueryAsync<ScanHistory>(
            "SELECT TOP 100 * FROM tbl_ScanHistory ORDER BY ScannedAt DESC");
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid scanId)
    {
        var (success, imagePath, thumbPath) = await _scanRepo.DeleteScanAsync(scanId);
        if (success)
        {
            TryDeletePhysicalFile(imagePath);
            TryDeletePhysicalFile(thumbPath);
            TempData["Success"] = "Scan photo record successfully delete ho gaya!";
        }
        else
        {
            TempData["Error"] = "Scan record nahi mila ya pehle hi delete ho chuka hai.";
        }
        return RedirectToPage();
    }

    private void TryDeletePhysicalFile(string? relPath)
    {
        if (string.IsNullOrWhiteSpace(relPath)) return;
        try
        {
            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var cleanRel = relPath.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(webRoot, cleanRel);
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
        catch { }
    }
}
