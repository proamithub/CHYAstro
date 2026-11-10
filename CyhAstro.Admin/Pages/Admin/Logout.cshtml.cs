using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CyhAstro.Admin.Pages.Admin;

public class LogoutModel : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        await HttpContext.SignOutAsync("AdminCookies");
        HttpContext.Session.Clear();
        return RedirectToPage("/Admin/Login");
    }
}
