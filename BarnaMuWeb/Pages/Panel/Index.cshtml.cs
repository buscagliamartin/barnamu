using BarnaMu.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace BarnaMu.Web.Pages.Panel;

[Authorize]
public class IndexModel : PageModel
{
    private readonly BarnaMuDb _db;

    public IndexModel(BarnaMuDb db) => _db = db;

    public AccountInfo Account { get; private set; } = null!;
    public IReadOnlyList<AccountCharacter> Characters { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(idStr, out var accountId))
            return RedirectToPage("/Logout");

        // Re-fetch VIP date from DB (cookie may be stale after admin changes it)
        var loginName = User.Identity!.Name ?? string.Empty;
        Account = new AccountInfo { Id = accountId, LoginName = loginName };

        try
        {
            // Get fresh VIP info by re-validating session via a lightweight query
            var freshAccount = await _db.GetAccountInfoAsync(accountId);
            if (freshAccount is not null)
                Account = freshAccount;
        }
        catch { /* if this fails, show panel anyway without VIP */ }

        Characters = await _db.GetAccountCharactersAsync(accountId);
        return Page();
    }
}
