using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Nivora.IdentityManager.Pages.Admin;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        return Page();
    }
}
