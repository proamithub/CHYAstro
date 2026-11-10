namespace CyhAstro.Core.Entities;

public class User
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string Gender { get; set; } = "Other";
    public DateOnly DateOfBirth { get; set; }
    public TimeOnly? TimeOfBirth { get; set; }
    public string? BirthCity { get; set; }
    public decimal? BirthLatitude { get; set; }
    public decimal? BirthLongitude { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public string Role { get; set; } = "User";
    public bool IsActive { get; set; } = true;
    public bool IsBlocked { get; set; } = false;
    public string? BlockReason { get; set; }
    public string? DeviceToken { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
