using PlanningPoker.IntegrationTests.Infrastructure;
using Xunit;

namespace PlanningPoker.IntegrationTests;

public abstract class MultiClientBehaviorTests(ITestHarness harness) : IAsyncLifetime {
    public Task InitializeAsync() =>
        Task.CompletedTask;

    public Task DisposeAsync() =>
        harness.DisposeAsync().AsTask();

    [Fact]
    public async Task JoiningParticipant_IsSeenByExistingParticipant() {
        var alice = await harness.CreateClientAsync();
        await alice.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3"]);

        var bob = await harness.CreateClientAsync();
        await bob.LoadAsync(alice.TestSessionId!);
        await bob.JoinAsync("Bob");

        await alice.WaitForAsync(() => alice.Others.Any(p => p.Name == "Bob"));
    }

    [Fact]
    public async Task JoiningParticipant_SeesExistingSessionState() {
        var alice = await harness.CreateClientAsync();
        await alice.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3"]);

        var bob = await harness.CreateClientAsync();
        await bob.LoadAsync(alice.TestSessionId!);

        Assert.Equal("Sprint Planning", bob.Session!.Title);
        Assert.Contains(bob.Session.Participants, p => p.Name == "Alice");
    }

    [Fact]
    public async Task AddedPoint_PropagatesToOtherParticipant() {
        var alice = await harness.CreateClientAsync();
        await alice.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3"]);

        var bob = await harness.CreateClientAsync();
        await bob.LoadAsync(alice.TestSessionId!);
        await bob.JoinAsync("Bob");
        await alice.WaitForAsync(() => alice.Others.Any(p => p.Name == "Bob"));

        alice.AddPoint("13");

        await bob.WaitForAsync(() => bob.Session!.Points.Contains("13"));
    }

    [Fact]
    public async Task TitleChange_PropagatesToOtherParticipant() {
        var alice = await harness.CreateClientAsync();
        await alice.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3"]);

        var bob = await harness.CreateClientAsync();
        await bob.LoadAsync(alice.TestSessionId!);
        await bob.JoinAsync("Bob");
        await alice.WaitForAsync(() => alice.Others.Any(p => p.Name == "Bob"));

        await alice.UpdateTitleAsync("Sprint 5 Planning");

        await bob.WaitForAsync(() => bob.Session!.Title == "Sprint 5 Planning");
    }

    [Fact]
    public async Task DeselectingPoints_PropagatesToOtherParticipant() {
        var alice = await harness.CreateClientAsync();
        await alice.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3"]);

        var bob = await harness.CreateClientAsync();
        await bob.LoadAsync(alice.TestSessionId!);
        await bob.JoinAsync("Bob");
        await alice.WaitForAsync(() => alice.Others.Any(p => p.Name == "Bob"));
        await bob.WaitForAsync(() => bob.Self is not null);

        bob.UpdatePoints("3");
        await alice.WaitForAsync(() => alice.Others.Single().Points == "3");

        bob.UpdatePoints("3");

        await alice.WaitForAsync(() => alice.Others.Single().Points == "");
    }

    [Fact]
    public async Task NameChange_PropagatesToOtherParticipant() {
        var alice = await harness.CreateClientAsync();
        await alice.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3"]);

        var bob = await harness.CreateClientAsync();
        await bob.LoadAsync(alice.TestSessionId!);
        await bob.JoinAsync("Bob");
        await alice.WaitForAsync(() => alice.Others.Any(p => p.Name == "Bob"));

        await bob.UpdateNameAsync("Bobby");

        await alice.WaitForAsync(() => alice.Others.Any(p => p.Name == "Bobby"));
    }

    [Fact]
    public async Task RevealingState_ShowsOthersSelectedPoints() {
        var alice = await harness.CreateClientAsync();
        await alice.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3"]);

        var bob = await harness.CreateClientAsync();
        await bob.LoadAsync(alice.TestSessionId!);
        await bob.JoinAsync("Bob");
        await alice.WaitForAsync(() => alice.Others.Any(p => p.Name == "Bob"));

        bob.UpdatePoints("3");
        await alice.WaitForAsync(() => alice.Others.Single().Points == "3");

        alice.UpdateState(State.Revealed);

        await bob.WaitForAsync(() => bob.Session!.State == State.Revealed);
        await alice.WaitForAsync(() => alice.Session!.State == State.Revealed);
        Assert.Equal("3", alice.Others.Single().Points);
    }

    [Fact]
    public async Task HidingState_ClearsSelectedPointsForOthers() {
        var alice = await harness.CreateClientAsync();
        await alice.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3"]);

        var bob = await harness.CreateClientAsync();
        await bob.LoadAsync(alice.TestSessionId!);
        await bob.JoinAsync("Bob");
        await alice.WaitForAsync(() => alice.Others.Any(p => p.Name == "Bob"));
        await bob.WaitForAsync(() => bob.Self is not null);

        bob.UpdatePoints("3");
        await alice.WaitForAsync(() => alice.Others.Single().Points == "3");
        alice.UpdateState(State.Revealed);
        await bob.WaitForAsync(() => bob.Session!.State == State.Revealed);

        alice.UpdateState(State.Hidden);

        await alice.WaitForAsync(() => alice.Session!.State == State.Hidden);
        await bob.WaitForAsync(() => bob.Self!.Points == "");
        await alice.WaitForAsync(() => alice.Others.Single().Points == "");
    }

    [Fact]
    public async Task SendingStar_IncrementsRecipientStarsForBothParticipants() {
        var alice = await harness.CreateClientAsync();
        await alice.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3"]);

        var bob = await harness.CreateClientAsync();
        await bob.LoadAsync(alice.TestSessionId!);
        await bob.JoinAsync("Bob");
        await alice.WaitForAsync(() => alice.Others.Any(p => p.Name == "Bob"));
        await bob.WaitForAsync(() => bob.Self is not null);

        alice.SendStarToParticipant(bob.Self!.ParticipantId);

        await bob.WaitForAsync(() => bob.Self!.Stars == 1);
        await alice.WaitForAsync(() => alice.Others.Single().Stars == 1);
    }

    [Fact]
    public async Task LeavingParticipant_IsRemovedForOthers() {
        var alice = await harness.CreateClientAsync();
        await alice.CreateAsync("Sprint Planning", "Alice", (string[])["1", "2", "3"]);

        var bob = await harness.CreateClientAsync();
        await bob.LoadAsync(alice.TestSessionId!);
        await bob.JoinAsync("Bob");
        await alice.WaitForAsync(() => alice.Others.Any(p => p.Name == "Bob"));

        await bob.LeaveAsync();

        await alice.WaitForAsync(() => !alice.Others.Any());
    }
}
