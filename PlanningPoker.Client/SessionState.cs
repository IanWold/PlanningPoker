using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using TypedSignalR.Client;
using Timer = System.Timers.Timer;

namespace PlanningPoker.Client;

public class SessionState(IEncryptionService encryptionService) : IClient, IHubConnectionObserver, IDisposable {
    public class Toast {
        readonly Timer _timer = new(5000);

        public string Message { get; init; }

        public DateTime Time { get; } = DateTime.Now;

        public bool IsExpired { get; set; }

        public Toast(string message, EventHandler? stateChanged) {
            Message = message;

            _timer.Elapsed += (_, _) => {
                IsExpired = true;
                stateChanged?.Invoke(this, EventArgs.Empty);
                _timer.Dispose();
            };
            _timer.Start();
        }
    }

    #region Private State

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private HubConnection? _connection;
    private IServer? _server;
    private IDisposable? _serverSubscription;

    private bool _isUpdateBelayed = false;

    #endregion

    #region Protected State

    protected string EncryptionKey { get; private set; } = string.Empty;
    protected string ParticipantId { get; } = Guid.NewGuid().ToString();
    protected string? SessionId { get; private set; }

    #endregion

    #region Public State

    public event EventHandler? OnStateChanged;

    public Session? Session { get; private set; }

    public string SessionUrl =>
        $"https://freeplanningpoker.io/session/{SessionId}#key={EncryptionKey}";

    public Participant? Self =>
        Session?.Participants?.FirstOrDefault(p => p.ParticipantId == ParticipantId);

    public IEnumerable<Participant> Others =>
        Session?.Participants?.Where(p => p.ParticipantId != ParticipantId) ?? [];

    public bool ShowShareNotification { get; private set; }

    public IEnumerable<Toast> Toasts { get; private set; } = [];

    public bool IsReconnecting { get; set; }

    #endregion

    #region Virtual Interface

    protected virtual Uri GetServerUri() =>
        new($"/sessions/hub?participantId={ParticipantId}");

    protected virtual void ConfigureConnection(HttpConnectionOptions options) { }

    protected virtual Task HandleClosedAsync() =>
        Task.CompletedTask;

    protected virtual Task HandleCreatedAsync() =>
        Task.CompletedTask;

    protected virtual Task HandleInitializedAsync(SessionState instance) =>
        Task.CompletedTask;
        
    #endregion

    private async Task EnsureInitialized() {
        if (_connection is not null) {
            return;
        }

        _connection = new HubConnectionBuilder()
            .WithUrl(GetServerUri(), ConfigureConnection)
            .WithAutomaticReconnect()
            .AddMessagePackProtocol()
            .Build();

        _server = _connection.CreateHubProxy<IServer>(_cancellationTokenSource.Token);
        _serverSubscription = _connection.Register<IClient>(this);

        await HandleInitializedAsync(this);
        
        await _connection.StartAsync();
    }

    private async Task HydrateSessionAsync() {
        var encryptedSession = await _server!.ConnectToSessionAsync(SessionId!);
        var decryptedParticipants = new List<Participant>();

        foreach (var participant in encryptedSession.Participants) {
            decryptedParticipants.Add(participant with { Name = await encryptionService.DecryptAsync(participant.Name) });
        }

        Session = encryptedSession with {
            Title = await encryptionService.DecryptAsync(encryptedSession.Title),
            Participants = decryptedParticipants
        };

        NotifyUpdate();
    }

