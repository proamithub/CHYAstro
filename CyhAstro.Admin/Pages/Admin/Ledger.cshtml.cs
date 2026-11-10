using CyhAstro.Core.DTOs;
using CyhAstro.Core.Entities;
using CyhAstro.Core.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CyhAstro.Admin.Pages.Admin;

public class LedgerModel : PageModel
{
    private readonly IAdminRepository _adminRepo;
    public LedgerModel(IAdminRepository adminRepo) => _adminRepo = adminRepo;

    public PagedResult<UserLedger> Ledger { get; set; } = new();
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }

    public async Task OnGetAsync(DateOnly? from, DateOnly? to, int page = 1)
    {
        From = from;
        To = to ?? DateOnly.FromDateTime(DateTime.Today);
        Ledger = await _adminRepo.GetLedgerAsync(null, From, To, page, 50);
    }
}
