namespace PlanningPoker;

public interface ISessionHub
{
    Task AddEffectAsync(string sessionId, EffectType effectType, string targetParticipantId);
    Task AddPointAsync(string sessionId, string point);
    Task<Session> ConnectToSessionAsync(string sessionId);
    Task<string> CreateSessionAsync(string title, IEnumerable<string> points);
    Task<string> JoinSessionAsync(string sessionId, string name);
    Task DisconnectFromSessionAsync(string sessionId);
    Task RemoveEffectAsync(string sessionId, Effect effect);
    Task RemovePointAsync(string sessionId, string point);
    Task SendStarToParticipantAsync(string sessionId, string participantId);
    Task UpdateParticipantPointsAsync(string sessionId, string points);
    Task UpdateSessionStateAsync(string sessionId, State state);
    Task UpdateSessionTitleAsync(string sessionId, string title);
    Task UpdateParticipantNameAsync(string sessionId, string name);
}
