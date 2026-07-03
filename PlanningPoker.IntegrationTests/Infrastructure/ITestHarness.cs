namespace PlanningPoker.IntegrationTests.Infrastructure;

/// <summary>
/// Produces <see cref="TestSessionState"/> clients for a behavior test without the test knowing
/// whether they're backed by InMemoryStore or RedisStore, or by one server instance or two.
/// </summary>
public interface ITestHarness : IAsyncDisposable {
    Task<TestSessionState> CreateClientAsync();
}
