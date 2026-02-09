using NotesAPI.Models;
using NotesAPI.Repositories.Interfaces;
using NotesAPI.Services.Interfaces;

namespace NotesAPI.Services;

public class NoteService(INoteRepository repository) : INoteService
{
    public async Task<List<Note>> GetAllAsync()
        => await repository.GetAllAsync();

    public async Task<Note?> GetByIdAsync(Guid id)
        => await repository.GetByIdAsync(id);

    public async Task AddAsync(Note note)
        => await repository.AddAsync(note);

    public async Task UpdateAsync(Note note)
        => await repository.UpdateAsync(note);

    public async Task DeleteAsync(Guid id)
        => await repository.DeleteAsync(id);
}