using ApiService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiService.Infrastructure.Storage;

public class ApiDbContext : DbContext
{
    public ApiDbContext(DbContextOptions<ApiDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Many-to-many: Employee <-> Permission
        modelBuilder
            .Entity<EmployeePermission>()
            .HasKey(ca => new { ca.EmployeeId, ca.PermissionId });
    }
    
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Permission> Permissions { get; set; }
}
