using System.ComponentModel.DataAnnotations;

namespace PennyWise.Data.Entities;

public class AppUser
{
    public int Id { get; set; }

    [Required, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(128)]
    public string FullName { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(8)]
    public string Currency { get; set; } = "USD";

    public decimal MonthlySavingsGoal { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<RecurringTransaction> RecurringTransactions { get; set; } = new List<RecurringTransaction>();
    public ICollection<SavingsGoal> SavingsGoals { get; set; } = new List<SavingsGoal>();
}
