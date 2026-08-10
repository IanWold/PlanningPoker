using PlanningPoker.Client;

namespace PlanningPoker.IntegrationTests;

/// <summary>
/// Produces <see cref="Client.Client"/>/<see cref="SessionStore"/>/<see cref="ToastStore"/> triples for a behavior test
/// without the test knowing whether they're backed by InMemoryStore or RedisStore, or by one server instance or two.
/// </summary>
public interface ITestHarness : IAsyncDisposable {
    Task<(Client.Client Client, SessionStore Store, ToastStore Toasts)> CreateClientAsync();
}
