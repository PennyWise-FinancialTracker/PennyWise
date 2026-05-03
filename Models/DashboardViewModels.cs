using System.Globalization;
using PennyWise.Data.Entities;

namespace PennyWise.Models;

public class MetricItem
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Trend { get; set; } = string.Empty;
    public string TrendDirection { get; set; } = "good"; // "good", "bad", or ""
    public string Icon { get; set; } = string.Empty;
    public string IconStyle { get; set; } = string.Empty; // "income", "expense", "balance", "savings"
}

public class SpendingCategory
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class MonthlyTrend
{
    public DateTime Month { get; set; }
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
    public decimal Savings => Income - Expenses;
    public decimal SavingsForChart => Savings > 0 ? Savings : 0m;
}

public class FinancialTrendChart
{
    public bool HasData { get; set; }
    public string IncomePoints { get; set; } = string.Empty;
    public string ExpensePoints { get; set; } = string.Empty;
    public string SavingsPoints { get; set; } = string.Empty;
    public string ExpenseAreaPoints { get; set; } = string.Empty;
    public List<ChartLabel> YAxisLabels { get; set; } = new();
    public List<ChartLabel> MonthLabels { get; set; } = new();
    public List<ChartMarker> IncomeMarkers { get; set; } = new();
    public List<ChartMarker> ExpenseMarkers { get; set; } = new();
    public List<ChartMarker> SavingsMarkers { get; set; } = new();
    public ChartTooltip Tooltip { get; set; } = new();
}

public class ChartLabel
{
    public string Text { get; set; } = string.Empty;
    public string X { get; set; } = string.Empty;
    public string Y { get; set; } = string.Empty;
    public string GridY { get; set; } = string.Empty;
}

