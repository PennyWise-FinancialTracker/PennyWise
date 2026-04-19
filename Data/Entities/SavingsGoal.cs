using System.ComponentModel.DataAnnotations;

namespace PennyWise.Data.Entities;

public class SavingsGoal
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public AppUser? User { get; set; }

    [Required, MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    public decimal TargetAmount { get; set; }

    public decimal CurrentAmount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
