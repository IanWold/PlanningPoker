namespace PlanningPoker.Server;

public interface IStore {
    Task AddPointAsync(string sessionId, string point);
    Task CreateParticipantAsync(string sessionId, string participantId, string name);
    Task<string> CreateSessionAsync(string title, IEnumerable<string> points);
    Task DeleteParticipantAsync(string sessionId, string participantId);
    Task<bool> ExistsSessionAsync(string sessionId);
    Task<Session?> GetSessionAsync(string sessionId);
    Task IncrementParticipantStarsAsync(string sessionId, string participantId, int count = 1);
    Task RemovePointAsync(string sessionId, string point);
    Task UpdateAllParticipantPointsAsync(string sessionId, string points = "");
    Task UpdateParticipantNameAsync(string sessionId, string participantId, string name);
    Task UpdateParticipantPointsAsync(string sessionId, string participantId, string points);
    Task UpdateSessionStateAsync(string sessionId, State state);
    Task UpdateSessionTitleAsync(string sessionId, string title);
}