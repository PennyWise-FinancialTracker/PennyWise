using Microsoft.EntityFrameworkCore;
using PennyWise.Data.Entities;

namespace PennyWise.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext db)
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
    }
}
