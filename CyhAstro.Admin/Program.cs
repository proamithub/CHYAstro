using CyhAstro.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Optional local configuration override for developer secrets (ignored by Git)
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// ══════════════════════════════════════════════
// 1. RAZOR PAGES + SESSION
// ══════════════════════════════════════════════
builder.Services.AddRazorPages(options =>
{
    // /Admin/* pages authorize hogi — sirf Admin role
    options.Conventions.AuthorizeFolder("/Admin");
    options.Conventions.AllowAnonymousToPage("/Admin/Login");
});

builder.Services.AddAuthentication("AdminCookies")
    .AddCookie("AdminCookies", options =>
    {
        options.LoginPath        = "/Admin/Login";
        options.LogoutPath       = "/Admin/Logout";
        options.AccessDeniedPath = "/Admin/Login";
        options.ExpireTimeSpan   = TimeSpan.FromMinutes(
            int.Parse(builder.Configuration["AdminSettings:SessionTimeoutMinutes"] ?? "60"));
        options.SlidingExpiration = true;
        options.Cookie.Name      = builder.Configuration["AdminSettings:CookieName"] ?? "CyhAstroAdmin";
        options.Cookie.HttpOnly  = true;
    });

builder.Services.AddAuthorization();

// ══════════════════════════════════════════════
// 2. INFRASTRUCTURE (Repository Pattern)
// ══════════════════════════════════════════════
builder.Services.AddInfrastructure(builder.Configuration);

// ══════════════════════════════════════════════
// 3. SESSION (flash messages ke liye)
// ══════════════════════════════════════════════
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opt =>
{
    opt.IdleTimeout       = TimeSpan.FromMinutes(60);
    opt.Cookie.HttpOnly   = true;
    opt.Cookie.IsEssential = true;
});

// ══════════════════════════════════════════════
// BUILD
// ══════════════════════════════════════════════
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Root → Admin Portal
app.MapGet("/", context =>
{
    context.Response.Redirect("/Admin");
    return Task.CompletedTask;
});

app.MapRazorPages();

app.Run();
