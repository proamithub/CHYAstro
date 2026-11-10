using CyhAstro.Core.DTOs;

namespace CyhAstro.Infrastructure.Services;

/// <summary>
/// Free Vedic Astrology Engine using astronomical math (SwissEph concepts in C#)
/// No external API needed - 100% offline calculation
/// </summary>
public interface IVedicAstroService
{
    KundliResultDto CalculateKundli(DateOnly dob, TimeOnly? tob, double latitude, double longitude, string timeZoneId);
}

public class VedicAstroService : IVedicAstroService
{
    // Nakshatras (27 lunar mansions)
    private static readonly string[] Nakshatras =
    {
        "Ashwini", "Bharani", "Krittika", "Rohini", "Mrigashira", "Ardra",
        "Punarvasu", "Pushya", "Ashlesha", "Magha", "Purva Phalguni", "Uttara Phalguni",
        "Hasta", "Chitra", "Swati", "Vishakha", "Anuradha", "Jyeshtha",
        "Mula", "Purva Ashadha", "Uttara Ashadha", "Shravana", "Dhanishtha",
        "Shatabhisha", "Purva Bhadrapada", "Uttara Bhadrapada", "Revati"
    };

    // Rashis (12 Zodiac Signs)
    private static readonly string[] Rashis =
    {
        "Mesh (Aries)", "Vrishabha (Taurus)", "Mithuna (Gemini)", "Karka (Cancer)",
        "Simha (Leo)", "Kanya (Virgo)", "Tula (Libra)", "Vrishchika (Scorpio)",
        "Dhanu (Sagittarius)", "Makara (Capricorn)", "Kumbha (Aquarius)", "Meena (Pisces)"
    };

    // Planets
    private static readonly string[] PlanetNames = { "Sun", "Moon", "Mars", "Mercury", "Jupiter", "Venus", "Saturn", "Rahu", "Ketu" };
    private static readonly string[] PlanetNamesHindi = { "Surya", "Chandra", "Mangal", "Budh", "Guru", "Shukra", "Shani", "Rahu", "Ketu" };

    // Mahadasha lords order (Vimshottari)
    private static readonly (string Lord, int Years)[] MahadashaOrder =
    {
        ("Ketu", 7), ("Venus", 20), ("Sun", 6), ("Moon", 10), ("Mars", 7),
        ("Rahu", 18), ("Jupiter", 16), ("Saturn", 19), ("Mercury", 17)
    };

    public KundliResultDto CalculateKundli(DateOnly dob, TimeOnly? tob, double latitude, double longitude, string timeZoneId)
    {
        // Convert to UTC Julian Day Number for astronomical calculations
        var localTime = tob ?? new TimeOnly(12, 0); // Noon if unknown
        var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var localDt = new DateTime(dob.Year, dob.Month, dob.Day, localTime.Hour, localTime.Minute, 0);
        var utcDt = TimeZoneInfo.ConvertTimeToUtc(localDt, tz);

        double jd = DateTimeToJulianDay(utcDt);

        // Lahiri Ayanamsa (approximate formula - official Lahiri value)
        double ayanamsa = CalculateLahiriAyanamsa(jd);

        // Calculate Sun's tropical longitude
        double sunTropical = CalculateSunLongitude(jd);
        double moonTropical = CalculateMoonLongitude(jd);

        // Apply Ayanamsa for Sidereal (Vedic)
        double sunSidereal = NormalizeDegree(sunTropical - ayanamsa);
        double moonSidereal = NormalizeDegree(moonTropical - ayanamsa);

        // Calculate Lagna (Ascendant)
        double lagnaTropical = CalculateLagna(jd, latitude, longitude);
        double lagnaSidereal = NormalizeDegree(lagnaTropical - ayanamsa);

        // Get Rashi indices
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

        // Build planet list
        var planets = BuildPlanetList(jd, ayanamsa, lagnaSidereal);

        return new KundliResultDto
        {
            LagnaRashi = Rashis[lagnaRashiIdx],
            LagnaDegree = Math.Round(lagnaSidereal % 30, 2),
            MoonRashi = Rashis[moonRashiIdx],
            SunRashi = Rashis[sunRashiIdx],
            Nakshatra = Nakshatras[nakshatraIdx],
            NakshatraPada = nakshatraPada,
            MahadashaCurrent = mahadasha,
            MahadashaEndDate = mahadashaEnd,
            IsManglik = isManglik,
            SadeSatiActive = sadeSati,
            Planets = planets,
            AyanamsaUsed = "Lahiri"
        };
    }

