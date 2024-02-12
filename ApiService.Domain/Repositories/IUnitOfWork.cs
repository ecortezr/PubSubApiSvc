using Microsoft.EntityFrameworkCore;

namespace ApiService.Domain.Repositories
{
	public interface IUnitOfWork
	{
        DbSet<T> Set<T>() where T : class;
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}

