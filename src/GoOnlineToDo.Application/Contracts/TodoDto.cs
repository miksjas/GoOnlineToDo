namespace GoOnlineToDo.Application.Contracts;

public record TodoDto(
    int Id,
    string Title,
    string? Description,
    DateTime DueDate,
    int PercentComplete,
    bool IsDone
);