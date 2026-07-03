using Testcontainers.Redis;
using Xunit;

namespace PlanningPoker.IntegrationTests.Infrastructure;

public class RedisFixture : IAsyncLifetime {
    public RedisContainer Container { get; } = new RedisBuilder("redis:7.0").Build();

    public Task InitializeAsync() =>
        Container.StartAsync();

    public Task DisposeAsync() =>
        Container.DisposeAsync().AsTask();
}
