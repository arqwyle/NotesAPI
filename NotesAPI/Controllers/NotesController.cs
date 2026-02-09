using Microsoft.AspNetCore.Mvc;
using NotesAPI.Dtos;
using NotesAPI.Mappers;
using NotesAPI.Services.Interfaces;

namespace NotesAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class NotesController(INoteService noteService, ICacheService cacheService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<NoteDto>>> GetAll()
    {
        const string cacheKey = "all_notes";

        var cachedNotes = await cacheService.GetAsync<List<NoteDto>>(cacheKey);
        if (cachedNotes != null)
        {
            return Ok(cachedNotes);
        }

        var notes = await noteService.GetAllAsync();
        var noteDtos = notes.Select(NoteMapper.ToDto).ToList();

        await cacheService.SetAsync(cacheKey, noteDtos, TimeSpan.FromMinutes(5));

        return Ok(noteDtos);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NoteDto>> GetById(Guid id)
    {
        var cacheKey = $"note_{id}";

        var cachedNote = await cacheService.GetAsync<NoteDto>(cacheKey);
        if (cachedNote != null)
        {
            return Ok(cachedNote);
        }

        var note = await noteService.GetByIdAsync(id);
        if (note == null) return NotFound();

        var noteDto = NoteMapper.ToDto(note);
        await cacheService.SetAsync(cacheKey, noteDto, TimeSpan.FromMinutes(10));

        return Ok(noteDto);
    }

    [HttpPost]
    public async Task<ActionResult<NoteCreateDto>> Create(NoteCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var note = NoteMapper.ToEntity(dto);
        await noteService.AddAsync(note);

        await cacheService.RemoveAsync("all_notes");

        return CreatedAtAction(nameof(GetById), new { id = note.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, NoteCreateDto dto)
    {
        var note = await noteService.GetByIdAsync(id);
        if (note == null) return NotFound();

        note.Name = dto.Name;
        note.Text = dto.Text;
        note.Date = dto.Date;

        await noteService.UpdateAsync(note);

        await cacheService.RemoveAsync($"note_{id}");
        await cacheService.RemoveAsync("all_notes");

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await noteService.GetByIdAsync(id);
        if (existing == null) return NotFound();

        await noteService.DeleteAsync(id);

        await cacheService.RemoveAsync($"note_{id}");
        await cacheService.RemoveAsync("all_notes");

        return NoContent();
    }
}