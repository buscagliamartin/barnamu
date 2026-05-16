// BarnaMu Web - Rankings.cshtml.cs

using BarnaMu.Web.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BarnaMu.Web.Pages;

public class RankingsModel : PageModel
{
    private readonly BarnaMuDb _db;

    public RankingsModel(BarnaMuDb db)
    {
        this._db = db;
    }

    /// <summary>Which ranking to show: "resets" (default), "level", or "guilds".</summary>
    public string Mode { get; private set; } = "resets";

    public IReadOnlyList<RankingRow> Rows { get; private set; } = Array.Empty<RankingRow>();
    public IReadOnlyList<GuildRankingRow> GuildRows { get; private set; } = Array.Empty<GuildRankingRow>();
    public string? Error { get; private set; }

    public async Task OnGetAsync(string? mode)
    {
        this.Mode = (mode ?? "resets").ToLowerInvariant() switch
        {
            "level" => "level",
            "guilds" => "guilds",
            _ => "resets",
        };

        try
        {
            switch (this.Mode)
            {
                case "level":
                    this.Rows = await this._db.GetTopByLevelAsync();
                    break;
                case "guilds":
                    this.GuildRows = await this._db.GetTopGuildsAsync();
                    break;
                default:
                    this.Rows = await this._db.GetTopByResetsAsync();
                    break;
            }
        }
        catch (Exception ex)
        {
            this.Error = ex.Message;
        }
    }
}
