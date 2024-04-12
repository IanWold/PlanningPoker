﻿namespace PlanningPoker;

public interface ISessionHubClient
{
    Task OnParticipantAdded(string participantId, string name);
    Task OnParticipantRemoved(string participantId);
    Task OnParticipantPointsUpdated(string participantId, string points);
    Task OnStateUpdated(State state);
    Task OnTitleUpdated(string title);
    Task OnParticipantNameUpdated(string participantId, string name);
}
