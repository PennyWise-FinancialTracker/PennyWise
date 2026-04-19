using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PennyWise.Data;
using PennyWise.Data.Entities;

namespace PennyWise.Pages.Dashboard;

public class SettingsModel : DashboardPageModel
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<AppUser> _passwordHasher;

    public SettingsModel(AppDbContext db, IPasswordHasher<AppUser> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public ProfileInput Profile { get; set; } = new();
    public PasswordInput Password { get; set; } = new();

    [TempData]
    public string? Message { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToPage("/Auth/Login");
        }

        var user = await _db.Users.FindAsync(userId);
        if (user is null) return RedirectToPage("/Auth/Login");

        Profile = new ProfileInput
        {
            FullName = user.FullName,
            Email = user.Email,
            Currency = user.Currency,
            MonthlySavingsGoal = user.MonthlySavingsGoal,
        };
        return Page();
    }

    public async Task<IActionResult> OnPostProfileAsync([Bind(Prefix = "Profile")] ProfileInput profile)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToPage("/Auth/Login");
        }

        Profile = profile;
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _db.Users.FindAsync(userId);
        if (user is null) return RedirectToPage("/Auth/Login");

        var email = profile.Email.Trim().ToLowerInvariant();
        var emailInUse = await _db.Users.AnyAsync(u => u.Id != userId && u.Email == email);
        if (emailInUse)
        {
            ModelState.AddModelError("Profile.Email", "That email is already in use.");
            return Page();
        }

        user.FullName = profile.FullName.Trim();
        user.Email = email;
        user.Currency = profile.Currency.Trim().ToUpperInvariant();
        user.MonthlySavingsGoal = profile.MonthlySavingsGoal;
        await _db.SaveChangesAsync();

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            AppUserClaims.Create(user));

        Message = "Profile updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostPasswordAsync([Bind(Prefix = "Password")] PasswordInput password)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToPage("/Auth/Login");
        }

        Password = password;
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        var user = await _db.Users.FindAsync(userId);
        if (user is null) return RedirectToPage("/Auth/Login");

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password.CurrentPassword);
        if (result == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError("Password.CurrentPassword", "Current password is incorrect.");
            await OnGetAsync();
            return Page();
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, password.NewPassword);
        await _db.SaveChangesAsync();

        Message = "Password updated.";
        return RedirectToPage();
    }

    public class ProfileInput
    {
        [Required]
        [Display(Name = "Full Name")]
        [StringLength(128)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(8)]
        public string Currency { get; set; } = "USD";

        [Range(0, 999999999)]
        [Display(Name = "Monthly Savings Goal")]
        public decimal MonthlySavingsGoal { get; set; }
    }

    public class PasswordInput
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [MinLength(8)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
