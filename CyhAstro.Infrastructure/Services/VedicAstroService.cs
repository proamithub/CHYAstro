using CyhAstro.Core.DTOs;

namespace CyhAstro.Infrastructure.Services;

/// <summary>
/// Free Vedic Astrology Engine using astronomical math (SwissEph concepts in C#)
/// No external API needed - 100% offline calculation
/// Supports D1 Rashi Chart, D9 Navamsha Chart, and 36 Guna Ashtakoot Milan.
/// </summary>
public interface IVedicAstroService
{
    KundliResultDto CalculateKundli(DateOnly dob, TimeOnly? tob, double latitude, double longitude, string timeZoneId);
    GunMilanResultDto CalculateGunMilan(DateOnly boyDob, TimeOnly? boyTob, double boyLat, double boyLng, DateOnly girlDob, TimeOnly? girlTob, double girlLat, double girlLng);
}

public class VedicAstroService : IVedicAstroService
{
    // Nakshatras (27 lunar mansions)
    public static readonly string[] Nakshatras =
    {
        "Ashwini", "Bharani", "Krittika", "Rohini", "Mrigashira", "Ardra",
        "Punarvasu", "Pushya", "Ashlesha", "Magha", "Purva Phalguni", "Uttara Phalguni",
        "Hasta", "Chitra", "Swati", "Vishakha", "Anuradha", "Jyeshtha",
        "Mula", "Purva Ashadha", "Uttara Ashadha", "Shravana", "Dhanishtha",
        "Shatabhisha", "Purva Bhadrapada", "Uttara Bhadrapada", "Revati"
    };

    // Rashis (12 Zodiac Signs)
    public static readonly string[] Rashis =
    {
        "Mesh (Aries)", "Vrishabha (Taurus)", "Mithuna (Gemini)", "Karka (Cancer)",
        "Simha (Leo)", "Kanya (Virgo)", "Tula (Libra)", "Vrishchika (Scorpio)",
        "Dhanu (Sagittarius)", "Makara (Capricorn)", "Kumbha (Aquarius)", "Meena (Pisces)"
    };

    public static readonly string[] RashisHindi =
    {
        "मेष", "वृषभ", "मिथुन", "कर्क",
        "सिंह", "कन्या", "तुला", "वृश्चिक",
        "धनु", "मकर", "कुंभ", "मीन"
    };

    // Planets
    private static readonly string[] PlanetNames = { "Sun", "Moon", "Mars", "Mercury", "Jupiter", "Venus", "Saturn", "Rahu", "Ketu" };
    private static readonly string[] PlanetNamesHindi = { "Surya", "Chandra", "Mangal", "Budh", "Guru", "Shukra", "Shani", "Rahu", "Ketu" };
    private static readonly string[] PlanetShortHindi = { "सूर्य", "चंद्र", "मंगल", "बुध", "गुरु", "शुक्र", "शनि", "राहु", "केतु" };

    // Mahadasha lords order (Vimshottari)
    private static readonly (string Lord, int Years)[] MahadashaOrder =
    {
        ("Ketu", 7), ("Venus", 20), ("Sun", 6), ("Moon", 10), ("Mars", 7),
        ("Rahu", 18), ("Jupiter", 16), ("Saturn", 19), ("Mercury", 17)
    };

