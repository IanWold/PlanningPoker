using PlanningPoker.Client;

namespace PlanningPoker.IntegrationTests;

// Alternates which of the two servers each new client connects to, so a two-client test
// exercises the Redis backplane relaying a broadcast from one server instance to the other.
public class RedisDualServerHarness(RedisFixture redis) : ITestHarness {
    readonly PlanningPokerFactory _factoryA = new(redis.Container.GetConnectionString());
    readonly PlanningPokerFactory _factoryB = new(redis.Container.GetConnectionString());
    readonly List<Client.Client> _clients = [];
    int _next = -1;

    public Task<(Client.Client Client, SessionStore Store)> CreateClientAsync() {
        var factory = Interlocked.Increment(ref _next) % 2 == 0 ? _factoryA : _factoryB;
        var store = new SessionStore();
        var client = new Client.Client(store, new ToastStore(), new TestSessionTransport(factory.Server.BaseAddress, factory.Server.CreateHandler), new TestEncryptionService());
        _clients.Add(client);
        return Task.FromResult((client, store));
    }

    public async ValueTask DisposeAsync() {
        foreach (var client in _clients) {
            await client.LeaveAsync();
        }

        await _factoryA.DisposeAsync();
        await _factoryB.DisposeAsync();
    }
}
