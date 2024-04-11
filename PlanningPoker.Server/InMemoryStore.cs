namespace PlanningPoker.Server;

public class InMemoryStore : IStore
{
    public Task CreateParticipantAsync(Guid sessionId, string participantId, string name)
    {
        throw new NotImplementedException();
    }

    public Task<Guid> CreateSessionAsync(string title)
    {
        throw new NotImplementedException();
    }

    public Task DeleteParticipantAsync(Guid sessionId, string participantId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistsSessionAsync(Guid sessionId)
    {
        throw new NotImplementedException();
    }

    public Task<Session?> GetSessionAsync(Guid sessionId)
    {
        throw new NotImplementedException();
    }

    public Task UpdateParticipantNameAsync(Guid sessionId, string participantId, string name)
    {
        throw new NotImplementedException();
    }

    public Task UpdateParticipantPointsAsync(Guid sessionId, string participantId, string points)
    {
        throw new NotImplementedException();
    }

    public Task UpdateSessionStateAsync(Guid sessionId, State state)
    {
        throw new NotImplementedException();
    }

    public Task UpdateSessionTitleAsync(Guid sessionId, string title)
    {
        throw new NotImplementedException();
    }
}