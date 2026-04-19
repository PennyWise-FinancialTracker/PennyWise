using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PennyWise.Data;
using PennyWise.Data.Entities;
using PennyWise.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PennyWise.Pages.Dashboard;

public abstract class DashboardPageModel : PageModel
{
    protected bool TryGetCurrentUserId(out int userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out userId);
    }

    protected static IQueryable<Category> VisibleCategories(AppDbContext db, int userId) =>
        db.Categories.Where(c => c.UserId == null || c.UserId == userId);

    protected static async Task<List<MonthlyTrend>> LoadMonthlyTrendsAsync(AppDbContext db, int userId, int monthCount = 6)
    {
        var monthStarts = Enumerable.Range(0, monthCount)
            .Select(offset => new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-(monthCount - 1) + offset))
            .ToList();
        var firstMonth = monthStarts.First();
        var afterLastMonth = monthStarts.Last().AddMonths(1);

        var transactions = await db.Transactions
            .Where(t => t.UserId == userId && t.Date >= firstMonth && t.Date < afterLastMonth)
            .ToListAsync();

        return monthStarts.Select(month =>
        {
            var nextMonth = month.AddMonths(1);
            var txns = transactions.Where(t => t.Date >= month && t.Date < nextMonth).ToList();
            return new MonthlyTrend
            {
                Month = month,
                Income = txns.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                Expenses = txns.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount),
            };
        }).ToList();
    }
}
