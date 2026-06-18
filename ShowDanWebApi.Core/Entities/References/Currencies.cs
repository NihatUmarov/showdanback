using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Currencies
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [StringLength(3)]
    public string Code { get; set; } = null!;

    [Required]
    [StringLength(10)]
    public string Symbol { get; set; } = null!;
}