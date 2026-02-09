using System.ComponentModel.DataAnnotations;

namespace NotesAPI.Dtos;

public record NoteCreateDto(
    [Required] string Name,
    [Required] string Text,
    [Required] DateTime Date);