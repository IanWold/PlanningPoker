using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using System.Text.Json;

namespace PlanningPoker.Server;

public class SessionHub(IDatabase database) : Hub<ISessionHubClient>, ISessionHub
{
    private async Task<Session?> GetSessionAsync(Guid sessionId) =>
        await database.StringGetAsync(sessionId.ToString()) is RedisValue session
        && session.HasValue
        ? JsonSerializer.Deserialize<Session>((string)session!)
        : null;

    private async Task UpdateSessionAsync(Guid sessionId, Session session) =>
        await database.StringSetAsync(sessionId.ToString(), JsonSerializer.Serialize(session));

    public async Task<Session> ConnectToSessionAsync(Guid sessionId)
    {
        if (!await database.KeyExistsAsync(sessionId.ToString()))
        {
            throw new InvalidOperationException($"Session {sessionId} does not exist.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId.ToString());

        return await GetSessionAsync(sessionId) ?? throw new Exception($"Unable to read session {sessionId}.");
    }

    public async Task<Guid> CreateSessionAsync(string title)
    {
        title = title.Trim();

        if (string.IsNullOrEmpty(title))
        {
            throw new ArgumentException($"There must be a title.");
        }

        Guid newGuid;
        bool insertResult;

        var newSession = new Session(title, [], State.Hidden);
        var newSessionJson = JsonSerializer.Serialize(newSession);

        do
        {
            newGuid = Guid.NewGuid();
            insertResult = await database.StringSetAsync(newGuid.ToString(), newSessionJson, when: When.NotExists);
        }
        while (!insertResult);

        await Groups.AddToGroupAsync(Context.ConnectionId, newGuid.ToString());

        return newGuid;
    }

    public async Task JoinSessionAsync(Guid sessionId, string name)
    {
        name = name.Trim();

        if (await GetSessionAsync(sessionId) is not Session session)
        {
            throw new InvalidOperationException($"Session {sessionId} does not exist.");
        }

        if (session.Participants.Any(p => p.Name.ToUpperInvariant() == name.ToUpperInvariant()))
        {
            throw new ArgumentException($"There is already a participant with the name '{name}' in this session.");
        }

        await UpdateSessionAsync(sessionId, session with {
            Participants = [.. session.Participants, new(name, "")]
        });

        await Clients.Group(sessionId.ToString()).OnParticipantAdded(name);
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId.ToString());
    }

    public async Task DisconnectFromSessionAsync(Guid sessionId, string name)
    {
        name = name.Trim();
        
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId.ToString());

        if (await GetSessionAsync(sessionId) is not Session session)
        {
            throw new InvalidOperationException($"Session {sessionId} does not exist.");
        }

        if (!session.Participants.Any(p => p.Name.ToUpperInvariant() == name.ToUpperInvariant()))
        {
            return;
        }

        await UpdateSessionAsync(sessionId, session with {
            Participants =
                session.Participants
                .Where(p => p.Name.ToUpperInvariant() != name.ToUpperInvariant())
                .ToList()
        });

        if (session.Participants.Count() == 1)
        {
            await database.KeyDeleteAsync(sessionId.ToString());
        }
        else
        {
            await Clients.OthersInGroup(sessionId.ToString()).OnParticipantRemoved(name);
        }
    }

    public async Task UpdateParticipantPointsAsync(Guid sessionId, string name, string points)
    {
        name = name.Trim();
        points = points.Trim();

        if (await GetSessionAsync(sessionId) is not Session session)
        {
            throw new InvalidOperationException($"Session {sessionId} does not exist.");
        }

        if (!session.Participants.Any(p => p.Name.ToUpperInvariant() == name.ToUpperInvariant()))
        {
            throw new ArgumentException($"There is no participant with the name '{name}' in this session.");
        }

        await UpdateSessionAsync(sessionId, session with {
            Participants =
                session.Participants
                .Select(p =>
                    p.Name.ToUpperInvariant() == name.ToUpperInvariant()
                        ? new(name, points)
                        : p
                )
                .ToList()
        });

        await Clients.OthersInGroup(sessionId.ToString()).OnParticipantPointsUpdated(
            name,
            session.State == State.Hidden && !string.IsNullOrEmpty(points)
                ? "points"
                : points
        );
    }

    public async Task UpdateSessionStateAsync(Guid sessionId, State state)
    {
        if (await GetSessionAsync(sessionId) is not Session session)
        {
            throw new InvalidOperationException($"Session {sessionId} does not exist.");
        }

        if (session.State == state)
        {
            return;
        }

        await UpdateSessionAsync(sessionId, session with {
            State = state,
            Participants = state == State.Hidden
                ? session.Participants.Select(p => new Participant(p.Name, "")).ToList()
                : session.Participants
        });
        
        if (state == State.Hidden)
        {
            await Clients.Group(sessionId.ToString()).OnHide();
        }
        else
        {
            await Clients.Group(sessionId.ToString()).OnReveal(session.Participants);
        }
    }

    public async Task UpdateSessionTitleAsync(Guid sessionId, string title)
    {
        title = title.Trim();

        if (string.IsNullOrEmpty(title))
        {
            throw new ArgumentException($"There must be a title.");
        }

        if (await GetSessionAsync(sessionId) is not Session session)
        {
            throw new InvalidOperationException($"Session {sessionId} does not exist.");
        }

        await UpdateSessionAsync(sessionId, session with {
            Title = title
        });
        
        await Clients.OthersInGroup(sessionId.ToString()).OnTitleUpdated(title);
    }
}
