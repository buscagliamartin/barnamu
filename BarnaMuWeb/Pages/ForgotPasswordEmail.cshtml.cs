// BarnaMu Web - ForgotPasswordEmail.cshtml.cs
// Email-based recovery: the player enters their account name; if it has an email on file
// we send a single-use reset link. Always shows the same neutral message so the page
// cannot be used to discover which accounts (or emails) exist.

using System.ComponentModel.DataAnnotations;
using BarnaMu.Web.Data;
using BarnaMu.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace BarnaMu.Web.Pages;

[EnableRateLimiting("recover")]
public class ForgotPasswordEmailModel : PageModel
{
    private readonly BarnaMuDb _db;
    private readonly EmailService _email;
    private readonly BarnaMuOptions _options;
    private readonly ILogger<ForgotPasswordEmailModel> _logger;

    public ForgotPasswordEmailModel(
        BarnaMuDb db, EmailService email, IOptions<BarnaMuOptions> options, ILogger<ForgotPasswordEmailModel> logger)
    {
        this._db = db;
        this._email = email;
        this._options = options.Value;
        this._logger = logger;
    }

    [BindProperty]
    [Required(ErrorMessage = "Ingresá tu nombre de cuenta.")]
    [Display(Name = "Nombre de cuenta")]
    public string LoginName { get; set; } = string.Empty;

    public bool Submitted { get; set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Panel/Index");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var request = await _db.CreatePasswordResetAsync(LoginName);
            if (request is not null)
            {
                var baseUrl = string.IsNullOrWhiteSpace(_options.PublicBaseUrl)
                    ? $"{Request.Scheme}://{Request.Host}"
                    : _options.PublicBaseUrl.TrimEnd('/');
                var link = $"{baseUrl}/ResetPassword?token={Uri.EscapeDataString(request.Token)}";

                var inner =
                    $"""
                    <p style="margin:0 0 14px;">Hola,</p>
                    <p style="margin:0 0 14px;">Pediste restablecer la contraseña de tu cuenta <b style="color:#e8b96a;">{System.Net.WebUtility.HtmlEncode(LoginName)}</b> en {System.Net.WebUtility.HtmlEncode(_options.ServerName)}.</p>
                    <p style="margin:0 0 4px;">Hacé clic en el botón para elegir una nueva contraseña. El enlace vence en <b>1 hora</b>.</p>
                    {EmailService.Button("Elegir nueva contraseña", link)}
                    <p style="margin:0 0 10px;font-size:13px;color:#65605a;">Si el botón no funciona, copiá y pegá este enlace en tu navegador:</p>
                    <p style="margin:0 0 22px;font-size:12px;word-break:break-all;"><a href="{link}" style="color:#c8963c;">{System.Net.WebUtility.HtmlEncode(link)}</a></p>
                    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin-top:6px;background-color:rgba(160,28,28,0.10);border:1px solid rgba(160,28,28,0.35);border-radius:8px;">
                      <tr><td style="padding:14px 16px;font-family:Arial,Helvetica,sans-serif;font-size:13px;line-height:1.6;color:#c4bfb2;">
                        <b style="color:#e8b96a;">¿No pediste esto?</b><br>
                        Si no fuiste vos, podés ignorar este correo con tranquilidad: <b>tu contraseña no cambia</b> a menos que abras el enlace de arriba. Nadie puede acceder a tu cuenta solo por recibir este mensaje, y el enlace deja de funcionar en 1 hora.<br><br>
                        Nunca te vamos a pedir tu contraseña ni tu security code por email. Si te preocupa la seguridad de tu cuenta, cambiá tu contraseña desde el sitio y avisanos en Discord.
                      </td></tr>
                    </table>
                    """;

                var html = _email.BrandedHtml("Recuperá tu contraseña", inner);
                await _email.SendAsync(request.Email, $"Recuperá tu contraseña — {_options.ServerName}", html);
                _logger.LogInformation("Password reset link issued for {LoginName} (IP {Ip}).",
                    LoginName, HttpContext.Connection.RemoteIpAddress);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password reset request failed for [{LoginName}].", LoginName);
        }

        // Always neutral — never reveal whether the account or its email exists.
        Submitted = true;
        return Page();
    }
}
