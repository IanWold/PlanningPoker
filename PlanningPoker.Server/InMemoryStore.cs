namespace PlanningPoker.Server;

public class InMemoryStore : IStore
{
    Task CreateParticipantAsync(Guid sessionId, string participantId, string name)
    {
        throw new NotImplementedException();
    }
    
    Task<Guid> CreateSessionAsync(string title)
    {
        throw new NotImplementedException();
    }

    Task DeleteParticipantAsync(Guid sessionId, string participantId)
    {
        throw new NotImplementedException();
    }

    Task<Session?> GetSessionAsync(Guid sessionId)
    {
        throw new NotImplementedException();
    }

    Task UpdateParticipantNameAsync(Guid sessionId, string name)
    {
        throw new NotImplementedException();
    }

    Task UpdateParticipantPointsAsync(Guid sessionId, string points)
    {
        throw new NotImplementedException();
    }

    Task UpdateSessionStateAsync(Guid sessionId, State state)
    {
        throw new NotImplementedException();
    }

    Task UpdateSessionTitleAsync(Guid sessionId, string title)
    {
        throw new NotImplementedException();
    }
}