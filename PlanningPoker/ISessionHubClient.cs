namespace PlanningPoker;

public interface ISessionHubClient
{
    Task OnParticipantAdded(string participantId, string name);
    Task OnParticipantNameUpdated(string participantId, string name);
    Task OnParticipantPointsUpdated(string participantId, string points);
    Task OnParticipantRemoved(string participantId);
    Task OnStarSentToParticipant(string participantId);
    Task OnStateUpdated(State state);
    Task OnTitleUpdated(string title);
}