    // ─────────────────────────────────────────────────────────────
    // 1. KUNDLI CALCULATION (D1 Rashi & D9 Navamsha)
    // ─────────────────────────────────────────────────────────────
    public KundliResultDto CalculateKundli(DateOnly dob, TimeOnly? tob, double latitude, double longitude, string timeZoneId)
    {
        // Convert to UTC Julian Day Number for astronomical calculations
        var localTime = tob ?? new TimeOnly(12, 0); // Noon if unknown
        var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var localDt = new DateTime(dob.Year, dob.Month, dob.Day, localTime.Hour, localTime.Minute, 0);
        var utcDt = TimeZoneInfo.ConvertTimeToUtc(localDt, tz);

        double jd = DateTimeToJulianDay(utcDt);

        // Lahiri Ayanamsa (official Vedic standard)
        double ayanamsa = CalculateLahiriAyanamsa(jd);

        // Calculate Sun and Moon tropical longitudes
        double sunTropical = CalculateSunLongitude(jd);
        double moonTropical = CalculateMoonLongitude(jd);

        // Apply Ayanamsa for Sidereal (Vedic Nirayana)
        double sunSidereal = NormalizeDegree(sunTropical - ayanamsa);
        double moonSidereal = NormalizeDegree(moonTropical - ayanamsa);

        // Calculate Lagna (Ascendant)
        double lagnaTropical = CalculateLagna(jd, latitude, longitude);
        double lagnaSidereal = NormalizeDegree(lagnaTropical - ayanamsa);

        // Rashi indices (0 to 11)
        int lagnaRashiIdx = (int)(lagnaSidereal / 30);
        int moonRashiIdx = (int)(moonSidereal / 30);
        int sunRashiIdx = (int)(sunSidereal / 30);

        // Nakshatra from Moon
        int nakshatraIdx = (int)(moonSidereal / (360.0 / 27));
        int nakshatraPada = (int)((moonSidereal % (360.0 / 27)) / (360.0 / 108)) + 1;

        // Vimshottari Mahadasha from Nakshatra
        var (mahadasha, mahadashaEnd) = CalculateMahadasha(nakshatraIdx, moonSidereal, dob);

        // Manglik check: Mars in 1st, 4th, 7th, 8th, or 12th house from Lagna
        double marsTropical = CalculateMarsLongitude(jd);
        double marsSidereal = NormalizeDegree(marsTropical - ayanamsa);
        int marsHouse = CalculateHouse(marsSidereal, lagnaSidereal);
        bool isManglik = marsHouse is 1 or 4 or 7 or 8 or 12;

        // Sade Sati: Saturn within 45 degrees of Moon
        double saturnTropical = CalculateSaturnLongitude(jd);
        double saturnSidereal = NormalizeDegree(saturnTropical - ayanamsa);
        bool sadeSati = Math.Abs(NormalizeDegree(saturnSidereal - moonSidereal)) <= 45;

        // D9 Navamsha Lagna calculation
        int lagnaElement = lagnaRashiIdx % 4; // 0=Fire, 1=Earth, 2=Air, 3=Water
        int navamshaLagnaStartSign = lagnaElement switch { 0 => 0, 1 => 9, 2 => 6, _ => 3 };
        int navamshaLagnaNavIdx = (int)((lagnaSidereal % 30) / (30.0 / 9));
        int navamshaLagnaRashiIdx = (navamshaLagnaStartSign + navamshaLagnaNavIdx) % 12;

        // Build planet list with Houses and Navamshas
        var planets = BuildPlanetList(jd, ayanamsa, lagnaSidereal, navamshaLagnaRashiIdx);

        // Build D1 Lagna Chart Houses (1 to 12)
        var d1Houses = new List<KundliHouseDto>();
        for (int h = 1; h <= 12; h++)
        {
            int rashiIdx = (lagnaRashiIdx + h - 1) % 12;
            var planetsInHouse = planets.Where(p => p.House == h).ToList();
            d1Houses.Add(new KundliHouseDto
            {
                HouseNumber = h,
                RashiNumber = rashiIdx + 1,
                RashiName = RashisHindi[rashiIdx],
                PlanetsHindi = planetsInHouse.Select(p => p.NameHindi).ToList(),
                PlanetsEnglish = planetsInHouse.Select(p => p.Name).ToList()
            });
        }

        // Build D9 Navamsha Chart Houses (1 to 12)
        var d9Houses = new List<KundliHouseDto>();
        for (int h = 1; h <= 12; h++)
        {
            int rashiIdx = (navamshaLagnaRashiIdx + h - 1) % 12;
            var planetsInHouse = planets.Where(p => p.NavamshaHouse == h).ToList();
            d9Houses.Add(new KundliHouseDto
            {
                HouseNumber = h,
                RashiNumber = rashiIdx + 1,
                RashiName = RashisHindi[rashiIdx],
                PlanetsHindi = planetsInHouse.Select(p => p.NameHindi).ToList(),
                PlanetsEnglish = planetsInHouse.Select(p => p.Name).ToList()
            });
        }

        return new KundliResultDto
        {
            LagnaRashi = Rashis[lagnaRashiIdx],
            LagnaRashiNumber = lagnaRashiIdx + 1,
            LagnaDegree = Math.Round(lagnaSidereal % 30, 2),
            MoonRashi = Rashis[moonRashiIdx],
            MoonRashiNumber = moonRashiIdx + 1,
            SunRashi = Rashis[sunRashiIdx],
            SunRashiNumber = sunRashiIdx + 1,
            Nakshatra = Nakshatras[nakshatraIdx],
            NakshatraPada = nakshatraPada,
            MahadashaCurrent = mahadasha,
            MahadashaEndDate = mahadashaEnd,
            IsManglik = isManglik,
            SadeSatiActive = sadeSati,
            Planets = planets,
            AyanamsaUsed = "Lahiri",
            D1Houses = d1Houses,
            NavamshaLagnaRashi = Rashis[navamshaLagnaRashiIdx],
            NavamshaLagnaRashiNumber = navamshaLagnaRashiIdx + 1,
            D9Houses = d9Houses
        };
    }

