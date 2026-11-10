using CyhAstro.Core.DTOs;
using CyhAstro.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CyhAstro.Admin.Pages.Admin;

public class AppVersionsModel : PageModel
{
    private readonly IAppVersionRepository _versionRepo;
    public AppVersionsModel(IAppVersionRepository versionRepo) => _versionRepo = versionRepo;

    public IEnumerable<AppVersionCheckDto> AndroidVersions { get; set; } = new List<AppVersionCheckDto>();

    public async Task OnGetAsync()
        => AndroidVersions = await _versionRepo.GetAllVersionsAsync("Android");

    public async Task<IActionResult> OnPostAsync(
        string platform, int versionCode, string versionName,
        string? downloadUrl, string? releaseNotes, bool isMandatory)
    {
        await _versionRepo.AddVersionAsync(platform, versionCode, versionName, downloadUrl, releaseNotes, isMandatory);
        TempData["Success"] = $"Version {versionName} ({platform}) successfully push ho gaya!";
        return RedirectToPage();
    }
}
