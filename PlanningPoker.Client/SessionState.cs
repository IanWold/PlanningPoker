using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace PlanningPoker.Client;

public class SessionState(NavigationManager navigationManager, IJSRuntime jsRuntime, ToastState toast) : ISessionHubClient, IDisposable {
    #region Internal State

    private readonly string _participantId = Guid.NewGuid().ToString();

    private HubConnection? _connection;
    private ISessionHub? _server;
    private string? _sessionId;
    private bool _isUpdateBelayed = false;
    private string _encryptionKey = string.Empty;

    #endregion

    #region Public State

    public event EventHandler? OnStateChanged;

    public Session? Session { get; private set; }

    public Participant? Self =>
        Session?.Participants?.FirstOrDefault(p => p.ParticipantId == _participantId);

    public IEnumerable<Participant> Others =>
        Session?.Participants?.Where(p => p.ParticipantId != _participantId) ?? [];

    public bool ShowShareNotification { get; private set; }

    public string SessionUrl =>
        $"https://freeplanningpoker.io/session/{_sessionId}#key={_encryptionKey}";

    public bool IsReconnecting { get; set; }

    #endregion

    private async Task<string> DecryptAsync(string value) =>
        await jsRuntime.InvokeAsync<string>("decrypt", value);

    private async Task<string> EncryptAsync(string value) =>
        await jsRuntime.InvokeAsync<string>("encrypt", value);

    private async Task EnsureInitialized() {
        if (_connection is not null) {
            return;
        }

        _connection = new HubConnectionBuilder()
            .WithUrl(navigationManager.ToAbsoluteUri($"/sessions/hub?participantId={_participantId}"))
            .WithAutomaticReconnect()
            .AddMessagePackProtocol()
            .Build();

        _connection.ClientRegistration<ISessionHubClient>(this);
        _server = _connection.ServerProxy<ISessionHub>();

        _connection.Reconnecting += OnReconnecting;
        _connection.Reconnected += OnReconnected;
        
        await jsRuntime.InvokeVoidAsync("setupSignalRBeforeUnloadListener", DotNetObjectReference.Create(this));
        
        await _connection.StartAsync();
    }

    private async Task HydrateSessionAsync() {
        var encryptedSession = await _server!.ConnectToSessionAsync(_sessionId!);
        var decryptedParticipants = new List<Participant>();

        foreach (var participant in encryptedSession.Participants) {
            decryptedParticipants.Add(participant with { Name = await DecryptAsync(participant.Name) });
        }

        Session = encryptedSession with {
            Title = await DecryptAsync(encryptedSession.Title),
            Participants = decryptedParticipants
        };

        NotifyUpdate();
    }