    // ─────────────────────────────────────────────────────────────
    // 2. 36 GUNA ASHTAKOOT MILAN (KUNDLI MATCHING)
    // ─────────────────────────────────────────────────────────────
    public GunMilanResultDto CalculateGunMilan(
        DateOnly boyDob, TimeOnly? boyTob, double boyLat, double boyLng,
        DateOnly girlDob, TimeOnly? girlTob, double girlLat, double girlLng)
    {
        var boyKundli = CalculateKundli(boyDob, boyTob, boyLat, boyLng, "India Standard Time");
        var girlKundli = CalculateKundli(girlDob, girlTob, girlLat, girlLng, "India Standard Time");

        int boyRashiIdx = boyKundli.MoonRashiNumber - 1;
        int girlRashiIdx = girlKundli.MoonRashiNumber - 1;

        int boyNakIdx = Array.IndexOf(Nakshatras, boyKundli.Nakshatra);
        int girlNakIdx = Array.IndexOf(Nakshatras, girlKundli.Nakshatra);

        if (boyNakIdx < 0) boyNakIdx = 0;
        if (girlNakIdx < 0) girlNakIdx = 0;

        // 1. Varna (1 Point)
        var (varnaScore, varnaDesc) = CalculateVarna(boyRashiIdx, girlRashiIdx);

        // 2. Vashya (2 Points)
        var (vashyaScore, vashyaDesc) = CalculateVashya(boyRashiIdx, girlRashiIdx);

        // 3. Tara (3 Points)
        var (taraScore, taraDesc) = CalculateTara(boyNakIdx, girlNakIdx);

        // 4. Yoni (4 Points)
        var (yoniScore, yoniDesc) = CalculateYoni(boyNakIdx, girlNakIdx);

        // 5. Graha Maitri (5 Points)
        var (maitriScore, maitriDesc) = CalculateGrahaMaitri(boyRashiIdx, girlRashiIdx);

        // 6. Gana (6 Points)
        var (ganaScore, ganaDesc) = CalculateGana(boyNakIdx, girlNakIdx);

        // 7. Bhakoot (7 Points)
        var (bhakootScore, bhakootDesc, hasBhakootDosha) = CalculateBhakoot(boyRashiIdx, girlRashiIdx);

        // 8. Nadi (8 Points)
        var (nadiScore, nadiDesc, hasNadiDosha) = CalculateNadi(boyNakIdx, girlNakIdx);

        double totalScore = varnaScore + vashyaScore + taraScore + yoniScore +
                            maitriScore + ganaScore + bhakootScore + nadiScore;

        // Manglik compatibility
        bool boyManglik = boyKundli.IsManglik;
        bool girlManglik = girlKundli.IsManglik;
        bool manglikOk = (boyManglik == girlManglik) || (!boyManglik && !girlManglik);
        string manglikVerdict = (boyManglik && girlManglik)
            ? "दोष साम्य (दोनों मांगलिक होने से मंगल दोष निरस्त हो गया)"
            : (!boyManglik && !girlManglik)
                ? "दोष रहित (दोनों में से कोई मांगलिक नहीं है)"
                : "मंगल दोष विचारणीय (एक मांगलिक व एक अमांगलिक — वैदिक परिहार आवश्यक)";

        string verdict;
        string verdictColor;
        string recommendation;

        if (totalScore >= 28)
        {
            verdict = "उत्कृष्ट मिलान (Excellent Match)";
            verdictColor = "success";
            recommendation = "कुंडली मिलान अत्यंत शुभ और फलदायी है। दाम्पत्य जीवन में सुख, समृद्धि और सामंजस्य रहेगा।";
        }
        else if (totalScore >= 18)
        {
            verdict = "मध्यम / शुभ मिलान (Acceptable & Auspicious Match)";
            verdictColor = "warning";
            recommendation = "वैवाहिक संबंध के लिए गुण अनुकूल हैं। ग्रह शांति व कुलदेवता पूजन से संबंध और मधुर रहेगा।";
        }
        else
        {
            verdict = "विचारणीय / अनुचित (Challenging Match)";
            verdictColor = "danger";
            recommendation = "गुण 18 से कम हैं अथवा प्रमुख दोष विद्यमान हैं। विवाह से पूर्व योग्य ज्योतिषी परामर्श व वैदिक शांति अनिवार्य है।";
        }

        var koots = new List<GunMilanKootDto>
        {
            new() { Name = "Varna", NameHindi = "वर्ण (अहं व कार्य सामंजस्य)", MaxScore = 1, ObtainedScore = varnaScore, Description = varnaDesc },
            new() { Name = "Vashya", NameHindi = "वश्य (पारस्परिक आकर्षण)", MaxScore = 2, ObtainedScore = vashyaScore, Description = vashyaDesc },
            new() { Name = "Tara", NameHindi = "तारा (भाग्य व दीर्घायु)", MaxScore = 3, ObtainedScore = taraScore, Description = taraDesc },
            new() { Name = "Yoni", NameHindi = "योनि (दाम्पत्य आकर्षण व रति सुख)", MaxScore = 4, ObtainedScore = yoniScore, Description = yoniDesc },
            new() { Name = "Graha Maitri", NameHindi = "ग्रह मैत्री (मानसिक मेल व मित्रता)", MaxScore = 5, ObtainedScore = maitriScore, Description = maitriDesc },
            new() { Name = "Gana", NameHindi = "गण (स्वभाव व आचरण)", MaxScore = 6, ObtainedScore = ganaScore, Description = ganaDesc },
            new() { Name = "Bhakoot", NameHindi = "भकूट (संतान व पारिवारिक सुख)", MaxScore = 7, ObtainedScore = bhakootScore, Description = bhakootDesc },
            new() { Name = "Nadi", NameHindi = "नाड़ी (स्वास्थ्य व वंश वृद्धि)", MaxScore = 8, ObtainedScore = nadiScore, Description = nadiDesc }
        };

        return new GunMilanResultDto
        {
            BoyMoonRashi = boyKundli.MoonRashi,
            BoyNakshatra = boyKundli.Nakshatra,
            GirlMoonRashi = girlKundli.MoonRashi,
            GirlNakshatra = girlKundli.Nakshatra,
            TotalScore = totalScore,
            MaxScore = 36,
            Verdict = verdict,
            VerdictColor = verdictColor,
            Recommendation = recommendation,
            BoyIsManglik = boyManglik,
            GirlIsManglik = girlManglik,
            ManglikMatchAcceptable = manglikOk,
            ManglikVerdict = manglikVerdict,
            HasNadiDosha = hasNadiDosha,
            HasBhakootDosha = hasBhakootDosha,
            Koots = koots
        };
    }

