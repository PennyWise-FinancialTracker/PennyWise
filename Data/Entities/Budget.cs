using System.ComponentModel.DataAnnotations;

namespace PennyWise.Data.Entities;

public class Budget
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public AppUser? User { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public decimal MonthlyLimit { get; set; }

    public int Month { get; set; }
    public int Year { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
