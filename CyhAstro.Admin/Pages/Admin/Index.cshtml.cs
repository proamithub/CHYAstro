using CyhAstro.Core.DTOs;
using CyhAstro.Core.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CyhAstro.Admin.Pages.Admin;

public class IndexModel : PageModel
{
    private readonly IAdminRepository _adminRepo;

    public IndexModel(IAdminRepository adminRepo)
    {
        _adminRepo = adminRepo;
    }

    public DashboardStatsDto Stats { get; set; } = new();

    public async Task OnGetAsync()
    {
        Stats = await _adminRepo.GetDashboardStatsAsync();
    }
}
