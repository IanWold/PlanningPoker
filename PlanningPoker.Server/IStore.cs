namespace PlanningPoker.Server;

public interface IStore
{
    Task CreateParticipantAsync(Guid sessionId, string participantId, string name);
    Task<Guid> CreateSessionAsync(string title);
    Task DeleteParticipantAsync(Guid sessionId, string participantId);
    Task<bool> ExistsSessionAsync(Guid sessionId);
    Task<Session?> GetSessionAsync(Guid sessionId);
    Task UpdateParticipantNameAsync(Guid sessionId, string name);
    Task UpdateParticipantPointsAsync(Guid sessionId, string points);
    Task UpdateSessionStateAsync(Guid sessionId, State state);
    Task UpdateSessionTitleAsync(Guid sessionId, string title);
}