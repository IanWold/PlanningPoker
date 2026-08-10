using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using TypedSignalR.Client;

namespace PlanningPoker.Client;

public class Client(SessionStore sessionStore, ToastStore toastStore, ISessionTransport transport, IEncryptionService encryptionService) : IClient, IHubConnectionObserver, IDisposable {
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private HubConnection? _connection;
    private IServer? _server;
    private IDisposable? _serverSubscription;

    private async Task EnsureInitialized() {
        if (_connection is not null) {
            return;
        }

        _connection = new HubConnectionBuilder()
            .WithUrl(transport.GetServerUri(sessionStore.ParticipantId), transport.ConfigureConnection)
            .WithAutomaticReconnect()
            .AddMessagePackProtocol()
            .Build();

        _server = _connection.CreateHubProxy<IServer>(_cancellationTokenSource.Token);
        _serverSubscription = _connection.Register<IClient>(this);

        await transport.HandleInitializedAsync(this);

        await _connection.StartAsync();
    }

    private async Task<Session> FetchSessionAsync(string sessionId) {
        var encryptedSession = await _server!.ConnectToSessionAsync(sessionId);
        var decryptedParticipants = new List<Participant>();

        foreach (var participant in encryptedSession.Participants) {
            decryptedParticipants.Add(participant with { Name = await encryptionService.DecryptAsync(participant.Name) });
        }

        return encryptedSession with {
            Title = await encryptionService.DecryptAsync(encryptedSession.Title),
            Participants = decryptedParticipants
        };
    }

    private string ResolveDisplayName(string participantId) =>
        participantId == sessionStore.ParticipantId
        ? "You"
        : sessionStore.Session!.Participants.FirstOrDefault(p => p.ParticipantId == participantId)?.Name ?? "Unknown Participant";

    public void AddPoint(string point) =>
        _server!.AddPointAsync(sessionStore.SessionId!, point.Trim()).Forget();

    public async Task CreateAsync(string title, string name, IEnumerable<string> pointValues) {
        title = title.Trim();
        name = name.Trim();

        await EnsureInitialized();

        var encryptionKey = await encryptionService.GetKeyAsync();
        var sessionId = await _server!.CreateSessionAsync(await encryptionService.EncryptAsync(title), pointValues);

        sessionStore.ResetSession(sessionId, encryptionKey, new(title, [], State.Hidden, pointValues), true);

        await JoinAsync(name);

        await transport.HandleCreatedAsync(sessionId, encryptionKey);
    }

    public void HideShareNotification() =>
        sessionStore.SetShowShareNotification(false);

    public async Task JoinAsync(string name) {
        name = name.Trim();
        await _server!.JoinSessionAsync(sessionStore.SessionId!, await encryptionService.EncryptAsync(name));
    }

    public async Task LeaveAsync() {
        if (_connection is null) {
            return;
        }

        await _server!.DisconnectFromSessionAsync(sessionStore.SessionId!);
        await _connection!.StopAsync();
        await _connection!.DisposeAsync();

        _connection = null;
        _server = null;

        sessionStore.Clear();
    }

    public async Task LoadAsync(string sessionId) {
        await EnsureInitialized();

        if (sessionStore.SessionId == sessionId) {
            return;
        }

        if (sessionStore.SessionId is not null) {
            await LeaveAsync();
            await EnsureInitialized();
        }

        var encryptionKey = await encryptionService.GetKeyAsync();
        var session = await FetchSessionAsync(sessionId);

        sessionStore.ResetSession(sessionId, encryptionKey, session);
    }

    public void RemovePoint(string point) =>
        _server!.RemovePointAsync(sessionStore.SessionId!, point).Forget();

    public void SendStarToParticipant(string participantId) =>
        _server!.SendStarToParticipantAsync(sessionStore.SessionId!, participantId).Forget();

    public async Task UpdateNameAsync(string name) {
        name = name.Trim();

        _server!.UpdateParticipantNameAsync(sessionStore.SessionId!, await encryptionService.EncryptAsync(name)).Forget();

        sessionStore.UpdateParticipant(sessionStore.ParticipantId, p => p with { Name = name });
    }

