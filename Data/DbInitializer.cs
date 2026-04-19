using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PennyWise.Data.Entities;

namespace PennyWise.Data;

public static class DbInitializer
{
    public const string DemoUserEmail = "demo@pennywise.local";
    public const string DemoUserPassword = "PennyWise123!";

    public static async Task InitializeAsync(AppDbContext db, IPasswordHasher<AppUser> passwordHasher)
    {
        await db.Database.MigrateAsync();

        var seedCategories = new[]
        {
            new Category { Name = "Food & Dining",  Color = "#4f46e5", Icon = "F", IsDefault = true },
            new Category { Name = "Transportation", Color = "#14b8a6", Icon = "T", IsDefault = true },
            new Category { Name = "Shopping",       Color = "#9aa8f5", Icon = "S", IsDefault = true },
            new Category { Name = "Utilities",      Color = "#f4a300", Icon = "U", IsDefault = true },
            new Category { Name = "Entertainment",  Color = "#ea4c89", Icon = "E", IsDefault = true },
            new Category { Name = "Health",         Color = "#22c55e", Icon = "H", IsDefault = true },
            new Category { Name = "Income",         Color = "#22c55e", Icon = "I", IsDefault = true },
        };

        foreach (var seed in seedCategories)
        {
            var existingCategory = await db.Categories.FirstOrDefaultAsync(c => c.UserId == null && c.Name == seed.Name);
            if (existingCategory is null)
            {
                db.Categories.Add(seed);
            }
            else
            {
                existingCategory.IsDefault = true;
                existingCategory.Icon = seed.Icon;
                existingCategory.Color = seed.Color;
            }
        }

        await db.SaveChangesAsync();

        var categories = await db.Categories.ToListAsync();
        Category cat(string name) => categories.First(c => c.UserId == null && c.Name == name);

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == DemoUserEmail);

        if (user is null)
        {
            user = new AppUser
            {
                Email = DemoUserEmail,
                FullName = "Demo User",
            };
            user.PasswordHash = passwordHasher.HashPassword(user, DemoUserPassword);
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }
        else if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            user.PasswordHash = passwordHasher.HashPassword(user, DemoUserPassword);
            await db.SaveChangesAsync();
        }

        var today = DateTime.UtcNow.Date;
        if (!await db.Transactions.AnyAsync(t => t.UserId == user.Id))
        {
            var txns = new List<Transaction>
            {
                new() { UserId = user.Id, CategoryId = cat("Food & Dining").Id,  Amount = 85.50m,  Description = "Grocery Shopping",      Type = TransactionType.Expense, Date = today.AddDays(-1) },
                new() { UserId = user.Id, CategoryId = cat("Income").Id,          Amount = 5000m,   Description = "Monthly Salary",        Type = TransactionType.Income,  Date = today.AddDays(-2) },
                new() { UserId = user.Id, CategoryId = cat("Utilities").Id,       Amount = 120m,    Description = "Electric Bill",         Type = TransactionType.Expense, Date = today.AddDays(-3) },
                new() { UserId = user.Id, CategoryId = cat("Transportation").Id,  Amount = 45m,     Description = "Gas Station",           Type = TransactionType.Expense, Date = today.AddDays(-4) },
                new() { UserId = user.Id, CategoryId = cat("Food & Dining").Id,  Amount = 68m,     Description = "Restaurant Dinner",     Type = TransactionType.Expense, Date = today.AddDays(-4) },
                new() { UserId = user.Id, CategoryId = cat("Shopping").Id,        Amount = 230m,    Description = "Online Shopping",       Type = TransactionType.Expense, Date = today.AddDays(-5) },
                new() { UserId = user.Id, CategoryId = cat("Health").Id,          Amount = 50m,     Description = "Gym Membership",        Type = TransactionType.Expense, Date = today.AddDays(-6) },
                new() { UserId = user.Id, CategoryId = cat("Income").Id,          Amount = 800m,    Description = "Freelance Project",     Type = TransactionType.Income,  Date = today.AddDays(-7) },
                new() { UserId = user.Id, CategoryId = cat("Entertainment").Id,   Amount = 15.99m,  Description = "Netflix Subscription",  Type = TransactionType.Expense, Date = today.AddDays(-8) },
                new() { UserId = user.Id, CategoryId = cat("Transportation").Id,  Amount = 18.50m,  Description = "Uber Ride",             Type = TransactionType.Expense, Date = today.AddDays(-9) },
                new() { UserId = user.Id, CategoryId = cat("Food & Dining").Id,  Amount = 42m,     Description = "Coffee Shop",           Type = TransactionType.Expense, Date = today.AddDays(-10) },
                new() { UserId = user.Id, CategoryId = cat("Shopping").Id,        Amount = 135m,    Description = "New Headphones",        Type = TransactionType.Expense, Date = today.AddDays(-11) },
                new() { UserId = user.Id, CategoryId = cat("Utilities").Id,       Amount = 60m,     Description = "Internet Bill",         Type = TransactionType.Expense, Date = today.AddDays(-12) },
                new() { UserId = user.Id, CategoryId = cat("Food & Dining").Id,  Amount = 92m,     Description = "Weekly Groceries",      Type = TransactionType.Expense, Date = today.AddDays(-13) },
                new() { UserId = user.Id, CategoryId = cat("Entertainment").Id,   Amount = 35m,     Description = "Movie Tickets",         Type = TransactionType.Expense, Date = today.AddDays(-14) },
                new() { UserId = user.Id, CategoryId = cat("Transportation").Id,  Amount = 52m,     Description = "Gas Station",           Type = TransactionType.Expense, Date = today.AddDays(-16) },
                new() { UserId = user.Id, CategoryId = cat("Food & Dining").Id,  Amount = 28m,     Description = "Lunch",                 Type = TransactionType.Expense, Date = today.AddDays(-17) },
                new() { UserId = user.Id, CategoryId = cat("Shopping").Id,        Amount = 78m,     Description = "Clothing",              Type = TransactionType.Expense, Date = today.AddDays(-19) },
                new() { UserId = user.Id, CategoryId = cat("Health").Id,          Amount = 22m,     Description = "Pharmacy",              Type = TransactionType.Expense, Date = today.AddDays(-21) },
                new() { UserId = user.Id, CategoryId = cat("Income").Id,          Amount = 200m,    Description = "Side Gig",              Type = TransactionType.Income,  Date = today.AddDays(-23) },
            };
            db.Transactions.AddRange(txns);
        }

        var month = today.Month;
        var year = today.Year;
        if (!await db.Budgets.AnyAsync(b => b.UserId == user.Id && b.Month == month && b.Year == year))
        {
            var budgets = new[]
            {
                new Budget { UserId = user.Id, CategoryId = cat("Food & Dining").Id,  MonthlyLimit = 600m,  Month = month, Year = year },
                new Budget { UserId = user.Id, CategoryId = cat("Transportation").Id, MonthlyLimit = 250m,  Month = month, Year = year },
                new Budget { UserId = user.Id, CategoryId = cat("Shopping").Id,       MonthlyLimit = 400m,  Month = month, Year = year },
                new Budget { UserId = user.Id, CategoryId = cat("Utilities").Id,      MonthlyLimit = 300m,  Month = month, Year = year },
                new Budget { UserId = user.Id, CategoryId = cat("Entertainment").Id,  MonthlyLimit = 150m,  Month = month, Year = year },
            };
            db.Budgets.AddRange(budgets);
        }

        await db.SaveChangesAsync();
    }
}
