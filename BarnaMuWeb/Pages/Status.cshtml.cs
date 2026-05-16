// BarnaMu Web - Status.cshtml.cs

using BarnaMu.Web.Data;
using BarnaMu.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BarnaMu.Web.Pages;

public class StatusModel : PageModel
{
    private readonly ServerStatusService _status;
    private readonly BarnaMuDb _db;

    public StatusModel(ServerStatusService status, BarnaMuDb db)
    {
        this._status = status;
        this._db = db;
    }

    public ServerStatusSnapshot Status { get; private set; } = ServerStatusSnapshot.Empty;
    public long TotalAccounts { get; private set; }
    public long TotalCharacters { get; private set; }
    public long NewAccounts7d { get; private set; }
    public long NewAccounts30d { get; private set; }
    public string? DbError { get; private set; }

    public async Task OnGetAsync()
    {
        this.Status = await this._status.GetStatusAsync(this.HttpContext.RequestAborted);

        try
        {
            this.TotalAccounts = await this._db.GetTotalAccountsAsync();
            this.TotalCharacters = await this._db.GetTotalCharactersAsync();
            this.NewAccounts7d = await this._db.GetNewAccountsLastDaysAsync(7);
            this.NewAccounts30d = await this._db.GetNewAccountsLastDaysAsync(30);
        }
        catch (Exception ex)
        {
            this.DbError = ex.Message;
        }
    }
}
