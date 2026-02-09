using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NotesAPI.Controllers;
using NotesAPI.Dtos;
using NotesAPI.Mappers;
using NotesAPI.Models;
using NotesAPI.Services.Interfaces;

namespace NotesAPITests;

public class NotesControllerTests
{
    private readonly Mock<INoteService> _serviceMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly NotesController _controller;

    public NotesControllerTests()
    {
        _serviceMock = new Mock<INoteService>();
        _cacheMock = new Mock<ICacheService>();
        _controller = new NotesController(_serviceMock.Object, _cacheMock.Object);
    }

    [Fact]
    public async Task GetAll_Cached_ReturnsFromCache()
    {
        var cachedNotes = new List<NoteDto> { new(Guid.NewGuid(), "Cached", "Text", DateTime.UtcNow) };
        _cacheMock.Setup(c => c.GetAsync<List<NoteDto>>("all_notes"))
                  .ReturnsAsync(cachedNotes);

        var result = await _controller.GetAll();

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(cachedNotes);
        _serviceMock.Verify(s => s.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task GetAll_NotCached_LoadsFromServiceAndCaches()
    {
        var dbNotes = new List<Note> { new() { Id = Guid.NewGuid(), Name = "DB", Text = "Text", Date = DateTime.UtcNow } };
        var dtoNotes = dbNotes.Select(NoteMapper.ToDto).ToList();

        _cacheMock.Setup(c => c.GetAsync<List<NoteDto>>("all_notes"))
                  .ReturnsAsync((List<NoteDto>?)null);
        _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(dbNotes);

        var result = await _controller.GetAll();

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(dtoNotes);
        _cacheMock.Verify(c => c.SetAsync("all_notes", dtoNotes, TimeSpan.FromMinutes(5)), Times.Once);
    }

    [Fact]
    public async Task GetById_Cached_ReturnsFromCache()
    {
        var id = Guid.NewGuid();
        var cachedNote = new NoteDto(id, "Cached", "Text", DateTime.UtcNow);
        _cacheMock.Setup(c => c.GetAsync<NoteDto>($"note_{id}"))
                  .ReturnsAsync(cachedNote);

        var result = await _controller.GetById(id);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(cachedNote);
        _serviceMock.Verify(s => s.GetByIdAsync(id), Times.Never);
    }

    [Fact]
    public async Task GetById_NotCached_Exists_LoadsAndCaches()
    {
        var id = Guid.NewGuid();
        var note = new Note { Id = id, Name = "DB", Text = "Text", Date = DateTime.UtcNow };
        var dto = NoteMapper.ToDto(note);

        _cacheMock.Setup(c => c.GetAsync<NoteDto>($"note_{id}"))
                  .ReturnsAsync((NoteDto?)null);
        _serviceMock.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(note);

        var result = await _controller.GetById(id);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(dto);
        _cacheMock.Verify(c => c.SetAsync($"note_{id}", dto, TimeSpan.FromMinutes(10)), Times.Once);
    }

    [Fact]
    public async Task GetById_NotCached_NotExists_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _cacheMock.Setup(c => c.GetAsync<NoteDto>($"note_{id}"))
                  .ReturnsAsync((NoteDto?)null);
        _serviceMock.Setup(s => s.GetByIdAsync(id)).ReturnsAsync((Note?)null);

        var result = await _controller.GetById(id);

        result.Result.Should().BeOfType<NotFoundResult>();
        _cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<NoteDto>(), It.IsAny<TimeSpan?>()), Times.Never);
    }

    [Fact]
    public async Task Create_ValidModel_AddsNoteAndInvalidatesCache()
    {
        var dto = new NoteCreateDto("New", "Text", DateTime.UtcNow);
        var note = NoteMapper.ToEntity(dto);
        note.Id = Guid.NewGuid();

        _serviceMock.Setup(s => s.AddAsync(It.IsAny<Note>()))
                    .Callback<Note>(n => n.Id = note.Id)
                    .Returns(Task.CompletedTask);

        var result = await _controller.Create(dto);

        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(_controller.GetById));
        createdResult.RouteValues!["id"].Should().Be(note.Id);
        createdResult.Value.Should().BeEquivalentTo(dto);

        _serviceMock.Verify(s => s.AddAsync(It.Is<Note>(n =>
            n.Name == dto.Name &&
            n.Text == dto.Text &&
            n.Date == dto.Date)), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync("all_notes"), Times.Once);
    }

    [Fact]
    public async Task Update_NonExistingNote_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        var dto = new NoteCreateDto("New", "Text", DateTime.UtcNow);
        _serviceMock.Setup(s => s.GetByIdAsync(id)).ReturnsAsync((Note?)null);

        var result = await _controller.Update(id, dto);

        result.Should().BeOfType<NotFoundResult>();
        _serviceMock.Verify(s => s.UpdateAsync(It.IsAny<Note>()), Times.Never);
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Update_ExistingNote_UpdatesAndInvalidatesCache()
    {
        var id = Guid.NewGuid();
        var existingNote = new Note { Id = id, Name = "Old", Text = "Old", Date = DateTime.UtcNow.AddDays(-1) };
        var dto = new NoteCreateDto("New", "New", DateTime.UtcNow);

        _serviceMock.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(existingNote);
        _serviceMock.Setup(s => s.UpdateAsync(It.IsAny<Note>())).Returns(Task.CompletedTask);

        var result = await _controller.Update(id, dto);

        result.Should().BeOfType<NoContentResult>();
        existingNote.Name.Should().Be(dto.Name);
        existingNote.Text.Should().Be(dto.Text);
        existingNote.Date.Should().Be(dto.Date);

        _serviceMock.Verify(s => s.UpdateAsync(existingNote), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync($"note_{id}"), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync("all_notes"), Times.Once);
    }

    [Fact]
    public async Task Delete_NonExistingNote_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetByIdAsync(id)).ReturnsAsync((Note?)null);

        var result = await _controller.Delete(id);

        result.Should().BeOfType<NotFoundResult>();
        _serviceMock.Verify(s => s.DeleteAsync(id), Times.Never);
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Delete_ExistingNote_DeletesAndInvalidatesCache()
    {
        var id = Guid.NewGuid();
        var note = new Note { Id = id, Name = "Test", Text = "Test", Date = DateTime.UtcNow };
        _serviceMock.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(note);
        _serviceMock.Setup(s => s.DeleteAsync(id)).Returns(Task.CompletedTask);

        var result = await _controller.Delete(id);

        result.Should().BeOfType<NoContentResult>();
        _serviceMock.Verify(s => s.DeleteAsync(id), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync($"note_{id}"), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync("all_notes"), Times.Once);
    }
}