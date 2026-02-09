using Microsoft.EntityFrameworkCore;
using NotesAPI.Models;

namespace NotesAPI.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Note> Notes => Set<Note>();
}