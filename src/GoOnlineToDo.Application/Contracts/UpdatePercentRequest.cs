namespace GoOnlineToDo.Application.Contracts;
public record UpdatePercentRequest
{
    public int Id { get; set; }
    public int PercentComplete { get; set; }

    public UpdatePercentRequest(int id, int percentComplete)
    {
        Id = id;
        PercentComplete = percentComplete;
    }
}
