using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PennyWise.Data;
using PennyWise.Data.Entities;

namespace PennyWise.Pages.Dashboard;

public class CategoriesModel : DashboardPageModel
{
    private readonly AppDbContext _db;

    public CategoriesModel(AppDbContext db) => _db = db;

    public List<CategoryRow> Categories { get; set; } = new();
    public CategoryInput Input { get; set; } = new();

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

    public async Task<IActionResult> OnPostAddAsync([Bind(Prefix = "Input")] CategoryInput input)
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
        var exists = await VisibleCategories(_db, userId)
            .AnyAsync(c => c.Name.ToLower() == name.ToLower());
        if (exists)
        {
            ModelState.AddModelError("Input.Name", "That category already exists.");
            await LoadPageAsync(userId);
            return Page();
        }

        _db.Categories.Add(new Category
        {
            UserId = userId,
            Name = name,
            Icon = string.IsNullOrWhiteSpace(input.Icon) ? name[..1].ToUpperInvariant() : input.Icon.Trim().ToUpperInvariant(),
            Color = string.IsNullOrWhiteSpace(input.Color) ? "#4f46e5" : input.Color.Trim(),
            IsDefault = false,
        });
        await _db.SaveChangesAsync();

        Message = "Category added.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditAsync(int id, string name, string icon, string color)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToPage("/Auth/Login");
        }

        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (category is null)
        {
            Message = "Only custom categories can be edited.";
            return RedirectToPage();
        }

        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            Message = "Category name is required.";
            return RedirectToPage();
        }

        var duplicate = await VisibleCategories(_db, userId)
            .AnyAsync(c => c.Id != id && c.Name.ToLower() == name.ToLower());
        if (duplicate)
        {
            Message = "A category with that name already exists.";
            return RedirectToPage();
        }

        category.Name = name;
        category.Icon = string.IsNullOrWhiteSpace(icon) ? name[..1].ToUpperInvariant() : icon.Trim().ToUpperInvariant();
        category.Color = string.IsNullOrWhiteSpace(color) ? "#4f46e5" : color.Trim();
        await _db.SaveChangesAsync();

        Message = "Category updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToPage("/Auth/Login");
        }

        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (category is null)
        {
            Message = "Only custom categories can be deleted.";
            return RedirectToPage();
        }

        var inUse = await _db.Transactions.AnyAsync(t => t.CategoryId == id) ||
            await _db.Budgets.AnyAsync(b => b.CategoryId == id) ||
            await _db.RecurringTransactions.AnyAsync(r => r.CategoryId == id);
        if (inUse)
        {
            Message = "Category is in use, so it cannot be deleted.";
            return RedirectToPage();
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        Message = "Category deleted.";
        return RedirectToPage();
    }

    private async Task LoadPageAsync(int userId)
    {
        var categories = await VisibleCategories(_db, userId)
            .OrderByDescending(c => c.UserId == null)
            .ThenBy(c => c.Name)
            .ToListAsync();

        var transactionCounts = await _db.Transactions
            .Where(t => t.UserId == userId)
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count);

        Categories = categories.Select(c => new CategoryRow
        {
            Id = c.Id,
            Name = c.Name,
            Icon = c.Icon,
            Color = c.Color,
            IsDefault = c.UserId == null || c.IsDefault,
            TransactionCount = transactionCounts.TryGetValue(c.Id, out var count) ? count : 0,
        }).ToList();
    }

    public class CategoryInput
    {
        [Required]
        [StringLength(64)]
        public string Name { get; set; } = string.Empty;

        [StringLength(16)]
        public string Icon { get; set; } = string.Empty;

        [StringLength(16)]
        public string Color { get; set; } = "#4f46e5";
    }

    public class CategoryRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public int TransactionCount { get; set; }
    }
}
