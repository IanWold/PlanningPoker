using Xunit;

namespace PlanningPoker.IntegrationTests;

public class MultiClientTests_InMemory() : MultiClientTests(new InMemorySingleServerHarness());

[Collection("Redis")]
public class MultiClientTests_Redis(RedisFixture redis) : MultiClientTests(new RedisSingleServerHarness(redis));

[Collection("Redis")]
public class MultiClientTests_RedisBackplane(RedisFixture redis) : MultiClientTests(new RedisDualServerHarness(redis));

public abstract class MultiClientTests(ITestHarness harness) : IAsyncLifetime {
    public Task InitializeAsync() =>
        Task.CompletedTask;

    public Task DisposeAsync() =>
        harness.DisposeAsync().AsTask();

    [Fact]
    public async Task JoiningParticipant_IsSeenByExistingParticipant() {
        var (alice, aliceStore) = await harness.CreateClientAsync();
        await alice.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3"]);

        var (bob, _) = await harness.CreateClientAsync();
        await bob.LoadAsync(aliceStore.SessionId!);
        await bob.JoinAsync("Bob");

        await aliceStore.WaitForAsync(() => aliceStore.Others.Any(p => p.Name == "Bob"));
    }

    [Fact]
    public async Task JoiningParticipant_SeesExistingSessionState() {
        var (alice, aliceStore) = await harness.CreateClientAsync();
        await alice.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3"]);

        var (bob, bobStore) = await harness.CreateClientAsync();
        await bob.LoadAsync(aliceStore.SessionId!);

        Assert.Equal("Sprint Planning", bobStore.Session!.Title);
        Assert.Contains(bobStore.Session.Participants, p => p.Name == "Alice");
    }

    [Fact]
    public async Task AddedPoint_PropagatesToOtherParticipant() {
        var (alice, aliceStore) = await harness.CreateClientAsync();
        await alice.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3"]);

        var (bob, bobStore) = await harness.CreateClientAsync();
        await bob.LoadAsync(aliceStore.SessionId!);
        await bob.JoinAsync("Bob");
        await aliceStore.WaitForAsync(() => aliceStore.Others.Any(p => p.Name == "Bob"));

        alice.AddPoint("13");

        await bobStore.WaitForAsync(() => bobStore.Session!.Points.Contains("13"));
    }

    [Fact]
    public async Task TitleChange_PropagatesToOtherParticipant() {
        var (alice, aliceStore) = await harness.CreateClientAsync();
        await alice.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3"]);

        var (bob, bobStore) = await harness.CreateClientAsync();
        await bob.LoadAsync(aliceStore.SessionId!);
        await bob.JoinAsync("Bob");
        await aliceStore.WaitForAsync(() => aliceStore.Others.Any(p => p.Name == "Bob"));

        await alice.UpdateTitleAsync("Sprint 5 Planning");

        await bobStore.WaitForAsync(() => bobStore.Session!.Title == "Sprint 5 Planning");
    }

    [Fact]
    public async Task DeselectingPoints_PropagatesToOtherParticipant() {
        var (alice, aliceStore) = await harness.CreateClientAsync();
        await alice.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3"]);

        var (bob, bobStore) = await harness.CreateClientAsync();
        await bob.LoadAsync(aliceStore.SessionId!);
        await bob.JoinAsync("Bob");
        await aliceStore.WaitForAsync(() => aliceStore.Others.Any(p => p.Name == "Bob"));
        await bobStore.WaitForAsync(() => bobStore.Self is not null);

        bob.UpdatePoints("3");
        await aliceStore.WaitForAsync(() => aliceStore.Others.Single().Points == "3");

        bob.UpdatePoints("3");

        await aliceStore.WaitForAsync(() => aliceStore.Others.Single().Points == "");
    }

