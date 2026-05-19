// BarnaMu Web - Char.cshtml.cs
// Perfil público de un personaje. Ruta: /char/{name}

using BarnaMu.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BarnaMu.Web.Pages;

public class CharModel : PageModel
{
    private readonly BarnaMuDb _db;

    public CharModel(BarnaMuDb db)
    {
        this._db = db;
    }

    public string CharName { get; private set; } = string.Empty;
    public CharacterProfile? Character { get; private set; }
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync(string name)
    {
        this.CharName = (name ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(this.CharName))
            return RedirectToPage("/Rankings");

        try
        {
            this.Character = await this._db.GetCharacterProfileAsync(this.CharName);
        }
        catch (Exception ex)
        {
            this.Error = ex.Message;
        }

        return Page();
    }
}
