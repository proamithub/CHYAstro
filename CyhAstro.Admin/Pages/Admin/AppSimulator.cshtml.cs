using CyhAstro.Core.DTOs;
using CyhAstro.Core.Entities;
using CyhAstro.Core.Interfaces;
using CyhAstro.Infrastructure.Data;
using CyhAstro.Infrastructure.Services;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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

    public AppSimulatorModel(
        ISqlConnectionFactory db,
        IUserRepository userRepo,
        IScanRepository scanRepo,
        IVedicAstroService astroSvc,
        IGeminiVisionService gemini,
        ISubscriptionRepository subRepo,
        ILocalizationService localizer)
    {
        _db = db;
        _userRepo = userRepo;
        _scanRepo = scanRepo;
        _astroSvc = astroSvc;
        _gemini = gemini;
        _subRepo = subRepo;
        _localizer = localizer;
    }

    public UserProfileDto? TestUserProfile { get; set; }
    public KundliResultDto? KundliResult { get; set; }
    public PalmAnalysisDto? PalmResult { get; set; }
    public FaceAnalysisDto? FaceResult { get; set; }
    public string ActiveTab { get; set; } = "palm";
    public string? AlertMessage { get; set; }
    public bool AlertSuccess { get; set; }
    public string CurrentLang => _localizer.CurrentLanguage;

    public async Task OnGetAsync(string tab = "palm")
    {
        ActiveTab = tab;
        await LoadTestUserAsync();
    }

    private async Task<Guid> GetOrCreateTestUserIdAsync()
    {
        using var conn = _db.CreateConnection();
        var id = await conn.QueryFirstOrDefaultAsync<Guid?>(
            "SELECT UserId FROM tbl_Users WHERE Email = 'tester@cyhastro.com'");

        if (id.HasValue && id.Value != Guid.Empty)
            return id.Value;

        // Create test mobile user with free credits
        var testId = Guid.NewGuid();
        var hash = BCrypt.Net.BCrypt.HashPassword("Test@123", workFactor: 11);
        await conn.ExecuteAsync(@"
            INSERT INTO tbl_Users (UserId, FullName, Email, PhoneNumber, PasswordHash, Gender, DateOfBirth, Role)
            VALUES (@UserId, 'Amit Sharma (Tester)', 'tester@cyhastro.com', '+919876543210', @Hash, 'Male', '1995-08-15', 'User');

            INSERT INTO tbl_Subscriptions (UserId, [Plan], PalmScansRemaining, FaceScansRemaining)
            VALUES (@UserId, 'Free', 3, 1);

            INSERT INTO tbl_UserLedger (UserId, TransactionType, AmountINR, CreditChange, BalanceAfter, Description, Status)
            VALUES (@UserId, 'WelcomeCredit', 0, 4, 4, 'Test user initialized with free credits.', 'Success');
        ", new { UserId = testId, Hash = hash });

        return testId;
    }

    private async Task LoadTestUserAsync()
    {
        var testId = await GetOrCreateTestUserIdAsync();
        TestUserProfile = await _userRepo.GetProfileAsync(testId);
    }

    // ── 1. TEST KUNDLI CALCULATION ─────────────────
    public async Task<IActionResult> OnPostCalculateKundliAsync(
        string dob, string tob, string city, double lat = 28.6139, double lng = 77.2090)
    {
        ActiveTab = "kundli";
        await LoadTestUserAsync();

        if (string.IsNullOrWhiteSpace(dob)) dob = "1995-08-15";
        if (string.IsNullOrWhiteSpace(tob)) tob = "14:30";

        var d = DateOnly.Parse(dob);
        var t = TimeOnly.Parse(tob);

        KundliResult = _astroSvc.CalculateKundli(d, t, lat, lng, "India Standard Time");
        AlertSuccess = true;
        AlertMessage = _localizer.CurrentLanguage switch
        {
            "Hindi" => $"कुंडली सफलतापूर्वक बनाई गई! लग्न: {KundliResult.LagnaRashi}, राशि: {KundliResult.MoonRashi}, नक्षत्र: {KundliResult.Nakshatra}",
            "English" => $"Kundli calculated! Ascendant: {KundliResult.LagnaRashi}, Moon Sign: {KundliResult.MoonRashi}, Nakshatra: {KundliResult.Nakshatra}",
            _ => $"Kundli calculate ho gayi! Lagna: {KundliResult.LagnaRashi}, Rashi: {KundliResult.MoonRashi}, Nakshatra: {KundliResult.Nakshatra}"
        };

        return Page();
    }

    // ── 2. TEST PALM SCAN ──────────────────────────
    public async Task<IActionResult> OnPostScanPalmAsync(IFormFile? palmImage, string handSide = "Right")
    {
        ActiveTab = "palm";
        await LoadTestUserAsync();

        var userId = TestUserProfile?.UserId ?? await GetOrCreateTestUserIdAsync();
        bool isPremium = TestUserProfile?.IsSubscriptionActive == true && TestUserProfile.Plan != "Free";

        if (palmImage != null && palmImage.Length > 0)
        {
            try
            {
                await using var stream = palmImage.OpenReadStream();
                PalmResult = await _gemini.AnalyzePalmAsync(stream, palmImage.ContentType, isPremium);
            }
            catch
            {
                // Fallback simulation if API key limit or offline
                PalmResult = GetMockPalmResult(isPremium);
            }
        }
        else
        {
            // Sample test simulation
            PalmResult = GetMockPalmResult(isPremium);
        }

        // Deduct 1 credit & save scan
        await _scanRepo.SaveScanAsync(
            userId, "Palm", handSide,
            "/uploads/samples/sample_palm.jpg", "/uploads/samples/thumb_palm.jpg",
            System.Text.Json.JsonSerializer.Serialize(PalmResult),
            PalmResult?.LifeLine?.Summary ?? "Palm scan complete");

        await LoadTestUserAsync();
        AlertSuccess = true;
        AlertMessage = _localizer.CurrentLanguage switch
        {
            "Hindi" => "हस्तरेखा स्कैन पूर्ण हुआ! AI ने जीवन, मस्तिष्क और हृदय रेखा का विश्लेषण किया।",
            "English" => "Palm scan complete! AI successfully analyzed your Life, Head, and Heart lines.",
            _ => "Palm scan complete! AI ne Jeevan, Mastishk aur Hriday rekha analyze kar li."
        };

        return Page();
    }

    // ── 3. TEST FACE SCAN ──────────────────────────
    public async Task<IActionResult> OnPostScanFaceAsync(IFormFile? faceImage)
    {
        ActiveTab = "face";
        await LoadTestUserAsync();

        var userId = TestUserProfile?.UserId ?? await GetOrCreateTestUserIdAsync();
        bool isPremium = TestUserProfile?.IsSubscriptionActive == true && TestUserProfile.Plan != "Free";

        if (faceImage != null && faceImage.Length > 0)
        {
            try
            {
                await using var stream = faceImage.OpenReadStream();
                FaceResult = await _gemini.AnalyzeFaceAsync(stream, faceImage.ContentType, isPremium);
            }
            catch
            {
                FaceResult = GetMockFaceResult(isPremium);
            }
        }
        else
        {
            FaceResult = GetMockFaceResult(isPremium);
        }

        await _scanRepo.SaveScanAsync(
            userId, "Face", null,
            "/uploads/samples/sample_face.jpg", "/uploads/samples/thumb_face.jpg",
            System.Text.Json.JsonSerializer.Serialize(FaceResult),
            FaceResult?.Forehead?.Summary ?? "Face scan complete");

        await LoadTestUserAsync();
        AlertSuccess = true;
        AlertMessage = _localizer.CurrentLanguage switch
        {
            "Hindi" => "सामुद्रिक शास्त्र (चेहरा) स्कैन पूर्ण हुआ! AI ने मस्तक और नयन का विश्लेषण किया।",
            "English" => "Face scan complete! Samudrik Shastra AI successfully analyzed facial zones.",
            _ => "Face scan complete! Samudrik Shastra AI ne chehre ke zones analyze kar liye."
        };

        return Page();
    }

    // ── 4. TEST SUBSCRIPTION UPGRADE ──────────────
    public async Task<IActionResult> OnPostUpgradeAsync(string plan)
    {
        ActiveTab = "wallet";
        await LoadTestUserAsync();

        var userId = TestUserProfile?.UserId ?? await GetOrCreateTestUserIdAsync();
        decimal amount = plan switch { "Weekly" => 99, "Monthly" => 299, "Yearly" => 1999, _ => 4999 };

        await _subRepo.UpgradePlanAsync(new SubscriptionUpgradeRequest(
            userId,
            plan,
            amount,
            "TestSimulatedRazorpay",
            "TXN_SIM_" + DateTime.UtcNow.Ticks
        ));

        await LoadTestUserAsync();
        AlertSuccess = true;
        AlertMessage = _localizer.Get("PlanActivated", plan);

        return Page();
    }

    // ── 5. TEST RESET CREDITS ──────────────────────
    public async Task<IActionResult> OnPostResetCreditsAsync()
    {
        ActiveTab = "wallet";
        await LoadTestUserAsync();
        var userId = TestUserProfile?.UserId ?? await GetOrCreateTestUserIdAsync();

        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE tbl_Subscriptions
            SET [Plan] = 'Free', PalmScansRemaining = 3, FaceScansRemaining = 1, ExpiryDate = NULL
            WHERE UserId = @UserId;
        ", new { UserId = userId });

        await LoadTestUserAsync();
        AlertSuccess = true;
        AlertMessage = "Test user reset to Free plan with 3 Palm & 1 Face scan credits.";
        return Page();
    }

    private PalmAnalysisDto GetMockPalmResult(bool isPremium) => new()
    {
        LifeLine = new() { Strength = "Strong", Summary = _localizer.CurrentLanguage == "Hindi" ? "जीवन रेखा लंबी, गहरी और सुदृढ़ है। उत्तम स्वास्थ्य और दीर्घायु के योग हैं।" : (_localizer.CurrentLanguage == "English" ? "Long, clear, and deep life line indicating vitality and resilience." : "Jeevan rekha lambi aur spasht hai. Achhi sehat aur dirghayu ke sanket hain."), Insights = new() { "Vitality: 88%", "Good stamina & family support" } },
        HeadLine = new() { Strength = "Strong", Summary = _localizer.CurrentLanguage == "Hindi" ? "मस्तिष्क रेखा स्पष्ट है। गहरी तार्किक सोच और निर्णय क्षमता को दर्शाती है।" : (_localizer.CurrentLanguage == "English" ? "Deep headline curving gently towards Moon mount, showing balanced logic and creativity." : "Mastishk rekha spasht hai. Logical thinking aur sahi decisions lene ki shamta darshati hai."), Insights = new() { "Analytical Thinking: High", "Focus in career" } },
        HeartLine = new() { Strength = "Moderate", Summary = _localizer.CurrentLanguage == "Hindi" ? "हृदय रेखा गुरु पर्वत की ओर जाती है। निष्ठावान और संवेदनशील व्यक्तित्व।" : (_localizer.CurrentLanguage == "English" ? "Heart line curves toward Jupiter mount showing loyalty, devotion, and compassion." : "Hriday rekha Guru parvat ki taraf mudti hai. Wafadar aur bhavuk vyaktitva."), Insights = new() { "Emotional depth", "Stable relationships" } },
        FateLine = isPremium ? new() { Strength = "Strong", Summary = _localizer.CurrentLanguage == "Hindi" ? "भाग्य रेखा स्पष्ट है। 28वें वर्ष से करियर में तीव्र प्रगति के संकेत हैं।" : (_localizer.CurrentLanguage == "English" ? "Prominent fate line showing rapid career advancement from age 28 onwards." : "Bhagya rekha saaf hai. 28 saal ki umar se career me tezi se vridhi ke sanket hain."), Insights = new() { "Career Peak: Age 32-45", "Independent business success" } }
                             : new() { IsLocked = true, Summary = _localizer.Get("FateLineLocked") },
        MountJupiter = isPremium ? new() { Strength = "Developed", Summary = "Guru Parvat unnat hai. Leadership aur samajik samman milega." } : new() { IsLocked = true, Summary = _localizer.Get("MountLocked") },
        MountVenus = isPremium ? new() { Strength = "Developed", Summary = "Shukra Parvat sundar hai. Kala, soundarya aur aakarshan ki kshamta." } : new() { IsLocked = true, Summary = _localizer.Get("MountLocked") },
        MountSun = isPremium ? new() { Strength = "Prominent", Summary = "Surya Parvat tejasvi hai. Naam, shohrat aur prashansa milegi." } : new() { IsLocked = true, Summary = _localizer.Get("MountLocked") }
    };

    private FaceAnalysisDto GetMockFaceResult(bool isPremium) => isPremium ? new()
    {
        Forehead = new() { Trait = "Visionary Intellect", Summary = _localizer.CurrentLanguage == "Hindi" ? "मस्तक चौड़ा और स्पष्ट है। उच्च बुद्धि और पूर्वजों के आशीर्वाद का प्रतीक।" : "Chowda aur spasht mastak. Tejasvi dimaag aur durdarshi soch.", Insight = "Destiny supports leadership roles" },
        Eyes = new() { Trait = "Deep Perception", Summary = "Aankhein chamakdaar aur tejasvi hain. Gehre vishleshan ki kshamta.", Insight = "Intuition is extraordinarily sharp" },
        Nose = new() { Trait = "Wealth Accumulator", Summary = "Naak seedhi aur moolyavaan hai. Dhan sanchay aur aatmavishwas ka sanket.", Insight = "Financial independence guaranteed" },
        LipsAndMouth = new() { Trait = "Eloquence", Summary = "Honth santulit hain. Madhur vaani aur prabhavshaali communication.", Insight = "Success in public speaking & business" },
        JawAndChin = new() { Trait = "Unshakable Willpower", Summary = "Mazboot thodi. Kathin paristhitiyon me haar na manne ka sanket.", Insight = "Long-term determination" }
    } : new()
    {
        Forehead = new() { IsLocked = true, Summary = _localizer.Get("FaceReadingLocked") },
        Eyebrows = new() { IsLocked = true, Summary = _localizer.Get("MountLocked") },
        Eyes = new() { IsLocked = true, Summary = _localizer.Get("MountLocked") },
        Nose = new() { IsLocked = true, Summary = _localizer.Get("MountLocked") },
        LipsAndMouth = new() { IsLocked = true, Summary = _localizer.Get("MountLocked") },
        JawAndChin = new() { IsLocked = true, Summary = _localizer.Get("MountLocked") }
    };
}