    private void NotifyUpdate() {
        if (!_isUpdateBelayed) {
            OnStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void NotifyParticipantAction(string participantId, Func<string, string> message) =>
        toast.Add(message(
            participantId == _participantId
            ? "You"
            : Session!.Participants.FirstOrDefault(p => p.ParticipantId == participantId)?.Name ?? "Unknown Participant"
        ));

    private void UpdateParticipant(string? participantId, Func<Participant, Participant> update) =>
        Session = Session! with {
            Participants = [.. Session!.Participants.Select(p => p.ParticipantId == participantId ? update(p) : p)]
        };

    public void AddPoint(string point) =>
        _server!.AddPointAsync(_sessionId!, point.Trim()).Forget();

    public async Task CreateAsync(string title, string name, IEnumerable<string> pointValues) {
        title = title.Trim();
        name = name.Trim();

        _isUpdateBelayed = true;

        await EnsureInitialized();

        _encryptionKey = await jsRuntime.InvokeAsync<string>("getEncryptionKey") ?? string.Empty;

        _sessionId = await _server!.CreateSessionAsync(await EncryptAsync(title), pointValues);
        Session = new(title, [], State.Hidden, pointValues);
        ShowShareNotification = true;

        await JoinAsync(name);

        _isUpdateBelayed = false;
        NotifyUpdate();

        navigationManager.NavigateTo($"/session/{_sessionId}#key={_encryptionKey}");
    }

    public void HideShareNotification() {
        ShowShareNotification = false;
        NotifyUpdate();
    }

    public async Task JoinAsync(string name) {
        name = name.Trim();
        await _server!.JoinSessionAsync(_sessionId!, await EncryptAsync(name));

        NotifyUpdate();
    }

    [JSInvokable("LeaveAsync")]
    public async Task LeaveAsync() {
        if (_connection is null) {
            return;
        }

        await _server!.DisconnectFromSessionAsync(_sessionId!);
        await _connection!.StopAsync();
        await _connection!.DisposeAsync();

        _sessionId = null;
        Session = null;

        _connection = null;
        _server = null;

        NotifyUpdate();
    }

    public async Task LoadAsync(string sessionId) {
        _isUpdateBelayed = true;

        await EnsureInitialized();

        if (_sessionId == sessionId) {
            _isUpdateBelayed = false;
            return;
        }

        if (_sessionId is not null) {
            await LeaveAsync();
        }

        _sessionId = sessionId;
        _encryptionKey = await jsRuntime.InvokeAsync<string>("getEncryptionKey") ?? string.Empty;
        _isUpdateBelayed = false;

        await HydrateSessionAsync();
    }

    public void RemovePoint(string point) =>
        _server!.RemovePointAsync(_sessionId!, point).Forget();

    public void SendStarToParticipant(string participantId) =>
        _server!.SendStarToParticipantAsync(_sessionId!, participantId).Forget();

    public async Task UpdateNameAsync(string name) {
        name = name.Trim();

        _server!.UpdateParticipantNameAsync(_sessionId!, await EncryptAsync(name)).Forget();

        UpdateParticipant(_participantId, p => p with { Name = name });

        NotifyUpdate();
    }

    public void UpdatePoints(string points) {
        points = points.Trim();

        if (points == Self?.Points) {
            points = "";
        }

        _server!.UpdateParticipantPointsAsync(_sessionId!, points).Forget();

        UpdateParticipant(_participantId, p => p with { Points = points });

        NotifyUpdate();
    }

    public void UpdateState(State state) =>
        _server!.UpdateSessionStateAsync(_sessionId!, state).Forget();

    public async Task UpdateTitleAsync(string title) {
        title = title.Trim();

        _server!.UpdateSessionTitleAsync(_sessionId!, await EncryptAsync(title)).Forget();

        Session = Session! with { Title = title };

        NotifyUpdate();
    }

    #region Connection Events

    private Task OnReconnecting(Exception? exception) {
        IsReconnecting = true;
        return Task.CompletedTask;
    }

    private async Task OnReconnected(string? connectionId) {
        IsReconnecting = false;
        await HydrateSessionAsync();
    }

    #endregion

    #region ISessionHubClient Implementation

    public async Task OnParticipantAdded(string participantId, string name) {
        name = await DecryptAsync(name);
        Session = Session! with { Participants = [.. Session!.Participants, new(participantId, name, "", 0)] };

        if (participantId != _participantId) {
            toast.Add($"{name} has joined!");
        }

        NotifyUpdate();
    }

    public async Task OnParticipantNameUpdated(string participantId, string name) {
        name = await DecryptAsync(name);
        var previousName = Session!.Participants.Single(p => p.ParticipantId == participantId).Name;

        UpdateParticipant(participantId, p => p with { Name = name });

        if (participantId != _participantId) {
            toast.Add($"{previousName} changed their name to {name}");
        }

        NotifyUpdate();
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

        if (participantId != _participantId) {
            toast.Add($"{name} has left");
        }

        NotifyUpdate();
        return Task.CompletedTask;
    }

    public Task OnPointAdded(string point, string actingParticipantId) {
        Session = Session! with { Points = [.. Session!.Points, point] };

        NotifyParticipantAction(actingParticipantId, name => $"{name} added point option \"{point}\"");
        NotifyUpdate();
        return Task.CompletedTask;
    }

    public Task OnPointRemoved(string point, string actingParticipantId) {
        Session = Session! with { Points = [.. Session!.Points.Except([point])] };

        NotifyParticipantAction(actingParticipantId, name => $"{name} removed point option \"{point}\"");
        NotifyUpdate();
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

        NotifyParticipantAction(actingParticipantId, name => $"{name} {(name == "You" ? "have" : "has")} {Enum.GetName(state)!.ToLower()} the cards");
        NotifyUpdate();
        return Task.CompletedTask;
    }

    public async Task OnTitleUpdated(string title, string actingParticipantId) {
        Session = Session! with { Title = await DecryptAsync(title) };

        NotifyParticipantAction(actingParticipantId, name => $"{name} updated the title to \"{Session.Title}\"");
        NotifyUpdate();
    }

    #endregion

    void IDisposable.Dispose() {
        LeaveAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}
