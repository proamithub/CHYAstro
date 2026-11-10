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
    private readonly IPdfReportService _pdfSvc;
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
        IPdfReportService pdfSvc,
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
        _pdfSvc = pdfSvc;
        _env = env;
        _config = config;
    }

    public UserProfileDto? TestUserProfile { get; set; }
    public KundliResultDto? KundliResult { get; set; }
    public GunMilanResultDto? MilanResult { get; set; }
    public string KundliViewMode { get; set; } = "d1";
    public PalmAnalysisDto? PalmResult { get; set; }
    public FaceAnalysisDto? FaceResult { get; set; }
    public string? UploadedImageUrl { get; set; }
    public string? GeneratedJwtToken { get; set; }
    public string ActiveTab { get; set; } = "login";
    public string AuthMode { get; set; } = "login"; // "login" or "register"
    public string? AlertMessage { get; set; }
    public bool AlertSuccess { get; set; }
    public string CurrentLang => _localizer.CurrentLanguage;
    public bool IsLoggedIn => TestUserProfile != null && HttpContext.Session.GetString("SimUserId") != null;

    public async Task OnGetAsync(string? tab = null, string? authMode = null)
    {
        AuthMode = authMode ?? "login";
        var uid = GetCurrentSimUserId();

        if (uid == Guid.Empty)
        {
            // Not logged in -> Show cosmic Shiva login screen by default
            ActiveTab = tab ?? "login";
            TestUserProfile = null;
        }
        else
        {
            // Logged in user
            ActiveTab = tab ?? (ActiveTab == "login" ? "palm" : ActiveTab);
            TestUserProfile = await _userRepo.GetProfileAsync(uid);
            GeneratedJwtToken = HttpContext.Session.GetString("SimJwtToken");
        }
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
        if (uid != Guid.Empty)
        {
            TestUserProfile = await _userRepo.GetProfileAsync(uid);
            GeneratedJwtToken = HttpContext.Session.GetString("SimJwtToken");
        }
    }

    // ── 1. USER LOGIN FROM SHIVA SCREEN ────────────
    public async Task<IActionResult> OnPostLoginAsync(string emailOrPhone, string password)
    {
        ActiveTab = "login";
        AuthMode = "login";

        if (string.IsNullOrWhiteSpace(emailOrPhone) || string.IsNullOrWhiteSpace(password))
        {
            AlertSuccess = false;
            AlertMessage = _localizer.Get("MissingEmailOrPhoneLogin");
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
            return Page();
        }

        if (isBlocked)
        {
            AlertSuccess = false;
            AlertMessage = _localizer.Get("AccountBlocked");
            return Page();
        }

        if (!BCrypt.Net.BCrypt.Verify(password, hash))
        {
            AlertSuccess = false;
            AlertMessage = _localizer.Get("InvalidPassword");
            return Page();
        }

        var profile = await _userRepo.GetProfileAsync(userId.Value);
        var token = CreateJwtToken(userId.Value, profile!.FullName, role ?? "User");

        HttpContext.Session.SetString("SimUserId", userId.Value.ToString());
        HttpContext.Session.SetString("SimJwtToken", token);

        TestUserProfile = profile;
        GeneratedJwtToken = token;
        ActiveTab = "palm"; // Enter App directly into Palm Scanner!
        AlertSuccess = true;
        AlertMessage = _localizer.Get("LoginSuccess", profile.FullName);
        return Page();
    }

    // ── 2. QUICK 1-CLICK DEMO LOGIN ────────────────
    public async Task<IActionResult> OnPostQuickDemoLoginAsync()
    {
        var testId = await GetOrCreateDefaultTestUserIdAsync();
        var profile = await _userRepo.GetProfileAsync(testId);
        var token = CreateJwtToken(testId, profile!.FullName, "User");

        HttpContext.Session.SetString("SimUserId", testId.ToString());
        HttpContext.Session.SetString("SimJwtToken", token);

        TestUserProfile = profile;
        GeneratedJwtToken = token;
        ActiveTab = "palm";
        AlertSuccess = true;
        AlertMessage = "Demo login successful! Swagat hai " + profile.FullName;
        return Page();
    }

    // ── 3. USER REGISTER FROM SHIVA SCREEN ─────────
    public async Task<IActionResult> OnPostRegisterAsync(
        string fullName, string email, string phone, string password, string gender, string dob)
    {
        ActiveTab = "login";
        AuthMode = "register";

        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
        {
            AlertSuccess = false;
            AlertMessage = _localizer.Get("MissingEmailOrPhone");
            return Page();
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            AlertSuccess = false;
            AlertMessage = _localizer.Get("PasswordMinLength");
            return Page();
        }

        var birthDate = DateOnly.TryParse(dob, out var d) ? d : new DateOnly(1995, 1, 1);
        var req = new RegisterRequest(
            FullName: string.IsNullOrWhiteSpace(fullName) ? "Devotee User" : fullName.Trim(),
            Email: string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            PhoneNumber: string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            Password: password.Trim(),
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
            return Page();
        }

        var profile = await _userRepo.GetProfileAsync(newUserId.Value);
        var token = CreateJwtToken(newUserId.Value, profile!.FullName, "User");

        HttpContext.Session.SetString("SimUserId", newUserId.Value.ToString());
        HttpContext.Session.SetString("SimJwtToken", token);

        TestUserProfile = profile;
        GeneratedJwtToken = token;
        ActiveTab = "palm";
        AlertSuccess = true;
        AlertMessage = _localizer.Get("RegistrationSuccess");
        return Page();
    }

    // ── 4. LOGOUT (RETURNS TO SHIVA SCREEN) ────────
    public IActionResult OnPostLogout()
    {
        HttpContext.Session.Remove("SimUserId");
        HttpContext.Session.Remove("SimJwtToken");
        TestUserProfile = null;
        GeneratedJwtToken = null;
        ActiveTab = "login";
        AuthMode = "login";
        AlertSuccess = true;
        AlertMessage = "Aap safaltapoorvak log out ho gaye hain. Mahadev ki kripa aap par bani rahe.";
        return Page();
    }

    // ── 5. REAL PALM SCAN (PHOTO REQUIRED) ─────────
    public async Task<IActionResult> OnPostScanPalmAsync(IFormFile? palmImage, string handSide = "Right")
    {
        ActiveTab = "palm";
        await LoadActiveUserAsync();

        if (TestUserProfile == null)
        {
            ActiveTab = "login";
            AlertSuccess = false;
            AlertMessage = "Pehle app me login karein.";
            return Page();
        }

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

        var userId = TestUserProfile.UserId;
        bool isPremium = TestUserProfile.IsSubscriptionActive && TestUserProfile.Plan != "Free";

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
            AlertMessage = "Gemini AI error: " + ex.Message;
            return Page();
        }

        var summaryText = PalmResult?.LifeLine?.Summary ?? "Palm scan complete";

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
                "Hindi" => "हस्तरेखा फोटो का सफल विश्लेषण! Google Gemini Vision AI ने रेखाओं और पर्वतों को स्कैन किया।",
                "English" => "Palm image successfully analyzed by Google Gemini Vision AI!",
                _ => "Palm photo successfully analyze ho gayi! Gemini Vision AI ne rekhaayein padh li hain."
            };
        }

        return Page();
    }

    // ── 6. REAL FACE SCAN (PHOTO REQUIRED) ─────────
    public async Task<IActionResult> OnPostScanFaceAsync(IFormFile? faceImage)
    {
        ActiveTab = "face";
        await LoadActiveUserAsync();

        if (TestUserProfile == null)
        {
            ActiveTab = "login";
            AlertSuccess = false;
            AlertMessage = "Pehle app me login karein.";
            return Page();
        }

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

        var userId = TestUserProfile.UserId;
        bool isPremium = TestUserProfile.IsSubscriptionActive && TestUserProfile.Plan != "Free";

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
            AlertMessage = "Gemini AI error: " + ex.Message;
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

    // ── 7. KUNDLI CALCULATION ──────────────────────
    public async Task<IActionResult> OnPostCalculateKundliAsync(
        string dob, string tob, string city, double lat = 28.6139, double lng = 77.2090)
    {
        ActiveTab = "kundli";
        KundliViewMode = "d1";
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

    // ── 7B. 36 GUNA ASHTAKOOT MILAN ────────────────
    public async Task<IActionResult> OnPostCalculateGunMilanAsync(
        string boyName, string boyDob, string boyTob,
        string girlName, string girlDob, string girlTob)
    {
        ActiveTab = "kundli";
        KundliViewMode = "match";
        await LoadActiveUserAsync();

        var bDob = DateOnly.TryParse(boyDob, out var bd) ? bd : new DateOnly(1995, 8, 15);
        var bTob = TimeOnly.TryParse(boyTob, out var bt) ? bt : (TimeOnly?)new TimeOnly(14, 30);
        var gDob = DateOnly.TryParse(girlDob, out var gd) ? gd : new DateOnly(1997, 11, 20);
        var gTob = TimeOnly.TryParse(girlTob, out var gt) ? gt : (TimeOnly?)new TimeOnly(10, 15);

        MilanResult = _astroSvc.CalculateGunMilan(
            bDob, bTob, 28.6139, 77.2090,
            gDob, gTob, 28.6139, 77.2090);

        AlertSuccess = true;
        AlertMessage = $"36 गुण मिलान पूर्ण! कुल अंक: {MilanResult.TotalScore}/36 — {MilanResult.Verdict}";
        return Page();
    }

    // ── 7C. DOWNLOAD KUNDLI PDF ────────────────────
    public IActionResult OnGetDownloadKundliPdf(
        string? name, string? dob, string? tob, string? city)
    {
        var d = DateOnly.TryParse(dob, out var bd) ? bd : new DateOnly(1995, 8, 15);
        var t = TimeOnly.TryParse(tob, out var bt) ? bt : (TimeOnly?)new TimeOnly(14, 30);
        var cityName = string.IsNullOrWhiteSpace(city) ? "New Delhi, India" : city;
        var nativeName = string.IsNullOrWhiteSpace(name) ? "Devotee User" : name;

        var result = _astroSvc.CalculateKundli(d, t, 28.6139, 77.2090, "India Standard Time");
        var pdfBytes = _pdfSvc.GenerateKundliReportPdf(result, nativeName, cityName, d, t);

        return File(pdfBytes, "application/pdf", $"Kundli_{nativeName.Replace(" ", "_")}.pdf");
    }

    // ── 8. SUBSCRIPTION UPGRADE ────────────────────
    public async Task<IActionResult> OnPostUpgradeAsync(string plan)
    {
        ActiveTab = "wallet";
        await LoadActiveUserAsync();

        if (TestUserProfile == null) return Page();

        var userId = TestUserProfile.UserId;
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

    // ── 9. RESET CREDITS ───────────────────────────
    public async Task<IActionResult> OnPostResetCreditsAsync()
    {
        ActiveTab = "wallet";
        await LoadActiveUserAsync();

        if (TestUserProfile == null) return Page();

        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE tbl_Subscriptions
            SET [Plan] = 'Free', PalmScansRemaining = 3, FaceScansRemaining = 1, ExpiryDate = NULL
            WHERE UserId = @UserId;
        ", new { UserId = TestUserProfile.UserId });

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
