using Xunit;

namespace PlanningPoker.IntegrationTests;

public class SingleClientTests_InMemory() : SingleClientTests(new InMemorySingleServerHarness());

[Collection("Redis")]
public class SingleClientTests_Redis(RedisFixture redis) : SingleClientTests(new RedisSingleServerHarness(redis));

public abstract class SingleClientTests(ITestHarness harness) : IAsyncLifetime {
    public Task InitializeAsync() =>
        Task.CompletedTask;

    public Task DisposeAsync() =>
        harness.DisposeAsync().AsTask();

    [Fact]
    public async Task CreateAsync_EstablishesInitialSessionState() {
        var (client, store) = await harness.CreateClientAsync();

        await client.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3", "5", "8"]);
        await store.WaitForAsync(() => store.Self is not null);

        Assert.Equal("Sprint Planning", store.Session!.Title);
        Assert.Equal("Alice", store.Self!.Name);
        Assert.Equal(State.Hidden, store.Session.State);
        Assert.Equal(["1", "2", "3", "5", "8"], store.Session.Points);
    }

    [Fact]
    public async Task UpdatePoints_SetsSelfPoints() {
        var (client, store) = await harness.CreateClientAsync();
        await client.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3"]);
        await store.WaitForAsync(() => store.Self is not null);

        client.UpdatePoints("2");

        await store.WaitForAsync(() => store.Self!.Points == "2");
    }

    [Fact]
    public async Task UpdatePoints_TogglesOffWhenReselectingTheSameOption() {
        var (client, store) = await harness.CreateClientAsync();
        await client.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3"]);
        await store.WaitForAsync(() => store.Self is not null);
        client.UpdatePoints("2");
        await store.WaitForAsync(() => store.Self!.Points == "2");

        client.UpdatePoints("2");

        await store.WaitForAsync(() => store.Self!.Points == "");
    }

    [Fact]
    public async Task AddPoint_AddsNewPointOption() {
        var (client, store) = await harness.CreateClientAsync();
        await client.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3"]);

        client.AddPoint("13");

        await store.WaitForAsync(() => store.Session!.Points.Contains("13"));
    }

    [Fact]
    public async Task RemovePoint_RemovesExistingPointOption() {
        var (client, store) = await harness.CreateClientAsync();
        await client.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3"]);

        client.RemovePoint("2");

        await store.WaitForAsync(() => !store.Session!.Points.Contains("2"));
    }

    [Fact]
    public async Task UpdateTitleAsync_SetsOwnTitle() {
        var (client, store) = await harness.CreateClientAsync();
        await client.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3"]);

        await client.UpdateTitleAsync("Sprint 5 Planning");

        Assert.Equal("Sprint 5 Planning", store.Session!.Title);
    }

    [Fact]
    public async Task UpdateNameAsync_SetsOwnName() {
        var (client, store) = await harness.CreateClientAsync();
        await client.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3"]);
        await store.WaitForAsync(() => store.Self is not null);

        await client.UpdateNameAsync("Alicia");

        Assert.Equal("Alicia", store.Self!.Name);
    }
}
