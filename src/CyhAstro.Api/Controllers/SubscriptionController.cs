using CyhAstro.Core.DTOs;
using CyhAstro.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CyhAstro.Api.Controllers;

/// <summary>
/// Subscription Controller — Plan status, upgrade aur paywall info
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class SubscriptionController : ControllerBase
{
    private readonly ISubscriptionRepository _subRepo;

    public SubscriptionController(ISubscriptionRepository subRepo)
        => _subRepo = subRepo;

    // ─────────────────────────────────────────────
    // GET /api/subscription/status
    // ─────────────────────────────────────────────
    /// <summary>Current plan + credits + expiry info.</summary>
    [HttpGet("status")]
    public async Task<ActionResult<Core.Entities.Subscription>> GetStatus()
    {
        var userId = GetCurrentUserId();
        var sub    = await _subRepo.GetSubscriptionAsync(userId);
        if (sub == null) return NotFound();
        return Ok(sub);
    }

    // ─────────────────────────────────────────────
    // GET /api/subscription/plans
    // ─────────────────────────────────────────────
    /// <summary>Available plans aur prices (DB se — admin se configurable).</summary>
    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> GetPlans()
    {
        var settings = await _subRepo.GetAppSettingsAsync();
        return Ok(new
        {
            plans = new[]
            {
                new
                {
                    id = "Free", name = "Free",
                    priceINR = 0,
                    palmScans = int.Parse(settings.GetValueOrDefault("FreeUserPalmScans", "1")),
                    faceScans = 0,
                    features = new[] { "1 Free Palm Scan", "Basic Kundli", "Daily Rashifal (2 lines)" },
                    isPopular = false
                },
                new
                {
                    id = "Weekly", name = "Saptahik (Weekly)",
                    priceINR = int.Parse(settings.GetValueOrDefault("PriceWeeklyINR", "99")),
                    palmScans = int.Parse(settings.GetValueOrDefault("PaidUserPalmScans", "999")),
                    faceScans = int.Parse(settings.GetValueOrDefault("PaidUserFaceScans", "999")),
                    features = new[] { "Unlimited Palm Scans", "Face Reading (Samudrik)", "Full Kundli + PDF", "AI Chat (Unlimited)" },
                    isPopular = false
                },
                new
                {
                    id = "Monthly", name = "Masik (Monthly)",
                    priceINR = int.Parse(settings.GetValueOrDefault("PriceMonthlyINR", "299")),
                    palmScans = int.Parse(settings.GetValueOrDefault("PaidUserPalmScans", "999")),
                    faceScans = int.Parse(settings.GetValueOrDefault("PaidUserFaceScans", "999")),
                    features = new[] { "Unlimited Palm Scans", "Face Reading", "Full Kundli + PDF Report", "AI Astrologer Chat", "Priority Support" },
                    isPopular = true
                },
                new
                {
                    id = "Yearly", name = "Varshik (Yearly)",
                    priceINR = int.Parse(settings.GetValueOrDefault("PriceYearlyINR", "1999")),
                    palmScans = int.Parse(settings.GetValueOrDefault("PaidUserPalmScans", "999")),
                    faceScans = int.Parse(settings.GetValueOrDefault("PaidUserFaceScans", "999")),
                    features = new[] { "Unlimited Everything", "Full Kundli + Premium PDF", "AI Chat Unlimited", "Priority Support", "Exclusive Features" },
                    isPopular = false
                }
            }
        });
    }

    // ─────────────────────────────────────────────
    // POST /api/subscription/upgrade
    // ─────────────────────────────────────────────
    /// <summary>
    /// Plan upgrade karo (payment gateway ne payment confirm karne ke baad call karo).
    /// ATOMIC: Plan update + Ledger entry ek hi DB transaction mein.
    /// </summary>
    [HttpPost("upgrade")]
    public async Task<ActionResult<object>> UpgradePlan([FromBody] SubscriptionUpgradeRequest req)
    {
        var userId = GetCurrentUserId();
        if (userId != req.UserId) return Forbid();

        var (code, expiry) = await _subRepo.UpgradePlanAsync(req);

        return code switch
        {
            0  => Ok(new
                 {
                     success  = true,
                     message  = $"{req.NewPlan} plan successfully activate ho gaya!",
                     expiryDate = expiry,
                     plan     = req.NewPlan
                 }),
            -1 => StatusCode(500, new { success = false, message = "Plan upgrade mein error aaya. Payment team se contact karein." }),
            _  => BadRequest(new { success = false, message = "Invalid request." })
        };
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
