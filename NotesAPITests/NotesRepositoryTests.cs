using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NotesAPI.Database;
using NotesAPI.Models;
using NotesAPI.Repositories;
using Testcontainers.PostgreSql;

namespace NotesAPITests;

public class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18")
        .WithDatabase("testdb")
        .WithUsername("testuser")
        .WithPassword("testpass")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

public class NoteRepositoryTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    private async Task<AppDbContext> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Notes\" RESTART IDENTITY CASCADE");
        return context;
    }

    [Fact]
    public async Task AddAsync_ShouldPersistNote()
    {
        await using var context = await CreateContextAsync();
        var repository = new NoteRepository(context);
        
        var note = new Note { Id = Guid.NewGuid(), Name = "Test", Text = "Text", Date = DateTime.UtcNow };

        await repository.AddAsync(note);

        var saved = await context.Notes.FindAsync(note.Id);
        saved.Should().NotBeNull();
        saved.Name.Should().Be(note.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsNote()
    {
        await using var context = await CreateContextAsync();
        var repository = new NoteRepository(context);
        
        var note = new Note { Id = Guid.NewGuid(), Name = "Test", Text = "Test", Date = DateTime.UtcNow };
        context.Notes.Add(note);
        await context.SaveChangesAsync();

        var result = await repository.GetByIdAsync(note.Id);

        result.Should().NotBeNull();
        result.Id.Should().Be(note.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        await using var context = await CreateContextAsync();
        var repository = new NoteRepository(context);
        
        var result = await repository.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllNotes()
    {
        await using var context = await CreateContextAsync();
        var repository = new NoteRepository(context);
        
        var note1 = new Note { Id = Guid.NewGuid(), Name = "1", Text = "Test", Date = DateTime.UtcNow };
        var note2 = new Note { Id = Guid.NewGuid(), Name = "2", Text = "Test", Date = DateTime.UtcNow };
        context.Notes.AddRange(note1, note2);
        await context.SaveChangesAsync();

        var result = await repository.GetAllAsync();

        result.Should().HaveCount(2);
        result.Should().ContainEquivalentOf(note1, options => options.Excluding(n => n.Date));
        result.Should().ContainEquivalentOf(note2, options => options.Excluding(n => n.Date));
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateNote()
    {
        await using var context = await CreateContextAsync();
        var repository = new NoteRepository(context);
        
        var note = new Note { Id = Guid.NewGuid(), Name = "Test", Text = "Test", Date = DateTime.UtcNow };
        context.Notes.Add(note);
        await context.SaveChangesAsync();

        note.Name = "New";
        await repository.UpdateAsync(note);

        var updated = await context.Notes.FindAsync(note.Id);
        updated!.Name.Should().Be("New");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveNote()
    {
        await using var context = await CreateContextAsync();
        var repository = new NoteRepository(context);
        
        var note = new Note { Id = Guid.NewGuid(), Name = "Test", Text = "Test", Date = DateTime.UtcNow };
        context.Notes.Add(note);
        await context.SaveChangesAsync();

        await repository.DeleteAsync(note.Id);

        var deleted = await context.Notes.FindAsync(note.Id);
        deleted.Should().BeNull();
    }
}