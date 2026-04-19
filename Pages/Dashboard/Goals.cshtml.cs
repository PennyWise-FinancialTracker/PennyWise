using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PennyWise.Data;
using PennyWise.Data.Entities;
using PennyWise.Models;

namespace PennyWise.Pages.Dashboard;

public class GoalsModel : DashboardPageModel
{
    private readonly AppDbContext _db;

    public GoalsModel(AppDbContext db) => _db = db;

    public List<SavingsGoalRow> Goals { get; set; } = new();
    public GoalInput Input { get; set; } = new();

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

    public async Task<IActionResult> OnPostAddAsync([Bind(Prefix = "Input")] GoalInput input)
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
        var exists = await _db.SavingsGoals.AnyAsync(g => g.UserId == userId && g.Name.ToLower() == name.ToLower());
        if (exists)
        {
            ModelState.AddModelError("Input.Name", "A goal with that name already exists.");
            await LoadPageAsync(userId);
            return Page();
        }

        _db.SavingsGoals.Add(new SavingsGoal
        {
            UserId = userId,
            Name = name,
            TargetAmount = input.TargetAmount,
            CurrentAmount = input.CurrentAmount,
        });
        await _db.SaveChangesAsync();

        Message = "Savings goal added.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditAsync(int id, string name, decimal currentAmount, decimal targetAmount)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToPage("/Auth/Login");
        }

        var goal = await _db.SavingsGoals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);
        if (goal is null || targetAmount <= 0 || currentAmount < 0 || string.IsNullOrWhiteSpace(name))
        {
            Message = "Savings goal could not be updated.";
            return RedirectToPage();
        }

        name = name.Trim();
        var duplicate = await _db.SavingsGoals.AnyAsync(g => g.UserId == userId && g.Id != id && g.Name.ToLower() == name.ToLower());
        if (duplicate)
        {
            Message = "A goal with that name already exists.";
            return RedirectToPage();
        }

        goal.Name = name;
        goal.CurrentAmount = currentAmount;
        goal.TargetAmount = targetAmount;
        await _db.SaveChangesAsync();

        Message = "Savings goal updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToPage("/Auth/Login");
        }

        var goal = await _db.SavingsGoals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);
        if (goal is not null)
        {
            _db.SavingsGoals.Remove(goal);
            await _db.SaveChangesAsync();
            Message = "Savings goal deleted.";
        }

        return RedirectToPage();
    }

    private async Task LoadPageAsync(int userId)
    {
        Goals = await _db.SavingsGoals
            .Where(g => g.UserId == userId)
            .OrderBy(g => g.Name)
            .Select(g => new SavingsGoalRow
            {
                Id = g.Id,
                Name = g.Name,
                CurrentAmount = g.CurrentAmount,
                TargetAmount = g.TargetAmount,
            })
            .ToListAsync();
    }

    public class GoalInput
    {
        [Required]
        [StringLength(128)]
        public string Name { get; set; } = string.Empty;

        [Range(0, 999999999)]
        [Display(Name = "Current Amount")]
        public decimal CurrentAmount { get; set; }

        [Range(0.01, 999999999)]
        [Display(Name = "Target Amount")]
        public decimal TargetAmount { get; set; }
    }
}
