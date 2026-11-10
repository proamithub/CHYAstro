using CyhAstro.Core.DTOs;
using CyhAstro.Core.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CyhAstro.Infrastructure.Services;

public class PdfReportService : IPdfReportService
{
    static PdfReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateKundliReportPdf(KundliResultDto kundli, string nativeName, string city, DateOnly dob, TimeOnly? tob)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(24);
                page.PageColor(Colors.Grey.Lighten5);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken3));

                // ── HEADER ──
                page.Header().Column(col =>
                {
                    col.Item().Background(Colors.Indigo.Darken4).Padding(14).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("ॐ श्री गणेशाय नमः").FontSize(14).Bold().FontColor(Colors.Amber.Lighten3);
                            c.Item().Text("CYHASTRO • VEDIC BIRTH CHART (जन्म कुंडली)").FontSize(18).Bold().FontColor(Colors.White);
                            c.Item().Text("Authentic Astronomical Mathematical Calculations (Lahiri Ayanamsa)").FontSize(9).FontColor(Colors.Grey.Lighten2);
                        });
                        row.ConstantItem(90).AlignRight().AlignMiddle().Text("CYHASTRO").FontSize(16).Bold().FontColor(Colors.Amber.Medium);
                    });
                });

                // ── BODY CONTENT ──
                page.Content().PaddingVertical(14).Column(col =>
                {
                    // 1. Native Birth Particulars Card
                    col.Item().Border(1).BorderColor(Colors.Amber.Darken1).Background(Colors.White).Padding(10).Column(c =>
                    {
                        c.Item().Text("जातक विवरण (Native Details)").FontSize(12).Bold().FontColor(Colors.Indigo.Darken3);
                        c.Item().PaddingTop(6).Row(r =>
                        {
                            r.RelativeItem().Column(sub =>
                            {
                                sub.Item().Text($"नाम (Name): {nativeName}").Bold();
                                sub.Item().Text($"जन्म तिथि (DOB): {dob:dd MMMM yyyy}");
                                sub.Item().Text($"जन्म समय (TOB): {tob?.ToString("hh:mm tt") ?? "12:00 PM (Noon)"}");
                            });
                            r.RelativeItem().Column(sub =>
                            {
                                sub.Item().Text($"जन्म स्थान (City): {city}");
                                sub.Item().Text($"अयनांश (Ayanamsa): {kundli.AyanamsaUsed}");
                                sub.Item().Text($"कुंडली प्रकार: वैदिक निरयण (Vedic Sidereal)");
                            });
                        });
                    });

                    // 2. Fundamental Panchang & Lagnas Card
                    col.Item().PaddingTop(12).Row(r =>
                    {
                        r.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.White).Padding(8).Column(box =>
                        {
                            box.Item().Text("लग्न (Ascendant)").FontSize(9).FontColor(Colors.Grey.Darken1);
                            box.Item().Text(kundli.LagnaRashi).FontSize(13).Bold().FontColor(Colors.Amber.Darken3);
                            box.Item().Text($"अंश: {kundli.LagnaDegree}°").FontSize(8).FontColor(Colors.Grey.Darken2);
                        });

                        r.ConstantItem(8);

                        r.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.White).Padding(8).Column(box =>
                        {
                            box.Item().Text("चंद्र राशि (Moon Sign)").FontSize(9).FontColor(Colors.Grey.Darken1);
                            box.Item().Text(kundli.MoonRashi).FontSize(13).Bold().FontColor(Colors.Teal.Darken2);
                            box.Item().Text($"नक्षत्र: {kundli.Nakshatra} (पद {kundli.NakshatraPada})").FontSize(8).FontColor(Colors.Grey.Darken2);
                        });

                        r.ConstantItem(8);

                        r.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.White).Padding(8).Column(box =>
                        {
                            box.Item().Text("सूर्य राशि (Sun Sign)").FontSize(9).FontColor(Colors.Grey.Darken1);
                            box.Item().Text(kundli.SunRashi).FontSize(13).Bold().FontColor(Colors.Orange.Darken2);
                            box.Item().Text($"नवमांश लग्न: {kundli.NavamshaLagnaRashi}").FontSize(8).FontColor(Colors.Grey.Darken2);
                        });
                    });

                    // 3. Dosha & Mahadasha Badges
                    col.Item().PaddingTop(12).Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.White).Padding(10).Column(c =>
                    {
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Column(sub =>
                            {
                                sub.Item().Text("विंशोत्तरी महादशा (Current Dasha):").Bold();
                                sub.Item().Text($"{kundli.MahadashaCurrent} महादशा (समाप्ति: {kundli.MahadashaEndDate:dd-MM-yyyy})")
                                    .FontSize(11).Bold().FontColor(Colors.Indigo.Medium);
                            });

                            r.RelativeItem().Column(sub =>
                            {
                                sub.Item().Text("मांगलिक दोष (Manglik Status):").Bold();
                                sub.Item().Text(kundli.IsManglik ? "हाँ (मांगलिक दोष सक्रिय)" : "नहीं (मांगलिक दोष मुक्त)")
                                    .FontSize(11).Bold().FontColor(kundli.IsManglik ? Colors.Red.Darken2 : Colors.Green.Darken2);
                            });

                            r.RelativeItem().Column(sub =>
                            {
                                sub.Item().Text("शनि साढ़े साती (Sade Sati):").Bold();
                                sub.Item().Text(kundli.SadeSatiActive ? "सक्रिय (Under Impact)" : "नहीं (Inactive)")
                                    .FontSize(11).Bold().FontColor(kundli.SadeSatiActive ? Colors.Red.Darken2 : Colors.Green.Darken2);
                            });
                        });
                    });

                    // 4. Detailed Planetary Positions Table
                    col.Item().PaddingTop(12).Text("समस्त नवग्रह स्थिति (Planetary Positions)").FontSize(12).Bold().FontColor(Colors.Indigo.Darken3);

                    col.Item().PaddingTop(4).Border(1).BorderColor(Colors.Grey.Lighten2).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.4f); // Planet
                            columns.RelativeColumn(1.8f); // Rashi
                            columns.RelativeColumn(1.0f); // Degree
                            columns.RelativeColumn(1.0f); // House
                            columns.RelativeColumn(1.8f); // Navamsha
                            columns.RelativeColumn(1.6f); // Status
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Indigo.Darken2).Padding(4).Text("ग्रह (Planet)").Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Indigo.Darken2).Padding(4).Text("राशि (Rashi)").Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Indigo.Darken2).Padding(4).Text("अंश (Deg)").Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Indigo.Darken2).Padding(4).Text("भाव (House)").Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Indigo.Darken2).Padding(4).Text("नवमांश (D9)").Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Indigo.Darken2).Padding(4).Text("अवस्था (Status)").Bold().FontColor(Colors.White);
                        });

                        // Rows
                        foreach (var p in kundli.Planets)
                        {
                            string status = p.IsExalted ? "⭐ उच्च (Exalted)" : p.IsDebilitated ? "⚠️ नीच (Debilitated)" : p.IsRetrograde ? "वक्री (Retro)" : "सामान्य";
                            var rowBg = p.IsExalted ? Colors.Amber.Lighten5 : p.IsDebilitated ? Colors.Red.Lighten5 : Colors.White;

                            table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{p.NameHindi} ({p.Name})").Bold();
                            table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(p.Rashi);
                            table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{p.Degree:F2}°");
                            table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"भाव {p.House}");
                            table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(p.NavamshaRashi);
                            table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(status);
                        }
                    });
                });

                // ── FOOTER ──
                page.Footer().Row(row =>
                {
                    row.RelativeItem().Text("CyhAstro Vedic Astrology Engine • सर्वाधिकार सुरक्षित").FontSize(8).FontColor(Colors.Grey.Darken1);
                    row.RelativeItem().AlignRight().Text($"दिनांक: {DateTime.Now:dd-MMM-yyyy}").FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });
        });

        return document.GeneratePdf();
    }
}
