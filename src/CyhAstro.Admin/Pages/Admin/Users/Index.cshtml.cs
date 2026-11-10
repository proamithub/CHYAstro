using CyhAstro.Core.DTOs;
using CyhAstro.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CyhAstro.Admin.Pages.Admin.Users;

public class IndexModel : PageModel
{
    private readonly IAdminRepository _adminRepo;

    public IndexModel(IAdminRepository adminRepo) => _adminRepo = adminRepo;

    public PagedResult<AdminUserListItemDto> Users { get; set; } = new();
    public string? Search { get; set; }
    public string? PlanFilter { get; set; }
    public string? StatusFilter { get; set; }

    public async Task OnGetAsync(string? search, string? plan, string? status, int page = 1)
    {
        Search = search;
        PlanFilter = plan;
        StatusFilter = status;
        Users = await _adminRepo.GetUsersListAsync(search, plan, status, page, 25);
    }

    public async Task<IActionResult> OnPostAdjustAsync(
        Guid targetUserId, string action, string? planName,
        int planDays, int creditsToAdd, string? adminRemark)
    {
        // Get admin's userId from session/claims
        var adminId = Guid.Parse(HttpContext.Session.GetString("AdminId") ?? Guid.Empty.ToString());
        await _adminRepo.AdjustUserAsync(targetUserId, adminId, action, planName, planDays, creditsToAdd, adminRemark);
        TempData["Success"] = $"Action '{action}' applied successfully!";
        return RedirectToPage();
    }
}
