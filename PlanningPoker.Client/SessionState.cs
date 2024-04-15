using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using DataJuggler.Cryptography;

namespace PlanningPoker.Client;

#pragma warning disable CS4014 // Task.Run fire-and-forget
public class SessionState(NavigationManager navigationManager, IJSRuntime jsRuntime) : ISessionHubClient, IDisposable
{
    #region Internal State

    private HubConnection? _connection;

    private ISessionHub? _server;

    private Guid? _sessionId;

    private string? _participantId;

    private bool _isUpdateBelayed = false;

    private string? _encryptionKey;

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

    public IEnumerable<string> Options { get; init; } = [ "0", "0.5", "1", "2", "3", "5", "8", "?" ];

    public bool ShowShareNotification { get; private set; }

    public string SessionUrl =>
        $"https://freeplanningpoker.io/session/{_sessionId}#key={_encryptionKey}";

    #endregion

    private string Decrypt(string toDecrypt) =>
        CryptographyHelper.DecryptString(toDecrypt, _encryptionKey);

    private string Encrypt(string toEncrypt) =>
        CryptographyHelper.EncryptString(toEncrypt, _encryptionKey);

    private async Task EnsureInitialized()
    {
        if (_connection is not null)
        {
            return;
        }

        _connection = new HubConnectionBuilder()
            .WithUrl(navigationManager.ToAbsoluteUri("/sessions/hub"))
            .Build();

        _connection.ClientRegistration<ISessionHubClient>(this);
        _server = _connection.ServerProxy<ISessionHub>();
        
        await jsRuntime.InvokeVoidAsync("setupSignalRBeforeUnloadListener", DotNetObjectReference.Create(this));
        
        await _connection.StartAsync();
    }

