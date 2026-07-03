namespace PlanningPoker.IntegrationTests;

public class RedisSingleServerHarness(RedisFixture redis) : ITestHarness {
    readonly PlanningPokerFactory _factory = new(redis.Container.GetConnectionString());
    readonly List<TestSessionState> _clients = [];

    public Task<TestSessionState> CreateClientAsync() {
        var client = new TestSessionState(_factory.Server.BaseAddress, _factory.Server.CreateHandler);
        _clients.Add(client);
        return Task.FromResult(client);
    }

    public async ValueTask DisposeAsync() {
        foreach (var client in _clients) {
            await client.LeaveAsync();
        }

        await _factory.DisposeAsync();
    }
}
