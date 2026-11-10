using CyhAstro.Core.DTOs;
using CyhAstro.Core.Entities;
using CyhAstro.Core.Interfaces;
using CyhAstro.Infrastructure.Data;
using CyhAstro.Infrastructure.Repositories;
using CyhAstro.Infrastructure.Services;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CyhAstro.Admin.Pages.Admin;

public class AppSimulatorModel : PageModel
{
    private readonly ISqlConnectionFactory _db;
    private readonly IUserRepository _userRepo;
    private readonly IScanRepository _scanRepo;
    private readonly IVedicAstroService _astroSvc;
    private readonly IGeminiVisionService _gemini;
    private readonly ISubscriptionRepository _subRepo;
    private readonly ILocalizationService _localizer;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;

    public AppSimulatorModel(
        ISqlConnectionFactory db,
        IUserRepository userRepo,
        IScanRepository scanRepo,
        IVedicAstroService astroSvc,
        IGeminiVisionService gemini,
        ISubscriptionRepository subRepo,
        ILocalizationService localizer,
        IWebHostEnvironment env,
        IConfiguration config)
    {
        _db = db;
        _userRepo = userRepo;
        _scanRepo = scanRepo;
        _astroSvc = astroSvc;
        _gemini = gemini;
        _subRepo = subRepo;
        _localizer = localizer;
        _env = env;
        _config = config;
    }

    public UserProfileDto? TestUserProfile { get; set; }
    public KundliResultDto? KundliResult { get; set; }
    public PalmAnalysisDto? PalmResult { get; set; }
    public FaceAnalysisDto? FaceResult { get; set; }
    public string? UploadedImageUrl { get; set; }
    public string? GeneratedJwtToken { get; set; }
    public string ActiveTab { get; set; } = "palm";
    public string? AlertMessage { get; set; }
    public bool AlertSuccess { get; set; }
    public string CurrentLang => _localizer.CurrentLanguage;

    public async Task OnGetAsync(string tab = "palm")
    {
        ActiveTab = tab;
        await LoadActiveUserAsync();
    }

    private Guid GetCurrentSimUserId()
    {
        var sess = HttpContext.Session.GetString("SimUserId");
        if (Guid.TryParse(sess, out var id) && id != Guid.Empty)
            return id;
        return Guid.Empty;
    }

