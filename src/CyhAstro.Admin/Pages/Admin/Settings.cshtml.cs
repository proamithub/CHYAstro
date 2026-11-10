using CyhAstro.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace CyhAstro.Admin.Pages.Admin;

public class SettingsModel : PageModel
{
    private readonly IAdminRepository _adminRepo;
    public SettingsModel(IAdminRepository adminRepo) => _adminRepo = adminRepo;

    public Dictionary<string, string> Settings { get; set; } = new();

    public async Task OnGetAsync()
        => Settings = await _adminRepo.GetAllSettingsAsync();

    public async Task<IActionResult> OnPostSavePricesAsync(
        string PriceWeeklyINR, string PriceMonthlyINR, string PriceYearlyINR)
    {
        var admin = User.FindFirstValue(ClaimTypes.Name) ?? "Admin";
        await _adminRepo.UpdateSettingAsync("PriceWeeklyINR",  PriceWeeklyINR,  admin);
        await _adminRepo.UpdateSettingAsync("PriceMonthlyINR", PriceMonthlyINR, admin);
        await _adminRepo.UpdateSettingAsync("PriceYearlyINR",  PriceYearlyINR,  admin);
        TempData["Success"] = "Plan prices update ho gayi!";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSaveQuotasAsync(
        string FreeUserPalmScans, string FreeUserFaceScans, string FreeUserChatLimit,
        string PaidUserPalmScans, string PaidUserFaceScans)
    {
        var admin = User.FindFirstValue(ClaimTypes.Name) ?? "Admin";
        await _adminRepo.UpdateSettingAsync("FreeUserPalmScans", FreeUserPalmScans, admin);
        await _adminRepo.UpdateSettingAsync("FreeUserFaceScans", FreeUserFaceScans, admin);
        await _adminRepo.UpdateSettingAsync("FreeUserChatLimit", FreeUserChatLimit, admin);
        await _adminRepo.UpdateSettingAsync("PaidUserPalmScans", PaidUserPalmScans, admin);
        await _adminRepo.UpdateSettingAsync("PaidUserFaceScans", PaidUserFaceScans, admin);
        TempData["Success"] = "Quotas update ho gayi!";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSavePromptsAsync(string GeminiPalmPrompt, string GeminiFacePrompt)
    {
        var admin = User.FindFirstValue(ClaimTypes.Name) ?? "Admin";
        await _adminRepo.UpdateSettingAsync("GeminiPalmPrompt", GeminiPalmPrompt, admin);
        await _adminRepo.UpdateSettingAsync("GeminiFacePrompt", GeminiFacePrompt, admin);
        TempData["Success"] = "AI Prompts update ho gaye!";
        return RedirectToPage();
    }
}
