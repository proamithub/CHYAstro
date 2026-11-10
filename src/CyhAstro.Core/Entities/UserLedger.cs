namespace CyhAstro.Core.Entities;

public class UserLedger
{
    public Guid LedgerId { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }     // Join from User
    public string? UserEmail { get; set; }    // Join from User
    public string? UserPhone { get; set; }    // Join from User
    public string TransactionType { get; set; } = string.Empty;
    public decimal AmountINR { get; set; }
    public int CreditChange { get; set; }
    public int BalanceAfter { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Success";
    public string? PaymentGateway { get; set; }
    public string? GatewayTxnId { get; set; }
    public string? AdminRemark { get; set; }
    public DateTime CreatedAt { get; set; }
}
