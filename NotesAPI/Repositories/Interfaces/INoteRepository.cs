using NotesAPI.Models;

namespace NotesAPI.Repositories.Interfaces;

public interface INoteRepository
{
    Task<Note?> GetByIdAsync(Guid id);
    Task<List<Note>> GetAllAsync();
    Task AddAsync(Note note);
    Task UpdateAsync(Note note);
    Task DeleteAsync(Guid id);
}