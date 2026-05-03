using System.ComponentModel.DataAnnotations;

namespace PennyWise.Data.Entities;

public enum AccountType
{
    Checking = 0,
    Savings = 1,
    Credit = 2,
    Cash = 3,
    Investment = 4,
}

public class Account
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public AppUser? User { get; set; }

    [Required, MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    public AccountType Type { get; set; } = AccountType.Checking;

    public decimal OpeningBalance { get; set; }

    [Required, MaxLength(7)]
    public string Color { get; set; } = "#4f46e5";

    [Required, MaxLength(2)]
    public string Icon { get; set; } = "A";

    public bool IsArchived { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
