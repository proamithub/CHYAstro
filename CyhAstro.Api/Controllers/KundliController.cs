using CyhAstro.Core.DTOs;
using CyhAstro.Core.Interfaces;
using CyhAstro.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace CyhAstro.Api.Controllers;

/// <summary>
/// Kundli Controller — Vedic Birth Chart calculation (100% offline, no paid API)
/// SwissEph math C# mein directly run hota hai
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class KundliController : ControllerBase
{
    private readonly IVedicAstroService _astroSvc;
    private readonly IKundliRepository  _kundliRepo;
    private readonly ILocalizationService _localizer;

    public KundliController(IVedicAstroService astroSvc, IKundliRepository kundliRepo, ILocalizationService localizer)
    {
        _astroSvc   = astroSvc;
        _kundliRepo = kundliRepo;
        _localizer  = localizer;
    }

    // ─────────────────────────────────────────────
    // POST /api/kundli/calculate
    // ─────────────────────────────────────────────
    /// <summary>
    /// DOB, TOB, City Lat/Long se poori Vedic Kundli calculate karo.
    /// Result DB mein save hoga. Offline bhi kaam karta hai (pure math).
    /// </summary>
    [HttpPost("calculate")]
    public async Task<ActionResult<KundliResultDto>> Calculate([FromBody] KundliRequest req)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        // Vedic calculation (C# math — no internet required)
        var result = _astroSvc.CalculateKundli(
            req.DateOfBirth,
            req.TimeOfBirth,
            req.Latitude,
            req.Longitude,
            req.TimeZoneId);

        // DB mein save karo (upsert via SP)
        var profile = new Core.Entities.KundliProfile
        {
            UserId           = userId,
            LagnaRashi       = result.LagnaRashi,
            LagnaDegree      = (decimal)result.LagnaDegree,
            MoonRashi        = result.MoonRashi,
            SunRashi         = result.SunRashi,
            Nakshatra        = result.Nakshatra,
            NakshatraPada    = result.NakshatraPada,
            MahadashaCurrent = result.MahadashaCurrent,
            MahadashaEndDate = result.MahadashaEndDate,
            IsManglik        = result.IsManglik,
            SadeSatiActive   = result.SadeSatiActive,
            PlanetaryJson    = JsonSerializer.Serialize(result.Planets),
            AyanamsaUsed     = result.AyanamsaUsed
        };

        await _kundliRepo.SaveKundliAsync(profile);

        return Ok(result);
    }

    // ─────────────────────────────────────────────
    // GET /api/kundli/my
    // ─────────────────────────────────────────────
    /// <summary>Meri saved Kundli lo (pehle se calculate ki hui).</summary>
    [HttpGet("my")]
    public async Task<ActionResult<Core.Entities.KundliProfile>> GetMyKundli()
    {
        var userId = GetCurrentUserId();
        var kundli = await _kundliRepo.GetKundliAsync(userId);
        if (kundli == null)
            return NotFound(new { message = _localizer.Get("KundliNotCalculated") });
        return Ok(kundli);
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
