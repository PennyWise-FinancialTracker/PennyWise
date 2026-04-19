using System.ComponentModel.DataAnnotations;

namespace PennyWise.Data.Entities;

public class RecurringTransaction
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

    public int DayOfMonth { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public int? LastGeneratedYear { get; set; }
    public int? LastGeneratedMonth { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
