using Microsoft.AspNetCore.Mvc.RazorPages;
using PennyWise.Models;

namespace PennyWise.Pages.Dashboard;

public class TransactionsModel : PageModel
{
    public string Period { get; set; } = string.Empty;
    public List<TransactionRow> Transactions { get; set; } = new();

    public void OnGet()
    {
        Period = "March activity";

        Transactions = new List<TransactionRow>
        {
            new() { Date = "3/10/2026", Description = "Grocery Shopping",    Category = "Food & Dining",   IsIncome = false, Amount = "-$85.50" },
            new() { Date = "3/9/2026",  Description = "Monthly Salary",      Category = "Income",          IsIncome = true,  Amount = "+$5000.00" },
            new() { Date = "3/8/2026",  Description = "Electric Bill",       Category = "Utilities",       IsIncome = false, Amount = "-$120.00" },
            new() { Date = "3/7/2026",  Description = "Gas Station",         Category = "Transportation",  IsIncome = false, Amount = "-$45.00" },
            new() { Date = "3/7/2026",  Description = "Restaurant Dinner",   Category = "Food & Dining",   IsIncome = false, Amount = "-$68.00" },
            new() { Date = "3/6/2026",  Description = "Online Shopping",     Category = "Shopping",        IsIncome = false, Amount = "-$230.00" },
            new() { Date = "3/5/2026",  Description = "Gym Membership",      Category = "Health",          IsIncome = false, Amount = "-$50.00" },
            new() { Date = "3/4/2026",  Description = "Freelance Project",   Category = "Income",          IsIncome = true,  Amount = "+$800.00" },
            new() { Date = "3/3/2026",  Description = "Netflix Subscription",Category = "Entertainment",   IsIncome = false, Amount = "-$15.99" },
            new() { Date = "3/2/2026",  Description = "Uber Ride",           Category = "Transportation",  IsIncome = false, Amount = "-$18.50" },
        };
    }
}
