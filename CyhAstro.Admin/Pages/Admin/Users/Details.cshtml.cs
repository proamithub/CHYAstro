using CyhAstro.Core.DTOs;
using CyhAstro.Core.Entities;
using CyhAstro.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace CyhAstro.Admin.Pages.Admin.Users;

public class DetailsModel : PageModel
{
    private readonly IUserRepository _userRepo;
    private readonly IScanRepository _scanRepo;
    private readonly IAdminRepository _adminRepo;
    private readonly IWebHostEnvironment _env;

    public DetailsModel(IUserRepository userRepo, IScanRepository scanRepo, IAdminRepository adminRepo, IWebHostEnvironment env)
    {
        _userRepo = userRepo;
        _scanRepo = scanRepo;
        _adminRepo = adminRepo;
        _env = env;
    }

    public UserProfileDto? Profile { get; set; }
    public IEnumerable<ScanHistory> UserScans { get; set; } = Enumerable.Empty<ScanHistory>();
    public PagedResult<UserLedger> UserLedgerEntries { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Profile = await _userRepo.GetProfileAsync(id);
        if (Profile == null)
            return NotFound();

        UserScans = await _scanRepo.GetUserScansAsync(id, 1, 50);
        UserLedgerEntries = await _adminRepo.GetLedgerAsync(id, null, null, 1, 50);

        return Page();
    }

    public async Task<IActionResult> OnPostApplyActionAsync(
        Guid id, string action, string? planName, int planDays, int creditsToAdd, string? adminRemark)
    {
        var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString();
        var adminId = Guid.TryParse(adminIdStr, out var g) ? g : Guid.Empty;

        await _adminRepo.AdjustUserAsync(id, adminId, action, planName, planDays, creditsToAdd, adminRemark);
        TempData["Success"] = $"Action '{action}' successfully complete hua!";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostToggleBlockAsync(Guid id)
    {
        var profile = await _userRepo.GetProfileAsync(id);
        if (profile != null)
        {
            var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString();
            var adminId = Guid.TryParse(adminIdStr, out var g) ? g : Guid.Empty;
            string action = profile.IsBlocked ? "Unblock" : "Block";
            await _adminRepo.AdjustUserAsync(id, adminId, action, null, 0, 0, "Toggled from Admin Details view");
            TempData["Success"] = $"User successfully {action}ed!";
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteScanAsync(Guid scanId, Guid userId)
    {
        var (success, imagePath, thumbPath) = await _scanRepo.DeleteScanAsync(scanId);
        if (success)
        {
            TryDeletePhysicalFile(imagePath);
            TryDeletePhysicalFile(thumbPath);
            TempData["Success"] = "User ka scan photo record aur image file successfully delete ho gaya!";
        }
        else
        {
            TempData["Error"] = "Scan record delete karne me truti hui ya record nahi mila.";
        }
        return RedirectToPage(new { id = userId });
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
