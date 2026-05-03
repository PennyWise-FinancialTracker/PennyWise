using System.ComponentModel.DataAnnotations;

namespace PennyWise.Data.Entities;

public enum RecurringFrequency
{
    Weekly = 0,
    BiWeekly = 1,
    Monthly = 2,
    Quarterly = 3,
    Annual = 4,
}

public class RecurringTransaction
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public AppUser? User { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public int? AccountId { get; set; }
    public Account? Account { get; set; }

    public decimal Amount { get; set; }

    [Required, MaxLength(256)]
    public string Description { get; set; } = string.Empty;

    public TransactionType Type { get; set; }

    public RecurringFrequency Frequency { get; set; } = RecurringFrequency.Monthly;

    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;

    public DateTime? LastGeneratedDate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
