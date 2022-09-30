namespace Assignment.Core;

public record TaskDTO(int Id, string Title, string AssignedToName, IReadOnlyCollection<string> Tags, State State);

public record TaskDetailsDTO(int Id, string Title, string Description, DateTime Created, string AssignedToName,
    IReadOnlyCollection<string> Tags, State State, DateTime StateUpdated);

public record TaskCreateDTO([StringLength(100)] string Title, int? AssignedToId, string? Description,
    ICollection<string> Tags);

public record TaskUpdateDTO(int Id, [StringLength(100)] string Title, int? AssignedToId, string? Description,
    ICollection<string> Tags, State State);