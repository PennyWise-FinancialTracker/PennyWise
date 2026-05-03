using Microsoft.EntityFrameworkCore;
using PennyWise.Data;
using PennyWise.Data.Entities;

namespace PennyWise.Services;

public static class RecurringScheduler
{
    public static DateTime AdvanceFrom(DateTime from, RecurringFrequency frequency) => frequency switch
    {
        RecurringFrequency.Weekly => from.AddDays(7),
        RecurringFrequency.BiWeekly => from.AddDays(14),
        RecurringFrequency.Monthly => from.AddMonths(1),
        RecurringFrequency.Quarterly => from.AddMonths(3),
        RecurringFrequency.Annual => from.AddYears(1),
        _ => from.AddMonths(1),
    };

    public static DateTime NextDueDate(RecurringTransaction rule) =>
        rule.LastGeneratedDate is null
            ? rule.StartDate
            : AdvanceFrom(rule.LastGeneratedDate.Value, rule.Frequency);

    public static int GenerateForRule(AppDbContext db, RecurringTransaction rule, DateTime today)
    {
        var nextDue = NextDueDate(rule);
        var generated = 0;

        while (nextDue.Date <= today.Date)
        {
            db.Transactions.Add(new Transaction
            {
                UserId = rule.UserId,
                CategoryId = rule.CategoryId,
                AccountId = rule.AccountId,
                Amount = rule.Amount,
                Description = rule.Description,
                Type = rule.Type,
                Date = nextDue.Date,
            });
            rule.LastGeneratedDate = nextDue.Date;
            generated++;
            nextDue = AdvanceFrom(nextDue, rule.Frequency);
        }

        return generated;
    }

    public static async Task<int> GenerateAllAsync(AppDbContext db, DateTime today, int? userId = null)
    {
        var query = db.RecurringTransactions.Where(r => r.IsActive);
        if (userId.HasValue)
        {
            query = query.Where(r => r.UserId == userId.Value);
        }

        var rules = await query.ToListAsync();
        var total = rules.Sum(rule => GenerateForRule(db, rule, today));

        if (total > 0)
        {
            await db.SaveChangesAsync();
        }

        return total;
    }

    public static string FormatFrequency(RecurringFrequency frequency) => frequency switch
    {
        RecurringFrequency.Weekly => "Weekly",
        RecurringFrequency.BiWeekly => "Bi-weekly",
        RecurringFrequency.Monthly => "Monthly",
        RecurringFrequency.Quarterly => "Quarterly",
        RecurringFrequency.Annual => "Annual",
        _ => frequency.ToString(),
    };
}
