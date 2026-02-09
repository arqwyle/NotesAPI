using Microsoft.EntityFrameworkCore;
using NotesAPI.Database;
using NotesAPI.Models;
using NotesAPI.Repositories.Interfaces;

namespace NotesAPI.Repositories;

public class NoteRepository(AppDbContext context) : INoteRepository
{
    public async Task<Note?> GetByIdAsync(Guid id)
    {
        return await context.Notes.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<Note>> GetAllAsync()
    {
        return await context.Notes.ToListAsync();
    }

    public async Task AddAsync(Note note)
    {
        await context.Notes.AddAsync(note);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Note note)
    {
        context.Notes.Update(note);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var note = await context.Notes.FindAsync(id);
        if (note != null)
        {
            context.Notes.Remove(note);
            await context.SaveChangesAsync();
        }
    }
}