namespace PlanningPoker;

public interface ISessionHub
{
    Task<Session> ConnectToSessionAsync(Guid sessionId);
    Task<Guid> CreateSessionAsync(string title);
    Task<string> JoinSessionAsync(Guid sessionId, string name);
    Task DisconnectFromSessionAsync(Guid sessionId);
    Task UpdateParticipantPointsAsync(Guid sessionId, string points);
    Task UpdateSessionStateAsync(Guid sessionId, State state);
    Task UpdateSessionTitleAsync(Guid sessionId, string title);
}
