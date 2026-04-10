using Microsoft.AspNetCore.Mvc.RazorPages;
using PennyWise.Models;

namespace PennyWise.Pages.Dashboard;

public class BudgetsModel : PageModel
{
    public decimal TotalBudgeted { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal TotalRemaining => TotalBudgeted - TotalSpent;
    public List<BudgetItem> BudgetItems { get; set; } = new();

    public void OnGet()
    {
        TotalBudgeted = 2350;
        TotalSpent = 2175;

        BudgetItems = new List<BudgetItem>
        {
            new() { Title = "Food & Dining",    Spent = 650, Limit = 800 },
            new() { Title = "Transportation",   Spent = 285, Limit = 300 },
            new() { Title = "Shopping",          Spent = 620, Limit = 500 },
            new() { Title = "Entertainment",     Spent = 145, Limit = 200 },
            new() { Title = "Utilities",         Spent = 380, Limit = 400 },
            new() { Title = "Health & Fitness",  Spent = 95,  Limit = 150 },
        };
    }
}