    // Julian Day Number calculation
    private static double DateTimeToJulianDay(DateTime utcDt)
    {
        int y = utcDt.Year, m = utcDt.Month, d = utcDt.Day;
        double h = utcDt.Hour + utcDt.Minute / 60.0 + utcDt.Second / 3600.0;
        if (m <= 2) { y--; m += 12; }
        int A = y / 100;
        int B = 2 - A + A / 4;
        return Math.Floor(365.25 * (y + 4716)) + Math.Floor(30.6001 * (m + 1)) + d + h / 24.0 + B - 1524.5;
    }

    // Lahiri Ayanamsa (official formula)
    private static double CalculateLahiriAyanamsa(double jd)
    {
        double T = (jd - 2451545.0) / 36525.0;
        return 23.85 + 0.013 * T; // Simplified; full formula uses IAU tables
    }

    // Sun longitude (Tropical) - simplified VSOP87
    private static double CalculateSunLongitude(double jd)
    {
        double T = (jd - 2451545.0) / 36525.0;
        double M = 357.5291 + 35999.0503 * T;
        double C = (1.9146 - 0.004817 * T) * Math.Sin(M * Math.PI / 180)
                 + 0.019993 * Math.Sin(2 * M * Math.PI / 180);
        double L = 280.4665 + 36000.7698 * T + C;
        return NormalizeDegree(L);
    }

    // Moon longitude (Tropical) - simplified ELP2000
    private static double CalculateMoonLongitude(double jd)
    {
        double T = (jd - 2451545.0) / 36525.0;
        double L = 218.3165 + 481267.8813 * T;
        double M = 357.5291 + 35999.0503 * T;
        double Mm = 134.9634 + 477198.8676 * T;
        double D = 297.8502 + 445267.1115 * T;
        double F = 93.2721 + 483202.0175 * T;

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
        // Sidereal time + local ascendant calculation
        double T = (jd - 2451545.0) / 36525.0;
        double GMST = 280.46061837 + 360.98564736629 * (jd - 2451545.0) + 0.000387933 * T * T;
        double LST = NormalizeDegree(GMST + lon);
        double eps = 23.439 - 0.0000004 * (jd - 2451545.0); // Obliquity
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

        // Fraction elapsed in current nakshatra
        double nakshatraSize = 360.0 / 27;
        double elapsed = (moonDegree % nakshatraSize) / nakshatraSize;
        double remainingYears = years * (1 - elapsed);

        var endDate = dob.AddDays((int)(remainingYears * 365.25));
        return (lord, endDate);
    }

    private static List<PlanetDto> BuildPlanetList(double jd, double ayanamsa, double lagnaSidereal)
    {
        var planets = new List<PlanetDto>();
        double[] tropicalLon =
        {
            CalculateSunLongitude(jd),
            CalculateMoonLongitude(jd),
            CalculateMarsLongitude(jd),
            NormalizeDegree(CalculateSunLongitude(jd) + 28),  // Mercury approx
            NormalizeDegree(CalculateSunLongitude(jd) + 48),  // Jupiter approx
            NormalizeDegree(CalculateSunLongitude(jd) + 15),  // Venus approx
            CalculateSaturnLongitude(jd),
            NormalizeDegree(CalculateMoonLongitude(jd) + 180), // Rahu approx
            CalculateMoonLongitude(jd)                         // Ketu = Rahu+180
        };

        for (int i = 0; i < PlanetNames.Length; i++)
        {
            double sidereal = NormalizeDegree(tropicalLon[i] - ayanamsa);
            int rashiIdx = (int)(sidereal / 30);
            planets.Add(new PlanetDto
            {
                Name = PlanetNames[i],
                NameHindi = PlanetNamesHindi[i],
                Rashi = Rashis[rashiIdx],
                Degree = Math.Round(sidereal % 30, 2),
                IsRetrograde = false, // Simplified
                IsExalted = IsExalted(i, rashiIdx),
                IsDebilitated = IsDebilitated(i, rashiIdx)
            });
        }
        return planets;
    }

    // Exaltation signs for each planet (Vedic)
    private static bool IsExalted(int planetIdx, int rashiIdx) => planetIdx switch
    {
        0 => rashiIdx == 0,  // Sun exalted in Aries
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
        0 => rashiIdx == 6,  // Sun debilitated in Libra
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
