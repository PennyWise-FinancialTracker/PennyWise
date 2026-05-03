using Microsoft.EntityFrameworkCore;
using PennyWise.Data.Entities;

namespace PennyWise.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<RecurringTransaction> RecurringTransactions => Set<RecurringTransaction>();
    public DbSet<SavingsGoal> SavingsGoals => Set<SavingsGoal>();
    public DbSet<Account> Accounts => Set<Account>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<AppUser>()
            .HasIndex(u => u.Email)
            .IsUnique();

        b.Entity<AppUser>()
            .Property(u => u.MonthlySavingsGoal)
            .HasPrecision(18, 2);

        b.Entity<Category>(e =>
        {
            e.HasOne(c => c.User)
                .WithMany(u => u.Categories)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(c => new { c.UserId, c.Name }).IsUnique();
        });

        b.Entity<Transaction>(e =>
        {
            e.Property(t => t.Amount).HasPrecision(18, 2);
            e.HasOne(t => t.User)
                .WithMany(u => u.Transactions)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(t => t.Category)
                .WithMany()
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.Account)
                .WithMany()
                .HasForeignKey(t => t.AccountId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(t => new { t.UserId, t.Date });
        });

        b.Entity<Account>(e =>
        {
            e.Property(x => x.OpeningBalance).HasPrecision(18, 2);
            e.HasOne(x => x.User)
                .WithMany(u => u.Accounts)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.UserId, x.Name }).IsUnique();
        });

        b.Entity<Budget>(e =>
        {
            e.Property(x => x.MonthlyLimit).HasPrecision(18, 2);
            e.HasOne(x => x.User)
                .WithMany(u => u.Budgets)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Category)
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.UserId, x.CategoryId, x.Year, x.Month }).IsUnique();
        });

        b.Entity<RecurringTransaction>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.HasOne(x => x.User)
                .WithMany(u => u.RecurringTransactions)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Category)
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.UserId, x.IsActive });
        });

        b.Entity<SavingsGoal>(e =>
        {
            e.Property(x => x.TargetAmount).HasPrecision(18, 2);
            e.Property(x => x.CurrentAmount).HasPrecision(18, 2);
            e.HasOne(x => x.User)
                .WithMany(u => u.SavingsGoals)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.UserId, x.Name }).IsUnique();
        });
    }
}