    private async Task<Guid> GetOrCreateDefaultTestUserIdAsync()
    {
        using var conn = _db.CreateConnection();
        var id = await conn.QueryFirstOrDefaultAsync<Guid?>(
            "SELECT UserId FROM tbl_Users WHERE Email = 'tester@cyhastro.com'");

        if (id.HasValue && id.Value != Guid.Empty)
            return id.Value;

        // Default test mobile user
        var testId = Guid.NewGuid();
        var hash = BCrypt.Net.BCrypt.HashPassword("Test@123", workFactor: 11);
        await conn.ExecuteAsync(@"
            INSERT INTO tbl_Users (UserId, FullName, Email, PhoneNumber, PasswordHash, Gender, DateOfBirth, Role)
            VALUES (@UserId, 'Amit Sharma (Tester)', 'tester@cyhastro.com', '+919876543210', @Hash, 'Male', '1995-08-15', 'User');

            INSERT INTO tbl_Subscriptions (UserId, [Plan], PalmScansRemaining, FaceScansRemaining)
            VALUES (@UserId, 'Free', 3, 1);

            INSERT INTO tbl_UserLedger (UserId, TransactionType, AmountINR, CreditChange, BalanceAfter, Description, Status)
            VALUES (@UserId, 'WelcomeCredit', 0, 4, 4, 'Welcome test account with 3 free Palm and 1 Face scans.', 'Success');
        ", new { UserId = testId, Hash = hash });

        return testId;
    }

    private async Task LoadActiveUserAsync()
    {
        var uid = GetCurrentSimUserId();
        if (uid == Guid.Empty)
        {
            uid = await GetOrCreateDefaultTestUserIdAsync();
            HttpContext.Session.SetString("SimUserId", uid.ToString());
        }

        TestUserProfile = await _userRepo.GetProfileAsync(uid);
        GeneratedJwtToken = HttpContext.Session.GetString("SimJwtToken");
    }

    // ── 1. USER LOGIN IN SIMULATOR ─────────────────
    public async Task<IActionResult> OnPostLoginAsync(string emailOrPhone, string password)
    {
        ActiveTab = "auth";

        if (string.IsNullOrWhiteSpace(emailOrPhone) || string.IsNullOrWhiteSpace(password))
        {
            AlertSuccess = false;
            AlertMessage = _localizer.Get("MissingEmailOrPhoneLogin");
            await LoadActiveUserAsync();
            return Page();
        }

        emailOrPhone = emailOrPhone.Trim();
        password = password.Trim();

        var userRepo = (UserRepository)_userRepo;
        bool isEmail = emailOrPhone.Contains('@');
        var (hash, userId, role, isBlocked) = await userRepo.GetLoginDataAsync(
            isEmail ? emailOrPhone : null,
            !isEmail ? emailOrPhone : null);

        if (hash == null || userId == null)
        {
            AlertSuccess = false;
            AlertMessage = _localizer.Get("UserNotFound");
            await LoadActiveUserAsync();
            return Page();
        }

        if (isBlocked)
        {
            AlertSuccess = false;
            AlertMessage = _localizer.Get("AccountBlocked");
            await LoadActiveUserAsync();
            return Page();
        }

        if (!BCrypt.Net.BCrypt.Verify(password, hash))
        {
            AlertSuccess = false;
            AlertMessage = _localizer.Get("InvalidPassword");
            await LoadActiveUserAsync();
            return Page();
        }

        var profile = await _userRepo.GetProfileAsync(userId.Value);
        var token = CreateJwtToken(userId.Value, profile!.FullName, role ?? "User");

        HttpContext.Session.SetString("SimUserId", userId.Value.ToString());
        HttpContext.Session.SetString("SimJwtToken", token);

        await LoadActiveUserAsync();
        AlertSuccess = true;
        AlertMessage = _localizer.Get("LoginSuccess", profile.FullName);
        return Page();
    }

    // ── 2. USER REGISTER IN SIMULATOR ──────────────
    public async Task<IActionResult> OnPostRegisterAsync(
        string fullName, string email, string phone, string password, string gender, string dob)
    {
        ActiveTab = "auth";

        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
        {
            AlertSuccess = false;
            AlertMessage = _localizer.Get("MissingEmailOrPhone");
            await LoadActiveUserAsync();
            return Page();
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            AlertSuccess = false;
            AlertMessage = _localizer.Get("PasswordMinLength");
            await LoadActiveUserAsync();
            return Page();
        }

        var birthDate = DateOnly.TryParse(dob, out var d) ? d : new DateOnly(1995, 1, 1);
        var req = new RegisterRequest(
            FullName: fullName ?? "App User",
            Email: string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            PhoneNumber: string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            Password: password,
            Gender: gender ?? "Male",
            DateOfBirth: birthDate,
            TimeOfBirth: null,
            BirthCity: "New Delhi",
            BirthLatitude: 28.6139m,
            BirthLongitude: 77.2090m,
            DeviceToken: "Simulated_Device_Token_" + Guid.NewGuid().ToString("N")[..8]
        );

        var (code, newUserId) = await _userRepo.RegisterOrLoginAsync(req);
        if (code == -1 || newUserId == null)
        {
            AlertSuccess = false;
            AlertMessage = _localizer.Get("ServerError");
            await LoadActiveUserAsync();
            return Page();
        }

        var profile = await _userRepo.GetProfileAsync(newUserId.Value);
        var token = CreateJwtToken(newUserId.Value, profile!.FullName, "User");

        HttpContext.Session.SetString("SimUserId", newUserId.Value.ToString());
        HttpContext.Session.SetString("SimJwtToken", token);

        await LoadActiveUserAsync();
        AlertSuccess = true;
        AlertMessage = _localizer.Get("RegistrationSuccess");
        return Page();
    }

    // ── 3. USER LOGOUT IN SIMULATOR ────────────────
    public async Task<IActionResult> OnPostLogoutAsync()
    {
        ActiveTab = "auth";
        HttpContext.Session.Remove("SimUserId");
        HttpContext.Session.Remove("SimJwtToken");
        await LoadActiveUserAsync();
        AlertSuccess = true;
        AlertMessage = "Aap app se log out ho gaye hain. Ab naye account se login/register karein.";
        return Page();
    }

    // ── 4. REAL PALM SCAN (IMAGE REQUIRED) ────────
    public async Task<IActionResult> OnPostScanPalmAsync(IFormFile? palmImage, string handSide = "Right")
    {
        ActiveTab = "palm";
        await LoadActiveUserAsync();

        // STRICT VALIDATION: Photo zaroori hai!
        if (palmImage == null || palmImage.Length == 0)
        {
            AlertSuccess = false;
            AlertMessage = _localizer.Get("ImageRequired") + " (Kripya pehle apne haath ki photo choose karein)";
            return Page();
        }

        if (!IsValidImage(palmImage.ContentType))
        {
            AlertSuccess = false;
            AlertMessage = _localizer.Get("InvalidImageFormat");
            return Page();
        }

        var userId = TestUserProfile?.UserId ?? await GetOrCreateDefaultTestUserIdAsync();
        bool isPremium = TestUserProfile?.IsSubscriptionActive == true && TestUserProfile.Plan != "Free";

        // Save real image to wwwroot/uploads
        var (imageRelPath, thumbRelPath) = await SaveUploadedFileAsync(palmImage, userId, "palm");
        UploadedImageUrl = imageRelPath;

        try
        {
            await using var stream = palmImage.OpenReadStream();
            PalmResult = await _gemini.AnalyzePalmAsync(stream, palmImage.ContentType, isPremium);
        }
        catch (Exception ex)
        {
            AlertSuccess = false;
            AlertMessage = "Gemini AI call failed: " + ex.Message;
            return Page();
        }

        var summaryText = PalmResult?.LifeLine?.Summary ?? "Palm scan complete";

        // Deduct 1 credit & save scan to DB
        var (resultCode, scanId, creditsLeft) = await _scanRepo.SaveScanAsync(
            userId, "Palm", handSide,
            imageRelPath, thumbRelPath,
            System.Text.Json.JsonSerializer.Serialize(PalmResult),
            summaryText);

        await LoadActiveUserAsync();

        if (resultCode == 1)
        {
            AlertSuccess = false;
            AlertMessage = _localizer.Get("PalmCreditsExhausted");
        }
        else
        {
            AlertSuccess = true;
            AlertMessage = _localizer.CurrentLanguage switch
            {
                "Hindi" => "हस्तरेखा फोटो का सफल विश्लेषण! AI ने रेखाओं और पर्वतों को स्कैन किया।",
                "English" => "Palm image successfully analyzed by Google Gemini Vision AI!",
                _ => "Palm photo successfully analyze ho gayi! Gemini Vision AI ne rekhaayein padh li hain."
            };
        }

        return Page();
    }

    // ── 5. REAL FACE SCAN (IMAGE REQUIRED) ────────
    public async Task<IActionResult> OnPostScanFaceAsync(IFormFile? faceImage)
    {
        ActiveTab = "face";
        await LoadActiveUserAsync();

        // STRICT VALIDATION: Photo zaroori hai!
        if (faceImage == null || faceImage.Length == 0)
        {
            AlertSuccess = false;
            AlertMessage = _localizer.Get("ImageRequired") + " (Kripya pehle apne chehre ki photo choose karein)";
            return Page();
        }

        if (!IsValidImage(faceImage.ContentType))
        {
            AlertSuccess = false;
            AlertMessage = _localizer.Get("InvalidImageFormat");
            return Page();
        }

        var userId = TestUserProfile?.UserId ?? await GetOrCreateDefaultTestUserIdAsync();
        bool isPremium = TestUserProfile?.IsSubscriptionActive == true && TestUserProfile.Plan != "Free";

        var (imageRelPath, thumbRelPath) = await SaveUploadedFileAsync(faceImage, userId, "face");
        UploadedImageUrl = imageRelPath;

        try
        {
            await using var stream = faceImage.OpenReadStream();
            FaceResult = await _gemini.AnalyzeFaceAsync(stream, faceImage.ContentType, isPremium);
        }
        catch (Exception ex)
        {
            AlertSuccess = false;
            AlertMessage = "Gemini AI call failed: " + ex.Message;
            return Page();
        }

        var summaryText = FaceResult?.Forehead?.Summary ?? "Face scan complete";

        var (resultCode, scanId, creditsLeft) = await _scanRepo.SaveScanAsync(
            userId, "Face", null,
            imageRelPath, thumbRelPath,
            System.Text.Json.JsonSerializer.Serialize(FaceResult),
            summaryText);

        await LoadActiveUserAsync();

        if (resultCode == 1)
        {
            AlertSuccess = false;
            AlertMessage = _localizer.Get("FaceCreditsExhausted");
        }
        else
        {
            AlertSuccess = true;
            AlertMessage = _localizer.CurrentLanguage switch
            {
                "Hindi" => "सामुद्रिक शास्त्र (चेहरा) फोटो का सफल विश्लेषण! AI ने मस्तक और नयन को स्कैन किया।",
                "English" => "Face image successfully analyzed by Samudrik Shastra AI!",
                _ => "Face photo successfully analyze ho gayi! Samudrik Shastra AI ne chehre ke zones padh liye."
            };
        }

        return Page();
    }

    // ── 6. KUNDLI CALCULATION ──────────────────────
    public async Task<IActionResult> OnPostCalculateKundliAsync(
        string dob, string tob, string city, double lat = 28.6139, double lng = 77.2090)
    {
        ActiveTab = "kundli";
        await LoadActiveUserAsync();

        if (string.IsNullOrWhiteSpace(dob)) dob = "1995-08-15";
        if (string.IsNullOrWhiteSpace(tob)) tob = "14:30";

        var d = DateOnly.Parse(dob);
        var t = TimeOnly.Parse(tob);

        KundliResult = _astroSvc.CalculateKundli(d, t, lat, lng, "India Standard Time");
        AlertSuccess = true;
        AlertMessage = _localizer.CurrentLanguage switch
        {
            "Hindi" => $"कुंडली गणना पूर्ण! लग्न: {KundliResult.LagnaRashi}, राशि: {KundliResult.MoonRashi}, नक्षत्र: {KundliResult.Nakshatra}",
            "English" => $"Kundli calculated! Ascendant: {KundliResult.LagnaRashi}, Moon: {KundliResult.MoonRashi}, Nakshatra: {KundliResult.Nakshatra}",
            _ => $"Kundli calculation complete! Lagna: {KundliResult.LagnaRashi}, Rashi: {KundliResult.MoonRashi}, Nakshatra: {KundliResult.Nakshatra}"
        };

        return Page();
    }

    // ── 7. SUBSCRIPTION UPGRADE ────────────────────
    public async Task<IActionResult> OnPostUpgradeAsync(string plan)
    {
        ActiveTab = "wallet";
        await LoadActiveUserAsync();

        var userId = TestUserProfile?.UserId ?? await GetOrCreateDefaultTestUserIdAsync();
        decimal amount = plan switch { "Weekly" => 99, "Monthly" => 299, "Yearly" => 1999, _ => 4999 };

        await _subRepo.UpgradePlanAsync(new SubscriptionUpgradeRequest(
            userId,
            plan,
            amount,
            "RazorpaySimulator",
            "TXN_APP_" + DateTime.UtcNow.Ticks
        ));

        await LoadActiveUserAsync();
        AlertSuccess = true;
        AlertMessage = _localizer.Get("PlanActivated", plan);
        return Page();
    }

    // ── 8. RESET CREDITS ───────────────────────────
    public async Task<IActionResult> OnPostResetCreditsAsync()
    {
        ActiveTab = "wallet";
        await LoadActiveUserAsync();
        var userId = TestUserProfile?.UserId ?? await GetOrCreateDefaultTestUserIdAsync();

        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE tbl_Subscriptions
            SET [Plan] = 'Free', PalmScansRemaining = 3, FaceScansRemaining = 1, ExpiryDate = NULL
            WHERE UserId = @UserId;
        ", new { UserId = userId });

        await LoadActiveUserAsync();
        AlertSuccess = true;
        AlertMessage = "User plan reset to Free with 3 Palm and 1 Face scan credits.";
        return Page();
    }

    private async Task<(string relativePath, string thumbPath)> SaveUploadedFileAsync(IFormFile file, Guid userId, string type)
    {
        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var folder = Path.Combine(webRoot, "uploads", userId.ToString(), type);
        Directory.CreateDirectory(folder);

        var fileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        var fullPath = Path.Combine(folder, fileName);

        await using (var fs = new FileStream(fullPath, FileMode.Create))
            await file.CopyToAsync(fs);

        var relativePath = $"/uploads/{userId}/{type}/{fileName}";
        return (relativePath, relativePath);
    }

    private static bool IsValidImage(string? contentType) =>
        contentType is "image/jpeg" or "image/png" or "image/webp" or "image/jpg";

    private string CreateJwtToken(Guid userId, string fullName, string role)
    {
        var key = _config["JwtSettings:SecretKey"] ?? "PassCyh123456789987654321987654321";
        var tokenHandler = new JwtSecurityTokenHandler();
        var keyBytes = Encoding.UTF8.GetBytes(key);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, fullName),
                new Claim(ClaimTypes.Role, role)
            }),
            Expires = DateTime.UtcNow.AddDays(30),
            Issuer = _config["JwtSettings:Issuer"] ?? "CyhAstro",
            Audience = _config["JwtSettings:Audience"] ?? "CyhAstroUsers",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
