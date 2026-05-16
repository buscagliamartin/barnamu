// BarnaMu Web - ReportBug.cshtml.cs

using System.ComponentModel.DataAnnotations;
using BarnaMu.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace BarnaMu.Web.Pages;

[EnableRateLimiting("bugreport")]
public class ReportBugModel : PageModel
{
    private readonly BarnaMuDb _db;
    private readonly ILogger<ReportBugModel> _logger;

    public ReportBugModel(BarnaMuDb db, ILogger<ReportBugModel> logger)
    {
        this._db = db;
        this._logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!this.ModelState.IsValid)
        {
            return this.Page();
        }

        try
        {
            await this._db.InsertBugReportAsync(
                reporter: this.Input.Reporter.Trim(),
                email: string.IsNullOrWhiteSpace(this.Input.Email) ? null : this.Input.Email.Trim(),
                title: this.Input.Title.Trim(),
                body: this.Input.Body.Trim(),
                ip: this.HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: this.HttpContext.Request.Headers.UserAgent.ToString());

            this.SuccessMessage = "¡Reporte enviado, gracias! Lo vamos a revisar pronto.";
            this.Input = new InputModel();
            this.ModelState.Clear();
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to save bug report from {Reporter}", this.Input.Reporter);
            this.ErrorMessage = "No pudimos guardar el reporte. Probá de nuevo o pasalo por Discord.";
        }

        return this.Page();
    }

    public class InputModel
    {
        [Required, StringLength(50, MinimumLength = 2)]
        [Display(Name = "Tu nombre o nick")]
        public string Reporter { get; set; } = string.Empty;

        [EmailAddress]
        [Display(Name = "Email (opcional, por si necesitamos más data)")]
        public string? Email { get; set; }

        [Required, StringLength(120, MinimumLength = 4)]
        [Display(Name = "Título corto del bug")]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(4000, MinimumLength = 10)]
        [Display(Name = "Descripción (pasos para reproducir, qué esperabas, qué pasó)")]
        public string Body { get; set; } = string.Empty;
    }
}