public class ChartMarker
{
    public string X { get; set; } = string.Empty;
    public string Y { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
}

public class ChartTooltip
{
    public string Month { get; set; } = string.Empty;
    public string Income { get; set; } = string.Empty;
    public string Expenses { get; set; } = string.Empty;
    public string Savings { get; set; } = string.Empty;
}

public class DonutSegment
{
    public int CategoryId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Percentage { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string DashArray { get; set; } = string.Empty;
    public string DashOffset { get; set; } = string.Empty;
}

public class RecentTransaction
{
    public string IconLetter { get; set; } = string.Empty;
    public bool IsIncome { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;
    public string When { get; set; } = string.Empty;
}

public class TransactionRow
{
    public int Id { get; set; }
    public DateTime RawDate { get; set; }
    public string Date { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string Category { get; set; } = string.Empty;
    public int? AccountId { get; set; }
    public string Account { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public bool IsIncome { get; set; }
    public decimal RawAmount { get; set; }
    public string Amount { get; set; } = string.Empty;
}

public class CategoryOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

public class AccountOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}

public class GoalOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class BudgetItem
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Spent { get; set; }
    public decimal Limit { get; set; }
    public int Percentage => Limit > 0 ? (int)Math.Min(100, Math.Round(Spent / Limit * 100)) : 0;
    public string Status => Percentage >= 100 ? "danger" : Percentage >= 75 ? "warn" : "good";
    public decimal OverAmount => Spent > Limit ? Spent - Limit : 0;
    public decimal Remaining => Limit > Spent ? Limit - Spent : 0;
    public string AlertMessage => OverAmount > 0
        ? $"You are over budget by {OverAmount.ToString("C2", CultureInfo.GetCultureInfo("en-US"))}."
        : Percentage >= 80
            ? $"You used {Percentage}% of your {Title} budget."
            : $"You have {Remaining.ToString("C2", CultureInfo.GetCultureInfo("en-US"))} left for {Title} this month.";
}

public class SavingsMonth
{
    public string Month { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class AnalyticsMetric
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Trend { get; set; } = string.Empty;
    public string TrendDirection { get; set; } = "good";
    public string Subtitle { get; set; } = string.Empty;
}

public class RecurringTransactionRow
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string Category { get; set; } = string.Empty;
    public int? AccountId { get; set; }
    public string Account { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public bool IsIncome { get; set; }
    public decimal RawAmount { get; set; }
    public string Amount { get; set; } = string.Empty;
    public RecurringFrequency Frequency { get; set; }
    public string FrequencyLabel { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public string NextDue { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string LastGenerated { get; set; } = "Never";
}

public class SavingsGoalRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal CurrentAmount { get; set; }
    public decimal TargetAmount { get; set; }
    public int Percentage => TargetAmount > 0 ? (int)Math.Min(100, Math.Round(CurrentAmount / TargetAmount * 100)) : 0;
    public decimal Remaining => TargetAmount > CurrentAmount ? TargetAmount - CurrentAmount : 0;
}

public static class DashboardCharts
{
    private const double ChartLeft = 90;
    private const double ChartRight = 840;
    private const double ChartTop = 40;
    private const double ChartBottom = 320;
    private const double ChartWidth = ChartRight - ChartLeft;
    private const double ChartHeight = ChartBottom - ChartTop;
    private const double DonutCircumference = 427.26;

    public static FinancialTrendChart BuildTrendChart(IReadOnlyList<MonthlyTrend> months)
    {
        var chart = new FinancialTrendChart();
        if (months.Count == 0)
        {
            return chart;
        }

        chart.HasData = months.Any(HasActivity);
        var chartMonths = chart.HasData ? TrimLeadingEmptyMonths(months) : months;

        var maxValue = chartMonths
            .SelectMany(m => new[] { m.Income, m.Expenses, m.SavingsForChart })
            .DefaultIfEmpty(0m)
            .Max();
        var chartMax = NiceCeiling(maxValue);

        chart.YAxisLabels = Enumerable.Range(0, 6)
            .Select(i =>
            {
                var value = chartMax - (chartMax * i / 5m);
                var y = ChartTop + (ChartHeight * i / 5);
                return new ChartLabel
                {
                    Text = FormatAxis(value),
                    X = FormatNumber(58),
                    Y = FormatNumber(y + 5),
                    GridY = FormatNumber(y),
                };
            })
            .ToList();

        chart.MonthLabels = chartMonths.Select((month, index) => new ChartLabel
        {
            Text = month.Month.ToString("MMM", CultureInfo.GetCultureInfo("en-US")),
            X = FormatNumber(GetX(index, chartMonths.Count)),
            Y = "345",
        }).ToList();

        chart.IncomeMarkers = BuildMarkers(chartMonths, m => m.Income, "Income", chartMax);
        chart.ExpenseMarkers = BuildMarkers(chartMonths, m => m.Expenses, "Expenses", chartMax);
        chart.SavingsMarkers = BuildMarkers(chartMonths, m => m.SavingsForChart, "Savings", chartMax);

        chart.IncomePoints = ToPoints(chart.IncomeMarkers);
        chart.ExpensePoints = ToPoints(chart.ExpenseMarkers);
        chart.SavingsPoints = ToPoints(chart.SavingsMarkers);
        chart.ExpenseAreaPoints = string.IsNullOrWhiteSpace(chart.ExpensePoints)
            ? string.Empty
            : $"{chart.ExpensePoints} {FormatNumber(ChartRight)},{FormatNumber(ChartBottom)} {FormatNumber(ChartLeft)},{FormatNumber(ChartBottom)}";

        var latest = chartMonths.Last();
        chart.Tooltip = new ChartTooltip
        {
            Month = latest.Month.ToString("MMM", CultureInfo.GetCultureInfo("en-US")),
            Income = FormatMoney(latest.Income),
            Expenses = FormatMoney(latest.Expenses),
            Savings = FormatMoney(latest.Savings),
        };

        return chart;
    }

    public static List<DonutSegment> BuildDonutSegments(IReadOnlyList<SpendingCategory> categories)
    {
        var total = categories.Sum(c => c.Amount);
        if (total <= 0) return new List<DonutSegment>();

        var offset = 0d;
        var segments = new List<DonutSegment>();
        foreach (var category in categories)
        {
            var segmentLength = (double)(category.Amount / total) * DonutCircumference;
            segments.Add(new DonutSegment
            {
                CategoryId = category.CategoryId,
                Label = category.Name,
                Value = FormatMoney(category.Amount),
                Percentage = $"{category.Amount / total:P0}",
                Color = category.Color,
                DashArray = $"{FormatNumber(segmentLength)} {FormatNumber(DonutCircumference - segmentLength)}",
                DashOffset = FormatNumber(-offset),
            });
            offset += segmentLength;
        }

        return segments;
    }

    private static List<ChartMarker> BuildMarkers(IReadOnlyList<MonthlyTrend> months, Func<MonthlyTrend, decimal> valueSelector, string series, decimal chartMax) =>
        months.Select((month, index) =>
        {
            var value = valueSelector(month);
            return new ChartMarker
            {
                X = FormatNumber(GetX(index, months.Count)),
                Y = FormatNumber(GetY(value, chartMax)),
                Label = $"{month.Month:MMM yyyy} {series}",
                Value = FormatMoney(value),
                Month = month.Month.Month,
                Year = month.Month.Year,
            };
        }).ToList();

    private static string ToPoints(IEnumerable<ChartMarker> markers) =>
        string.Join(" ", markers.Select(m => $"{m.X},{m.Y}"));

    private static List<MonthlyTrend> TrimLeadingEmptyMonths(IReadOnlyList<MonthlyTrend> months)
    {
        var firstActiveIndex = months.ToList().FindIndex(HasActivity);
        return firstActiveIndex < 0 ? months.ToList() : months.Skip(firstActiveIndex).ToList();
    }

    private static bool HasActivity(MonthlyTrend month) =>
        month.Income > 0 || month.Expenses > 0;

    private static double GetX(int index, int count) =>
        count <= 1 ? ChartLeft + (ChartWidth / 2) : ChartLeft + (ChartWidth * index / (count - 1));

    private static double GetY(decimal value, decimal chartMax)
    {
        var clamped = Math.Clamp(value, 0m, chartMax);
        return ChartBottom - ((double)(clamped / chartMax) * ChartHeight);
    }

    private static decimal NiceCeiling(decimal value)
    {
        if (value <= 0) return 1000m;

        var magnitude = (decimal)Math.Pow(10, Math.Floor(Math.Log10((double)value)));
        var normalized = value / magnitude;
        var nice = normalized <= 1 ? 1m : normalized <= 2 ? 2m : normalized <= 5 ? 5m : 10m;
        return nice * magnitude;
    }

    private static string FormatAxis(decimal value)
    {
        if (value >= 1000m)
        {
            return $"{value / 1000m:0.#}k";
        }

        return value.ToString("0", CultureInfo.InvariantCulture);
    }

    private static string FormatMoney(decimal value) =>
        value.ToString("C0", CultureInfo.GetCultureInfo("en-US"));

    private static string FormatNumber(double value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);
}
