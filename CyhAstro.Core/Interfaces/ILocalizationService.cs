namespace CyhAstro.Core.Interfaces;

/// <summary>
/// Centralized Localization Service
/// Admin can configure system-wide active language: "Hindi", "English", or "Hinglish".
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Gets active language (Default: "Hinglish", or "Hindi", "English")
    /// </summary>
    string CurrentLanguage { get; }

    /// <summary>
    /// Changes or warms active language in cache
    /// </summary>
    void SetLanguage(string language);

    /// <summary>
    /// Gets localized message for the current active language.
    /// </summary>
    string Get(string key, params object[] args);

    /// <summary>
    /// Gets localized message for a specific language override.
    /// </summary>
    string GetForLanguage(string language, string key, params object[] args);
}
