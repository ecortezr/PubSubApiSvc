using System.ComponentModel.DataAnnotations;

namespace ApiService.Domain.Entities;

public class Permission
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = default!;

    public ICollection<EmployeePermission> EmployeePermissions { get; set; } =
        new List<EmployeePermission>();
}

