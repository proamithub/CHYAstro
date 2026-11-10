namespace CyhAstro.Core.Entities;

public class ScanHistory
{
    public Guid ScanId { get; set; }
    public Guid UserId { get; set; }
    public string ScanType { get; set; } = string.Empty;    // Palm, Face
    public string? HandSide { get; set; }                   // Left, Right
    public string ImagePath { get; set; } = string.Empty;
    public string? ThumbnailPath { get; set; }
    public string? AiRawResponse { get; set; }
    public string? SummaryText { get; set; }
    public bool IsFullReportUnlocked { get; set; }
    public string SyncStatus { get; set; } = "Synced";     // Pending, Synced, Failed
    public DateTime ScannedAt { get; set; }
    public DateTime? SyncedAt { get; set; }
}
