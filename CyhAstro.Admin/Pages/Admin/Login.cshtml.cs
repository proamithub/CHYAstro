using CyhAstro.Infrastructure.Data;
using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace CyhAstro.Admin.Pages.Admin;

[Microsoft.AspNetCore.Authorization.AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly ISqlConnectionFactory _db;

    public LoginModel(ISqlConnectionFactory db) => _db = db;

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnPostAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ErrorMessage = "Email aur Password dono zaroori hain.";
            return Page();
        }

        // DB se admin user lo
        using var conn = _db.CreateConnection();
        var admin = await conn.QuerySingleOrDefaultAsync(
            "SELECT UserId, FullName, PasswordHash, Role FROM tbl_Users WHERE Email = @Email AND Role = 'Admin' AND IsActive = 1",
            new { Email = email });

        if (admin == null)
        {
            ErrorMessage = "Admin account nahi mila ya access nahi hai.";
            return Page();
        }

        // Password verify
        if (!BCrypt.Net.BCrypt.Verify(password, (string)admin.PasswordHash))
        {
            ErrorMessage = "Password galat hai.";
            return Page();
        }

        // Cookie auth — claims set karo
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, ((Guid)admin.UserId).ToString()),
            new(ClaimTypes.Name,           (string)admin.FullName),
            new(ClaimTypes.Email,          email),
            new(ClaimTypes.Role,           "Admin")
        };

        var identity  = new ClaimsIdentity(claims, "AdminCookies");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("AdminCookies", principal,
            new AuthenticationProperties { IsPersistent = true });

        // Session mein AdminId store karo (adjust actions ke liye)
        HttpContext.Session.SetString("AdminId", ((Guid)admin.UserId).ToString());
        HttpContext.Session.SetString("AdminName", (string)admin.FullName);

        return RedirectToPage("/Admin/Index");
    }

    public IActionResult OnGet()
    {
        // Already logged in? Dashboard pe bhejo
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Admin/Index");
        return Page();
    }
}
