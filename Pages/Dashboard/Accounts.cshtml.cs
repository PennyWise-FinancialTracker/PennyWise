using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PennyWise.Data;
using PennyWise.Data.Entities;

namespace PennyWise.Pages.Dashboard;

public class AccountsModel : DashboardPageModel
{
    private static readonly CultureInfo Us = CultureInfo.GetCultureInfo("en-US");

    private readonly AppDbContext _db;

    public AccountsModel(AppDbContext db) => _db = db;

    public List<AccountRow> Accounts { get; set; } = new();
    public AccountInput Input { get; set; } = new();

    public string TotalBalance { get; set; } = string.Empty;

    [TempData]
    public string? Message { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToPage("/Auth/Login");
        }

        await LoadPageAsync(userId);
        return Page();
    }

    public async Task<IActionResult> OnPostAddAsync([Bind(Prefix = "Input")] AccountInput input)
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

        var name = input.Name.Trim();
        var exists = await _db.Accounts.AnyAsync(a => a.UserId == userId && a.Name.ToLower() == name.ToLower());
        if (exists)
        {
            ModelState.AddModelError("Input.Name", "An account with that name already exists.");
            await LoadPageAsync(userId);
            return Page();
        }

        _db.Accounts.Add(new Account
        {
            UserId = userId,
            Name = name,
            Type = input.Type,
            OpeningBalance = input.OpeningBalance,
            Icon = string.IsNullOrWhiteSpace(input.Icon) ? name[..1].ToUpperInvariant() : input.Icon.Trim().ToUpperInvariant(),
            Color = string.IsNullOrWhiteSpace(input.Color) ? DefaultColorFor(input.Type) : input.Color.Trim(),
        });
        await _db.SaveChangesAsync();

        Message = "Account added.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditAsync(int id, string name, AccountType type, decimal openingBalance, string icon, string color, bool isArchived)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToPage("/Auth/Login");
        }

        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (account is null)
        {
            Message = "Account not found.";
            return RedirectToPage();
        }

        name = name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            Message = "Account name is required.";
            return RedirectToPage();
        }

        var duplicate = await _db.Accounts.AnyAsync(a => a.UserId == userId && a.Id != id && a.Name.ToLower() == name.ToLower());
        if (duplicate)
        {
            Message = "An account with that name already exists.";
            return RedirectToPage();
        }

        account.Name = name;
        account.Type = type;
        account.OpeningBalance = openingBalance;
        account.Icon = string.IsNullOrWhiteSpace(icon) ? name[..1].ToUpperInvariant() : icon.Trim().ToUpperInvariant();
        account.Color = string.IsNullOrWhiteSpace(color) ? DefaultColorFor(type) : color.Trim();
        account.IsArchived = isArchived;
        await _db.SaveChangesAsync();

        Message = "Account updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToPage("/Auth/Login");
        }

        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (account is null)
        {
            Message = "Account not found.";
            return RedirectToPage();
        }

        var inUse = await _db.Transactions.AnyAsync(t => t.AccountId == id) ||
            await _db.RecurringTransactions.AnyAsync(r => r.AccountId == id);
        if (inUse)
        {
            account.IsArchived = true;
            await _db.SaveChangesAsync();
            Message = "Account had activity, so it was archived instead of deleted.";
            return RedirectToPage();
        }

        _db.Accounts.Remove(account);
        await _db.SaveChangesAsync();
        Message = "Account deleted.";
        return RedirectToPage();
    }

    private async Task LoadPageAsync(int userId)
    {
        var accounts = await _db.Accounts
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.IsArchived)
            .ThenBy(a => a.Name)
            .ToListAsync();

        var totals = await _db.Transactions
            .Where(t => t.UserId == userId && t.AccountId != null)
            .GroupBy(t => new { t.AccountId, t.Type })
            .Select(g => new { g.Key.AccountId, g.Key.Type, Sum = g.Sum(t => t.Amount), Count = g.Count() })
            .ToListAsync();

        Accounts = accounts.Select(a =>
        {
            var income = totals.Where(t => t.AccountId == a.Id && t.Type == TransactionType.Income).Sum(t => t.Sum);
            var expense = totals.Where(t => t.AccountId == a.Id && t.Type == TransactionType.Expense).Sum(t => t.Sum);
            var count = totals.Where(t => t.AccountId == a.Id).Sum(t => t.Count);
            var balance = a.OpeningBalance + income - expense;

            return new AccountRow
            {
                Id = a.Id,
                Name = a.Name,
                Type = a.Type,
                TypeLabel = FormatType(a.Type),
                Icon = a.Icon,
                Color = a.Color,
                OpeningBalance = a.OpeningBalance,
                Balance = balance,
                BalanceFormatted = balance.ToString("C2", Us),
                TransactionCount = count,
                IsArchived = a.IsArchived,
            };
        }).ToList();

        var net = Accounts.Where(a => !a.IsArchived).Sum(a => a.Balance);
        TotalBalance = net.ToString("C2", Us);
    }

    public static string FormatType(AccountType type) => type switch
    {
        AccountType.Checking => "Checking",
        AccountType.Savings => "Savings",
        AccountType.Credit => "Credit Card",
        AccountType.Cash => "Cash",
        AccountType.Investment => "Investment",
        _ => type.ToString(),
    };

    private static string DefaultColorFor(AccountType type) => type switch
    {
        AccountType.Checking => "#4f46e5",
        AccountType.Savings => "#14b8a6",
        AccountType.Credit => "#ea4c89",
        AccountType.Cash => "#22c55e",
        AccountType.Investment => "#f4a300",
        _ => "#4f46e5",
    };

    public class AccountInput
    {
        [Required]
        [StringLength(64)]
        public string Name { get; set; } = string.Empty;

        public AccountType Type { get; set; } = AccountType.Checking;

        [Display(Name = "Opening Balance")]
        public decimal OpeningBalance { get; set; }

        [StringLength(2)]
        public string Icon { get; set; } = string.Empty;

        [StringLength(7)]
        public string Color { get; set; } = string.Empty;
    }

    public class AccountRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public AccountType Type { get; set; }
        public string TypeLabel { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; }
        public decimal Balance { get; set; }
        public string BalanceFormatted { get; set; } = string.Empty;
        public int TransactionCount { get; set; }
        public bool IsArchived { get; set; }
    }
}
