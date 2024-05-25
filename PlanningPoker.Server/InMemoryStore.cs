namespace PlanningPoker.Server;

public class InMemoryStore : IStore {
    static readonly Dictionary<string, Session> _sessions = [];

    private static Task UpdateSession(string sessionId, Func<Session, Session> update) {
        var session = _sessions[sessionId];
        _sessions[sessionId] = update(session);
        return Task.CompletedTask;
    }

    private static Task UpdateParticipant(string sessionId, string participantId, Func<Participant, Participant> update) {
        var participant = _sessions[sessionId].Participants.SingleOrDefault(p => p.ParticipantId == participantId);
        return UpdateSession(sessionId, session => session with { Participants = [ ..session.Participants.Except([participant]), update(participant!) ] });
    }

    public Task CreateParticipantAsync(string sessionId, string participantId, string name) =>
        UpdateSession(sessionId, session => session with {
            Participants = [..session.Participants, new(participantId, name, "", 0) ]
        });

    public Task AddPointAsync(string sessionId, string point) =>
        UpdateSession(sessionId, session => session with {
            Points = [..session.Points, point]
        });

    public Task<string> CreateSessionAsync(string title, IEnumerable<string> points) {
        string newSessionId;

        do {
            newSessionId = Guid.NewGuid().ToString().Split('-').First();
        }
        while (_sessions.ContainsKey(newSessionId));

        _sessions.Add(newSessionId, new(title, [], State.Hidden, points));

        return Task.FromResult(newSessionId);
    }

    public Task DeleteParticipantAsync(string sessionId, string participantId) =>
        UpdateSession(sessionId, session => session with {
            Participants = session.Participants.Where(p => p.ParticipantId != participantId).ToArray()
        });

    public Task<bool> ExistsSessionAsync(string sessionId) =>
        Task.FromResult(_sessions.ContainsKey(sessionId));

    public Task<Session?> GetSessionAsync(string sessionId) =>
        Task.FromResult(_sessions.TryGetValue(sessionId, out var session) ? session : null);

    public Task IncrementParticipantStarsAsync(string sessionId, string participantId, int count = 1) =>
        UpdateParticipant(sessionId, participantId, participant => participant with {
            Stars = participant.Stars + count
        });

    public Task RemovePointAsync(string sessionId, string point) =>
        UpdateSession(sessionId, session => session with {
            Points = session.Points.Except([point]).ToArray()
        });

    public Task UpdateAllParticipantPointsAsync(string sessionId, string points = "") =>
        UpdateSession(sessionId, session => session with {
            Participants = session.Participants.Select(p => p with { Points = points}).ToArray()
        });

    public Task UpdateParticipantNameAsync(string sessionId, string participantId, string name) =>
        UpdateParticipant(sessionId, participantId, participant => participant with {
            Name = name
        });

    public Task UpdateParticipantPointsAsync(string sessionId, string participantId, string points) =>
        UpdateParticipant(sessionId, participantId, participant => participant with {
            Points = points
        });

    public Task UpdateSessionStateAsync(string sessionId, State state) =>
        UpdateSession(sessionId, session => session with {
            State = state
        });

    public Task UpdateSessionTitleAsync(string sessionId, string title) =>
        UpdateSession(sessionId, session => session with {
            Title = title
        });
}
