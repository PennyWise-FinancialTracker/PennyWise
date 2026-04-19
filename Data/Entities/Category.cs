using System.ComponentModel.DataAnnotations;

namespace PennyWise.Data.Entities;

public class Category
{
    public int Id { get; set; }

    [Required, MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(16)]
    public string Icon { get; set; } = string.Empty;

    [MaxLength(16)]
    public string Color { get; set; } = "#4f46e5";
}
