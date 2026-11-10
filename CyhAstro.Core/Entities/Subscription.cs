namespace CyhAstro.Core.Entities;

public class Subscription
{
    public Guid SubId { get; set; }
    public Guid UserId { get; set; }
    public string Plan { get; set; } = "Free";
    public DateTime? StartDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; }
    public int PalmScansRemaining { get; set; } = 1;
    public int FaceScansRemaining { get; set; } = 0;
    public int ChatQuestionsToday { get; set; } = 3;
    public DateTime QuotaResetDate { get; set; }
    public string? PaymentGateway { get; set; }
    public string? GatewayTxnId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
