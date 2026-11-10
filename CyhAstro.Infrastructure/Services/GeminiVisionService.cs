using CyhAstro.Core.DTOs;
using CyhAstro.Core.Interfaces;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CyhAstro.Infrastructure.Services;

public interface IGeminiVisionService
{
    Task<PalmAnalysisDto?> AnalyzePalmAsync(Stream imageStream, string mimeType, bool isPremiumUser);
    Task<FaceAnalysisDto?> AnalyzeFaceAsync(Stream imageStream, string mimeType, bool isPremiumUser);
}

public class GeminiVisionService : IGeminiVisionService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _palmPrompt;
    private readonly string _facePrompt;
    private readonly ILocalizationService _localizer;

    public GeminiVisionService(HttpClient httpClient, string apiKey, string palmPrompt, string facePrompt, ILocalizationService localizer)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _palmPrompt = palmPrompt;
        _facePrompt = facePrompt;
        _localizer = localizer;
    }

    private string GetLanguageInstruction() => _localizer.CurrentLanguage switch
    {
        "Hindi" => "\nIMPORTANT: Provide all summaries, strength notes, and insights in pure, respectful Hindi (Devanagari script).",
        "English" => "\nIMPORTANT: Provide all summaries, strength notes, and insights in clear, modern English.",
        _ => "\nIMPORTANT: Provide all summaries, strength notes, and insights in natural, friendly Hinglish (Hindi written in Latin alphabet, e.g. 'Aapki jeevan rekha lambi aur spasht hai')."
    };

    public async Task<PalmAnalysisDto?> AnalyzePalmAsync(Stream imageStream, string mimeType, bool isPremiumUser)
    {
        var base64 = await StreamToBase64Async(imageStream);
        var effectivePrompt = _palmPrompt + GetLanguageInstruction();
        var json = await CallGeminiAsync(effectivePrompt, base64, mimeType);
        if (json == null) return null;

        var result = ParsePalmJson(json);

        // Lock premium fields for free users
        if (!isPremiumUser)
        {
            result.FateLine.IsLocked = true;
            result.FateLine.Summary = _localizer.Get("FateLineLocked");
            result.FateLine.Insights = new() { "🔒 Unlock with Premium", "🔒 Career & Destiny Insights" };

            result.MountJupiter.IsLocked = true;
            result.MountJupiter.Summary = _localizer.Get("MountLocked");
            result.MountVenus.IsLocked = true;
            result.MountVenus.Summary = _localizer.Get("MountLocked");
            result.MountSun.IsLocked = true;
            result.MountSun.Summary = _localizer.Get("MountLocked");
        }

        return result;
    }

    public async Task<FaceAnalysisDto?> AnalyzeFaceAsync(Stream imageStream, string mimeType, bool isPremiumUser)
    {
        if (!isPremiumUser)
        {
            // Face scan is premium only
            return new FaceAnalysisDto
            {
                Forehead = new() { IsLocked = true, Summary = _localizer.Get("FaceReadingLocked") },
                Eyebrows = new() { IsLocked = true, Summary = _localizer.Get("MountLocked") },
                Eyes = new() { IsLocked = true, Summary = _localizer.Get("MountLocked") },
                Nose = new() { IsLocked = true, Summary = _localizer.Get("MountLocked") },
                LipsAndMouth = new() { IsLocked = true, Summary = _localizer.Get("MountLocked") },
                JawAndChin = new() { IsLocked = true, Summary = _localizer.Get("MountLocked") }
            };
        }

        var base64 = await StreamToBase64Async(imageStream);
        var effectivePrompt = _facePrompt + GetLanguageInstruction();
        var json = await CallGeminiAsync(effectivePrompt, base64, mimeType);
        if (json == null) return null;

        return ParseFaceJson(json);
    }

    private async Task<string?> CallGeminiAsync(string prompt, string base64Image, string mimeType)
    {
        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = prompt },
                        new { inline_data = new { mime_type = mimeType, data = base64Image } }
                    }
                }
            },
            generationConfig = new
            {
                responseMimeType = "application/json",
                temperature = 0.4
            }
        };

        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";
        var response = await _httpClient.PostAsJsonAsync(endpoint, payload);

        if (!response.IsSuccessStatusCode) return null;

        var responseBody = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseBody);
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return text;
    }

    private static async Task<string> StreamToBase64Async(Stream stream)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return Convert.ToBase64String(ms.ToArray());
    }

    private static PalmAnalysisDto ParsePalmJson(string json)
    {
        try
        {
            // Clean JSON markers if present
            json = json.Trim().TrimStart('`').TrimEnd('`');
            if (json.StartsWith("json")) json = json[4..].Trim();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new PalmAnalysisDto
            {
                LifeLine = ParseLine(root, "lifeLine"),
                HeadLine = ParseLine(root, "headLine"),
                HeartLine = ParseLine(root, "heartLine"),
                FateLine = ParseLine(root, "fateLine"),
                MountJupiter = ParseLine(root, "mountJupiter"),
                MountVenus = ParseLine(root, "mountVenus"),
                MountSun = ParseLine(root, "mountSun"),
            };
        }
        catch
        {
            return new PalmAnalysisDto
            {
                LifeLine = new() { Summary = "Analysis complete. Please try again for detailed results.", Strength = "Moderate", Insights = new() { "Palm analysis done", "Review your Life Line" } }
            };
        }
    }

    private static PalmLineDto ParseLine(JsonElement root, string key)
    {
        if (!root.TryGetProperty(key, out var el)) return new PalmLineDto();
        return new PalmLineDto
        {
            Summary = el.TryGetProperty("summary", out var s) ? s.GetString() ?? "" : "",
            Strength = el.TryGetProperty("strength", out var st) ? st.GetString() ?? "Moderate" : "Moderate",
            Insights = el.TryGetProperty("insights", out var ins)
                ? ins.EnumerateArray().Select(x => x.GetString() ?? "").ToList()
                : new List<string>()
        };
    }

    private static FaceAnalysisDto ParseFaceJson(string json)
    {
        try
        {
            json = json.Trim().TrimStart('`').TrimEnd('`');
            if (json.StartsWith("json")) json = json[4..].Trim();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new FaceAnalysisDto
            {
                Forehead = ParseZone(root, "forehead"),
                Eyebrows = ParseZone(root, "eyebrows"),
                Eyes = ParseZone(root, "eyes"),
                Nose = ParseZone(root, "nose"),
                LipsAndMouth = ParseZone(root, "lipsAndMouth"),
                JawAndChin = ParseZone(root, "jawAndChin"),
            };
        }
        catch
        {
            return new FaceAnalysisDto();
        }
    }

    private static FaceZoneDto ParseZone(JsonElement root, string key)
    {
        if (!root.TryGetProperty(key, out var el)) return new FaceZoneDto();
        return new FaceZoneDto
        {
            Summary = el.TryGetProperty("summary", out var s) ? s.GetString() ?? "" : "",
            Trait = el.TryGetProperty("trait", out var t) ? t.GetString() ?? "" : "",
            Insight = el.TryGetProperty("insight", out var i) ? i.GetString() ?? "" : "",
        };
    }
}
