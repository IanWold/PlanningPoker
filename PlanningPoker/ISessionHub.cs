namespace PlanningPoker;

public interface ISessionHub
{
    Task<Session> ConnectToSessionAsync(Guid sessionId);
    Task<Guid> CreateSessionAsync(string title);
    Task JoinSessionAsync(Guid sessionId, string name);
    Task DisconnectFromSessionAsync(Guid sessionId, string name);
    Task UpdateParticipantPointsAsync(Guid sessionId, string name, string points);
    Task UpdateSessionStateAsync(Guid sessionId, State state);
    Task UpdateSessionTitleAsync(Guid sessionId, string title);
}
