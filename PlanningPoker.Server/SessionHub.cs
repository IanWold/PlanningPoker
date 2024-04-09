using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace PlanningPoker.Server;

public class SessionHub(IDatabase database) : Hub<ISessionHubClient>, ISessionHub
{
    private async Task<Session> GetSessionNewAsync(Guid sessionId)
    {
        var participantIds = await database.ListRangeAsync($"{sessionId}:participants");
        var participantKeys = participantIds.Select(i => $"{sessionId}:participants:{i}");
        var batch = database.CreateBatch();

        var sessionTask = batch.HashGetAllAsync(sessionId.ToString());
        var participantTasks = participantKeys.Select(k => (key: k, task: batch.HashGetAllAsync(k)));
        batch.Execute();

        var sessionLocker = new {};
        var session = new Session("", [], State.Hidden);

        Task[] readTasks = [
            Task.Run(async () => {
                var sessionHash = await sessionTask;
                lock (sessionLocker)
                {
                    session = session with {
                        Title = sessionHash.Single(h => h.Name == nameof(Session.Title)).Value!,
                        State = Enum.Parse<State>((string)sessionHash.Single(h => h.Name == nameof(Session.State)).Value!)
                    };
                }
            }),
            ..participantTasks.Select(t => Task.Run(async () => {
                var participantHash = await t.task;
                var participant = new Participant(
                    t.key,
                    participantHash.Single(h => h.Name == nameof(Participant.Name)).Value!,
                    participantHash.Single(h => h.Name == nameof(Participant.Points)).Value!
                );
                lock (sessionLocker)
                {
                    session = session with {
                        Participants = [..session.Participants, participant]
                    };
                }
            }))
        ];

        await Task.WhenAll(readTasks);

        return session;
    }

    public async Task<Session> ConnectToSessionAsync(Guid sessionId)
    {
        if (!await database.KeyExistsAsync(sessionId.ToString()))
        {
            throw new InvalidOperationException($"Session {sessionId} does not exist.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId.ToString());

        return await GetSessionNewAsync(sessionId) ?? throw new Exception($"Unable to read session {sessionId}.");
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

            insertResult =
                await transaction.HashSetAsync(key: newGuid.ToString(),
                    hashFields: [
                        new HashEntry(nameof(Session.Title), title),
                        new HashEntry(nameof(Session.State), Enum.GetName(State.Hidden))
                    ]
                )
                .ContinueWith(_ => transaction.Execute());
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
            ]
        );

        await database.ListRightPushAsync($"{sessionId}:participants", Context.ConnectionId);

        await Clients.Group(sessionId.ToString()).OnParticipantAdded(Context.ConnectionId, name);
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId.ToString());

        return Context.ConnectionId;
    }

    public async Task DisconnectFromSessionAsync(Guid sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId.ToString());

        await database.ListRemoveAsync($"{sessionId}:participants", Context.ConnectionId);
        await database.KeyDeleteAsync($"{sessionId}:participants:{Context.ConnectionId}");

        if (await database.ListLengthAsync($"{sessionId}:participants") == 0)
        {
            await database.KeyDeleteAsync(sessionId.ToString());
            await database.KeyDeleteAsync($"{sessionId}:participants");
        }
        else
        {
            await Clients.OthersInGroup(sessionId.ToString()).OnParticipantRemoved(Context.ConnectionId);
        }
    }

    public async Task UpdateParticipantPointsAsync(Guid sessionId, string points)
    {
        points = points.Trim();

        if (await database.HashGetAsync(sessionId.ToString(), nameof(Session.State)) is var stateString && !Enum.TryParse<State>(stateString, out var state))
        {
            throw new InvalidOperationException($"Session {sessionId} does not exist.");
        }

        if (!database.KeyExists($"{sessionId}:participants:{Context.ConnectionId}"))
        {
            throw new ArgumentException($"There is no participant with id '{Context.ConnectionId}' in this session.");
        }

        await database.HashSetAsync($"{sessionId}:participants:{Context.ConnectionId}", [ new HashEntry(nameof(Participant.Points), points) ]);

        await Clients.OthersInGroup(sessionId.ToString()).OnParticipantPointsUpdated(
            Context.ConnectionId,
            state == State.Hidden && !string.IsNullOrEmpty(points)
                ? "points"
                : points
        );
    }

    public async Task UpdateSessionStateAsync(Guid sessionId, State state)
    {
        if (await database.HashGetAsync(sessionId.ToString(), nameof(Session.State)) is var stateString && !Enum.TryParse<State>(stateString, out var currentState))
        {
            throw new InvalidOperationException($"Session {sessionId} does not exist.");
        }

        if (currentState == state)
        {
            return;
        }

        if (state == State.Hidden)
        {
            await Clients.Group(sessionId.ToString()).OnHide();
        }
        else
        {
            var session = await GetSessionNewAsync(sessionId);
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

        if (!await database.KeyExistsAsync(sessionId.ToString()))
        {
            throw new InvalidOperationException($"Session {sessionId} does not exist.");
        }

        await database.HashSetAsync(sessionId.ToString(), [ new HashEntry(nameof(Session.Title), title) ]);
        
        await Clients.OthersInGroup(sessionId.ToString()).OnTitleUpdated(title);
    }
}
