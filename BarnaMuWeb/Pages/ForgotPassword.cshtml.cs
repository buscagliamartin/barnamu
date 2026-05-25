// BarnaMu Web - ForgotPassword.cshtml.cs
// Security-code-based password recovery. The player proves ownership with the numeric
// security code they chose at registration (no email/SMTP needed). Rate-limited to make
// brute-forcing the code impractical.

using System.ComponentModel.DataAnnotations;
using BarnaMu.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace BarnaMu.Web.Pages;

[EnableRateLimiting("recover")]
public class ForgotPasswordModel : PageModel
{
    private readonly BarnaMuDb _db;
    private readonly ILogger<ForgotPasswordModel> _logger;

    public ForgotPasswordModel(BarnaMuDb db, ILogger<ForgotPasswordModel> logger)
    {
        this._db = db;
        this._logger = logger;
    }

    [BindProperty]
    [Required(ErrorMessage = "Ingresá tu nombre de cuenta.")]
    [Display(Name = "Nombre de cuenta")]
    public string LoginName { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Ingresá tu security code.")]
    [Display(Name = "Security code")]
    public string SecurityCode { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Elegí una nueva contraseña.")]
    [StringLength(20, MinimumLength = 6, ErrorMessage = "Entre 6 y 20 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva contraseña")]
    public string NewPassword { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Repetí la nueva contraseña.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Las contraseñas no coinciden.")]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
    public bool Success { get; set; }

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

        PasswordResetResult result;
        try
        {
            result = await _db.ResetPasswordWithSecurityCodeAsync(LoginName, SecurityCode, NewPassword);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password recovery failed for [{LoginName}].", LoginName);
            ErrorMessage = "Hubo un problema. Intentá de nuevo en unos minutos.";
            return Page();
        }

        switch (result)
        {
            case PasswordResetResult.Reset:
                _logger.LogInformation("Password recovered for {LoginName} (IP {Ip}).",
                    LoginName, HttpContext.Connection.RemoteIpAddress);
                Success = true;
                return Page();

            default:
                // Same message for "no such account" and "wrong code" — no account enumeration.
                ErrorMessage = "El nombre de cuenta o el security code no coinciden.";
                return Page();
        }
    }
}