    private void NotifyUpdate(string? message = null) {
        if (message is not null) {
            Toasts = [.. Toasts, new Toast(message, OnStateChanged)];
        }

        if (!_isUpdateBelayed) {
            OnStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void NotifyUpdate(string participantId, Func<string, string> message) =>
        NotifyUpdate(message(
            participantId == ParticipantId
            ? "You"
            : Session!.Participants.FirstOrDefault(p => p.ParticipantId == participantId)?.Name ?? "Unknown Participant"
        ));

    private void UpdateParticipant(string? participantId, Func<Participant, Participant> update) =>
        Session = Session! with {
            Participants = [.. Session!.Participants.Select(p => p.ParticipantId == participantId ? update(p) : p)]
        };

    public void AddPoint(string point) =>
        _server!.AddPointAsync(SessionId!, point.Trim()).Forget();

    public async Task CreateAsync(string title, string name, IEnumerable<string> pointValues) {
        title = title.Trim();
        name = name.Trim();

        _isUpdateBelayed = true;

        await EnsureInitialized();

        EncryptionKey = await encryptionService.GetKeyAsync();

        SessionId = await _server!.CreateSessionAsync(await encryptionService.EncryptAsync(title), pointValues);
        Session = new(title, [], State.Hidden, pointValues);
        ShowShareNotification = true;

        await JoinAsync(name);

        _isUpdateBelayed = false;
        NotifyUpdate();

        await HandleCreatedAsync();
    }

    public void HideShareNotification() {
        ShowShareNotification = false;
        NotifyUpdate();
    }

    public async Task JoinAsync(string name) {
        name = name.Trim();
        await _server!.JoinSessionAsync(SessionId!, await encryptionService.EncryptAsync(name));

        NotifyUpdate();
    }

    public async Task LeaveAsync() {
        if (_connection is null) {
            return;
        }

        await _server!.DisconnectFromSessionAsync(SessionId!);
        await _connection!.StopAsync();
        await _connection!.DisposeAsync();

        SessionId = null;
        Session = null;

        _connection = null;
        _server = null;

        NotifyUpdate();
    }

    public async Task LoadAsync(string sessionId) {
        _isUpdateBelayed = true;

        await EnsureInitialized();

        if (SessionId == sessionId) {
            _isUpdateBelayed = false;
            return;
        }

        if (SessionId is not null) {
            await LeaveAsync();
        }

        SessionId = sessionId;
        EncryptionKey = await encryptionService.GetKeyAsync();
        _isUpdateBelayed = false;

        await HydrateSessionAsync();
    }

    public void RemovePoint(string point) =>
        _server!.RemovePointAsync(SessionId!, point).Forget();

    public void SendStarToParticipant(string participantId) =>
        _server!.SendStarToParticipantAsync(SessionId!, participantId).Forget();

    public async Task UpdateNameAsync(string name) {
        name = name.Trim();

        _server!.UpdateParticipantNameAsync(SessionId!, await encryptionService.EncryptAsync(name)).Forget();

        UpdateParticipant(ParticipantId, p => p with { Name = name });

        NotifyUpdate();
    }

    public void UpdatePoints(string points) {
        points = points.Trim();

        if (points == Self?.Points) {
            points = "";
        }

        _server!.UpdateParticipantPointsAsync(SessionId!, points).Forget();

        UpdateParticipant(ParticipantId, p => p with { Points = points });

        NotifyUpdate();
    }

    public void UpdateState(State state) =>
        _server!.UpdateSessionStateAsync(SessionId!, state).Forget();

    public async Task UpdateTitleAsync(string title) {
        title = title.Trim();

        _server!.UpdateSessionTitleAsync(SessionId!, await encryptionService.EncryptAsync(title)).Forget();

        Session = Session! with { Title = title };

        NotifyUpdate();
    }

    #region IHubConnectionObserver Implementation

    public async Task OnReconnected(string? connectionId) {
        IsReconnecting = false;
        await HydrateSessionAsync();
    }

    public async Task OnReconnecting(Exception? exception) {
        IsReconnecting = true;
    }

    public async Task OnClosed(Exception? exception) {
        await LeaveAsync();
        await HandleClosedAsync();
    }

    #endregion

    #region IClient Implementation

    public async Task OnParticipantAdded(string participantId, string name) {
        name = await encryptionService.DecryptAsync(name);
        Session = Session! with { Participants = [.. Session!.Participants, new(participantId, name, "", 0)] };

        NotifyUpdate(participantId != ParticipantId
            ? $"{name} has joined!"
            : null
        );
    }

    public async Task OnParticipantNameUpdated(string participantId, string name) {
        name = await encryptionService.DecryptAsync(name);
        var previousName = Session!.Participants.Single(p => p.ParticipantId == participantId).Name;

        UpdateParticipant(participantId, p => p with { Name = name });

        NotifyUpdate(participantId != ParticipantId
            ? $"{previousName} changed their name to {name}"
            : null
        );
    }

    public Task OnParticipantPointsUpdated(string participantId, string points) {
        UpdateParticipant(participantId, p => p with { Points = points });

        NotifyUpdate();
        return Task.CompletedTask;
    }

    public Task OnParticipantRemoved(string participantId) {
        var name = Session!.Participants.Single(p => p.ParticipantId == participantId).Name;

        Session = Session! with {
            Participants = [..
                Session!.Participants
                .Where(p => p.ParticipantId != participantId)
            ]
        };

        NotifyUpdate(participantId != ParticipantId
            ? $"{name} has left"
            : null
        );
        return Task.CompletedTask;
    }

    public Task OnPointAdded(string point, string actingParticipantId) {
        Session = Session! with { Points = [.. Session!.Points, point] };

        NotifyUpdate(actingParticipantId, name => $"{name} added point option \"{point}\"");
        return Task.CompletedTask;
    }

    public Task OnPointRemoved(string point, string actingParticipantId) {
        Session = Session! with { Points = [.. Session!.Points.Except([point])] };

        NotifyUpdate(actingParticipantId, name => $"{name} removed point option \"{point}\"");
        return Task.CompletedTask;
    }

    public Task OnStarSentToParticipant(string participantId) {
        UpdateParticipant(participantId, p => p with { Stars = p.Stars + 1 });

        NotifyUpdate();
        return Task.CompletedTask;
    }

    public Task OnStateUpdated(State state, string actingParticipantId) {
        Session = Session! with {
            State = state,
            Participants = 
                state == State.Revealed
                ? Session!.Participants
                : [.. Session!.Participants.Select(p => p with { Points = "" })]
        };

        NotifyUpdate(actingParticipantId, name => $"{name} {(name == "You" ? "have" : "has")} {Enum.GetName(state)!.ToLower()} the cards");
        return Task.CompletedTask;
    }

    public async Task OnTitleUpdated(string title, string actingParticipantId) {
        Session = Session! with { Title = await encryptionService.DecryptAsync(title) };

        NotifyUpdate(actingParticipantId, name => $"{name} updated the title to \"{Session.Title}\"");
    }

    #endregion

    void IDisposable.Dispose() {
        LeaveAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        _serverSubscription?.Dispose();
        GC.SuppressFinalize(this);
    }
}
