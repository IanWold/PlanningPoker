namespace PlanningPoker.Client.Web;

public closed record FormStatus {
    public sealed record Idle : FormStatus;

    public sealed record Submitting : FormStatus;

    public sealed record Failed(string Message) : FormStatus;
}
