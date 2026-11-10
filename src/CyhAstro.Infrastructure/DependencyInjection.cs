using CyhAstro.Core.Interfaces;
using CyhAstro.Infrastructure.Data;
using CyhAstro.Infrastructure.Repositories;
using CyhAstro.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CyhAstro.Infrastructure;

/// <summary>
/// Infrastructure layer ka single entry point for DI registration.
/// API aur Admin dono sirf ye ek method call karenge: services.AddInfrastructure(config)
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── 1. SQL Connection Factory ─────────────────────────────────
        var connStr = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddSingleton<ISqlConnectionFactory>(_ => new SqlConnectionFactory(connStr));

        // ── 2. Repositories (Repository Pattern) ──────────────────────
        // Interface → Implementation mapping
        // Controller/PageModel sirf Interface dekhega, concrete class nahi
        services.AddScoped<IUserRepository,         UserRepository>();
        services.AddScoped<IScanRepository,         ScanRepository>();
        services.AddScoped<IKundliRepository,       KundliRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IAdminRepository,        AdminRepository>();
        services.AddScoped<IAppVersionRepository,   AppVersionRepository>();

        // ── 3. AI & Domain Services ───────────────────────────────────
        services.AddSingleton<IVedicAstroService,   VedicAstroService>();

        // Gemini Vision — HttpClient factory se banayenge
        services.AddHttpClient("GeminiClient", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddScoped<IGeminiVisionService>(sp =>
        {
            var config    = sp.GetRequiredService<IConfiguration>();
            var factory   = sp.GetRequiredService<IHttpClientFactory>();
            var http      = factory.CreateClient("GeminiClient");
            var apiKey    = config["GeminiSettings:ApiKey"]
                            ?? throw new InvalidOperationException("GeminiSettings:ApiKey is not configured.");

            // Prompts DB se load hoti hain (admin configure karta hai)
            // Fallback: hardcoded defaults
            var palmPrompt = """
                You are an expert Vedic Palmistry (Hast Rekha) analyst.
                Analyze the given palm image carefully.
                Identify: 1) Life Line 2) Head Line 3) Heart Line 4) Fate Line
                5) Mount of Jupiter 6) Mount of Venus 7) Mount of Sun.
                For each: summary (1 sentence), strength (Weak/Moderate/Strong), insights (array of 2 strings).
                Respond ONLY in valid JSON with keys: lifeLine, headLine, heartLine, fateLine,
                mountJupiter, mountVenus, mountSun.
                """;

            var facePrompt = """
                You are an expert in Samudrik Shastra (Indian Face Reading / Physiognomy).
                Analyze the given frontal face image.
                Examine: 1) Forehead 2) Eyebrows 3) Eyes 4) Nose 5) Lips & Mouth 6) Jaw & Chin.
                For each: summary (string), trait (positive personality trait), insight (key life insight).
                Respond ONLY in valid JSON with keys: forehead, eyebrows, eyes, nose, lipsAndMouth, jawAndChin.
                """;

            return new GeminiVisionService(http, apiKey, palmPrompt, facePrompt);
        });

        return services;
    }
}
