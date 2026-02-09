using FluentAssertions;
using Moq;
using NotesAPI.Models;
using NotesAPI.Repositories.Interfaces;
using NotesAPI.Services;

namespace NotesAPITests;

public class NoteServiceTests
{
    private readonly Mock<INoteRepository> _repositoryMock;
    private readonly NoteService _service;

    public NoteServiceTests()
    {
        _repositoryMock = new Mock<INoteRepository>();
        _service = new NoteService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldCallRepository()
    {
        var notes = new List<Note> { new() { Id = Guid.NewGuid(), Name = "Test", Text = "Test", Date = DateTime.Now } };
        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(notes);

        var result = await _service.GetAllAsync();

        result.Should().BeEquivalentTo(notes);
        _repositoryMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldCallRepository()
    {
        var id = Guid.NewGuid();
        var note = new Note { Id = id, Name = "Test", Text = "Test", Date = DateTime.Now };
        _repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(note);

        var result = await _service.GetByIdAsync(id);

        result.Should().Be(note);
        _repositoryMock.Verify(r => r.GetByIdAsync(id), Times.Once);
    }

    [Fact]
    public async Task AddAsync_ShouldCallRepository()
    {
        var note = new Note { Id = Guid.NewGuid(), Name = "Test", Text = "Test", Date = DateTime.Now };

        await _service.AddAsync(note);

        _repositoryMock.Verify(r => r.AddAsync(note), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldCallRepository()
    {
        var note = new Note { Id = Guid.NewGuid(), Name = "Test", Text = "Test", Date = DateTime.Now };

        await _service.UpdateAsync(note);

        _repositoryMock.Verify(r => r.UpdateAsync(note), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldCallRepository()
    {
        var id = Guid.NewGuid();

        await _service.DeleteAsync(id);

        _repositoryMock.Verify(r => r.DeleteAsync(id), Times.Once);
    }
}