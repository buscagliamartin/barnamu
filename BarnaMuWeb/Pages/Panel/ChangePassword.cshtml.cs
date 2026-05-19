using BarnaMu.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace BarnaMu.Web.Pages.Panel;

[Authorize]
public class ChangePasswordModel : PageModel
{
    private readonly BarnaMuDb _db;

    public ChangePasswordModel(BarnaMuDb db) => _db = db;

    [BindProperty]
    [Required(ErrorMessage = "Ingresá tu contraseña actual.")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Ingresá la nueva contraseña.")]
    [MinLength(6, ErrorMessage = "Mínimo 6 caracteres.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Confirmá la nueva contraseña.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
    public bool Success { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var loginName = User.Identity!.Name!;
        var account   = await _db.ValidateLoginAsync(loginName, CurrentPassword);
        if (account is null)
        {
            ErrorMessage = "La contraseña actual es incorrecta.";
            return Page();
        }

        await _db.ChangePasswordAsync(account.Id, NewPassword);
        Success = true;
        return Page();
    }
}
