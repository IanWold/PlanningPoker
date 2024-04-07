using Microsoft.AspNetCore.SignalR;
using static PlanningPoker.Server.TestState;

namespace PlanningPoker.Server;

public class SessionHub : Hub<ISessionHubClient>, ISessionHub
{
    public async Task<Session> ConnectToSessionAsync(Guid sessionId)
    {
        if (!Sessions.TryGetValue(sessionId, out Session? value))
        {
            throw new InvalidOperationException($"Session {sessionId} does not exist.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId.ToString());

        return value;
    }

    public async Task<Guid> CreateSessionAsync(string title)
    {
        title = title.Trim();

        if (string.IsNullOrEmpty(title))
        {
            throw new ArgumentException($"There must be a title.");
        }

        Guid newGuid;

        do
        {
            newGuid = Guid.NewGuid();
        }
        while (Sessions.ContainsKey(newGuid));

        Sessions.Add(newGuid, new(title, [], State.Hidden));

        await Groups.AddToGroupAsync(Context.ConnectionId, newGuid.ToString());

        return newGuid;
    }

    public async Task JoinSessionAsync(Guid sessionId, string name)
    {
        name = name.Trim();

        if (!Sessions.TryGetValue(sessionId, out Session? value))
        {
            throw new InvalidOperationException($"Session {sessionId} does not exist.");
        }

        if (value.Participants.Any(p => p.Name.ToUpperInvariant() == name.ToUpperInvariant()))
        {
            throw new ArgumentException($"There is already a participant with the name '{name}' in this session.");
        }

        Sessions[sessionId] = value with {
            Participants = [.. value.Participants, new(name, "")]
        };

        await Clients.Group(sessionId.ToString()).OnParticipantAdded(name);
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId.ToString());
    }

    public async Task DisconnectFromSessionAsync(Guid sessionId, string name)
    {
        name = name.Trim();
        
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId.ToString());

        if (!Sessions.TryGetValue(sessionId, out Session? value))
        {
            throw new InvalidOperationException($"Session {sessionId} does not exist.");
        }

        if (!value.Participants.Any(p => p.Name.ToUpperInvariant() == name.ToUpperInvariant()))
        {
            return;
        }

        Sessions[sessionId] = value with {
            Participants =
                value.Participants
                .Where(p => p.Name.ToUpperInvariant() != name.ToUpperInvariant())
                .ToList()
        };

        if (value.Participants.Count() == 1)
        {
            Sessions.Remove(sessionId);
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

        if (!Sessions.TryGetValue(sessionId, out Session? value))
        {
            throw new InvalidOperationException($"Session {sessionId} does not exist.");
        }

        if (!value.Participants.Any(p => p.Name.ToUpperInvariant() == name.ToUpperInvariant()))
        {
            throw new ArgumentException($"There is no participant with the name '{name}' in this session.");
        }

        Sessions[sessionId] = value with {
            Participants =
                value.Participants
                .Select(p =>
                    p.Name.ToUpperInvariant() == name.ToUpperInvariant()
                        ? new(name, points)
                        : p
                )
                .ToList()
        };

        await Clients.OthersInGroup(sessionId.ToString()).OnParticipantPointsUpdated(
            name,
            value.State == State.Hidden && !string.IsNullOrEmpty(points)
                ? "points"
                : points
        );
    }

    public async Task UpdateSessionStateAsync(Guid sessionId, State state)
    {
        if (!Sessions.TryGetValue(sessionId, out Session? value))
        {
            throw new InvalidOperationException($"Session {sessionId} does not exist.");
        }

        if (value.State == state)
        {
            return;
        }

        Sessions[sessionId] = value with {
            State = state,
            Participants = state == State.Hidden
                ? value.Participants.Select(p => new Participant(p.Name, "")).ToList()
                : value.Participants
        };
        
        if (state == State.Hidden)
        {
            await Clients.Group(sessionId.ToString()).OnHide();
        }
        else
        {
            await Clients.Group(sessionId.ToString()).OnReveal(value.Participants);
        }
    }

    public async Task UpdateSessionTitleAsync(Guid sessionId, string title)
    {
        title = title.Trim();

        if (string.IsNullOrEmpty(title))
        {
            throw new ArgumentException($"There must be a title.");
        }

        if (!Sessions.TryGetValue(sessionId, out Session? value))
        {
            throw new InvalidOperationException($"Session {sessionId} does not exist.");
        }

        Sessions[sessionId] = value with {
            Title = title
        };
        
        await Clients.OthersInGroup(sessionId.ToString()).OnTitleUpdated(title);
    }
}

static class TestState
{
    public static Dictionary<Guid, Session> Sessions { get; set; } = [];
}
