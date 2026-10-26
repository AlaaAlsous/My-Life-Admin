using My_Life_Admin.Models;
using My_Life_Admin.Repositories;

namespace My_Life_Admin.Services;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepository;

    public DocumentService(IDocumentRepository documentRepository)
    {
        _documentRepository = documentRepository;
    }

    public async Task<Document?> GetByIdAsync(int id)
    {
        return await _documentRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Document>> GetAllAsync()
    {
        return await _documentRepository.GetAllAsync();
    }

    public async Task<IEnumerable<Document>> SearchAsync(string query)
    {
        return await _documentRepository.SearchAsync(query);
    }

    public async Task<IEnumerable<Document>> GetExpiringWithinAsync(int days)
    {
        return await _documentRepository.GetExpiringWithinAsync(days);
    }

    public async Task AddAsync(Document document)
    {
        await _documentRepository.AddAsync(document);
        await _documentRepository.SaveChangesAsync();
    }

    public async Task UpdateAsync(Document document)
    {
        _documentRepository.Update(document);
        await _documentRepository.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var document = await _documentRepository.GetByIdAsync(id);

        if (document == null)
        {
            return;
        }

        _documentRepository.Delete(document);
        await _documentRepository.SaveChangesAsync();
    }
}