    private void NotifyUpdate()
    {
        if (!_isUpdateBelayed)
        {
            OnStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task CreateAsync(string title, string name)
    {
        title = title.Trim();
        name = name.Trim();

        _isUpdateBelayed = true;

        await EnsureInitialized();

        _encryptionKey = Guid.NewGuid().ToString().Split('-').First();
        
        _sessionId = await _server!.CreateSessionAsync(Encrypt(title));
        Session = new(title, [], State.Hidden);
        ShowShareNotification = true;

        await JoinAsync(name);

        _isUpdateBelayed = false;

        NotifyUpdate();

        navigationManager.NavigateTo($"/session/{_sessionId}#key={_encryptionKey}");
    }

    public void HideShareNotification()
    {
        ShowShareNotification = false;
        NotifyUpdate();
    }

    public async Task JoinAsync(string name)
    {
        name = name.Trim();

        await EnsureInitialized();
        var participantId = await _server!.JoinSessionAsync(_sessionId!.Value, Encrypt(name));

        _participantId = participantId;

        NotifyUpdate();
    }

    [JSInvokable("LeaveAsync")]
    public async Task LeaveAsync()
    {
        if (_connection is null)
        {
            return;
        }

        await EnsureInitialized();
        await _server!.DisconnectFromSessionAsync(_sessionId!.Value);
        await _connection!.StopAsync();
        await _connection!.DisposeAsync();

        _sessionId = null;
        Session = null;
        _participantId = null;

        _connection = null;
        _server = null;

        NotifyUpdate();
    }

    public async Task LoadAsync(Guid sessionId, string encryptionKey)
    {
        _isUpdateBelayed = true;

        await EnsureInitialized();

        if (_sessionId == sessionId)
        {
            _isUpdateBelayed = false;
            return;
        }

        if (_sessionId is not null)
        {
            await LeaveAsync();
        }

        _sessionId = sessionId;
        _encryptionKey = encryptionKey;
        
        var encryptedSession = await _server!.ConnectToSessionAsync(_sessionId!.Value);
        Session = encryptedSession with
        {
            Title = Decrypt(encryptedSession.Title),
            Participants = encryptedSession.Participants.Select(p => p with { Name = Decrypt(p.Name) }).ToArray()
        };

        _isUpdateBelayed = false;
        NotifyUpdate();
    }

    public async Task SendStarToParticipantAsync(string participantId)
    {
        await EnsureInitialized();
        Task.Run(async () => await _server!.SendStarToParticipantAsync(_sessionId!.Value, participantId));
    }

    public async Task UpdateNameAsync(string name)
    {
        name = name.Trim();

        await EnsureInitialized();
        Task.Run(async () => await _server!.UpdateParticipantNameAsync(_sessionId!.Value, Encrypt(name)));

        Session = Session! with {
            Participants = [
                .. Session!.Participants.Where(p => p.ParticipantId != _participantId),
                Session!.Participants.Single(p => p.ParticipantId == _participantId) with { Name = name }
            ]
        };

        NotifyUpdate();
    }

    public async Task UpdatePointsAsync(string points)
    {
        points = points.Trim();

        if (points == Self?.Points)
        {
            points = "";
        }

        await EnsureInitialized();
        Task.Run(async () =>await _server!.UpdateParticipantPointsAsync(_sessionId!.Value, points));

        Session = Session! with {
            Participants = [
                .. Session!.Participants.Where(p => p.ParticipantId != _participantId),
                Session!.Participants.Single(p => p.ParticipantId == _participantId) with { Points = points }
            ]
        };

        NotifyUpdate();
    }

    public async Task UpdateStateAsync(State state)
    {
        await EnsureInitialized();
        Task.Run(async () => await _server!.UpdateSessionStateAsync(_sessionId!.Value, state));
    }

    public async Task UpdateTitleAsync(string title)
    {
        title = title.Trim();

        await EnsureInitialized();
        Task.Run(async () => await _server!.UpdateSessionTitleAsync(_sessionId!.Value, Encrypt(title)));

        Session = Session! with {
            Title = title
        };

        NotifyUpdate();
    }

    #region ISessionHubClient Implementation

    public async Task OnParticipantAdded(string participantId, string name)
    {
        await EnsureInitialized();

        Session = Session! with {
            Participants = [.. Session!.Participants, new(participantId, Decrypt(name), "", 0)]
        };

        NotifyUpdate();
    }

    public async Task OnParticipantNameUpdated(string participantId, string name)
    {
        await EnsureInitialized();
        
        Session = Session! with {
            Participants = [
                .. Session!.Participants.Where(p => p.ParticipantId != participantId),
                Session!.Participants.Single(p => p.ParticipantId == participantId) with { Name = Decrypt(name) }
            ]
        };

        NotifyUpdate();
    }

    public async Task OnParticipantPointsUpdated(string participantId, string points)
    {
        await EnsureInitialized();

        Session = Session! with {
            Participants =
                Session!.Participants
                .Select(p =>
                    p.ParticipantId == participantId
                        ? p with { Points = points }
                        : p
                )
                .ToList()
        };

        NotifyUpdate();
    }

    public async Task OnParticipantRemoved(string participantId)
    {
        await EnsureInitialized();

        Session = Session! with {
            Participants =
                Session!.Participants
                .Where(p => p.ParticipantId != participantId)
                .ToList()
        };

        NotifyUpdate();
    }

    public async Task OnStarSentToParticipant(string participantId)
    {
        await EnsureInitialized();

        Session = Session! with {
            Participants =
                Session!.Participants
                .Select(p =>
                    p.ParticipantId == participantId
                        ? p with { Stars = p.Stars + 1 }
                        : p
                )
                .ToList()
        };

        NotifyUpdate();
    }

    public async Task OnStateUpdated(State state)
    {
        await EnsureInitialized();

        Session = Session! with {
            State = state,
            Participants = 
                state == State.Revealed
                ? Session!.Participants
                : Session!.Participants.Select(p => p with { Points = "" }).ToList()
        };

        NotifyUpdate();
    }

    public async Task OnTitleUpdated(string title)
    {
        await EnsureInitialized();

        Session = Session! with {
            Title = Decrypt(title)
        };

        NotifyUpdate();
    }

    #endregion

    void IDisposable.Dispose()
    {
        LeaveAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }
}
#pragma warning restore CS4014