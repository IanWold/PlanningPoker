namespace PlanningPoker;

public interface ISessionHubClient
{
    Task OnParticipantAdded(string name);
    Task OnParticipantRemoved(string name);
    Task OnParticipantPointsUpdated(string name, string points);
    Task OnReveal(IEnumerable<Participant> participants);
    Task OnHide();
    Task OnTitleUpdated(string title);
}
