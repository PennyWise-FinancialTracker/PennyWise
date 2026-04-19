using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PennyWise.Data;
using PennyWise.Data.Entities;
using PennyWise.Models;

namespace PennyWise.Pages.Dashboard;

public class BudgetsModel : DashboardPageModel
{
    private readonly AppDbContext _db;

    public BudgetsModel(AppDbContext db) => _db = db;

    public decimal TotalBudgeted { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal TotalRemaining => TotalBudgeted - TotalSpent;
    public List<BudgetItem> BudgetItems { get; set; } = new();
    public List<CategoryOption> CategoryOptions { get; set; } = new();
    public BudgetInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int Month { get; set; } = DateTime.Today.Month;

    [BindProperty(SupportsGet = true)]
    public int Year { get; set; } = DateTime.Today.Year;

    [TempData]
    public string? Message { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToPage("/Auth/Login");
        }

        NormalizeMonthYear();
        Input = new BudgetInput { Month = Month, Year = Year };
        await LoadPageAsync(userId, Year, Month);
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync([Bind(Prefix = "Input")] BudgetInput input)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToPage("/Auth/Login");
        }

        Input = input;
        Month = input.Month;
        Year = input.Year;
        NormalizeMonthYear();

        if (!ModelState.IsValid)
        {
            await LoadPageAsync(userId, Year, Month);
            return Page();
        }

        if (!await SpendingCategoryIsVisibleAsync(userId, input.CategoryId))
        {
            ModelState.AddModelError("Input.CategoryId", "Choose a valid spending category.");
            await LoadPageAsync(userId, Year, Month);
            return Page();
        }

        var budget = await _db.Budgets.FirstOrDefaultAsync(b =>
            b.UserId == userId &&
            b.CategoryId == input.CategoryId &&
            b.Year == input.Year &&
            b.Month == input.Month);

        if (budget is null)
        {
            _db.Budgets.Add(new Budget
            {
                UserId = userId,
                CategoryId = input.CategoryId,
                MonthlyLimit = input.MonthlyLimit,
                Year = input.Year,
                Month = input.Month,
            });
            Message = "Budget added.";
        }
        else
        {
            budget.MonthlyLimit = input.MonthlyLimit;
            Message = "Budget updated.";
        }

        await _db.SaveChangesAsync();
        return RedirectToPage(new { input.Month, input.Year });
    }

    public async Task<IActionResult> OnPostEditAsync(int id, decimal monthlyLimit)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToPage("/Auth/Login");
        }

        var budget = await _db.Budgets.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
        if (budget is null || monthlyLimit <= 0)
        {
            Message = "Budget could not be updated.";
            return RedirectToPage();
        }

        budget.MonthlyLimit = monthlyLimit;
        await _db.SaveChangesAsync();
        Message = "Budget updated.";
        return RedirectToPage(new { budget.Month, budget.Year });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToPage("/Auth/Login");
        }

        var budget = await _db.Budgets.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
        if (budget is not null)
        {
            var routeValues = new { budget.Month, budget.Year };
            _db.Budgets.Remove(budget);
            await _db.SaveChangesAsync();
            Message = "Budget deleted.";
            return RedirectToPage(routeValues);
        }

        return RedirectToPage();
    }

    private async Task LoadPageAsync(int userId, int year, int month)
    {
        CategoryOptions = await VisibleCategories(_db, userId)
            .Where(c => c.Name != "Income")
            .OrderBy(c => c.Name)
            .Select(c => new CategoryOption { Id = c.Id, Name = c.Name, Color = c.Color, IsDefault = c.IsDefault })
            .ToListAsync();

        var monthStart = new DateTime(year, month, 1);
        var nextMonth = monthStart.AddMonths(1);

        var spentByCategory = await _db.Transactions
            .Where(t =>
                t.UserId == userId &&
                t.Type == TransactionType.Expense &&
                t.Date >= monthStart &&
                t.Date < nextMonth)
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key, Amount = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Amount);

        var budgets = await _db.Budgets
            .Where(b => b.UserId == userId && b.Year == year && b.Month == month)
            .Include(b => b.Category)
            .OrderBy(b => b.Category!.Name)
            .ToListAsync();

        BudgetItems = budgets.Select(b => new BudgetItem
        {
            Id = b.Id,
            CategoryId = b.CategoryId,
            Month = b.Month,
            Year = b.Year,
            Title = b.Category?.Name ?? "Category",
            Spent = spentByCategory.TryGetValue(b.CategoryId, out var spent) ? spent : 0m,
            Limit = b.MonthlyLimit,
        }).ToList();

        TotalBudgeted = BudgetItems.Sum(b => b.Limit);
        TotalSpent = BudgetItems.Sum(b => b.Spent);
    }

    private async Task<bool> SpendingCategoryIsVisibleAsync(int userId, int categoryId) =>
        await VisibleCategories(_db, userId).AnyAsync(c => c.Id == categoryId && c.Name != "Income");

    private void NormalizeMonthYear()
    {
        if (Year < 2000 || Year > 2100) Year = DateTime.Today.Year;
        if (Month < 1 || Month > 12) Month = DateTime.Today.Month;
    }

    public class BudgetInput
    {
        [Range(1, int.MaxValue, ErrorMessage = "Choose a category.")]
        public int CategoryId { get; set; }

        [Range(0.01, 999999999, ErrorMessage = "Monthly limit must be greater than zero.")]
        [Display(Name = "Monthly Limit")]
        public decimal MonthlyLimit { get; set; }

        [Range(1, 12)]
        public int Month { get; set; }

        [Range(2000, 2100)]
        public int Year { get; set; }
    }
}
