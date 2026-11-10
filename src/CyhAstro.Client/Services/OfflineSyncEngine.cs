using System.Text.Json;
using CyhAstro.Core.DTOs;
using CyhAstro.Core.Entities;

namespace CyhAstro.Client.Services;

/// <summary>
/// Offline Sync Engine — Enables the hybrid app to work 100% offline without internet.
/// Stores pending scans in a local queue and automatically pushes them to the server when internet returns.
/// </summary>
public class OfflineSyncEngine
{
    private readonly AstroApiClient _apiClient;
    private readonly List<QueuedOfflineScan> _localPendingQueue = new();

    public OfflineSyncEngine(AstroApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public bool IsOnline { get; set; } = true;
    public int PendingQueueCount => _localPendingQueue.Count;

    public event Action<int>? OnPendingCountChanged;
    public event Action<string>? OnSyncCompleted;

    /// <summary>
    /// Save a scan locally when user is offline (no internet).
    /// </summary>
    public void QueueScanOffline(string scanType, string? handSide, byte[] imageBytes, string fileName)
    {
        var item = new QueuedOfflineScan
        {
            Id = Guid.NewGuid(),
            ScanType = scanType,
            HandSide = handSide,
            ImageBytes = imageBytes,
            FileName = fileName,
            ScannedAt = DateTime.UtcNow
        };

        _localPendingQueue.Add(item);
        OnPendingCountChanged?.Invoke(_localPendingQueue.Count);
    }

    /// <summary>
    /// Trigger auto-sync when network connectivity returns.
    /// Pushes all queued items to the server.
    /// </summary>
    public async Task<int> ProcessPendingSyncQueueAsync()
    {
        if (!_localPendingQueue.Any())
            return 0;

        int syncedCount = 0;
        var pendingItems = _localPendingQueue.ToList();

        foreach (var item in pendingItems)
        {
            try
            {
                using var ms = new MemoryStream(item.ImageBytes);
                if (item.ScanType == "Palm")
                {
                    await _apiClient.ScanPalmAsync(ms, item.FileName, item.HandSide ?? "Right");
                }
                else
                {
                    await _apiClient.ScanFaceAsync(ms, item.FileName);
                }

                _localPendingQueue.Remove(item);
                syncedCount++;
            }
            catch
            {
                // Network failed or server unreachable; item stays in queue for next sync retry
                break;
            }
        }

        OnPendingCountChanged?.Invoke(_localPendingQueue.Count);
        if (syncedCount > 0)
        {
            OnSyncCompleted?.Invoke($"{syncedCount} scans successfully synced with the server!");
        }

        return syncedCount;
    }
}

public class QueuedOfflineScan
{
    public Guid Id { get; set; }
    public string ScanType { get; set; } = "Palm";
    public string? HandSide { get; set; }
    public byte[] ImageBytes { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public DateTime ScannedAt { get; set; }
}
