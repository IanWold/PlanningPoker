using PlanningPoker.IntegrationTests.Infrastructure;
using Xunit;

namespace PlanningPoker.IntegrationTests;

public abstract class SingleClientBehaviorTests(ITestHarness harness) : IAsyncLifetime {
    public Task InitializeAsync() =>
        Task.CompletedTask;

    public Task DisposeAsync() =>
        harness.DisposeAsync().AsTask();

    [Fact]
    public async Task CreateAsync_EstablishesInitialSessionState() {
        var client = await harness.CreateClientAsync();

        await client.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3", "5", "8"]);
        await client.WaitForAsync(() => client.Self is not null);

        Assert.Equal("Sprint Planning", client.Session!.Title);
        Assert.Equal("Alice", client.Self!.Name);
        Assert.Equal(State.Hidden, client.Session.State);
        Assert.Equal(["1", "2", "3", "5", "8"], client.Session.Points);
    }

    [Fact]
    public async Task UpdatePoints_SetsSelfPoints() {
        var client = await harness.CreateClientAsync();
        await client.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3"]);
        await client.WaitForAsync(() => client.Self is not null);

        client.UpdatePoints("2");

        await client.WaitForAsync(() => client.Self!.Points == "2");
    }

    [Fact]
    public async Task UpdatePoints_TogglesOffWhenReselectingTheSameOption() {
        var client = await harness.CreateClientAsync();
        await client.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3"]);
        await client.WaitForAsync(() => client.Self is not null);
        client.UpdatePoints("2");
        await client.WaitForAsync(() => client.Self!.Points == "2");

        client.UpdatePoints("2");

        await client.WaitForAsync(() => client.Self!.Points == "");
    }

    [Fact]
    public async Task AddPoint_AddsNewPointOption() {
        var client = await harness.CreateClientAsync();
        await client.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3"]);

        client.AddPoint("13");

        await client.WaitForAsync(() => client.Session!.Points.Contains("13"));
    }

    [Fact]
    public async Task RemovePoint_RemovesExistingPointOption() {
        var client = await harness.CreateClientAsync();
        await client.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3"]);

        client.RemovePoint("2");

        await client.WaitForAsync(() => !client.Session!.Points.Contains("2"));
    }

    [Fact]
    public async Task UpdateTitleAsync_SetsOwnTitle() {
        var client = await harness.CreateClientAsync();
        await client.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3"]);

        await client.UpdateTitleAsync("Sprint 5 Planning");

        Assert.Equal("Sprint 5 Planning", client.Session!.Title);
    }

    [Fact]
    public async Task UpdateNameAsync_SetsOwnName() {
        var client = await harness.CreateClientAsync();
        await client.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3"]);
        await client.WaitForAsync(() => client.Self is not null);

        await client.UpdateNameAsync("Alicia");

        Assert.Equal("Alicia", client.Self!.Name);
    }
}
