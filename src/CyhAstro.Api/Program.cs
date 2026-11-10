using CyhAstro.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ══════════════════════════════════════════════
// 1. CONTROLLERS + SWAGGER
// ══════════════════════════════════════════════
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "✦ CyhAstro API",
        Version     = "v1",
        Description = "Vedic Astrology | Palm Reading | Face Scan | Subscription Management"
    });

    // JWT Auth header Swagger mein
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "JWT token yahan paste karein: Bearer {your_token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ══════════════════════════════════════════════
// 2. JWT AUTHENTICATION
// ══════════════════════════════════════════════
var jwtKey = builder.Configuration["JwtSettings:SecretKey"]
             ?? throw new InvalidOperationException("JwtSettings:SecretKey not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer           = true,
            ValidIssuer              = builder.Configuration["JwtSettings:Issuer"],
            ValidateAudience         = true,
            ValidAudience            = builder.Configuration["JwtSettings:Audience"],
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ══════════════════════════════════════════════
// 3. INFRASTRUCTURE (Repository Pattern + Services)
//    Single method call — DependencyInjection.cs se
// ══════════════════════════════════════════════
builder.Services.AddInfrastructure(builder.Configuration);

// ══════════════════════════════════════════════
// 4. CORS (Mobile App ke liye)
// ══════════════════════════════════════════════
builder.Services.AddCors(options =>
{
    options.AddPolicy("MobileApp", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// ══════════════════════════════════════════════
// 5. FILE UPLOAD SIZE LIMIT
// ══════════════════════════════════════════════
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 52_428_800; // 50 MB
});

// ══════════════════════════════════════════════
// BUILD
// ══════════════════════════════════════════════
var app = builder.Build();

// ── Swagger (Dev + Prod dono mein available, production mein URL guard lagao) ──
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CyhAstro API v1");
    c.RoutePrefix   = "swagger";
    c.DocumentTitle = "✦ CyhAstro API Explorer";
});

// ── Static files (uploaded images serve karne ke liye) ──
app.UseStaticFiles();

// ── HTTPS + CORS ──
app.UseHttpsRedirection();
app.UseCors("MobileApp");

// ── Auth ──
app.UseAuthentication();
app.UseAuthorization();

// ── Controllers ──
app.MapControllers();

// ── Root → Swagger ──
app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger");
    return Task.CompletedTask;
});

app.Run();
