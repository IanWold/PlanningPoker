namespace PlanningPoker.Server;

public interface IStore
{
    Task CreateParticipantAsync(string sessionId, string participantId, string name);
    Task<string> CreateSessionAsync(string title);
    Task DeleteParticipantAsync(string sessionId, string participantId);
    Task<bool> ExistsSessionAsync(string sessionId);
    Task<Session?> GetSessionAsync(string sessionId);
    Task IncrementParticipantStarsAsync(string sessionId, string participantId, int count = 1);
    Task UpdateAllParticipantPointsAsync(string sessionId, string points = "");
    Task UpdateParticipantNameAsync(string sessionId, string participantId, string name);
    Task UpdateParticipantPointsAsync(string sessionId, string participantId, string points);
    Task UpdateSessionStateAsync(string sessionId, State state);
    Task UpdateSessionTitleAsync(string sessionId, string title);
}