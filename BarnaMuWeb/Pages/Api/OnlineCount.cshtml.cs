// BarnaMu Web - Api/OnlineCount.cshtml.cs
// Minimal JSON endpoint: GET /api/online-count → { "count": N }
// Devuelve la cantidad de cuentas con State != 0 en la DB de OpenMU.

using BarnaMu.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BarnaMu.Web.Pages.Api;

public class OnlineCountModel : PageModel
{
    private readonly BarnaMuDb _db;

    public OnlineCountModel(BarnaMuDb db)
    {
        this._db = db;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var count = await this._db.GetOnlinePlayersAsync();
            return new JsonResult(new { count });
        }
        catch
        {
            // Si la DB no responde, devolvemos 0 en lugar de un error 500.
            return new JsonResult(new { count = 0 });
        }
    }
}
