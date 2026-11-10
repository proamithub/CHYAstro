using CyhAstro.Core.Interfaces;
using CyhAstro.Infrastructure.Data;
using Dapper;

namespace CyhAstro.Infrastructure.Services;

public class LocalizationService : ILocalizationService
{
    private readonly ISqlConnectionFactory _db;
    private string _cachedLanguage = "Hinglish";
    private DateTime _lastChecked = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(2);

    public LocalizationService(ISqlConnectionFactory db)
    {
        _db = db;
    }

    public string CurrentLanguage
    {
        get
        {
            if (DateTime.UtcNow - _lastChecked > CacheDuration)
            {
                RefreshFromDb();
            }
            return _cachedLanguage;
        }
    }

    public void SetLanguage(string language)
    {
        if (string.IsNullOrWhiteSpace(language)) return;
        var normalized = NormalizeLanguage(language);
        _cachedLanguage = normalized;
        _lastChecked = DateTime.UtcNow;
    }

    private void RefreshFromDb()
    {
        try
        {
            using var conn = _db.CreateConnection();
            var val = conn.QueryFirstOrDefault<string>(
                "SELECT SettingValue FROM tbl_AppSettings WHERE SettingKey = 'DefaultLanguage'");
            if (!string.IsNullOrWhiteSpace(val))
            {
                _cachedLanguage = NormalizeLanguage(val);
            }
            _lastChecked = DateTime.UtcNow;
        }
        catch
        {
            // Fallback to current cached or default
            _lastChecked = DateTime.UtcNow;
        }
    }

    private static string NormalizeLanguage(string lang) => lang.Trim().ToLowerInvariant() switch
    {
        "hindi" or "hi" => "Hindi",
        "english" or "en" => "English",
        _ => "Hinglish"
    };

    public string Get(string key, params object[] args)
    {
        return GetForLanguage(CurrentLanguage, key, args);
    }

    public string GetForLanguage(string language, string key, params object[] args)
    {
        var lang = NormalizeLanguage(language);

        if (!Catalog.TryGetValue(key, out var translations))
        {
            return args.Length > 0 ? string.Format(key, args) : key;
        }

        string template = lang switch
        {
            "Hindi" => translations.Hindi,
            "English" => translations.English,
            _ => translations.Hinglish
        };

        try
        {
            return args.Length > 0 ? string.Format(template, args) : template;
        }
        catch
        {
            return template;
        }
    }