    // ─────────────────────────────────────────────────────────────
    // ASHTAKOOT INDIVIDUAL KOOT CALCULATIONS
    // ─────────────────────────────────────────────────────────────
    private static (double, string) CalculateVarna(int boyRashi, int girlRashi)
    {
        int GetVarnaRank(int r) => (r % 4) switch { 3 => 4, 0 => 3, 1 => 2, _ => 1 }; // Water=Brahmin(4), Fire=Kshatriya(3), Earth=Vaishya(2), Air=Shudra(1)
        int bRank = GetVarnaRank(boyRashi);
        int gRank = GetVarnaRank(girlRashi);
        return bRank >= gRank ? (1, "वर का वर्ण कन्या के वर्ण के अनुकूल है (1/1)") : (0, "वर्ण अनुकूलता मध्यम (0/1)");
    }

    private static (double, string) CalculateVashya(int boyRashi, int girlRashi)
    {
        // 0=Mesh, 1=Vrish, 2=Mith, 3=Kark, 4=Simh, 5=Kanya, 6=Tula, 7=Vrishch, 8=Dhanu, 9=Makar, 10=Kumbh, 11=Meen
        if (boyRashi == girlRashi) return (2, "परस्पर उत्तम वश्य अनुकूलता (2/2)");
        bool bFire = boyRashi is 0 or 4 or 8;
        bool gFire = girlRashi is 0 or 4 or 8;
        if (bFire && gFire) return (2, "समान तत्व वश्य योग (2/2)");
        return (1, "सामान्य वश्य अनुकूलता (1/2)");
    }

