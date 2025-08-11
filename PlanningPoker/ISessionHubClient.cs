namespace PlanningPoker;

public interface ISessionHubClient {
    Task OnParticipantAdded(string participantId, string name);
    Task OnParticipantNameUpdated(string participantId, string name);
    Task OnParticipantPointsUpdated(string participantId, string points);
    Task OnParticipantRemoved(string participantId);
    Task OnPointAdded(string point, string actingParticipantId);
    Task OnPointRemoved(string point, string actingParticipantId);
    Task OnStarSentToParticipant(string participantId);
    Task OnStateUpdated(State state, string actingParticipantId);
    Task OnTitleUpdated(string title, string actingParticipantId);
}