    // Comprehensive Tri-Lingual Dictionary: Hinglish, Hindi, English
    private static readonly Dictionary<string, (string Hinglish, string Hindi, string English)> Catalog =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["MissingEmailOrPhone"] = (
                "Email ya Phone number zaroori hai.",
                "ईमेल या फ़ोन नंबर आवश्यक है।",
                "Email or phone number is required."
            ),
            ["PasswordMinLength"] = (
                "Password kam se kam 6 characters ka hona chahiye.",
                "पासवर्ड कम से कम 6 अक्षरों का होना चाहिए।",
                "Password must be at least 6 characters long."
            ),
            ["ServerError"] = (
                "Server error. Dobara try karein.",
                "सर्वर त्रुटि। कृपया पुनः प्रयास करें।",
                "Server error. Please try again."
            ),
            ["RegistrationSuccess"] = (
                "Registration successful! Swagat hai CyhAstro mein.",
                "पंजीकरण सफल! CyhAstro में आपका हार्दिक स्वागत है।",
                "Registration successful! Welcome to CyhAstro."
            ),
            ["LoginSuccess"] = (
                "Login successful! Namaste {0}",
                "लॉगिन सफल! नमस्ते {0}",
                "Login successful! Welcome {0}"
            ),
            ["MissingEmailOrPhoneLogin"] = (
                "Email ya Phone zaroori hai.",
                "ईमेल या फ़ोन नंबर आवश्यक है।",
                "Email or Phone is required."
            ),
            ["UserNotFound"] = (
                "User nahi mila. Pehle register karein.",
                "उपयोगकर्ता नहीं मिला। कृपया पहले पंजीकरण करें।",
                "User not found. Please register first."
            ),
            ["AccountBlocked"] = (
                "Aapka account block kar diya gaya hai. Support se contact karein.",
                "आपका खाता ब्लॉक कर दिया गया है। कृपया सहायता टीम से संपर्क करें।",
                "Your account has been blocked. Please contact support."
            ),
            ["InvalidPassword"] = (
                "Password galat hai.",
                "पासवर्ड गलत है।",
                "Invalid password."
            ),
            ["ImageRequired"] = (
                "Image file zaroori hai.",
                "फोटो / छवि फ़ाइल आवश्यक है।",
                "Image file is required."
            ),
            ["InvalidImageFormat"] = (
                "Sirf JPG, PNG ya WEBP images accept hoti hain.",
                "केवल JPG, PNG या WEBP प्रारूप ही स्वीकार्य हैं।",
                "Only JPG, PNG or WEBP images are accepted."
            ),
            ["PalmCreditsExhausted"] = (
                "Palm scan credits khatam ho gaye hain. Premium plan lein unlimited scans ke liye!",
                "हस्तरेखा स्कैन क्रेडिट समाप्त हो गए हैं। असीमित स्कैन के लिए प्रीमियम प्लान लें!",
                "Palm scan credits exhausted. Upgrade to Premium for unlimited scans!"
            ),
            ["FaceCreditsExhausted"] = (
                "Face scan credits nahi hain. Premium plan activate karein.",
                "सामुद्रिक शास्त्र (चेहरा) स्कैन क्रेडिट नहीं हैं। कृपया प्रीमियम प्लान सक्रिय करें।",
                "Face scan credits exhausted. Please activate a premium plan."
            ),
            ["KundliNotCalculated"] = (
                "Kundli abhi calculate nahi ki. Pehle /api/kundli/calculate call karein.",
                "कुंडली अभी परिकलित नहीं की गई है। कृपया पहले जन्म विवरण दर्ज करें।",
                "Kundli has not been calculated yet. Please calculate first."
            ),
            ["PlanActivated"] = (
                "{0} plan successfully activate ho gaya!",
                "{0} प्लान सफलतापूर्वक सक्रिय हो गया है!",
                "{0} plan has been successfully activated!"
            ),
            ["PlanUpgradeError"] = (
                "Plan upgrade mein error aaya. Payment team se contact karein.",
                "प्लान अपग्रेड में त्रुटि आई। कृपया भुगतान सहायता टीम से संपर्क करें।",
                "Error occurred during plan upgrade. Please contact payment support."
            ),
            ["InvalidRequest"] = (
                "Invalid request. Kripya check karein.",
                "अमान्य अनुरोध।",
                "Invalid request."
            ),
            ["AdminEmailPasswordRequired"] = (
                "Email aur Password dono zaroori hain.",
                "ईमेल और पासवर्ड दोनों आवश्यक हैं।",
                "Both email and password are required."
            ),
            ["AdminAccountNotFound"] = (
                "Admin account nahi mila ya access nahi hai.",
                "व्यवस्थापक खाता नहीं मिला या अनुमति नहीं है।",
                "Admin account not found or access denied."
            ),
            ["AdminPricesUpdated"] = (
                "Plan prices update ho gayi!",
                "प्लान मूल्य सफलतापूर्वक अपडेट हो गए!",
                "Plan prices updated successfully!"
            ),
            ["AdminQuotasUpdated"] = (
                "Quotas update ho gayi!",
                "यूजर कोटा सफलतापूर्वक अपडेट हो गया!",
                "Quotas updated successfully!"
            ),
            ["AdminPromptsUpdated"] = (
                "AI Prompts update ho gaye!",
                "AI प्रॉम्प्ट्स सफलतापूर्वक अपडेट हो गए!",
                "AI Prompts updated successfully!"
            ),
            ["AdminLanguageUpdated"] = (
                "System bhasha '{0}' set ho gayi! Sabhi response ab {0} mein aayenge.",
                "सिस्टम भाषा '{0}' सफलतापूर्वक सेट हो गई! अब सभी संदेश {0} में प्रदर्शित होंगे।",
                "System language set to '{0}'! All responses will now be served in {0}."
            ),
            ["AdminVersionPushed"] = (
                "Version {0} ({1}) successfully push ho gaya!",
                "संस्करण {0} ({1}) सफलतापूर्वक जारी कर दिया गया!",
                "Version {0} ({1}) pushed successfully!"
            ),
            ["AdminActionCompleted"] = (
                "Action '{0}' successfully complete hua!",
                "कार्य '{0}' सफलतापूर्वक पूर्ण हुआ!",
                "Action '{0}' completed successfully!"
            ),
            ["FateLineLocked"] = (
                "🔒 Bhagya Rekha (Fate Line) dekhne ke liye Premium plan lein.",
                "🔒 भाग्य रेखा विश्लेषण अनलॉक करने के लिए प्रीमियम प्लान लें।",
                "🔒 Subscribe to unlock your Fate Line (Bhagya Rekha) analysis."
            ),
            ["MountLocked"] = (
                "🔒 Premium Feature - Unlock karne ke liye subscribe karein",
                "🔒 प्रीमियम सुविधा - अनलॉक करने के लिए सदस्यता लें",
                "🔒 Premium Feature - Subscribe to unlock"
            ),
            ["FaceReadingLocked"] = (
                "🔒 Face Reading (Samudrik Shastra) dekhne ke liye Premium plan lein",
                "🔒 सामुद्रिक शास्त्र (चेहरा दर्शन) अनलॉक करने के लिए प्रीमियम प्लान लें",
                "🔒 Subscribe to unlock Face Reading (Samudrik Shastra)"
            ),
            ["LifeLineAnalysisDone"] = (
                "Analysis poora hua. Vistaar se dekhne ke liye dobara check karein.",
                "विश्लेषण पूर्ण हुआ। विस्तृत परिणाम के लिए पुनः देखें।",
                "Analysis complete. Please try again for detailed results."
            ),
            ["CosmicOnline"] = (
                "✦ Cosmic Intelligence Online ✦",
                "✦ दिव्य ज्योतिषीय प्रज्ञा ऑनलाइन ✦",
                "✦ Cosmic Intelligence Online ✦"
            )
        };
}
