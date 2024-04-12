using System.Collections.Concurrent;
using StackExchange.Redis;

namespace PlanningPoker.Server;

public class RedisStore(IDatabase database) : IStore
{
    public async Task CreateParticipantAsync(Guid sessionId, string participantId, string name)
    {
        await database.HashSetAsync(
            $"{sessionId}:participants:{participantId}",
            [
                new HashEntry(nameof(Participant.Name), name),
                new HashEntry(nameof(Participant.Points), "")
            ],
            flags: CommandFlags.FireAndForget
        );

        await database.ListRightPushAsync($"{sessionId}:participants", participantId, flags: CommandFlags.FireAndForget);
    }

    public async Task<Guid> CreateSessionAsync(string title)
    {
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

        return newGuid;
    }
    
    public async Task DeleteParticipantAsync(Guid sessionId, string participantId)
    {
        await database.ListRemoveAsync($"{sessionId}:participants", participantId, flags: CommandFlags.FireAndForget);
        await database.KeyDeleteAsync($"{sessionId}:participants:{participantId}", flags: CommandFlags.FireAndForget);

        if (await database.ListLengthAsync($"{sessionId}:participants") == 0)
        {
            await database.KeyDeleteAsync(sessionId.ToString(), flags: CommandFlags.FireAndForget);
            await database.KeyDeleteAsync($"{sessionId}:participants", flags: CommandFlags.FireAndForget);
        }
    }

    public async Task<bool> ExistsSessionAsync(Guid sessionId)
    {
        return await database.KeyExistsAsync(sessionId.ToString());
    }

    public async Task<Session?> GetSessionAsync(Guid sessionId)
    {
        var participantIds = await database.ListRangeAsync($"{sessionId}:participants");

        var session = new Session("", [], State.Hidden);
        var participants = new ConcurrentBag<Participant>();

        Task[] readTasks = [
            database
                .HashGetAllAsync(sessionId.ToString())
                .ContinueWith(s =>
                    s.Result.Length > 0
                        ? session = new Session(
                            Title: s.Result.Single(h => h.Name == nameof(Session.Title)).Value!,
                            Participants: [],
                            State: Enum.Parse<State>((string)s.Result.Single(h => h.Name == nameof(Session.State)).Value!)
                        )
                        : null
                ),
            ..participantIds.Select(i =>
                database
                    .HashGetAllAsync($"{sessionId}:participants:{i}")
                    .ContinueWith(p =>
                        participants.Add(new(
                            i!,
                            p.Result.Single(h => h.Name == nameof(Participant.Name)).Value!,
                            p.Result.Single(h => h.Name == nameof(Participant.Points)).Value!
                        ))
                    )
            )
        ];

        await Task.WhenAll(readTasks);

        if (session is not null)
        {
            session = session with { Participants = participants.ToArray() };
        }

        return session;
    }
    
    public async Task UpdateAllParticipantPointsAsync(Guid sessionId, string points = "")
    {
        var participantIds = await database.ListRangeAsync($"{sessionId}:participants");
        Parallel.ForEach(participantIds, i => database.HashSet($"{sessionId}:participants:{i}", [ new HashEntry(nameof(Participant.Points), points) ], flags: CommandFlags.FireAndForget));
    }

    public async Task UpdateParticipantNameAsync(Guid sessionId, string participantId, string name)
    {
        await database.HashSetAsync($"{sessionId}:participants:{participantId}", [ new HashEntry(nameof(Participant.Name), name) ], flags: CommandFlags.FireAndForget);
    }
    
    public async Task UpdateParticipantPointsAsync(Guid sessionId, string participantId, string points)
    {
        await database.HashSetAsync($"{sessionId}:participants:{participantId}", [ new HashEntry(nameof(Participant.Points), points) ], flags: CommandFlags.FireAndForget);
    }

    public async Task UpdateSessionStateAsync(Guid sessionId, State state)
    {
        await database.HashSetAsync(sessionId.ToString(), [ new HashEntry(nameof(Session.State), Enum.GetName(state)) ], flags: CommandFlags.FireAndForget);
    }

    public async Task UpdateSessionTitleAsync(Guid sessionId, string title)
    {
        await database.HashSetAsync(sessionId.ToString(), [ new HashEntry(nameof(Session.Title), title) ], flags: CommandFlags.FireAndForget);
    }
}
