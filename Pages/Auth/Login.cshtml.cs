using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PennyWise.Pages.Auth;

public class LoginModel : PageModel
{
    [BindProperty]
    public LoginInput Input { get; set; } = new();

    [TempData]
    public string? Message { get; set; }

    public void OnGet()
    {

    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        Message = $"Welcome back, {Input.Email}!";
        return RedirectToPage();
    }

    public class LoginInput
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
