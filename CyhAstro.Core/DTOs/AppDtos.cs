namespace CyhAstro.Core.DTOs;

// ===== AUTH =====
public record RegisterRequest(
    string FullName,
    string? Email,
    string? PhoneNumber,
    string Password,
    string Gender,
    DateOnly DateOfBirth,
    TimeOnly? TimeOfBirth,
    string? BirthCity,
    decimal? BirthLatitude,
    decimal? BirthLongitude,
    string? DeviceToken
);

public record LoginRequest(string? Email, string? PhoneNumber, string Password, string? DeviceToken);

public record AuthResponse(
    bool Success,
    string Message,
    string? Token,
    Guid? UserId,
    string? Role,
    UserProfileDto? Profile
);

// ===== USER PROFILE =====
public class UserProfileDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string Gender { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public TimeOnly? TimeOfBirth { get; set; }
    public string? BirthCity { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public bool IsBlocked { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Subscription info
    public string Plan { get; set; } = "Free";
    public bool IsSubscriptionActive { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int PalmScansRemaining { get; set; }
    public int FaceScansRemaining { get; set; }
    public int ChatQuestionsToday { get; set; }

    // Kundli snapshot
    public string? LagnaRashi { get; set; }
    public string? MoonRashi { get; set; }
    public string? SunRashi { get; set; }
    public string? Nakshatra { get; set; }
    public int? NakshatraPada { get; set; }
    public string? MahadashaCurrent { get; set; }
    public DateOnly? MahadashaEndDate { get; set; }
    public bool IsManglik { get; set; }
    public bool SadeSatiActive { get; set; }
}

// ===== KUNDLI =====
public record KundliRequest(
    Guid UserId,
    DateOnly DateOfBirth,
    TimeOnly? TimeOfBirth,
    string BirthCity,
    double Latitude,
    double Longitude,
    string TimeZoneId
);

public class KundliHouseDto
{
    public int HouseNumber { get; set; } // 1 to 12
    public int RashiNumber { get; set; } // 1 to 12
    public string RashiName { get; set; } = string.Empty;
    public List<string> PlanetsHindi { get; set; } = new();
    public List<string> PlanetsEnglish { get; set; } = new();
}

public class KundliResultDto
{
    public string LagnaRashi { get; set; } = string.Empty;
    public int LagnaRashiNumber { get; set; }
    public double LagnaDegree { get; set; }
    public string MoonRashi { get; set; } = string.Empty;
    public int MoonRashiNumber { get; set; }
    public string SunRashi { get; set; } = string.Empty;
    public int SunRashiNumber { get; set; }
    public string Nakshatra { get; set; } = string.Empty;
    public int NakshatraPada { get; set; }
    public string MahadashaCurrent { get; set; } = string.Empty;
    public DateOnly MahadashaEndDate { get; set; }
    public bool IsManglik { get; set; }
    public bool SadeSatiActive { get; set; }
    public List<PlanetDto> Planets { get; set; } = new();
    public string AyanamsaUsed { get; set; } = "Lahiri";

    // 12 Houses for D1 Lagna Chart
    public List<KundliHouseDto> D1Houses { get; set; } = new();

    // D9 Navamsha Chart
    public string NavamshaLagnaRashi { get; set; } = string.Empty;
    public int NavamshaLagnaRashiNumber { get; set; }
    public List<KundliHouseDto> D9Houses { get; set; } = new();
}

public class PlanetDto
{
    public string Name { get; set; } = string.Empty;
    public string NameHindi { get; set; } = string.Empty;
    public string Rashi { get; set; } = string.Empty;
    public int RashiNumber { get; set; }
    public double Degree { get; set; }
    public int House { get; set; } // 1 to 12
    public bool IsRetrograde { get; set; }
    public bool IsExalted { get; set; }
    public bool IsDebilitated { get; set; }
    public string NavamshaRashi { get; set; } = string.Empty;
    public int NavamshaRashiNumber { get; set; }
    public int NavamshaHouse { get; set; }
}

// ===== 36 GUNA MILAN (ASHTAKOOT) =====
public record GunMilanRequest(
    string BoyName,
    DateOnly BoyDob,
    TimeOnly? BoyTob,
    string? BoyCity,
    double BoyLat,
    double BoyLng,
    string GirlName,
    DateOnly GirlDob,
    TimeOnly? GirlTob,
    string? GirlCity,
    double GirlLat,
    double GirlLng
);

public class GunMilanKootDto
{
    public string Name { get; set; } = string.Empty;
    public string NameHindi { get; set; } = string.Empty;
    public double MaxScore { get; set; }
    public double ObtainedScore { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class GunMilanResultDto
{
    public string BoyMoonRashi { get; set; } = string.Empty;
    public string BoyNakshatra { get; set; } = string.Empty;
    public string GirlMoonRashi { get; set; } = string.Empty;
    public string GirlNakshatra { get; set; } = string.Empty;

    public double TotalScore { get; set; } // Out of 36
    public double MaxScore { get; set; } = 36;
    public string Verdict { get; set; } = string.Empty;
    public string VerdictColor { get; set; } = "success"; // "success", "warning", "danger"
    public string Recommendation { get; set; } = string.Empty;

    public bool BoyIsManglik { get; set; }
    public bool GirlIsManglik { get; set; }
    public bool ManglikMatchAcceptable { get; set; }
    public string ManglikVerdict { get; set; } = string.Empty;

    public bool HasNadiDosha { get; set; }
    public bool HasBhakootDosha { get; set; }

    public List<GunMilanKootDto> Koots { get; set; } = new();
}

// ===== SCAN =====
public class ScanResultDto
{
    public Guid ScanId { get; set; }
    public string ScanType { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int CreditsLeft { get; set; }

    // Palm analysis
    public PalmAnalysisDto? PalmAnalysis { get; set; }

    // Face analysis
    public FaceAnalysisDto? FaceAnalysis { get; set; }
}

public class PalmLineDto
{
    public string Summary { get; set; } = string.Empty;
    public string Strength { get; set; } = string.Empty;  // Weak, Moderate, Strong
    public List<string> Insights { get; set; } = new();
    public bool IsLocked { get; set; } = false;
}

public class PalmAnalysisDto
{
    public PalmLineDto LifeLine { get; set; } = new();
    public PalmLineDto HeadLine { get; set; } = new();
    public PalmLineDto HeartLine { get; set; } = new();
    public PalmLineDto FateLine { get; set; } = new();
    public PalmLineDto MountJupiter { get; set; } = new();
    public PalmLineDto MountVenus { get; set; } = new();
    public PalmLineDto MountSun { get; set; } = new();
}

public class FaceZoneDto
{
    public string Summary { get; set; } = string.Empty;
    public string Trait { get; set; } = string.Empty;
    public string Insight { get; set; } = string.Empty;
    public bool IsLocked { get; set; } = false;
}

public class FaceAnalysisDto
{
    public FaceZoneDto Forehead { get; set; } = new();
    public FaceZoneDto Eyebrows { get; set; } = new();
    public FaceZoneDto Eyes { get; set; } = new();
    public FaceZoneDto Nose { get; set; } = new();
    public FaceZoneDto LipsAndMouth { get; set; } = new();
    public FaceZoneDto JawAndChin { get; set; } = new();
}

// ===== SUBSCRIPTION =====
public record SubscriptionUpgradeRequest(
    Guid UserId,
    string NewPlan,
    decimal AmountPaidINR,
    string PaymentGateway,
    string GatewayTxnId
);

// ===== APP VERSION =====
public class AppVersionCheckDto
{
    public int VersionCode { get; set; }
    public string VersionName { get; set; } = string.Empty;
    public string? DownloadUrl { get; set; }
    public string? PlayStoreUrl { get; set; }
    public string? ReleaseNotes { get; set; }
    public bool IsMandatory { get; set; }
    public bool UpdateAvailable { get; set; }
}

// ===== ADMIN =====
public class DashboardStatsDto
{
    public int TotalUsers { get; set; }
    public int NewUsersToday { get; set; }
    public int ActiveSubscribers { get; set; }
    public int TotalScansToday { get; set; }
    public int PalmScansToday { get; set; }
    public int FaceScansToday { get; set; }
    public decimal RevenueToday { get; set; }
    public decimal RevenueThisMonth { get; set; }
    public decimal RevenueTotalAllTime { get; set; }
}

public class AdminUserListItemDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string Gender { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public bool IsBlocked { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Plan { get; set; } = "Free";
    public bool IsSubActive { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int PalmScansRemaining { get; set; }
    public int FaceScansRemaining { get; set; }
    public int TotalScans { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;
}
