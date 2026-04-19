using System.ComponentModel.DataAnnotations;

namespace PennyWise.Data.Entities;

public enum TransactionType
{
    Expense = 0,
    Income = 1,
}

public class Transaction
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public AppUser? User { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public decimal Amount { get; set; }

    [Required, MaxLength(256)]
    public string Description { get; set; } = string.Empty;

    public TransactionType Type { get; set; }

    public DateTime Date { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
