using Microsoft.EntityFrameworkCore;
using My_Life_Admin.Data;

namespace My_Life_Admin.Repositories;

public class Repository<T>(ApplicationDbContext db) : IRepository<T> where T : class
{
    public async Task<T?> GetByIdAsync(int id)
    {
        return await db.Set<T>().FindAsync(id);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await db.Set<T>().ToListAsync();
    }

    public async Task AddAsync(T entity)
    {
        await db.Set<T>().AddAsync(entity);
    }

    public void Update(T entity)
    {
        db.Set<T>().Update(entity);
    }

    public void Delete(T entity)
    {
        db.Set<T>().Remove(entity);
    }

    public Task SaveChangesAsync()
    {
        return db.SaveChangesAsync();
    }
}