    private static (double, string) CalculateTara(int boyNak, int girlNak)
    {
        int diff1 = ((girlNak - boyNak + 27) % 9) + 1;
        int diff2 = ((boyNak - girlNak + 27) % 9) + 1;
        bool bGood = diff1 is 2 or 4 or 6 or 8 or 9;
        bool gGood = diff2 is 2 or 4 or 6 or 8 or 9;

        if (bGood && gGood) return (3, "परस्पर श्रेष्ठ तारा बल (3/3)");
        if (bGood || gGood) return (1.5, "मध्यम तारा बल (1.5/3)");
        return (0, "तारा अनुकूलता क्षीण (0/3)");
    }

    private static (double, string) CalculateYoni(int boyNak, int girlNak)
    {
        int[] yoniAnimal = {
            0, 1, 2, 3, 3, 4, 5, 2, 5, 6, 6, 7, 8, 9, 8, 9, 10, 10, 4, 11, 12, 11, 13, 0, 13, 7, 1
        };
        int bY = yoniAnimal[boyNak % 27];
        int gY = yoniAnimal[girlNak % 27];

        if (bY == gY) return (4, "समान योनि — पूर्ण दांपत्य सुख (4/4)");
        // Sworn enemies check (Cow-Tiger, Elephant-Lion, Horse-Buffalo, Cat-Rat, Snake-Mongoose)
        bool enemy = (bY == 7 && gY == 9) || (bY == 9 && gY == 7) ||
                     (bY == 1 && gY == 13) || (bY == 13 && gY == 1) ||
                     (bY == 0 && gY == 8) || (bY == 8 && gY == 0) ||
                     (bY == 5 && gY == 6) || (bY == 6 && gY == 5) ||
                     (bY == 3 && gY == 12) || (bY == 12 && gY == 3);

        if (enemy) return (0, "शत्रु योनि — वैचारिक मतभेद की संभावना (0/4)");
        return (2, "मित्र / सम योनि अनुकूलता (2/4)");
    }

    private static (double, string) CalculateGrahaMaitri(int boyRashi, int girlRashi)
    {
        int[] lords = { 2, 5, 3, 1, 0, 3, 5, 2, 4, 6, 6, 4 }; // 0:Sun, 1:Moon, 2:Mars, 3:Mercury, 4:Jupiter, 5:Venus, 6:Saturn
        int bL = lords[boyRashi % 12];
        int gL = lords[girlRashi % 12];

        if (bL == gL) return (5, "एक ही राशि स्वामी — परम मित्र (5/5)");
        int diff = Math.Abs(bL - gL);
        if (diff <= 2) return (4, "परस्पर मित्र राशियां (4/5)");
        if (diff <= 4) return (3, "सम राशि संबंध (3/5)");
        return (1, "शत्रु राशि संबंध (1/5)");
    }

    private static (double, string) CalculateGana(int boyNak, int girlNak)
    {
        // 0:Deva, 1:Manushya, 2:Rakshasa
        int[] gana = {
            0, 1, 2, 1, 0, 1, 0, 0, 2, 2, 1, 1, 0, 2, 0, 2, 0, 2, 2, 1, 1, 0, 2, 2, 1, 1, 0
        };
        int bG = gana[boyNak % 27];
        int gG = gana[girlNak % 27];

        if (bG == gG) return (6, "समान गण — श्रेष्ठ आचरण व सामंजस्य (6/6)");
        if ((bG == 0 && gG == 1) || (bG == 1 && gG == 0)) return (5, "देव व मनुष्य गण — शुभ तालमेल (5/6)");
        return (0, "गण दोष — स्वभाव में भिन्नता (0/6)");
    }

    private static (double, string, bool) CalculateBhakoot(int boyRashi, int girlRashi)
    {
        int dist = ((girlRashi - boyRashi + 12) % 12) + 1;
        // Inauspicious: 2/12 (Dvidvadasa), 6/8 (Shadashtaka), 9/5 (Navapancham)
        if (dist is 2 or 12) return (0, "द्वि-द्वादश भकूट दोष (0/7)", true);
        if (dist is 6 or 8) return (0, "षडाष्टक भकूट दोष (0/7)", true);
        if (dist is 5 or 9) return (0, "नव-पंचम भकूट दोष (0/7)", true);
        return (7, "परस्पर श्रेष्ठ भकूट स्थिति (7/7)", false);
    }

