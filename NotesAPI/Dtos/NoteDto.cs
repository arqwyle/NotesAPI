using System.ComponentModel.DataAnnotations;

namespace NotesAPI.Dtos;

public record NoteDto(
    [Required] Guid Id,
    [Required] string Name,
    [Required] string Text,
    [Required] DateTime Date);