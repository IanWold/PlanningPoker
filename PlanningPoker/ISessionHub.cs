namespace PlanningPoker;

public interface ISessionHub
{
    Task<Session> ConnectToSessionAsync(Guid sessionId);
    Task<Guid> CreateSessionAsync(string title);
    Task<string> JoinSessionAsync(Guid sessionId, string name);
    Task DisconnectFromSessionAsync(Guid sessionId);
    Task SendStarToParticipantAsync(Guid sessionId, string participantId);
    Task UpdateParticipantPointsAsync(Guid sessionId, string points);
    Task UpdateSessionStateAsync(Guid sessionId, State state);
    Task UpdateSessionTitleAsync(Guid sessionId, string title);
    Task UpdateParticipantNameAsync(Guid sessionId, string name);
}
