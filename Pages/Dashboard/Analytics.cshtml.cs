using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PennyWise.Data;
using PennyWise.Data.Entities;
using PennyWise.Models;

namespace PennyWise.Pages.Dashboard;

public class AnalyticsModel : DashboardPageModel
{
    private readonly AppDbContext _db;

    public AnalyticsModel(AppDbContext db) => _db = db;

    public List<AnalyticsMetric> Metrics { get; set; } = new();
    public List<SavingsMonth> SavingsBreakdown { get; set; } = new();
    public FinancialTrendChart TrendChart { get; set; } = new();

    public async Task OnGetAsync()
    {
        if (!TryGetCurrentUserId(out var userId)) return;

        var monthly = await LoadMonthlyTrendsAsync(_db, userId);
        TrendChart = DashboardCharts.BuildTrendChart(monthly);

        var current = monthly.Last();
        var previous = monthly.Count > 1 ? monthly[^2] : current;
        var savingsRate = current.Income > 0 ? current.Savings / current.Income : 0m;
        var bestMonth = monthly.OrderByDescending(m => m.Savings).First();

        Metrics = new List<AnalyticsMetric>
        {
            new() { Label = "Income Trend",  Value = FormatMoney(current.Income),   Trend = FormatTrend(current.Income, previous.Income),    TrendDirection = TrendDirection(current.Income, previous.Income, true), Subtitle = $"vs last month: {FormatMoney(previous.Income)}" },
            new() { Label = "Expense Trend", Value = FormatMoney(current.Expenses), Trend = FormatTrend(current.Expenses, previous.Expenses), TrendDirection = TrendDirection(current.Expenses, previous.Expenses, false), Subtitle = $"vs last month: {FormatMoney(previous.Expenses)}" },
            new() { Label = "Savings Rate",  Value = savingsRate.ToString("P1"),    Trend = savingsRate.ToString("P1"),                     TrendDirection = savingsRate >= 0.2m ? "good" : "bad", Subtitle = "Of your total income" },
            new() { Label = "Best Month",    Value = FormatMoney(bestMonth.Savings), Trend = bestMonth.Month.ToString("MMM"),                TrendDirection = bestMonth.Savings >= 0 ? "good" : "bad", Subtitle = "Highest savings in this window" },
        };

        var colors = new[] { "#14b8a6", "#4f46e5", "#9aa8f5", "#f4a300", "#ea4c89", "#00a65a" };
        SavingsBreakdown = monthly.Select((month, index) => new SavingsMonth
        {
            Month = month.Month.ToString("MMMM"),
            Color = colors[index % colors.Length],
            Amount = month.Savings,
        }).ToList();
    }

    private static string FormatMoney(decimal value) =>
        value.ToString("C0", System.Globalization.CultureInfo.GetCultureInfo("en-US"));

    private static string FormatTrend(decimal current, decimal previous)
    {
        if (previous == 0 && current == 0) return "0.0%";
        if (previous == 0) return "+100.0%";

        var percent = (current - previous) / Math.Abs(previous);
        return percent.ToString("+0.0%;-0.0%;0.0%");
    }

    private static string TrendDirection(decimal current, decimal previous, bool higherIsGood)
    {
        if (current == previous) return "good";
        var increased = current > previous;
        return increased == higherIsGood ? "good" : "bad";
    }
}
