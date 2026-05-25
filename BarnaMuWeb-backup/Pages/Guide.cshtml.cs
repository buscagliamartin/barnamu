// BarnaMu Web - Guide.cshtml.cs
// Game guide page. Sections are loaded from Content/guide.json so Martin can edit content
// without recompiling. Each section has an "id" (anchor) and an HTML body — Martin writes
// the JSON himself so the HTML is trusted.

using System.Text.Json;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BarnaMu.Web.Pages;

public class GuideModel : PageModel
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IWebHostEnvironment _env;
    private readonly ILogger<GuideModel> _logger;

    public GuideModel(IWebHostEnvironment env, ILogger<GuideModel> logger)
    {
        this._env = env;
        this._logger = logger;
    }

    public IReadOnlyList<GuideSection> Sections { get; private set; } = Array.Empty<GuideSection>();
    public string? Error { get; private set; }

    public async Task OnGetAsync()
    {
        var path = Path.Combine(this._env.ContentRootPath, "Content", "guide.json");
        if (!System.IO.File.Exists(path))
        {
            this.Error = "Content/guide.json no encontrado.";
            return;
        }

        try
        {
            await using var stream = System.IO.File.OpenRead(path);
            var root = await JsonSerializer.DeserializeAsync<GuideRoot>(stream, JsonOpts);
            this.Sections = root?.Sections ?? new List<GuideSection>();
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to parse guide.json");
            this.Error = "No se pudo parsear guide.json.";
        }
    }

    public class GuideRoot
    {
        public List<GuideSection> Sections { get; set; } = new();
    }

    public class GuideSection
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string BodyHtml { get; set; } = string.Empty;
    }
}
