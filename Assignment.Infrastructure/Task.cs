using System.ComponentModel.DataAnnotations;

namespace Assignment.Infrastructure;

public class Task
{
    [Key] public int Id { get; set; }

    [Required][StringLength(100)] public string? Title { get; set; }

    public User? AssignedTo { get; set; }

    public string? Description { get; set; }

    [Required] public State State { get; set; }

    public ICollection<Tag>? Tags { get; set; }
    public DateTime Created { get; internal set; }
    public DateTime StateUpdated { get; internal set; }
}
