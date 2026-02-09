using NotesAPI.Models;

namespace NotesAPI.Services.Interfaces;

public interface INoteService
{
    Task<List<Note>> GetAllAsync();
    Task<Note?> GetByIdAsync(Guid id);
    Task AddAsync(Note note);
    Task UpdateAsync(Note note);
    Task DeleteAsync(Guid id);
}