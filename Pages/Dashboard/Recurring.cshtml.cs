using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PennyWise.Data;
using PennyWise.Data.Entities;
using PennyWise.Models;
using PennyWise.Services;

namespace PennyWise.Pages.Dashboard;

public class RecurringModel : DashboardPageModel
{
    private readonly AppDbContext _db;

    public RecurringModel(AppDbContext db) => _db = db;

    public List<CategoryOption> CategoryOptions { get; set; } = new();
    public List<AccountOption> AccountOptions { get; set; } = new();
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

        Input = new RecurringInput
        {
            Type = TransactionType.Expense,
            Frequency = RecurringFrequency.Monthly,
            StartDate = DateTime.UtcNow.Date,
        };
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

        if (input.AccountId.HasValue && !await _db.Accounts.AnyAsync(a => a.Id == input.AccountId.Value && a.UserId == userId && !a.IsArchived))
        {
            ModelState.AddModelError("Input.AccountId", "Choose a valid account.");
            await LoadPageAsync(userId);
            return Page();
        }

        _db.RecurringTransactions.Add(new RecurringTransaction
        {
            UserId = userId,
            CategoryId = input.CategoryId,
            AccountId = input.AccountId,
            Amount = input.Amount,
            Description = input.Description.Trim(),
            Type = input.Type,
            Frequency = input.Frequency,
            StartDate = input.StartDate.Date,
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
        int? accountId,
        TransactionType type,
        decimal amount,
        RecurringFrequency frequency,
        DateTime startDate,
        bool isActive)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToPage("/Auth/Login");
        }

        var recurring = await _db.RecurringTransactions.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        if (recurring is null || amount <= 0 || string.IsNullOrWhiteSpace(description) ||
            !await VisibleCategories(_db, userId).AnyAsync(c => c.Id == categoryId) ||
            (accountId.HasValue && !await _db.Accounts.AnyAsync(a => a.Id == accountId.Value && a.UserId == userId)))
        {
            Message = "Recurring transaction could not be updated.";
            return RedirectToPage();
        }

        recurring.Description = description.Trim();
        recurring.CategoryId = categoryId;
        recurring.AccountId = accountId;
        recurring.Type = type;
        recurring.Amount = amount;
        recurring.Frequency = frequency;
        recurring.StartDate = startDate.Date;
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

        var generated = await RecurringScheduler.GenerateAllAsync(_db, DateTime.UtcNow.Date, userId);
        Message = generated == 1 ? "Generated 1 transaction." : $"Generated {generated} transactions.";
        return RedirectToPage();
    }

    private async Task LoadPageAsync(int userId)
    {
        CategoryOptions = await VisibleCategories(_db, userId)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryOption { Id = c.Id, Name = c.Name, Color = c.Color, IsDefault = c.IsDefault })
            .ToListAsync();

        AccountOptions = await _db.Accounts
            .Where(a => a.UserId == userId && !a.IsArchived)
            .OrderBy(a => a.Name)
            .Select(a => new AccountOption { Id = a.Id, Name = a.Name, Color = a.Color })
            .ToListAsync();

        var rows = await _db.RecurringTransactions
            .Where(r => r.UserId == userId)
            .Include(r => r.Category)
            .Include(r => r.Account)
            .OrderBy(r => r.StartDate)
            .ThenBy(r => r.Description)
            .ToListAsync();

        var us = CultureInfo.GetCultureInfo("en-US");
        RecurringTransactions = rows.Select(r => new RecurringTransactionRow
        {
            Id = r.Id,
            Description = r.Description,
            CategoryId = r.CategoryId,
            Category = r.Category?.Name ?? string.Empty,
            AccountId = r.AccountId,
            Account = r.Account?.Name ?? "Unassigned",
            Type = r.Type,
            IsIncome = r.Type == TransactionType.Income,
            RawAmount = r.Amount,
            Amount = (r.Type == TransactionType.Income ? "+" : "-") + r.Amount.ToString("C2", us),
            Frequency = r.Frequency,
            FrequencyLabel = RecurringScheduler.FormatFrequency(r.Frequency),
            StartDate = r.StartDate,
            NextDue = r.IsActive ? RecurringScheduler.NextDueDate(r).ToString("MMM d, yyyy", us) : "Paused",
            IsActive = r.IsActive,
            LastGenerated = r.LastGeneratedDate.HasValue
                ? r.LastGeneratedDate.Value.ToString("MMM d, yyyy", us)
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

        [Display(Name = "Account")]
        public int? AccountId { get; set; }

        [Range(0.01, 999999999, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        public TransactionType Type { get; set; } = TransactionType.Expense;

        public RecurringFrequency Frequency { get; set; } = RecurringFrequency.Monthly;

        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;
    }
}