    public void UpdatePoints(string points) {
        points = points.Trim();

        if (points == sessionStore.Self?.Points) {
            points = "";
        }

        _server!.UpdateParticipantPointsAsync(sessionStore.SessionId!, points).Forget();

        sessionStore.UpdateParticipant(sessionStore.ParticipantId, p => p with { Points = points });
    }

    public void UpdateState(State state) =>
        _server!.UpdateSessionStateAsync(sessionStore.SessionId!, state).Forget();

    public async Task UpdateTitleAsync(string title) {
        title = title.Trim();

        _server!.UpdateSessionTitleAsync(sessionStore.SessionId!, await encryptionService.EncryptAsync(title)).Forget();

        sessionStore.SetTitle(title);
    }

    #region IHubConnectionObserver Implementation

    public async Task OnReconnected(string? connectionId) {
        sessionStore.SetReconnecting(false);
        sessionStore.SetSession(await FetchSessionAsync(sessionStore.SessionId!));
    }

    public async Task OnReconnecting(Exception? exception) {
        sessionStore.SetReconnecting(true);
    }

    public async Task OnClosed(Exception? exception) {
        await LeaveAsync();
        await transport.HandleClosedAsync();
    }

    #endregion

    #region IClient Implementation

    public async Task OnParticipantAdded(string participantId, string name) {
        name = await encryptionService.DecryptAsync(name);

        sessionStore.AddParticipant(new(participantId, name, "", 0));

        if (participantId != sessionStore.ParticipantId) {
            toastStore.Add($"{name} has joined!");
        }
    }

    public async Task OnParticipantNameUpdated(string participantId, string name) {
        name = await encryptionService.DecryptAsync(name);
        var previousName = sessionStore.Session!.Participants.Single(p => p.ParticipantId == participantId).Name;

        sessionStore.UpdateParticipant(participantId, p => p with { Name = name });

        if (participantId != sessionStore.ParticipantId) {
            toastStore.Add($"{previousName} changed their name to {name}");
        }
    }

    public Task OnParticipantPointsUpdated(string participantId, string points) {
        sessionStore.UpdateParticipant(participantId, p => p with { Points = points });
        return Task.CompletedTask;
    }

    public Task OnParticipantRemoved(string participantId) {
        var name = sessionStore.Session!.Participants.Single(p => p.ParticipantId == participantId).Name;

        sessionStore.RemoveParticipant(participantId);

        if (participantId != sessionStore.ParticipantId) {
            toastStore.Add($"{name} has left");
        }
        return Task.CompletedTask;
    }

    public Task OnPointAdded(string point, string actingParticipantId) {
        sessionStore.AddPoint(point);

        var name = ResolveDisplayName(actingParticipantId);
        toastStore.Add($"{name} added point option \"{point}\"");
        return Task.CompletedTask;
    }

    public Task OnPointRemoved(string point, string actingParticipantId) {
        sessionStore.RemovePoint(point);

        var name = ResolveDisplayName(actingParticipantId);
        toastStore.Add($"{name} removed point option \"{point}\"");
        return Task.CompletedTask;
    }

    public Task OnStarSentToParticipant(string participantId) {
        sessionStore.UpdateParticipant(participantId, p => p with { Stars = p.Stars + 1 });
        return Task.CompletedTask;
    }

    public Task OnStateUpdated(State state, string actingParticipantId) {
        sessionStore.SetState(state);

        var name = ResolveDisplayName(actingParticipantId);
        toastStore.Add($"{name} {(name == "You" ? "have" : "has")} {Enum.GetName(state)!.ToLower()} the cards");
        return Task.CompletedTask;
    }

    public async Task OnTitleUpdated(string title, string actingParticipantId) {
        var decryptedTitle = await encryptionService.DecryptAsync(title);
        sessionStore.SetTitle(decryptedTitle);

        var name = ResolveDisplayName(actingParticipantId);
        toastStore.Add($"{name} updated the title to \"{decryptedTitle}\"");
    }

    #endregion

    void IDisposable.Dispose() {
        LeaveAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        _serverSubscription?.Dispose();
        GC.SuppressFinalize(this);
    }
}
