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
    private readonly IPdfReportService _pdfSvc;

    public KundliController(
        IVedicAstroService astroSvc,
        IKundliRepository kundliRepo,
        ILocalizationService localizer,
        IPdfReportService pdfSvc)
    {
        _astroSvc   = astroSvc;
        _kundliRepo = kundliRepo;
        _localizer  = localizer;
        _pdfSvc     = pdfSvc;
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
    // POST /api/kundli/match (36 Guna Ashtakoot Milan)
    // ─────────────────────────────────────────────
    /// <summary>36 Guna Ashtakoot Kundli Matching for Marriage.</summary>
    [HttpPost("match")]
    public ActionResult<GunMilanResultDto> MatchKundli([FromBody] GunMilanRequest req)
    {
        var result = _astroSvc.CalculateGunMilan(
            req.BoyDob, req.BoyTob, req.BoyLat, req.BoyLng,
            req.GirlDob, req.GirlTob, req.GirlLat, req.GirlLng);

        return Ok(result);
    }

    // ─────────────────────────────────────────────
    // GET /api/kundli/pdf
    // ─────────────────────────────────────────────
    /// <summary>Download Vedic Kundli Report in PDF format.</summary>
    [HttpGet("pdf")]
    public async Task<IActionResult> DownloadPdf(
        [FromQuery] string? name, [FromQuery] string? dob, [FromQuery] string? tob,
        [FromQuery] string? city, [FromQuery] double lat = 28.6139, [FromQuery] double lng = 77.2090)
    {
        var d = DateOnly.TryParse(dob, out var parsedDob) ? parsedDob : new DateOnly(1995, 8, 15);
        var t = TimeOnly.TryParse(tob, out var parsedTob) ? parsedTob : (TimeOnly?)null;
        var cityName = string.IsNullOrWhiteSpace(city) ? "New Delhi, India" : city;
        var nativeName = string.IsNullOrWhiteSpace(name) ? "Devotee User" : name;

        var result = _astroSvc.CalculateKundli(d, t, lat, lng, "India Standard Time");
        var pdfBytes = _pdfSvc.GenerateKundliReportPdf(result, nativeName, cityName, d, t);

        return File(pdfBytes, "application/pdf", $"Kundli_{nativeName.Replace(" ", "_")}.pdf");
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
