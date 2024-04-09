using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace PlanningPoker.Server;

public class SessionHub(IDatabase database) : Hub<ISessionHubClient>, ISessionHub
{
    private async Task<Session?> GetSessionAsync(Guid sessionId)
    {
        var participantIds = await database.ListRangeAsync($"{sessionId}:participants");

        var session = new Session("", [], State.Hidden);
        var participants = new ConcurrentBag<Participant>();

        Task[] readTasks = [
            database.HashGetAllAsync(sessionId.ToString()).ContinueWith(s =>
                s.Result.Length > 0
                    ? session = new Session(
                        Title: s.Result.Single(h => h.Name == nameof(Session.Title)).Value!,
                        Participants: [],
                        State: Enum.Parse<State>((string)s.Result.Single(h => h.Name == nameof(Session.State)).Value!)
                    )
                    : null
            ),
            ..participantIds.Select(i => database.HashGetAllAsync($"{sessionId}:participants:{i}").ContinueWith(p =>
                participants.Add(new Participant(
                    i!,
                    p.Result.Single(h => h.Name == nameof(Participant.Name)).Value!,
                    p.Result.Single(h => h.Name == nameof(Participant.Points)).Value!
                )))
            )
        ];

        await Task.WhenAll(readTasks);

        if (session is not null)
        {
            session = session with { Participants = participants.ToArray() };
        }

        return session;
    }

    public async Task<Session> ConnectToSessionAsync(Guid sessionId)
    {
        if (await GetSessionAsync(sessionId) is not Session session)
        {
            throw new InvalidOperationException($"Session {sessionId} does not exist.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId.ToString());

        return session;
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

        do
        {
            newGuid = Guid.NewGuid();

            var transaction = database.CreateTransaction();

            transaction.AddCondition(Condition.KeyNotExists(newGuid.ToString()));
            var _ = transaction.HashSetAsync(key: newGuid.ToString(),
                hashFields: [
                    new HashEntry(nameof(Session.Title), title),
                    new HashEntry(nameof(Session.State), Enum.GetName(State.Hidden))
                ]
            );

            insertResult = await transaction.ExecuteAsync();
        }
        while (!insertResult);

        await Groups.AddToGroupAsync(Context.ConnectionId, newGuid.ToString());

        return newGuid;
    }

    public async Task<string> JoinSessionAsync(Guid sessionId, string name)
    {
        name = name.Trim();

        if (!await database.KeyExistsAsync(sessionId.ToString()))
        {
            throw new InvalidOperationException($"Session {sessionId} does not exist.");
        }

        await database.HashSetAsync(
            $"{sessionId}:participants:{Context.ConnectionId}",
            [
                new HashEntry(nameof(Participant.Name), name),
                new HashEntry(nameof(Participant.Points), "")
            ], flags: CommandFlags.FireAndForget
        );

        await database.ListRightPushAsync($"{sessionId}:participants", Context.ConnectionId, flags: CommandFlags.FireAndForget);

        await Clients.Group(sessionId.ToString()).OnParticipantAdded(Context.ConnectionId, name);
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId.ToString());

        return Context.ConnectionId;
    }

    public async Task DisconnectFromSessionAsync(Guid sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId.ToString());

        await database.ListRemoveAsync($"{sessionId}:participants", Context.ConnectionId, flags: CommandFlags.FireAndForget);
        await database.KeyDeleteAsync($"{sessionId}:participants:{Context.ConnectionId}", flags: CommandFlags.FireAndForget);

        if (await database.ListLengthAsync($"{sessionId}:participants") == 0)
        {
            await database.KeyDeleteAsync(sessionId.ToString(), flags: CommandFlags.FireAndForget);
            await database.KeyDeleteAsync($"{sessionId}:participants", flags: CommandFlags.FireAndForget);
        }
        else
        {
            await Clients.OthersInGroup(sessionId.ToString()).OnParticipantRemoved(Context.ConnectionId);
        }
    }

    public async Task UpdateParticipantPointsAsync(Guid sessionId, string points)
    {
        points = points.Trim();

        await database.HashSetAsync($"{sessionId}:participants:{Context.ConnectionId}", [ new HashEntry(nameof(Participant.Points), points) ], flags: CommandFlags.FireAndForget);

        await Clients.OthersInGroup(sessionId.ToString()).OnParticipantPointsUpdated(
            Context.ConnectionId,
            points
        );
    }

    public async Task UpdateSessionStateAsync(Guid sessionId, State state)
    {
        await database.HashSetAsync(sessionId.ToString(), [ new HashEntry(nameof(Session.State), Enum.GetName(state)) ], flags: CommandFlags.FireAndForget);

        if (state == State.Hidden)
        {
            await Clients.Group(sessionId.ToString()).OnHide();

            var participantIds = await database.ListRangeAsync($"{sessionId}:participants");
            Parallel.ForEach(participantIds, i => database.HashSet($"{sessionId}:participants:{i}", [ new HashEntry(nameof(Participant.Points), "") ], flags: CommandFlags.FireAndForget));
        }
        else
        {
            await Clients.Group(sessionId.ToString()).OnReveal();
        }
    }

    public async Task UpdateSessionTitleAsync(Guid sessionId, string title)
    {
        title = title.Trim();

        if (string.IsNullOrEmpty(title))
        {
            throw new ArgumentException($"There must be a title.");
        }

        await database.HashSetAsync(sessionId.ToString(), [ new HashEntry(nameof(Session.Title), title) ], flags: CommandFlags.FireAndForget);
        
        await Clients.OthersInGroup(sessionId.ToString()).OnTitleUpdated(title);
    }
}
