namespace CyhAstro.Core.Entities;

public class KundliProfile
{
    public Guid KundliId { get; set; }
    public Guid UserId { get; set; }
    public string? LagnaRashi { get; set; }
    public decimal? LagnaDegree { get; set; }
    public string? MoonRashi { get; set; }
    public string? SunRashi { get; set; }
    public string? Nakshatra { get; set; }
    public int? NakshatraPada { get; set; }
    public string? MahadashaCurrent { get; set; }
    public DateOnly? MahadashaEndDate { get; set; }
    public bool IsManglik { get; set; }
    public bool SadeSatiActive { get; set; }
    public string? PlanetaryJson { get; set; }
    public string AyanamsaUsed { get; set; } = "Lahiri";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
