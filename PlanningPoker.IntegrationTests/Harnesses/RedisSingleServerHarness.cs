using PlanningPoker.Client;

namespace PlanningPoker.IntegrationTests;

public class RedisSingleServerHarness(RedisFixture redis) : ITestHarness {
    readonly PlanningPokerFactory _factory = new(redis.Container.GetConnectionString());
    readonly List<Client.Client> _clients = [];

    public Task<(Client.Client Client, SessionStore Store, ToastStore Toasts)> CreateClientAsync() {
        var store = new SessionStore();
        var toasts = new ToastStore();
        var client = new Client.Client(store, toasts, new TestSessionTransport(_factory.Server.BaseAddress, _factory.Server.CreateHandler), new TestEncryptionService());
        _clients.Add(client);
        return Task.FromResult((client, store, toasts));
    }

    public async ValueTask DisposeAsync() {
        foreach (var client in _clients) {
            await client.LeaveAsync();
        }

        await _factory.DisposeAsync();
    }
}
