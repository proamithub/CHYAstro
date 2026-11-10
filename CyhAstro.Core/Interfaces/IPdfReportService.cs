using CyhAstro.Core.DTOs;

namespace CyhAstro.Core.Interfaces;

public interface IPdfReportService
{
    byte[] GenerateKundliReportPdf(KundliResultDto kundli, string nativeName, string city, DateOnly dob, TimeOnly? tob);
}
