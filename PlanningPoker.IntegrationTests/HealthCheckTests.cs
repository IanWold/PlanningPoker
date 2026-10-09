using System.Net;
using Xunit;

namespace PlanningPoker.IntegrationTests;

public class HealthCheckTests {
    [Fact]
    public async Task InMemory_IsLiveAndReady() {
        await using var factory = new PlanningPokerFactory();
        using var http = factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/health/ready")).StatusCode);
    }

    [Fact]
    public async Task UnreachableRedis_IsLiveButNotReady() {
        await using var factory = new PlanningPokerFactory("localhost:1,abortConnect=false");
        using var http = factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await http.GetAsync("/health/ready")).StatusCode);
    }
}

[Collection("Redis")]
public class HealthCheckTests_Redis(RedisFixture redis) {
    [Fact]
    public async Task Redis_IsLiveAndReady() {
        await using var factory = new PlanningPokerFactory(redis.Container.GetConnectionString());
        using var http = factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/health/ready")).StatusCode);
    }
}
