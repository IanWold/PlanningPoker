using StackExchange.Redis;

namespace PlanningPoker.Server;

public class RedisStore(IDatabase database) : IStore
{
    public async Task AddPointAsync(string sessionId, string point) =>
        await database.ListRightPushAsync($"{sessionId}:points", point, flags: CommandFlags.FireAndForget);

    public async Task CreateParticipantAsync(string sessionId, string participantId, string name) {
        await database.HashSetAsync(
            key: $"{sessionId}:participants:{participantId}",
            hashFields: [
                new HashEntry(nameof(Participant.Name), name),
                new HashEntry(nameof(Participant.Points), ""),
                new HashEntry(nameof(Participant.Stars), 0)
            ],
            flags: CommandFlags.FireAndForget
        );

        await database.ListRightPushAsync($"{sessionId}:participants", participantId, flags: CommandFlags.FireAndForget);

        await database.KeyExpireAsync($"{sessionId}:participants:{participantId}", DateTime.UtcNow.AddDays(1), flags: CommandFlags.FireAndForget);
        await database.KeyExpireAsync($"{sessionId}:participants", DateTime.UtcNow.AddDays(1), when: ExpireWhen.HasNoExpiry, flags: CommandFlags.FireAndForget);
    }

    public async Task<string> CreateSessionAsync(string title, IEnumerable<string> points) {
        string newSessionId;
        bool insertResult;

        do {
            newSessionId = Guid.NewGuid().ToString().Split('-').First();

            var transaction = database.CreateTransaction();

            transaction.AddCondition(Condition.KeyNotExists(newSessionId));
            var _ = transaction.HashSetAsync(
                key: newSessionId,
                hashFields: [
                    new HashEntry(nameof(Session.Title), title),
                    new HashEntry(nameof(Session.State), Enum.GetName(State.Hidden))
                ]
            );

            insertResult = await transaction.ExecuteAsync();
        }
        while (!insertResult);

#pragma warning disable CS4014 // Fire and forget
        Task.Run(async () => {
            foreach (var point in points) {
                await database.ListRightPushAsync($"{newSessionId}:points", point);
            }
        });
#pragma warning restore CS4014

        await database.KeyExpireAsync($"{newSessionId}", DateTime.UtcNow.AddDays(1), flags: CommandFlags.FireAndForget);
        await database.KeyExpireAsync($"{newSessionId}:points", DateTime.UtcNow.AddDays(1), flags: CommandFlags.FireAndForget);

        return newSessionId;
    }
    
    public async Task DeleteParticipantAsync(string sessionId, string participantId) {
        await database.ListRemoveAsync($"{sessionId}:participants", participantId, flags: CommandFlags.FireAndForget);
        await database.KeyDeleteAsync($"{sessionId}:participants:{participantId}", flags: CommandFlags.FireAndForget);

        if (await database.ListLengthAsync($"{sessionId}:participants") == 0) {
            await database.KeyDeleteAsync(sessionId, flags: CommandFlags.FireAndForget);
            await database.KeyDeleteAsync($"{sessionId}:participants", flags: CommandFlags.FireAndForget);
            await database.KeyDeleteAsync($"{sessionId}:points", flags: CommandFlags.FireAndForget);
        }
    }

    public async Task<bool> ExistsSessionAsync(string sessionId) =>
        await database.KeyExistsAsync(sessionId);

    public async Task<Session?> GetSessionAsync(string sessionId) {
        var participantIds = await database.ListRangeAsync($"{sessionId}:participants");

        var getSessionTask = database.HashGetAllAsync(sessionId);
        var getPointsTask = database.ListRangeAsync($"{sessionId}:points");
        var getParticipantTasks = participantIds.Select(i => (id: i, task: database.HashGetAllAsync($"{sessionId}:participants:{i}")));

        await Task.WhenAll([getSessionTask, getPointsTask, .. getParticipantTasks.Select(p => p.task)]);

        return getSessionTask.Result.Length > 0
            ? new Session(
                Title: getSessionTask.Result.Single(h => h.Name == nameof(Session.Title)).Value!,
                Participants: getParticipantTasks.Select(p => new Participant(
                    p.id!,
                    p.task.Result.Single(h => h.Name == nameof(Participant.Name)).Value!,
                    p.task.Result.Single(h => h.Name == nameof(Participant.Points)).Value!,
                    (int)p.task.Result.Single(h => h.Name == nameof(Participant.Stars)).Value
                )),
                State: Enum.Parse<State>((string)getSessionTask.Result.Single(h => h.Name == nameof(Session.State)).Value!),
                Points: getPointsTask.Result?.Select(v => (string)v!)?.ToArray() ?? []
            )
            : null;
    }

    public async Task IncrementParticipantStarsAsync(string sessionId, string participantId, int count = 1) =>
        await database.HashIncrementAsync($"{sessionId}:participants:{participantId}", nameof(Participant.Stars), count, flags: CommandFlags.FireAndForget);

    public async Task RemovePointAsync(string sessionId, string point) =>
        await database.ListRemoveAsync($"{sessionId}:points", point, flags: CommandFlags.FireAndForget);

    public async Task UpdateAllParticipantPointsAsync(string sessionId, string points = "") {
        var participantIds = await database.ListRangeAsync($"{sessionId}:participants");
        Parallel.ForEach(participantIds, i => database.HashSet($"{sessionId}:participants:{i}", [ new HashEntry(nameof(Participant.Points), points) ], flags: CommandFlags.FireAndForget));
    }

    public async Task UpdateParticipantId(string sessionId, string oldParticipantId, string newParticipantId) {
        await database.ListRemoveAsync($"{sessionId}:participants", oldParticipantId, 0, CommandFlags.FireAndForget);
        await database.ListRightPushAsync($"{sessionId}:participants", newParticipantId, flags: CommandFlags.FireAndForget);

        var oldParticipant = await database.HashGetAllAsync($"{sessionId}:participants:{oldParticipantId}");

        await database.KeyDeleteAsync($"{sessionId}:participants:{oldParticipantId}", CommandFlags.FireAndForget);
        await database.HashSetAsync(
            key: $"{sessionId}:participants:{newParticipantId}",
            hashFields: [
                new HashEntry(nameof(Participant.Name), oldParticipant.Single(e => e.Name == nameof(Participant.Name)).Value),
                new HashEntry(nameof(Participant.Points), oldParticipant.Single(e => e.Name == nameof(Participant.Points)).Value),
                new HashEntry(nameof(Participant.Stars), oldParticipant.Single(e => e.Name == nameof(Participant.Stars)).Value)
            ],
            flags: CommandFlags.FireAndForget
        );

        await database.KeyExpireAsync($"{sessionId}:participants:{newParticipantId}", DateTime.UtcNow.AddDays(1), flags: CommandFlags.FireAndForget);
        await database.KeyExpireAsync($"{sessionId}:participants", DateTime.UtcNow.AddDays(1), when: ExpireWhen.HasNoExpiry, flags: CommandFlags.FireAndForget);
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
