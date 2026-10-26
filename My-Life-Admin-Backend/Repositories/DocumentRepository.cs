using Microsoft.EntityFrameworkCore;
using My_Life_Admin.Data;
using My_Life_Admin.Models;

namespace My_Life_Admin.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly ApplicationDbContext _context;

    public DocumentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Document?> GetByIdAsync(int id)
    {
        return await _context.Documents.FindAsync(id);
    }

    public async Task<IEnumerable<Document>> GetAllAsync()
    {
        return await _context.Documents.ToListAsync();
    }

    public async Task AddAsync(Document document)
    {
        await _context.Documents.AddAsync(document);
    }

    public void Update(Document document)
    {
        _context.Documents.Update(document);
    }

    public void Delete(Document document)
    {
        _context.Documents.Remove(document);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Document>> SearchAsync(string query)
    {
        return await _context.Documents
            .Where(d =>
                d.Title.Contains(query) ||
                (d.Description != null && d.Description.Contains(query)) ||
                (d.Category != null && d.Category.Contains(query)))
            .ToListAsync();
    }

    public async Task<IEnumerable<Document>> GetExpiringWithinAsync(int days)
    {
        var today = DateTime.UtcNow;
        var expiryLimit = today.AddDays(days);

        return await _context.Documents
            .Where(d =>
                d.ExpiryDate.HasValue &&
                d.ExpiryDate.Value >= today &&
                d.ExpiryDate.Value <= expiryLimit)
            .ToListAsync();
    }
}