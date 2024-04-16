namespace PlanningPoker;

public interface ISessionHub
{
    Task<Session> ConnectToSessionAsync(string sessionId);
    Task<string> CreateSessionAsync(string title);
    Task<string> JoinSessionAsync(string sessionId, string name);
    Task DisconnectFromSessionAsync(string sessionId);
    Task SendStarToParticipantAsync(string sessionId, string participantId);
    Task UpdateParticipantPointsAsync(string sessionId, string points);
    Task UpdateSessionStateAsync(string sessionId, State state);
    Task UpdateSessionTitleAsync(string sessionId, string title);
    Task UpdateParticipantNameAsync(string sessionId, string name);
}