    [Fact]
    public async Task NameChange_PropagatesToOtherParticipant() {
        var (alice, aliceStore) = await harness.CreateClientAsync();
        await alice.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3"]);

        var (bob, _) = await harness.CreateClientAsync();
        await bob.LoadAsync(aliceStore.SessionId!);
        await bob.JoinAsync("Bob");
        await aliceStore.WaitForAsync(() => aliceStore.Others.Any(p => p.Name == "Bob"));

        await bob.UpdateNameAsync("Bobby");

        await aliceStore.WaitForAsync(() => aliceStore.Others.Any(p => p.Name == "Bobby"));
    }

    [Fact]
    public async Task RevealingState_ShowsOthersSelectedPoints() {
        var (alice, aliceStore) = await harness.CreateClientAsync();
        await alice.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3"]);

        var (bob, bobStore) = await harness.CreateClientAsync();
        await bob.LoadAsync(aliceStore.SessionId!);
        await bob.JoinAsync("Bob");
        await aliceStore.WaitForAsync(() => aliceStore.Others.Any(p => p.Name == "Bob"));

        bob.UpdatePoints("3");
        await aliceStore.WaitForAsync(() => aliceStore.Others.Single().Points == "3");

        alice.UpdateState(State.Revealed);

        await bobStore.WaitForAsync(() => bobStore.Session!.State == State.Revealed);
        await aliceStore.WaitForAsync(() => aliceStore.Session!.State == State.Revealed);
        Assert.Equal("3", aliceStore.Others.Single().Points);
    }

    [Fact]
    public async Task HidingState_ClearsSelectedPointsForOthers() {
        var (alice, aliceStore) = await harness.CreateClientAsync();
        await alice.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3"]);

        var (bob, bobStore) = await harness.CreateClientAsync();
        await bob.LoadAsync(aliceStore.SessionId!);
        await bob.JoinAsync("Bob");
        await aliceStore.WaitForAsync(() => aliceStore.Others.Any(p => p.Name == "Bob"));
        await bobStore.WaitForAsync(() => bobStore.Self is not null);

        bob.UpdatePoints("3");
        await aliceStore.WaitForAsync(() => aliceStore.Others.Single().Points == "3");
        alice.UpdateState(State.Revealed);
        await bobStore.WaitForAsync(() => bobStore.Session!.State == State.Revealed);

        alice.UpdateState(State.Hidden);

        await aliceStore.WaitForAsync(() => aliceStore.Session!.State == State.Hidden);
        await bobStore.WaitForAsync(() => bobStore.Self!.Points == "");
        await aliceStore.WaitForAsync(() => aliceStore.Others.Single().Points == "");
    }

    [Fact]
    public async Task SendingStar_IncrementsRecipientStarsForBothParticipants() {
        var (alice, aliceStore) = await harness.CreateClientAsync();
        await alice.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3"]);

        var (bob, bobStore) = await harness.CreateClientAsync();
        await bob.LoadAsync(aliceStore.SessionId!);
        await bob.JoinAsync("Bob");
        await aliceStore.WaitForAsync(() => aliceStore.Others.Any(p => p.Name == "Bob"));
        await bobStore.WaitForAsync(() => bobStore.Self is not null);

        alice.SendStarToParticipant(bobStore.Self!.ParticipantId);

        await bobStore.WaitForAsync(() => bobStore.Self!.Stars == 1);
        await aliceStore.WaitForAsync(() => aliceStore.Others.Single().Stars == 1);
    }

    [Fact]
    public async Task LeavingParticipant_IsRemovedForOthers() {
        var (alice, aliceStore) = await harness.CreateClientAsync();
        await alice.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3"]);

        var (bob, _) = await harness.CreateClientAsync();
        await bob.LoadAsync(aliceStore.SessionId!);
        await bob.JoinAsync("Bob");
        await aliceStore.WaitForAsync(() => aliceStore.Others.Any(p => p.Name == "Bob"));

        await bob.LeaveAsync();

        await aliceStore.WaitForAsync(() => !aliceStore.Others.Any());
    }
}
