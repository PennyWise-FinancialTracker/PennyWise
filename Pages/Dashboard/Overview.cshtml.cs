using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PennyWise.Data;
using PennyWise.Data.Entities;
using PennyWise.Models;

namespace PennyWise.Pages.Dashboard;

public class OverviewModel : DashboardPageModel
{
    private readonly AppDbContext _db;

    public OverviewModel(AppDbContext db) => _db = db;

    public string LastUpdated { get; set; } = string.Empty;
    public List<MetricItem> Metrics { get; set; } = new();
    public List<SpendingCategory> SpendingCategories { get; set; } = new();
    public FinancialTrendChart TrendChart { get; set; } = new();
    public List<DonutSegment> SpendingSegments { get; set; } = new();
    public List<RecentTransaction> RecentTransactions { get; set; } = new();

    public async Task OnGetAsync()
    {
        LastUpdated = $"Updated {DateTime.Now:MMMM d, yyyy}";

        if (!TryGetCurrentUserId(out var userId)) return;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return;

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var monthTxns = await _db.Transactions
            .Where(t => t.UserId == userId && t.Date >= monthStart)
            .Include(t => t.Category)
            .ToListAsync();

        var income = monthTxns.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var expenses = monthTxns.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        var balance = income - expenses;
        var savings = balance > 0 ? balance : 0m;
        var savingsTrend = user.MonthlySavingsGoal > 0
            ? $"{Math.Min(100, Math.Round(savings / user.MonthlySavingsGoal * 100))}% of goal"
            : "This month";

        Metrics = new List<MetricItem>
        {
            new() { Label = "Total Income",   Value = FormatMoney(income),   Trend = "This month", TrendDirection = "good", Icon = "+", IconStyle = "income" },
            new() { Label = "Total Expenses", Value = FormatMoney(expenses), Trend = "This month", TrendDirection = "bad",  Icon = "-", IconStyle = "expense" },
            new() { Label = "Balance",        Value = FormatMoney(balance),  Trend = "Current",    TrendDirection = "",     Icon = "=", IconStyle = "balance" },
            new() { Label = "Savings",        Value = FormatMoney(savings),  Trend = savingsTrend, TrendDirection = "good", Icon = "S", IconStyle = "savings" },
        };

        SpendingCategories = monthTxns
            .Where(t => t.Type == TransactionType.Expense && t.Category != null)
            .GroupBy(t => t.Category!)
            .Select(g => new SpendingCategory
            {
                CategoryId = g.Key.Id,
                Name = g.Key.Name,
                Color = g.Key.Color,
                Amount = g.Sum(t => t.Amount),
            })
            .OrderByDescending(c => c.Amount)
            .Take(5)
            .ToList();

        var monthlyTrends = await LoadMonthlyTrendsAsync(_db, userId);
        TrendChart = DashboardCharts.BuildTrendChart(monthlyTrends);
        SpendingSegments = DashboardCharts.BuildDonutSegments(SpendingCategories);

        var recent = await _db.Transactions
            .Where(t => t.UserId == userId)
            .Include(t => t.Category)
            .OrderByDescending(t => t.Date)
            .Take(5)
            .ToListAsync();

        RecentTransactions = recent.Select(t => new RecentTransaction
        {
            IconLetter = t.Category?.Icon ?? t.Description[..1].ToUpper(),
            IsIncome = t.Type == TransactionType.Income,
            Title = t.Description,
            Category = t.Category?.Name ?? string.Empty,
            Amount = (t.Type == TransactionType.Income ? "+" : string.Empty) + FormatMoney(t.Amount),
            When = RelativeWhen(t.Date),
        }).ToList();
    }

    private static string FormatMoney(decimal v) => v.ToString("C0", System.Globalization.CultureInfo.GetCultureInfo("en-US"));

    private static string RelativeWhen(DateTime date)
    {
        var days = (int)(DateTime.UtcNow.Date - date.Date).TotalDays;
        return days switch
        {
            <= 0 => "Today",
            1 => "Yesterday",
            < 7 => $"{days} days ago",
            < 30 => $"{days / 7} weeks ago",
            _ => date.ToString("MMM d"),
        };
    }
}
