using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace PlanningPoker.Client;

public class SessionState(NavigationManager navigationManager, IJSRuntime jsRuntime, ToastState toast) : ISessionHubClient, IDisposable {
    #region Internal State

    private HubConnection? _connection;

    private ISessionHub? _server;

    private string? _sessionId;

    private string? _participantId;

    private bool _isUpdateBelayed = false;

    private string _encryptionKey = string.Empty;

    #endregion

    #region Public State

    public event EventHandler? OnStateChanged;

    public Session? Session { get; private set; }

    public Participant? Self =>
        _participantId is null
        ? null
        : Session?.Participants?.FirstOrDefault(p => p.ParticipantId == _participantId);

    public IEnumerable<Participant> Others =>
        Session?.Participants?.Where(p => p.ParticipantId != _participantId) ?? [];

    public bool ShowShareNotification { get; private set; }

    public bool IsReconnecting { get; private set; }

    public bool IsClosed { get; private set; }

    public string SessionUrl =>
        $"https://freeplanningpoker.io/session/{_sessionId}#key={_encryptionKey}";

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
            .WithUrl(navigationManager.ToAbsoluteUri("/sessions/hub"))
            .AddMessagePackProtocol()
            .Build();

        _connection.ClientRegistration<ISessionHubClient>(this);
        _server = _connection.ServerProxy<ISessionHub>();

        _connection.Reconnecting += OnReconnectingAsync;
        _connection.Reconnected += OnReconnectedAsync;
        _connection.Closed += OnClosedAsync;
        
        await jsRuntime.InvokeVoidAsync("setupSignalRBeforeUnloadListener", DotNetObjectReference.Create(this));
        