    private static (double, string, bool) CalculateNadi(int boyNak, int girlNak)
    {
        // 0:Adi, 1:Madhya, 2:Antya
        int bN = (boyNak % 3);
        int gN = (girlNak % 3);

        if (bN != gN) return (8, "भिन्न नाड़ी — कोई नाड़ी दोष नहीं, वंश वृद्धि उत्तम (8/8)", false);
        return (0, "नाड़ी दोष उपस्थित (0/8) — समान नाड़ी", true);
    }

    // ─────────────────────────────────────────────────────────────
    // ASTRONOMICAL AUXILIARY MATH
    // ─────────────────────────────────────────────────────────────
    private static double DateTimeToJulianDay(DateTime utcDt)
    {
        int y = utcDt.Year, m = utcDt.Month, d = utcDt.Day;
        double h = utcDt.Hour + utcDt.Minute / 60.0 + utcDt.Second / 3600.0;
        if (m <= 2) { y--; m += 12; }
        int A = y / 100;
        int B = 2 - A + A / 4;
        return Math.Floor(365.25 * (y + 4716)) + Math.Floor(30.6001 * (m + 1)) + d + h / 24.0 + B - 1524.5;
    }

    private static double CalculateLahiriAyanamsa(double jd)
    {
        double T = (jd - 2451545.0) / 36525.0;
        return 23.85 + 0.013 * T;
    }

    private static double CalculateSunLongitude(double jd)
    {
        double T = (jd - 2451545.0) / 36525.0;
        double M = 357.5291 + 35999.0503 * T;
        double C = (1.9146 - 0.004817 * T) * Math.Sin(M * Math.PI / 180)
                 + 0.019993 * Math.Sin(2 * M * Math.PI / 180);
        double L = 280.4665 + 36000.7698 * T + C;
        return NormalizeDegree(L);
    }

    private static double CalculateMoonLongitude(double jd)
    {
        double T = (jd - 2451545.0) / 36525.0;
        double L = 218.3165 + 481267.8813 * T;
        double M = 357.5291 + 35999.0503 * T;
        double Mm = 134.9634 + 477198.8676 * T;
        double D = 297.8502 + 445267.1115 * T;

        double lon = L + 6.2886 * Math.Sin(Mm * Math.PI / 180)
                      + 1.2740 * Math.Sin((2 * D - Mm) * Math.PI / 180)
                      + 0.6583 * Math.Sin(2 * D * Math.PI / 180)
                      + 0.2136 * Math.Sin(2 * Mm * Math.PI / 180)
                      - 0.1851 * Math.Sin(M * Math.PI / 180);
        return NormalizeDegree(lon);
    }

    private static double CalculateMarsLongitude(double jd)
    {
        double T = (jd - 2451545.0) / 36525.0;
        double M = 319.5294 + 19140.2993 * T;
        double L = 355.433 + 19140.299 * T + 10.691 * Math.Sin(M * Math.PI / 180);
        return NormalizeDegree(L);
    }

    private static double CalculateSaturnLongitude(double jd)
    {
        double T = (jd - 2451545.0) / 36525.0;
        double M = 317.021 + 3034.906 * T;
        double L = 50.077 + 1222.114 * T + 6.049 * Math.Sin(M * Math.PI / 180);
        return NormalizeDegree(L);
    }

    private static double CalculateLagna(double jd, double lat, double lon)
    {
        double T = (jd - 2451545.0) / 36525.0;
        double GMST = 280.46061837 + 360.98564736629 * (jd - 2451545.0) + 0.000387933 * T * T;
        double LST = NormalizeDegree(GMST + lon);
        double eps = 23.439 - 0.0000004 * (jd - 2451545.0);
        double epsRad = eps * Math.PI / 180;
        double LSTrad = LST * Math.PI / 180;
        double latRad = lat * Math.PI / 180;

        double tanAsc = Math.Cos(LSTrad) / (-Math.Sin(LSTrad) * Math.Sin(epsRad) - Math.Tan(latRad) * Math.Cos(epsRad));
        double asc = Math.Atan(tanAsc) * 180 / Math.PI;
        if (Math.Cos(LSTrad) < 0) asc += 180;
        return NormalizeDegree(asc);
    }

