using CyhAstro.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CyhAstro.Api.Controllers;

/// <summary>
/// App Controller — In-App version check aur update management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AppController : ControllerBase
{
    private readonly IAppVersionRepository _versionRepo;
    private readonly ILocalizationService _localizer;

    public AppController(IAppVersionRepository versionRepo, ILocalizationService localizer)
    {
        _versionRepo = versionRepo;
        _localizer   = localizer;
    }

    // ─────────────────────────────────────────────
    // GET /api/app/check-update?platform=Android&currentVersion=1
    // ─────────────────────────────────────────────
    /// <summary>
    /// App update check. Mobile app startup par call karo.
    /// UpdateAvailable = true hone par user ko notify karo.
    /// IsMandatory = true hone par force update dialog dikhao.
    /// </summary>
    [HttpGet("check-update")]
    public async Task<ActionResult<Core.DTOs.AppVersionCheckDto>> CheckUpdate(
        [FromQuery] string platform = "Android",
        [FromQuery] int currentVersion = 1)
    {
        var result = await _versionRepo.CheckVersionAsync(platform, currentVersion);
        if (result == null)
            return Ok(new Core.DTOs.AppVersionCheckDto { UpdateAvailable = false });

        return Ok(result);
    }

    // ─────────────────────────────────────────────
    // GET /api/app/health
    // ─────────────────────────────────────────────
    /// <summary>Server health check (mobile app connectivity test ke liye).</summary>
    [HttpGet("health")]
    public ActionResult Health() =>
        Ok(new
        {
            status    = "healthy",
            app       = "CyhAstro API",
            version   = "1.0.0",
            language  = _localizer.CurrentLanguage,
            timestamp = DateTime.UtcNow,
            message   = _localizer.Get("CosmicOnline")
        });
}
