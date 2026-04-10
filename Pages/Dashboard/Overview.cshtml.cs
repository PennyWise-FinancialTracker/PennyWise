using Microsoft.AspNetCore.Mvc.RazorPages;
using PennyWise.Models;

namespace PennyWise.Pages.Dashboard;

public class OverviewModel : PageModel
{
    public string LastUpdated { get; set; } = string.Empty;
    public List<MetricItem> Metrics { get; set; } = new();
    public List<SpendingCategory> SpendingCategories { get; set; } = new();
    public List<RecentTransaction> RecentTransactions { get; set; } = new();

    public void OnGet()
    {
        LastUpdated = "Updated March 12, 2026";

        Metrics = new List<MetricItem>
        {
            new() { Label = "Total Income",   Value = "$5,000", Trend = "+12%",  TrendDirection = "good", Icon = "+", IconStyle = "income" },
            new() { Label = "Total Expenses", Value = "$3,600", Trend = "+8%",   TrendDirection = "bad",  Icon = "-", IconStyle = "expense" },
            new() { Label = "Balance",        Value = "$1,400", Trend = "Current", TrendDirection = "",   Icon = "=", IconStyle = "balance" },
            new() { Label = "Savings",        Value = "$1,400", Trend = "+15%",  TrendDirection = "good", Icon = "S", IconStyle = "savings" },
        };

        SpendingCategories = new List<SpendingCategory>
        {
            new() { Name = "Food & Dining",   Color = "#4f46e5", Amount = 850 },
            new() { Name = "Transportation",  Color = "#14b8a6", Amount = 420 },
            new() { Name = "Shopping",         Color = "#9aa8f5", Amount = 680 },
            new() { Name = "Utilities",        Color = "#f4a300", Amount = 380 },
            new() { Name = "Entertainment",    Color = "#ea4c89", Amount = 270 },
        };

        RecentTransactions = new List<RecentTransaction>
        {
            new() { IconLetter = "C", IsIncome = false, Title = "Grocery Store",  Category = "Food & Dining",   Amount = "$85.50",    When = "Today" },
            new() { IconLetter = "I", IsIncome = true,  Title = "Salary Deposit", Category = "Income",          Amount = "+$5000.00", When = "Yesterday" },
            new() { IconLetter = "U", IsIncome = false, Title = "Electric Bill",  Category = "Utilities",       Amount = "$120.00",   When = "2 days ago" },
            new() { IconLetter = "T", IsIncome = false, Title = "Gas Station",    Category = "Transportation",  Amount = "$45.00",    When = "3 days ago" },
            new() { IconLetter = "F", IsIncome = false, Title = "Restaurant",     Category = "Food & Dining",   Amount = "$68.00",    When = "3 days ago" },
        };
    }
}
