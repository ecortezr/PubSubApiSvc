using System.ComponentModel.DataAnnotations;

namespace ApiService.Domain.Entities;

public class EmployeePermission
{
    public int EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public int PermissionId { get; set; }

    public Permission? Permission { get; set; }
}

