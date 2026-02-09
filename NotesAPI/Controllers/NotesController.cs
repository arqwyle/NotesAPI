using Microsoft.AspNetCore.Mvc;
using NotesAPI.Dtos;
using NotesAPI.Mappers;
using NotesAPI.Services.Interfaces;

namespace NotesAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class NotesController(INoteService noteService, ICacheService cacheService) : ControllerBase
{
    /// <summary>
    /// Получает все заметки.
    /// Данные кешируются на 5 минут для уменьшения нагрузки на БД.
    /// </summary>
    /// <returns>Список всех заметок в формате DTO.</returns>
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

    /// <summary>
    /// Получает заметку по уникальному идентификатору.
    /// Отдельно кешируется каждая заметка на 10 минут.
    /// </summary>
    /// <param name="id">Уникальный идентификатор заметки.</param>
    /// <returns>Заметка в формате DTO или 404, если не найдена.</returns>
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

    /// <summary>
    /// Создаёт новую заметку.
    /// После создания инвалидируется кеш списка всех заметок.
    /// </summary>
    /// <param name="dto">Данные для создания заметки.</param>
    /// <returns>Созданная заметка с кодом 201 Created.</returns>
    [HttpPost]
    public async Task<ActionResult<NoteCreateDto>> Create(NoteCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var note = NoteMapper.ToEntity(dto);
        await noteService.AddAsync(note);

        await cacheService.RemoveAsync("all_notes");

        return CreatedAtAction(nameof(GetById), new { id = note.Id }, dto);
    }

    /// <summary>
    /// Обновляет существующую заметку по идентификатору.
    /// Инвалидирует кеш как самой заметки, так и общего списка.
    /// </summary>
    /// <param name="id">Идентификатор обновляемой заметки.</param>
    /// <param name="dto">Новые данные заметки.</param>
    /// <returns>Код 204 NoContent при успехе или 404, если заметка не найдена.</returns>
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

    /// <summary>
    /// Удаляет заметку по идентификатору.
    /// Инвалидирует кеш удалённой заметки и общего списка.
    /// </summary>
    /// <param name="id">Идентификатор удаляемой заметки.</param>
    /// <returns>Код 204 NoContent при успехе или 404, если заметка не найдена.</returns>
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