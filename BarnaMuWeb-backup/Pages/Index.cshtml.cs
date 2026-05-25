// BarnaMu Web - Index.cshtml.cs
// Home page: news (from Content/news.json) + a quick status pill + headline stats.

using System.Text.Json;
using BarnaMu.Web.Data;
using BarnaMu.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BarnaMu.Web.Pages;

public class IndexModel : PageModel
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly BarnaMuDb _db;
    private readonly ServerStatusService _status;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(BarnaMuDb db, ServerStatusService status, IWebHostEnvironment env, ILogger<IndexModel> logger)
    {
        this._db = db;
        this._status = status;
        this._env = env;
        this._logger = logger;
    }

    public IReadOnlyList<NewsItem> News { get; private set; } = Array.Empty<NewsItem>();
    public ServerStatusSnapshot Status { get; private set; } = ServerStatusSnapshot.Empty;
    public long TotalAccounts { get; private set; }
    public long TotalCharacters { get; private set; }
    public long NewAccounts7d { get; private set; }

    public async Task OnGetAsync()
    {
        this.News = await this.LoadNewsAsync();

        try { this.Status = await this._status.GetStatusAsync(this.HttpContext.RequestAborted); }
        catch (Exception ex) { this._logger.LogWarning(ex, "Could not load server status for home page."); }

        try
        {
            this.TotalAccounts = await this._db.GetTotalAccountsAsync();
            this.TotalCharacters = await this._db.GetTotalCharactersAsync();
            this.NewAccounts7d = await this._db.GetNewAccountsLastDaysAsync(7);
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(ex, "Could not load DB stats for home page.");
        }
    }

    private async Task<IReadOnlyList<NewsItem>> LoadNewsAsync()
    {
        var path = Path.Combine(this._env.ContentRootPath, "Content", "news.json");
        if (!System.IO.File.Exists(path))
        {
            return Array.Empty<NewsItem>();
        }

        try
        {
            await using var stream = System.IO.File.OpenRead(path);
            var items = await JsonSerializer.DeserializeAsync<List<NewsItem>>(stream, JsonOpts);
            return items?.OrderByDescending(n => n.Date).ToList() ?? new List<NewsItem>();
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to parse news.json");
            return Array.Empty<NewsItem>();
        }
    }

    public class NewsItem
    {
        public string Date { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }
}