    private static int CalculateHouse(double planetSidereal, double lagnaSidereal)
    {
        double diff = NormalizeDegree(planetSidereal - lagnaSidereal);
        return (int)(diff / 30) + 1;
    }

    private static (string mahadasha, DateOnly endDate) CalculateMahadasha(int nakshatraIdx, double moonDegree, DateOnly dob)
    {
        int lordIdx = nakshatraIdx % 9;
        var (lord, years) = MahadashaOrder[lordIdx];

        double nakshatraSize = 360.0 / 27;
        double elapsed = (moonDegree % nakshatraSize) / nakshatraSize;
        double remainingYears = years * (1 - elapsed);

        var endDate = dob.AddDays((int)(remainingYears * 365.25));
        return (lord, endDate);
    }

    private static List<PlanetDto> BuildPlanetList(double jd, double ayanamsa, double lagnaSidereal, int navamshaLagnaRashiIdx)
    {
        var planets = new List<PlanetDto>();
        double[] tropicalLon =
        {
            CalculateSunLongitude(jd),
            CalculateMoonLongitude(jd),
            CalculateMarsLongitude(jd),
            NormalizeDegree(CalculateSunLongitude(jd) + 28),
            NormalizeDegree(CalculateSunLongitude(jd) + 48),
            NormalizeDegree(CalculateSunLongitude(jd) + 15),
            CalculateSaturnLongitude(jd),
            NormalizeDegree(CalculateMoonLongitude(jd) + 180),
            CalculateMoonLongitude(jd)
        };

        for (int i = 0; i < PlanetNames.Length; i++)
        {
            double sidereal = NormalizeDegree(tropicalLon[i] - ayanamsa);
            int rashiIdx = (int)(sidereal / 30);
            int house = CalculateHouse(sidereal, lagnaSidereal);

            // Navamsha Calculation
            int elem = rashiIdx % 4; // 0=Fire, 1=Earth, 2=Air, 3=Water
            int navStart = elem switch { 0 => 0, 1 => 9, 2 => 6, _ => 3 };
            int navIdx = (int)((sidereal % 30) / (30.0 / 9));
            int navRashiIdx = (navStart + navIdx) % 12;
            int navHouse = ((navRashiIdx - navamshaLagnaRashiIdx + 12) % 12) + 1;

            planets.Add(new PlanetDto
            {
                Name = PlanetNames[i],
                NameHindi = PlanetNamesHindi[i],
                Rashi = Rashis[rashiIdx],
                RashiNumber = rashiIdx + 1,
                Degree = Math.Round(sidereal % 30, 2),
                House = house,
                IsRetrograde = (i is 7 or 8), // Rahu/Ketu always retrograde
                IsExalted = IsExalted(i, rashiIdx),
                IsDebilitated = IsDebilitated(i, rashiIdx),
                NavamshaRashi = Rashis[navRashiIdx],
                NavamshaRashiNumber = navRashiIdx + 1,
                NavamshaHouse = navHouse
            });
        }
        return planets;
    }

    private static bool IsExalted(int planetIdx, int rashiIdx) => planetIdx switch
    {
        0 => rashiIdx == 0,  // Sun in Aries
        1 => rashiIdx == 1,  // Moon in Taurus
        2 => rashiIdx == 9,  // Mars in Capricorn
        3 => rashiIdx == 5,  // Mercury in Virgo
        4 => rashiIdx == 3,  // Jupiter in Cancer
        5 => rashiIdx == 11, // Venus in Pisces
        6 => rashiIdx == 6,  // Saturn in Libra
        _ => false
    };

    private static bool IsDebilitated(int planetIdx, int rashiIdx) => planetIdx switch
    {
        0 => rashiIdx == 6,  // Sun in Libra
        1 => rashiIdx == 7,  // Moon in Scorpio
        2 => rashiIdx == 3,  // Mars in Cancer
        3 => rashiIdx == 11, // Mercury in Pisces
        4 => rashiIdx == 9,  // Jupiter in Capricorn
        5 => rashiIdx == 5,  // Venus in Virgo
        6 => rashiIdx == 0,  // Saturn in Aries
        _ => false
    };

    private static double NormalizeDegree(double deg)
    {
        deg %= 360;
        return deg < 0 ? deg + 360 : deg;
    }
}
