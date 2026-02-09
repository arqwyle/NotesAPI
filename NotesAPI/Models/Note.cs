using System.ComponentModel.DataAnnotations;
using NotesAPI.Models.Base;

namespace NotesAPI.Models;

public class Note : BaseModel
{
    [MaxLength(256)]
    public required string Name { get; set; }
    
    [MaxLength(256)]
    public required string Text { get; set; }
    
    public required DateTime Date { get; set; }
}