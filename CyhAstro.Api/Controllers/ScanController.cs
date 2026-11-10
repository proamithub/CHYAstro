using CyhAstro.Core.DTOs;
using CyhAstro.Core.Interfaces;
using CyhAstro.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CyhAstro.Api.Controllers;

/// <summary>
/// Scan Controller — Palm (Hastrekha) aur Face (Samudrik Shastra) scan
/// Image upload → OpenCV preprocessing → Gemini Flash Vision → DB save (ATOMIC)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ScanController : ControllerBase
{
    private readonly IScanRepository      _scanRepo;
    private readonly IUserRepository      _userRepo;
    private readonly IGeminiVisionService _gemini;
    private readonly IWebHostEnvironment  _env;
    private readonly IConfiguration       _config;
    private readonly ILogger<ScanController> _logger;
    private readonly ILocalizationService _localizer;

    public ScanController(
        IScanRepository scanRepo,
        IUserRepository userRepo,
        IGeminiVisionService gemini,
        IWebHostEnvironment env,
        IConfiguration config,
        ILogger<ScanController> logger,
        ILocalizationService localizer)
    {
        _scanRepo  = scanRepo;
        _userRepo  = userRepo;
        _gemini    = gemini;
        _env       = env;
        _config    = config;
        _logger    = logger;
        _localizer = localizer;
    }

    // ─────────────────────────────────────────────
    // POST /api/scan/palm
    // ─────────────────────────────────────────────
    /// <summary>
    /// Palm (Hast Rekha) scan karo.
    /// Free user: Life, Head, Heart lines milenge. Fate + Mounts locked rahenge.
    /// Premium user: Poori 7-line + 3-mount analysis.
    /// </summary>
    [HttpPost("palm")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10_485_760)] // 10 MB max
    public async Task<ActionResult<ScanResultDto>> ScanPalm([FromForm] PalmScanRequest request)
    {
        var image = request.Image;
        var handSide = string.IsNullOrWhiteSpace(request.HandSide) ? "Right" : request.HandSide;
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        // Image validation
        if (image == null || image.Length == 0)
            return BadRequest(new ScanResultDto { Success = false, ErrorMessage = _localizer.Get("ImageRequired") });

        if (!IsValidImage(image.ContentType))
            return BadRequest(new ScanResultDto { Success = false, ErrorMessage = _localizer.Get("InvalidImageFormat") });

        // User profile + plan check
        var profile = await _userRepo.GetProfileAsync(userId);
        if (profile == null) return Unauthorized();

        bool isPremium = profile.IsSubscriptionActive && profile.Plan != "Free";

        // Image save karo disk par
        var (imagePath, thumbPath) = await SaveImageAsync(image, userId, "palm");

        // Gemini Vision se palm analysis lo
        PalmAnalysisDto? palmResult = null;
        try
        {
            await using var stream = image.OpenReadStream();
            palmResult = await _gemini.AnalyzePalmAsync(stream, image.ContentType, isPremium);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini palm analysis failed for user {UserId}", userId);
            // Gemini fail hone par bhi scan save hoga — result baad mein sync hoga
        }

        var summaryText = palmResult?.LifeLine?.Summary ?? "Analysis processing...";

        // ATOMIC: Credit deduct + Scan save (SP ke andar transaction)
        var (resultCode, scanId, creditsLeft) = await _scanRepo.SaveScanAsync(
            userId, "Palm", handSide,
            imagePath, thumbPath,
            System.Text.Json.JsonSerializer.Serialize(palmResult),
            summaryText);

        return resultCode switch
        {
            1  => Ok(new ScanResultDto
                 {
                     Success      = false,
                     ScanType     = "Palm",
                     ErrorMessage = _localizer.Get("PalmCreditsExhausted"),
                     CreditsLeft  = 0
                 }),
            -1 => StatusCode(500, new ScanResultDto { Success = false, ErrorMessage = _localizer.Get("ServerError") }),
            _  => Ok(new ScanResultDto
                 {
                     ScanId       = scanId,
                     ScanType     = "Palm",
                     Success      = true,
                     CreditsLeft  = creditsLeft,
                     PalmAnalysis = palmResult
                 })
        };
    }

    // ─────────────────────────────────────────────
    // POST /api/scan/face
    // ─────────────────────────────────────────────
    /// <summary>
    /// Face (Samudrik Shastra) scan karo.
    /// Premium only feature — Free users ko paywall response milega.
    /// </summary>
    [HttpPost("face")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10_485_760)]
    public async Task<ActionResult<ScanResultDto>> ScanFace([FromForm] FaceScanRequest request)
    {
        var image = request.Image;
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        if (image == null || image.Length == 0)
            return BadRequest(new ScanResultDto { Success = false, ErrorMessage = _localizer.Get("ImageRequired") });

        if (!IsValidImage(image.ContentType))
            return BadRequest(new ScanResultDto { Success = false, ErrorMessage = _localizer.Get("InvalidImageFormat") });

        var profile = await _userRepo.GetProfileAsync(userId);
        if (profile == null) return Unauthorized();

        bool isPremium = profile.IsSubscriptionActive && profile.Plan != "Free";

        // Image save
        var (imagePath, thumbPath) = await SaveImageAsync(image, userId, "face");

        // Face analysis
        FaceAnalysisDto? faceResult = null;
        try
        {
            await using var stream = image.OpenReadStream();
            faceResult = await _gemini.AnalyzeFaceAsync(stream, image.ContentType, isPremium);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini face analysis failed for user {UserId}", userId);
        }

        // ATOMIC save
        var (resultCode, scanId, creditsLeft) = await _scanRepo.SaveScanAsync(
            userId, "Face", null,
            imagePath, thumbPath,
            System.Text.Json.JsonSerializer.Serialize(faceResult),
            faceResult?.Forehead?.Summary ?? "Analysis processing...");

        return resultCode switch
        {
            1  => Ok(new ScanResultDto
                 {
                     Success      = false,
                     ScanType     = "Face",
                     ErrorMessage = _localizer.Get("FaceCreditsExhausted"),
                     CreditsLeft  = 0
                 }),
            -1 => StatusCode(500, new ScanResultDto { Success = false, ErrorMessage = _localizer.Get("ServerError") }),
            _  => Ok(new ScanResultDto
                 {
                     ScanId       = scanId,
                     ScanType     = "Face",
                     Success      = true,
                     CreditsLeft  = creditsLeft,
                     FaceAnalysis = faceResult
                 })
        };
    }

    // ─────────────────────────────────────────────
    // POST /api/scan/sync
    // ─────────────────────────────────────────────
    /// <summary>
    /// Mobile app ka offline queue sync karo.
    /// Net aane par pending scans server pe upload hote hain.
    /// </summary>
    [HttpPost("sync")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(52_428_800)] // 50 MB (multiple images)
    public async Task<ActionResult> SyncOfflineScans([FromForm] SyncOfflineScansRequest request)
    {
        var images = request.Images;
        var scanMetaJson = request.ScanMetaJson;
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        // scanMetaJson = [{scanType, handSide, scannedAt}, ...] mobile se aata hai
        var metas = System.Text.Json.JsonSerializer.Deserialize<List<OfflineScanMeta>>(scanMetaJson)
                    ?? new List<OfflineScanMeta>();

        var results = new List<object>();
        for (int i = 0; i < images.Count && i < metas.Count; i++)
        {
            var img  = images[i];
            var meta = metas[i];

            if (!IsValidImage(img.ContentType)) continue;

            var (imagePath, thumbPath) = await SaveImageAsync(img, userId, meta.ScanType.ToLower());
            var scanId = await _scanRepo.SaveOfflineScanAsync(userId, meta.ScanType, meta.HandSide, imagePath, meta.ScannedAt);

            // AI analysis background mein chalaao (fire and forget ya Hangfire baad mein)
            _ = Task.Run(async () =>
            {
                try
                {
                    var profile  = await _userRepo.GetProfileAsync(userId);
                    bool premium = profile?.IsSubscriptionActive == true && profile.Plan != "Free";

                    await using var stream = img.OpenReadStream();
                    if (meta.ScanType == "Palm")
                    {
                        var result = await _gemini.AnalyzePalmAsync(stream, img.ContentType, premium);
                        await _scanRepo.UpdateAiResultAsync(scanId,
                            System.Text.Json.JsonSerializer.Serialize(result),
                            result?.LifeLine?.Summary ?? "Done");
                    }
                    else
                    {
                        var result = await _gemini.AnalyzeFaceAsync(stream, img.ContentType, premium);
                        await _scanRepo.UpdateAiResultAsync(scanId,
                            System.Text.Json.JsonSerializer.Serialize(result),
                            result?.Forehead?.Summary ?? "Done");
                    }
                }
                catch { /* Log and ignore — scan already saved */ }
            });

            results.Add(new { scanId, status = "queued" });
        }

        return Ok(new { synced = results.Count, results });
    }

    // ─────────────────────────────────────────────
    // GET /api/scan/history
    // ─────────────────────────────────────────────
    /// <summary>Meri scan history (paginated).</summary>
    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<Core.Entities.ScanHistory>>> GetHistory(int page = 1)
    {
        var userId = GetCurrentUserId();
        var scans  = await _scanRepo.GetUserScansAsync(userId, page, 20);
        return Ok(scans);
    }

    // ─────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────
    private async Task<(string imagePath, string thumbPath)> SaveImageAsync(IFormFile file, Guid userId, string type)
    {
        var uploadsBase = _config["FileStorage:UploadsPath"] ?? "wwwroot/uploads";
        var userFolder  = Path.Combine(_env.ContentRootPath, uploadsBase, userId.ToString(), type);
        Directory.CreateDirectory(userFolder);

        var fileName  = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        var fullPath  = Path.Combine(userFolder, fileName);
        var thumbPath = Path.Combine(userFolder, "thumb_" + fileName);

        await using (var fs = new FileStream(fullPath, FileMode.Create))
            await file.CopyToAsync(fs);

        // Simple thumbnail copy (OpenCvSharp add karne ke baad resize hoga)
        System.IO.File.Copy(fullPath, thumbPath, overwrite: true);

        // Web-relative path store karo (DB mein)
        var relativePath = $"/uploads/{userId}/{type}/{fileName}";
        var thumbRelPath = $"/uploads/{userId}/{type}/thumb_{fileName}";

        return (relativePath, thumbRelPath);
    }

    private static bool IsValidImage(string? contentType) =>
        contentType is "image/jpeg" or "image/png" or "image/webp" or "image/jpg";

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}

/// <summary>Offline scan metadata (mobile se aata hai JSON mein)</summary>
public record OfflineScanMeta(string ScanType, string? HandSide, DateTime ScannedAt);

/// <summary>Palm scan multipart request</summary>
public class PalmScanRequest
{
    public IFormFile Image { get; set; } = null!;
    public string HandSide { get; set; } = "Right";
}

/// <summary>Face scan multipart request</summary>
public class FaceScanRequest
{
    public IFormFile Image { get; set; } = null!;
}

/// <summary>Sync offline scans multipart request</summary>
public class SyncOfflineScansRequest
{
    public List<IFormFile> Images { get; set; } = new();
    public string ScanMetaJson { get; set; } = string.Empty;
}
