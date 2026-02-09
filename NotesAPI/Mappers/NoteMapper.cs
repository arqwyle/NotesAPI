using NotesAPI.Dtos;
using NotesAPI.Models;

namespace NotesAPI.Mappers;

public static class NoteMapper
{
    public static NoteDto ToDto(Note entity)
    {
        return new NoteDto(
            entity.Id,
            entity.Name,
            entity.Text,
            entity.Date
        );
    }

    public static Note ToEntity(NoteCreateDto dto)
    {
        return new Note
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Text = dto.Text,
            Date = dto.Date
        };
    }
}