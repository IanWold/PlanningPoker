using System.Collections.Concurrent;
using StackExchange.Redis;

namespace PlanningPoker.Server;

public class RedisStore(IDatabase database) : IStore
{
    public async Task AddPointAsync(string sessionId, string point) =>
        await database.ListRightPushAsync($"{sessionId}:points", point, flags: CommandFlags.FireAndForget);

    public async Task CreateParticipantAsync(string sessionId, string participantId, string name)
    {
        await database.HashSetAsync(
            $"{sessionId}:participants:{participantId}",
            [
                new HashEntry(nameof(Participant.Name), name),
                new HashEntry(nameof(Participant.Points), ""),
                new HashEntry(nameof(Participant.Stars), 0)
            ],
            flags: CommandFlags.FireAndForget
        );

        await database.ListRightPushAsync($"{sessionId}:participants", participantId, flags: CommandFlags.FireAndForget);
    }

    public async Task<string> CreateSessionAsync(string title, IEnumerable<string> points)
    {
        string newSessionId;
        bool insertResult;

        do
        {
            newSessionId = Guid.NewGuid().ToString().Split('-').First();

            var transaction = database.CreateTransaction();

            transaction.AddCondition(Condition.KeyNotExists(newSessionId));
            var _ = transaction.HashSetAsync(key: newSessionId,
                hashFields: [
                    new HashEntry(nameof(Session.Title), title),
                    new HashEntry(nameof(Session.State), Enum.GetName(State.Hidden))
                ]
            );

            insertResult = await transaction.ExecuteAsync();
        }
        while (!insertResult);

#pragma warning disable CS4014 // Fire and forget
        Task.Run(async () =>
        {
            foreach (var point in points)
            {
                await database.ListRightPushAsync($"{newSessionId}:points", point);
            }
        });
#pragma warning restore CS4014

        return newSessionId;
    }
    
    public async Task DeleteParticipantAsync(string sessionId, string participantId)
    {
        await database.ListRemoveAsync($"{sessionId}:participants", participantId, flags: CommandFlags.FireAndForget);
        await database.KeyDeleteAsync($"{sessionId}:participants:{participantId}", flags: CommandFlags.FireAndForget);

        if (await database.ListLengthAsync($"{sessionId}:participants") == 0)
        {
            await database.KeyDeleteAsync(sessionId, flags: CommandFlags.FireAndForget);
            await database.KeyDeleteAsync($"{sessionId}:participants", flags: CommandFlags.FireAndForget);
        }
    }

    public async Task<bool> ExistsSessionAsync(string sessionId) =>
        await database.KeyExistsAsync(sessionId);

    public async Task<Session?> GetSessionAsync(string sessionId)
    {
        var participantIds = await database.ListRangeAsync($"{sessionId}:participants");

        var session = new Session("", [], State.Hidden, []);
        var participants = new ConcurrentBag<Participant>();
        var points = Enumerable.Empty<string>();

        Task[] readTasks = [
            database
                .HashGetAllAsync(sessionId)
                .ContinueWith(s =>
                    s.Result.Length > 0
                        ? session = new Session(
                            Title: s.Result.Single(h => h.Name == nameof(Session.Title)).Value!,
                            Participants: [],
                            State: Enum.Parse<State>((string)s.Result.Single(h => h.Name == nameof(Session.State)).Value!),
                            Points: []
                        )
                        : null
                ),
            database.ListRangeAsync($"{sessionId}:points")
                .ContinueWith(p => points = p?.Result?.Select(v => (string)v!)?.ToArray() ?? []),
            ..participantIds.Select(i =>
                database
                    .HashGetAllAsync($"{sessionId}:participants:{i}")
                    .ContinueWith(p =>
                        participants.Add(new(
                            i!,
                            p.Result.Single(h => h.Name == nameof(Participant.Name)).Value!,
                            p.Result.Single(h => h.Name == nameof(Participant.Points)).Value!,
                            (int)p.Result.Single(h => h.Name == nameof(Participant.Stars)).Value
                        ))
                    )
            )
        ];

        await Task.WhenAll(readTasks);

        if (session is not null)
        {
            session = session with
            {
                Participants = [.. participants],
                Points = points
            };
        }

        return session;
    }

    public async Task IncrementParticipantStarsAsync(string sessionId, string participantId, int count = 1) =>
        await database.HashIncrementAsync($"{sessionId}:participants:{participantId}", nameof(Participant.Stars), count, flags: CommandFlags.FireAndForget);

    public async Task RemovePointAsync(string sessionId, string point) =>
        await database.ListRemoveAsync($"{sessionId}:points", point, flags: CommandFlags.FireAndForget);

    public async Task UpdateAllParticipantPointsAsync(string sessionId, string points = "")
    {
        var participantIds = await database.ListRangeAsync($"{sessionId}:participants");
        Parallel.ForEach(participantIds, i => database.HashSet($"{sessionId}:participants:{i}", [ new HashEntry(nameof(Participant.Points), points) ], flags: CommandFlags.FireAndForget));
    }

    public async Task UpdateParticipantNameAsync(string sessionId, string participantId, string name) =>
        await database.HashSetAsync($"{sessionId}:participants:{participantId}", [new HashEntry(nameof(Participant.Name), name)], flags: CommandFlags.FireAndForget);

    public async Task UpdateParticipantPointsAsync(string sessionId, string participantId, string points) =>
        await database.HashSetAsync($"{sessionId}:participants:{participantId}", [new HashEntry(nameof(Participant.Points), points)], flags: CommandFlags.FireAndForget);

    public async Task UpdateSessionStateAsync(string sessionId, State state) =>
        await database.HashSetAsync(sessionId, [new HashEntry(nameof(Session.State), Enum.GetName(state))], flags: CommandFlags.FireAndForget);

    public async Task UpdateSessionTitleAsync(string sessionId, string title) =>
        await database.HashSetAsync(sessionId, [new HashEntry(nameof(Session.Title), title)], flags: CommandFlags.FireAndForget);
}
