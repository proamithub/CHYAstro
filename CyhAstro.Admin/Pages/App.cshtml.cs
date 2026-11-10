using CyhAstro.Core.Interfaces;
using CyhAstro.Infrastructure.Data;
using CyhAstro.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace CyhAstro.Admin.Pages;

public class AppModel : Admin.AppSimulatorModel
{
    public AppModel(
        ISqlConnectionFactory db,
        IUserRepository userRepo,
        IScanRepository scanRepo,
        IVedicAstroService astroSvc,
        IGeminiVisionService gemini,
        ISubscriptionRepository subRepo,
        ILocalizationService localizer,
        IPdfReportService pdfSvc,
        IWebHostEnvironment env,
        IConfiguration config)
        : base(db, userRepo, scanRepo, astroSvc, gemini, subRepo, localizer, pdfSvc, env, config)
    {
    }
}
