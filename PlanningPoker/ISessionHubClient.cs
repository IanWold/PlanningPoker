﻿namespace PlanningPoker;

public interface ISessionHubClient
{
    Task OnEffectAdded(Effect effect);
    Task OnEffectRemoved(Effect effect);
    Task OnParticipantAdded(string participantId, string name);
    Task OnParticipantNameUpdated(string participantId, string name);
    Task OnParticipantPointsUpdated(string participantId, string points);
    Task OnParticipantRemoved(string participantId);
    Task OnPointAdded(string point);
    Task OnPointRemoved(string point);
    Task OnStarSentToParticipant(string participantId);
    Task OnStateUpdated(State state);
    Task OnTitleUpdated(string title);
}
