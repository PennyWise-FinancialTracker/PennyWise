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
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public decimal Amount { get; set; }
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
    public string Date { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsIncome { get; set; }
    public string Amount { get; set; } = string.Empty;
}

public class BudgetItem
{
    public string Title { get; set; } = string.Empty;
    public decimal Spent { get; set; }
    public decimal Limit { get; set; }
    public int Percentage => Limit > 0 ? (int)Math.Min(100, Math.Round(Spent / Limit * 100)) : 0;
    public string Status => Percentage >= 100 ? "danger" : Percentage >= 75 ? "warn" : "good";
    public decimal OverAmount => Spent > Limit ? Spent - Limit : 0;
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
