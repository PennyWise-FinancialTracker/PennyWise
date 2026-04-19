using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PennyWise.Data;
using PennyWise.Data.Entities;
using PennyWise.Models;

namespace PennyWise.Pages.Dashboard;

public class TransactionsModel : PageModel
{
    private readonly AppDbContext _db;

    public TransactionsModel(AppDbContext db) => _db = db;

    public string Period { get; set; } = string.Empty;
    public List<TransactionRow> Transactions { get; set; } = new();

    public async Task OnGetAsync()
    {
        Period = $"{DateTime.Now:MMMM} activity";

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == DbInitializer.DemoUserEmail);
        if (user is null) return;

        var rows = await _db.Transactions
            .Where(t => t.UserId == user.Id)
            .Include(t => t.Category)
            .OrderByDescending(t => t.Date)
            .ToListAsync();

        Transactions = rows.Select(t => new TransactionRow
        {
            Date = t.Date.ToString("M/d/yyyy"),
            Description = t.Description,
            Category = t.Category?.Name ?? string.Empty,
            IsIncome = t.Type == TransactionType.Income,
            Amount = (t.Type == TransactionType.Income ? "+" : "-") + t.Amount.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-US")),
        }).ToList();
    }
}
