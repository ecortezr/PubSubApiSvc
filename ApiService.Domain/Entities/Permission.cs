using System.ComponentModel.DataAnnotations;

namespace ApiService.Domain.Entities;

public class Employee
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string FirstName { get; set; } = default!;

    [Required]
    [StringLength(200)]
    public string LastName { get; set; } = default!;
}

