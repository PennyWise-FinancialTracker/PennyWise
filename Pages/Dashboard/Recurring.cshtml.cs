using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PennyWise.Data;
using PennyWise.Data.Entities;
using PennyWise.Models;

namespace PennyWise.Pages.Dashboard;

public class RecurringModel : DashboardPageModel
{
    private readonly AppDbContext _db;

    public RecurringModel(AppDbContext db) => _db = db;

    public List<CategoryOption> CategoryOptions { get; set; } = new();
    public List<RecurringTransactionRow> RecurringTransactions { get; set; } = new();
    public RecurringInput Input { get; set; } = new();

    [TempData]
    public string? Message { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToPage("/Auth/Login");
        }

        Input = new RecurringInput { Type = TransactionType.Expense, DayOfMonth = 1 };
        await LoadPageAsync(userId);
        return Page();
    }

    public async Task<IActionResult> OnPostAddAsync([Bind(Prefix = "Input")] RecurringInput input)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToPage("/Auth/Login");
        }

        Input = input;
        if (!ModelState.IsValid)
        {
            await LoadPageAsync(userId);
            return Page();
        }

        if (!await VisibleCategories(_db, userId).AnyAsync(c => c.Id == input.CategoryId))
        {
            ModelState.AddModelError("Input.CategoryId", "Choose a valid category.");
            await LoadPageAsync(userId);
            return Page();
        }

        _db.RecurringTransactions.Add(new RecurringTransaction
        {
            UserId = userId,
            CategoryId = input.CategoryId,
            Amount = input.Amount,
            Description = input.Description.Trim(),
            Type = input.Type,
            DayOfMonth = input.DayOfMonth,
            IsActive = true,
        });
        await _db.SaveChangesAsync();

        Message = "Recurring transaction added.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditAsync(
        int id,
        string description,
        int categoryId,
        TransactionType type,
        decimal amount,
        int dayOfMonth,
        bool isActive)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToPage("/Auth/Login");
        }

        var recurring = await _db.RecurringTransactions.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        if (recurring is null || amount <= 0 || string.IsNullOrWhiteSpace(description) || dayOfMonth is < 1 or > 31 ||
            !await VisibleCategories(_db, userId).AnyAsync(c => c.Id == categoryId))
        {
            Message = "Recurring transaction could not be updated.";
            return RedirectToPage();
        }

        recurring.Description = description.Trim();
        recurring.CategoryId = categoryId;
        recurring.Type = type;
        recurring.Amount = amount;
        recurring.DayOfMonth = dayOfMonth;
        recurring.IsActive = isActive;
        await _db.SaveChangesAsync();

        Message = "Recurring transaction updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToPage("/Auth/Login");
        }

        var recurring = await _db.RecurringTransactions.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        if (recurring is not null)
        {
            _db.RecurringTransactions.Remove(recurring);
            await _db.SaveChangesAsync();
            Message = "Recurring transaction deleted.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostGenerateAsync()
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToPage("/Auth/Login");
        }

        var today = DateTime.Today;
        var rules = await _db.RecurringTransactions
            .Where(r => r.UserId == userId && r.IsActive)
            .ToListAsync();

        var generated = 0;
        foreach (var rule in rules)
        {
            var day = Math.Min(rule.DayOfMonth, DateTime.DaysInMonth(today.Year, today.Month));
            var dueDate = new DateTime(today.Year, today.Month, day);
            var alreadyGenerated = rule.LastGeneratedYear == today.Year && rule.LastGeneratedMonth == today.Month;
            if (dueDate > today || alreadyGenerated) continue;

            _db.Transactions.Add(new Transaction
            {
                UserId = userId,
                CategoryId = rule.CategoryId,
                Amount = rule.Amount,
                Description = rule.Description,
                Type = rule.Type,
                Date = dueDate,
            });
            rule.LastGeneratedYear = today.Year;
            rule.LastGeneratedMonth = today.Month;
            generated++;
        }

        await _db.SaveChangesAsync();
        Message = generated == 1 ? "Generated 1 transaction." : $"Generated {generated} transactions.";
        return RedirectToPage();
    }

    private async Task LoadPageAsync(int userId)
    {
        CategoryOptions = await VisibleCategories(_db, userId)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryOption { Id = c.Id, Name = c.Name, Color = c.Color, IsDefault = c.IsDefault })
            .ToListAsync();

        var rows = await _db.RecurringTransactions
            .Where(r => r.UserId == userId)
            .Include(r => r.Category)
            .OrderBy(r => r.DayOfMonth)
            .ThenBy(r => r.Description)
            .ToListAsync();

        RecurringTransactions = rows.Select(r => new RecurringTransactionRow
        {
            Id = r.Id,
            Description = r.Description,
            CategoryId = r.CategoryId,
            Category = r.Category?.Name ?? string.Empty,
            Type = r.Type,
            IsIncome = r.Type == TransactionType.Income,
            RawAmount = r.Amount,
            Amount = (r.Type == TransactionType.Income ? "+" : "-") + r.Amount.ToString("C2", CultureInfo.GetCultureInfo("en-US")),
            DayOfMonth = r.DayOfMonth,
            IsActive = r.IsActive,
            LastGenerated = r.LastGeneratedYear.HasValue && r.LastGeneratedMonth.HasValue
                ? $"{r.LastGeneratedMonth}/{r.LastGeneratedYear}"
                : "Never",
        }).ToList();
    }

    public class RecurringInput
    {
        [Required]
        [StringLength(256)]
        public string Description { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Choose a category.")]
        public int CategoryId { get; set; }

        [Range(0.01, 999999999, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        public TransactionType Type { get; set; } = TransactionType.Expense;

        [Range(1, 31)]
        [Display(Name = "Day of Month")]
        public int DayOfMonth { get; set; } = 1;
    }
}