        await _connection.StartAsync();
    }

    private Task OnReconnectingAsync(Exception? exception) {
        IsReconnecting = true;
        NotifyUpdate();
        return Task.CompletedTask;
    }

    private async Task OnReconnectedAsync(string? newParticipantId) {
        IsReconnecting = false;

        if (newParticipantId is not null) {
            Session = await _server!.UpdateParticipantIdAsync(_sessionId!, _participantId!);
            _participantId = newParticipantId;
        }

        NotifyUpdate();
    }

    private Task OnClosedAsync(Exception? exception) {
        IsClosed = true;
        NotifyUpdate();
        return Task.CompletedTask;
    }

    private void NotifyUpdate() {
        if (!_isUpdateBelayed) {
            OnStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task AddPointAsync(string point) {
        point = point.Trim();

        await EnsureInitialized();

        _server!.AddPointAsync(_sessionId!, point).Forget();
    }

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

        await EnsureInitialized();
        var participantId = await _server!.JoinSessionAsync(_sessionId!, await EncryptAsync(name));

        _participantId = participantId;

        NotifyUpdate();
    }

    [JSInvokable("LeaveAsync")]
    public async Task LeaveAsync() {
        if (_connection is null) {
            return;
        }

        await EnsureInitialized();
        await _server!.DisconnectFromSessionAsync(_sessionId!);
        await _connection!.StopAsync();
        await _connection!.DisposeAsync();

        _sessionId = null;
        Session = null;
        _participantId = null;

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
        
        var encryptedSession = await _server!.ConnectToSessionAsync(_sessionId!);
        var decryptedParticipants = new List<Participant>();

        foreach (var participant in encryptedSession.Participants) {
            decryptedParticipants.Add(participant with { Name = await DecryptAsync(participant.Name )});
        }

        Session = encryptedSession with {
            Title = await DecryptAsync(encryptedSession.Title),
            Participants = decryptedParticipants
        };

        _isUpdateBelayed = false;
        NotifyUpdate();
    }

    public async Task RemovePointAsync(string point) {
        await EnsureInitialized();

        _server!.RemovePointAsync(_sessionId!, point).Forget();
    }

    public async Task SendStarToParticipantAsync(string participantId) {
        await EnsureInitialized();
        _server!.SendStarToParticipantAsync(_sessionId!, participantId).Forget();
    }

    public async Task UpdateNameAsync(string name) {
        name = name.Trim();

        await EnsureInitialized();
        _server!.UpdateParticipantNameAsync(_sessionId!, await EncryptAsync(name)).Forget();

        Session = Session! with {
            Participants = [
                .. Session!.Participants.Where(p => p.ParticipantId != _participantId),
                Session!.Participants.Single(p => p.ParticipantId == _participantId) with { Name = name }
            ]
        };

        NotifyUpdate();
    }

    public async Task UpdatePointsAsync(string points) {
        points = points.Trim();

        if (points == Self?.Points) {
            points = "";
        }

        await EnsureInitialized();
        _server!.UpdateParticipantPointsAsync(_sessionId!, points).Forget();

        Session = Session! with {
            Participants = [
                .. Session!.Participants.Where(p => p.ParticipantId != _participantId),
                Session!.Participants.Single(p => p.ParticipantId == _participantId) with { Points = points }
            ]
        };

        NotifyUpdate();
    }

    public async Task UpdateStateAsync(State state) {
        await EnsureInitialized();
        _server!.UpdateSessionStateAsync(_sessionId!, state).Forget();
    }

    public async Task UpdateTitleAsync(string title) {
        title = title.Trim();

        await EnsureInitialized();
        _server!.UpdateSessionTitleAsync(_sessionId!, await EncryptAsync(title)).Forget();

        Session = Session! with {
            Title = title
        };

        NotifyUpdate();
    }

    #region ISessionHubClient Implementation

    public async Task OnParticipantAdded(string participantId, string name) {
        await EnsureInitialized();

        name = await DecryptAsync(name);

        Session = Session! with {
            Participants = [.. Session!.Participants, new(participantId, name, "", 0)]
        };

        if (participantId != _participantId) {
            toast.Add($"{name} has joined!");
        }

        NotifyUpdate();
    }

    public async Task OnParticipantIdUpdated(string oldParticipantId, string newParticipantId) {
        await EnsureInitialized();

        Session = Session! with {
            Participants = [..
                Session!.Participants
                .Select(p =>
                    p.ParticipantId == oldParticipantId
                    ? p with { ParticipantId = newParticipantId }
                    : p
                )
            ]
        };

        NotifyUpdate();
    }

    public async Task OnParticipantNameUpdated(string participantId, string name) {
        await EnsureInitialized();

        name = await DecryptAsync(name);
        var previousName = Session!.Participants.Single(p => p.ParticipantId == participantId).Name;
        
        Session = Session! with {
            Participants = [..
                Session!.Participants
                .Select(p =>
                    p.ParticipantId == participantId
                    ? p with { Name = name }
                    : p
                )
            ]
        };

        if (participantId != _participantId) {
            toast.Add($"{previousName} changed their name to {name}");
        }

        NotifyUpdate();
    }

    public async Task OnParticipantPointsUpdated(string participantId, string points) {
        await EnsureInitialized();

        Session = Session! with {
            Participants = [..
                Session!.Participants
                .Select(p =>
                    p.ParticipantId == participantId
                    ? p with { Points = points }
                    : p
                )
            ]
        };

        NotifyUpdate();
    }

    public async Task OnParticipantRemoved(string participantId) {
        await EnsureInitialized();

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
    }

    public async Task OnPointAdded(string point) {
        await EnsureInitialized();

        Session = Session! with {
            Points = [..Session!.Points, point]
        };

        NotifyUpdate();
    }

    public async Task OnPointRemoved(string point) {
        await EnsureInitialized();

        Session = Session! with {
            Points = Session!.Points.Except([point]).ToArray()
        };

        NotifyUpdate();
    }

    public async Task OnStarSentToParticipant(string participantId) {
        await EnsureInitialized();

        Session = Session! with {
            Participants = [..
                Session!.Participants
                .Select(p =>
                    p.ParticipantId == participantId
                    ? p with { Stars = p.Stars + 1 }
                    : p
                )
            ]
        };

        NotifyUpdate();
    }

    public async Task OnStateUpdated(State state) {
        await EnsureInitialized();

        Session = Session! with {
            State = state,
            Participants = 
                state == State.Revealed
                ? Session!.Participants
                : [.. Session!.Participants.Select(p => p with { Points = "" })]
        };

        NotifyUpdate();
    }

    public async Task OnTitleUpdated(string title) {
        await EnsureInitialized();

        Session = Session! with {
            Title = await DecryptAsync(title)
        };

        NotifyUpdate();
    }

    #endregion

    void IDisposable.Dispose() {
        LeaveAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }
}
