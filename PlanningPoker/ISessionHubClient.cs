namespace PlanningPoker;

public interface ISessionHubClient
{
    Task OnParticipantAdded(string participantId, string name);
    Task OnParticipantRemoved(string participantId);
    Task OnParticipantPointsUpdated(string participantId, string points);
    Task OnReveal(IEnumerable<Participant> participants);
    Task OnHide();
    Task OnTitleUpdated(string title);
}
