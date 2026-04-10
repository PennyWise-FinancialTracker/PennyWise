using Microsoft.AspNetCore.Mvc.RazorPages;
using PennyWise.Models;

namespace PennyWise.Pages.Dashboard;

public class AnalyticsModel : PageModel
{
    public List<AnalyticsMetric> Metrics { get; set; } = new();
    public List<SavingsMonth> SavingsBreakdown { get; set; } = new();

    public void OnGet()
    {
        Metrics = new List<AnalyticsMetric>
        {
            new() { Label = "Income Trend",  Value = "$5,000",  Trend = "+8.7%",  TrendDirection = "good", Subtitle = "vs last month: $4,600" },
            new() { Label = "Expense Trend", Value = "$3,600",  Trend = "+16.1%", TrendDirection = "bad",  Subtitle = "vs last month: $3,100" },
            new() { Label = "Savings Rate",  Value = "28.0%",   Trend = "28.0%",  TrendDirection = "good", Subtitle = "Of your total income" },
            new() { Label = "Best Month",    Value = "$1,520",  Trend = "June",   TrendDirection = "good", Subtitle = "Highest projected savings" },
        };

        SavingsBreakdown = new List<SavingsMonth>
        {
            new() { Month = "January",  Color = "#14b8a6", Amount = 1050 },
            new() { Month = "February", Color = "#4f46e5", Amount = 1120 },
            new() { Month = "March",    Color = "#9aa8f5", Amount = 980 },
            new() { Month = "April",    Color = "#f4a300", Amount = 1400 },
            new() { Month = "May",      Color = "#ea4c89", Amount = 1260 },
            new() { Month = "June",     Color = "#00a65a", Amount = 1520 },
        };
    }
}
