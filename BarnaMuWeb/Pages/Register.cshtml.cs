// BarnaMu Web - Register.cshtml.cs
// Account registration. Writes directly into data."Account" using the same BCrypt hashing
// OpenMU uses, so the account is immediately loginable in-game.

using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using BarnaMu.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace BarnaMu.Web.Pages;

[EnableRateLimiting("register")]
public class RegisterModel : PageModel
{
    private static readonly Regex AlphanumericRegex = new("^[a-zA-Z0-9]+$", RegexOptions.Compiled);

    private readonly BarnaMuDb _db;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(BarnaMuDb db, ILogger<RegisterModel> logger)
    {
        this._db = db;
        this._logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!this.ModelState.IsValid)
        {
            return this.Page();
        }

        // The MU client only sends ASCII alphanumerics for login names and security codes;
        // anything else won't be loginable in-game even if it lands in the DB cleanly.
        if (!AlphanumericRegex.IsMatch(this.Input.LoginName))
        {
            this.ModelState.AddModelError(nameof(this.Input.LoginName), "Solo letras y números.");
            return this.Page();
        }

        if (!Regex.IsMatch(this.Input.SecurityCode, "^[0-9]+$"))
        {
            this.ModelState.AddModelError(nameof(this.Input.SecurityCode), "El security code tiene que ser numérico (lo vas a usar para borrar personajes).");
            return this.Page();
        }

        if (this.Input.Password != this.Input.ConfirmPassword)
        {
            this.ModelState.AddModelError(nameof(this.Input.ConfirmPassword), "Las contraseñas no coinciden.");
            return this.Page();
        }

        var result = await this._db.CreateAccountAsync(
            this.Input.LoginName,
            this.Input.Password,
            this.Input.SecurityCode,
            string.IsNullOrWhiteSpace(this.Input.Email) ? null : this.Input.Email);

        switch (result)
        {
            case AccountCreateResult.Created:
                this._logger.LogInformation("New account registered: {LoginName} (IP {Ip})",
                    this.Input.LoginName, this.HttpContext.Connection.RemoteIpAddress);
                this.SuccessMessage = $"Cuenta '{this.Input.LoginName}' creada. ¡Ya podés loguearte en el juego!";
                this.Input = new InputModel();
                this.ModelState.Clear();
                return this.Page();

            case AccountCreateResult.AlreadyExists:
                this.ModelState.AddModelError(nameof(this.Input.LoginName), "Ese nombre de cuenta ya está usado.");
                return this.Page();

            default:
                this.ErrorMessage = "Hubo un problema creando la cuenta. Intentá de nuevo en unos minutos o reportalo en Discord.";
                return this.Page();
        }
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Elegí un nombre de cuenta.")]
        [StringLength(10, MinimumLength = 3, ErrorMessage = "Entre 3 y 10 caracteres.")]
        [Display(Name = "Nombre de cuenta")]
        public string LoginName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Elegí una contraseña.")]
        [StringLength(20, MinimumLength = 6, ErrorMessage = "Entre 6 y 20 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Repetí la contraseña.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Elegí un security code numérico de 4-10 dígitos.")]
        [StringLength(10, MinimumLength = 4, ErrorMessage = "Entre 4 y 10 dígitos.")]
        [Display(Name = "Security code (numérico)")]
        public string SecurityCode { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email inválido.")]
        [Display(Name = "Email (opcional)")]
        public string? Email { get; set; }
    }
}
