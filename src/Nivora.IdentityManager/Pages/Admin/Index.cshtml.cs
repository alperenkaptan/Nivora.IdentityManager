using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nivora.IdentityManager.Helpers;

namespace Nivora.IdentityManager.Pages.Admin;

public class IndexModel : PageModel
{
    private readonly IdentityApiClient _api;
    private readonly IConfiguration _config;

    public IndexModel(IdentityApiClient api, IConfiguration config)
    {
        _api = api;
        _config = config;
    }

    public bool IsForbidden { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var me = await _api.GetMeWithRefreshAsync(HttpContext.Session);
        var adminEmail = _config["AdminSeed:Email"];

        if (!me.Success || !string.Equals(me.Data?.Email, adminEmail, StringComparison.OrdinalIgnoreCase))
        {
            IsForbidden = true;
            Response.StatusCode = 403;
            return Page();
        }

        return Page();
    }
}
