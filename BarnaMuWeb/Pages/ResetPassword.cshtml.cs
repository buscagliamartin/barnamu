// BarnaMu Web - ResetPassword.cshtml.cs
// Completes the email-based recovery: validates the single-use token from the link and
// lets the player set a new password.

using System.ComponentModel.DataAnnotations;
using BarnaMu.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace BarnaMu.Web.Pages;

[EnableRateLimiting("recover")]
public class ResetPasswordModel : PageModel
{
    private readonly BarnaMuDb _db;
    private readonly ILogger<ResetPasswordModel> _logger;

    public ResetPasswordModel(BarnaMuDb db, ILogger<ResetPasswordModel> logger)
    {
        this._db = db;
        this._logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string Token { get; set; } = string.Empty;

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

    public bool TokenValid { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        this.TokenValid = !string.IsNullOrWhiteSpace(Token) && await _db.IsResetTokenValidAsync(Token);
    }

    public async Task OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Token) || !await _db.IsResetTokenValidAsync(Token))
        {
            this.TokenValid = false;
            return;
        }

        this.TokenValid = true;
        if (!ModelState.IsValid)
            return;

        PasswordResetResult result;
        try
        {
            result = await _db.ConsumePasswordResetAsync(Token, NewPassword);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reset-token consumption failed.");
            ErrorMessage = "Hubo un problema. Intentá de nuevo en unos minutos.";
            return;
        }

        if (result == PasswordResetResult.Reset)
        {
            Success = true;
        }
        else
        {
            TokenValid = false;
        }
    }
}
