using CyhAstro.Core.DTOs;

namespace CyhAstro.Client.Services;

/// <summary>
/// In-App Update Engine — Checks server for newer app releases.
/// Provides direct APK download URLs, mandatory update guards, and release notes.
/// </summary>
public class InAppUpdater
{
    private readonly AstroApiClient _apiClient;

    public InAppUpdater(AstroApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public int CurrentAppVersionCode { get; set; } = 1;
    public string CurrentAppVersionName { get; set; } = "1.0.0";

    public async Task<AppUpdateStatus> CheckForUpdatesAsync(string platform = "Android")
    {
        try
        {
            var result = await _apiClient.CheckAppUpdateAsync(platform, CurrentAppVersionCode);
            if (result == null || !result.UpdateAvailable)
            {
                return new AppUpdateStatus { HasUpdate = false };
            }

            return new AppUpdateStatus
            {
                HasUpdate = true,
                IsMandatory = result.IsMandatory,
                NewVersionName = result.VersionName,
                NewVersionCode = result.VersionCode,
                DownloadUrl = result.DownloadUrl,
                PlayStoreUrl = result.PlayStoreUrl,
                ReleaseNotes = result.ReleaseNotes
            };
        }
        catch
        {
            return new AppUpdateStatus { HasUpdate = false };
        }
    }
}

public class AppUpdateStatus
{
    public bool HasUpdate { get; set; }
    public bool IsMandatory { get; set; }
    public string? NewVersionName { get; set; }
    public int NewVersionCode { get; set; }
    public string? DownloadUrl { get; set; }
    public string? PlayStoreUrl { get; set; }
    public string? ReleaseNotes { get; set; }
}
