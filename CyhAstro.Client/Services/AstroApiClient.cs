using System.Net.Http.Headers;
using System.Net.Http.Json;
using CyhAstro.Core.DTOs;
using CyhAstro.Core.Entities;

namespace CyhAstro.Client.Services;

/// <summary>
/// Client API Service — Handles HTTP communication between Mobile/Hybrid client and CyhAstro.Api backend.
/// Supports JWT authentication token caching and multipart image uploads.
/// </summary>
public class AstroApiClient
{
    private readonly HttpClient _http;
    private string? _authToken;

    public AstroApiClient(HttpClient http)
    {
        _http = http;
    }

    public void SetAuthToken(string? token)
    {
        _authToken = token;
        if (!string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            _http.DefaultRequestHeaders.Authorization = null;
        }
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_authToken);

    // ── Auth ──
    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/auth/register", request);
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        if (result?.Success == true && !string.IsNullOrEmpty(result.Token))
        {
            SetAuthToken(result.Token);
        }
        return result;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", request);
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        if (result?.Success == true && !string.IsNullOrEmpty(result.Token))
        {
            SetAuthToken(result.Token);
        }
        return result;
    }

    public async Task<UserProfileDto?> GetProfileAsync()
    {
        return await _http.GetFromJsonAsync<UserProfileDto>("api/auth/profile");
    }

    // ── Kundli ──
    public async Task<KundliResultDto?> CalculateKundliAsync(KundliRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/kundli/calculate", request);
        return await response.Content.ReadFromJsonAsync<KundliResultDto>();
    }

    public async Task<KundliProfile?> GetMyKundliAsync()
    {
        return await _http.GetFromJsonAsync<KundliProfile>("api/kundli/my");
    }

    // ── Scan: Palm & Face ──
    public async Task<ScanResultDto?> ScanPalmAsync(Stream imageStream, string fileName, string handSide = "Right")
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(imageStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "image", fileName);
        content.Add(new StringContent(handSide), "handSide");

        var response = await _http.PostAsync("api/scan/palm", content);
        return await response.Content.ReadFromJsonAsync<ScanResultDto>();
    }

    public async Task<ScanResultDto?> ScanFaceAsync(Stream imageStream, string fileName)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(imageStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "image", fileName);

        var response = await _http.PostAsync("api/scan/face", content);
        return await response.Content.ReadFromJsonAsync<ScanResultDto>();
    }

    public async Task<IEnumerable<ScanHistory>?> GetScanHistoryAsync(int page = 1)
    {
        return await _http.GetFromJsonAsync<IEnumerable<ScanHistory>>($"api/scan/history?page={page}");
    }

    // ── Subscriptions ──
    public async Task<Subscription?> GetSubscriptionStatusAsync()
    {
        return await _http.GetFromJsonAsync<Subscription>("api/subscription/status");
    }

    // ── App Update Check ──
    public async Task<AppVersionCheckDto?> CheckAppUpdateAsync(string platform, int currentVersionCode)
    {
        return await _http.GetFromJsonAsync<AppVersionCheckDto>(
            $"api/app/check-update?platform={platform}&currentVersion={currentVersionCode}");
    }
}
