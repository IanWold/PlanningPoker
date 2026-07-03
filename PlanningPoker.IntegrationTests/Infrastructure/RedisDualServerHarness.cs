namespace PlanningPoker.IntegrationTests.Infrastructure;

// Alternates which of the two servers each new client connects to, so a two-client test
// exercises the Redis backplane relaying a broadcast from one server instance to the other.
public class RedisDualServerHarness(RedisFixture redis) : ITestHarness {
    readonly PlanningPokerFactory _factoryA = new(redis.Container.GetConnectionString());
    readonly PlanningPokerFactory _factoryB = new(redis.Container.GetConnectionString());
    readonly List<TestSessionState> _clients = [];
    int _next = -1;

    public Task<TestSessionState> CreateClientAsync() {
        var factory = Interlocked.Increment(ref _next) % 2 == 0 ? _factoryA : _factoryB;
        var client = new TestSessionState(factory.Server.BaseAddress, factory.Server.CreateHandler);
        _clients.Add(client);
        return Task.FromResult(client);
    }

    public async ValueTask DisposeAsync() {
        foreach (var client in _clients) {
            await client.LeaveAsync();
        }

        await _factoryA.DisposeAsync();
        await _factoryB.DisposeAsync();
    }
}
