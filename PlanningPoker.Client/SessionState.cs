using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace PlanningPoker.Client;

public class SessionState(NavigationManager navigationManager, IJSRuntime jsRuntime) : ISessionHubClient, IDisposable
{
    private HubConnection? _connection;

    private ISessionHub? _server;

    private string? _name;

    private bool _isUpdateBelayed = false;

    public event EventHandler? OnStateChanged;

    public Guid? SessionId { get; private set; }

    public Session? Session { get; private set; }

    public Participant? Self =>
        _name is null
        ? null
        : Session?.Participants?.FirstOrDefault(p => p.Name.ToUpperInvariant() == _name.ToUpperInvariant());

    public IEnumerable<Participant> Others =>
        Session?.Participants?.Where(p => p.Name.ToUpperInvariant() != _name?.ToUpperInvariant()) ?? [];

    public readonly IEnumerable<string> Options = [
        "0.5",
        "1",
        "2",
        "3",
        "5",
        "8",
        "?"
    ];

    public bool ShowShareNotification { get; set; }

    private void NotifyUpdate()
    {
        if (!_isUpdateBelayed)
        {
            OnStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

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

    public async Task LoadAsync(Guid sessionId)
    {
        _isUpdateBelayed = true;

        await EnsureInitialized();

        if (SessionId == sessionId)
        {
            _isUpdateBelayed = false;
            return;
        }

        if (SessionId is not null)
        {
            await LeaveAsync();
        }

        SessionId = sessionId;
        Session = await _server!.ConnectToSessionAsync(SessionId!.Value);

        _isUpdateBelayed = false;
        NotifyUpdate();
    }

    public async Task CreateAsync(string title, string name)
    {
        _isUpdateBelayed = true;

        await EnsureInitialized();
        
        SessionId = await _server!.CreateSessionAsync(title);
        Session = new(title, [], State.Hidden);
        ShowShareNotification = true;

        await JoinAsync(name);

        _isUpdateBelayed = false;

        NotifyUpdate();

        navigationManager.NavigateTo($"/session/{SessionId}");
    }

    public void HideShareNotification()
    {
        ShowShareNotification = false;
        NotifyUpdate();
    }

    public async Task JoinAsync(string name)
    {
        await EnsureInitialized();
        await _server!.JoinSessionAsync(SessionId!.Value, name);

        _name = name;

        NotifyUpdate();
    }

    public async Task HideAsync()
    {
        await EnsureInitialized();
        await _server!.UpdateSessionStateAsync(SessionId!.Value, State.Hidden);
    }

    public async Task RevealAsync()
    {
        await EnsureInitialized();
        await _server!.UpdateSessionStateAsync(SessionId!.Value, State.Revealed);
    }

    public async Task UpdatePointsAsync(string points)
    {
        await EnsureInitialized();
        await _server!.UpdateParticipantPointsAsync(SessionId!.Value, _name!, points);

        Session = Session! with {
            Participants = [.. Session!.Participants.Where(p => p.Name != _name), Session!.Participants.Single(p => p.Name == _name) with { Points = points }]
        };

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
        await _server!.DisconnectFromSessionAsync(SessionId!.Value, _name!);
        await _connection!.StopAsync();
        await _connection!.DisposeAsync();

        SessionId = null;
        Session = null;
        _name = null;

        _connection = null;
        _server = null;

        NotifyUpdate();
    }

    public async Task UpdateTitle(string title)
    {
        await EnsureInitialized();
        await _server!.UpdateSessionTitleAsync(SessionId!.Value, title);

        Session = Session! with {
            Title = title
        };

        NotifyUpdate();
    }

    public async Task OnHide()
    {
        await EnsureInitialized();

        Session = Session! with {
            State = State.Hidden,
            Participants = Session!.Participants.Select(p => new Participant(p.Name, "")).ToList()
        };

        NotifyUpdate();
    }

    public async Task OnParticipantAdded(string name)
    {
        await EnsureInitialized();

        Session = Session! with {
            Participants = [.. Session!.Participants, new(name, "")]
        };

        NotifyUpdate();
    }

    public async Task OnParticipantPointsUpdated(string name, string points)
    {
        await EnsureInitialized();

        Session = Session! with {
            Participants =
                Session!.Participants
                .Select(p =>
                    p.Name.ToUpperInvariant() == name.ToUpperInvariant()
                        ? new(name, points)
                        : p
                )
                .ToList()
        };

        NotifyUpdate();
    }

    public async Task OnParticipantRemoved(string name)
    {
        await EnsureInitialized();

        Session = Session! with {
            Participants =
                Session!.Participants
                .Where(p => p.Name.ToUpperInvariant() != name.ToUpperInvariant())
                .ToList()
        };

        NotifyUpdate();
    }

    public async Task OnReveal(IEnumerable<Participant> participants)
    {
        await EnsureInitialized();

        Session = Session! with {
            State = State.Revealed,
            Participants = participants
        };

        NotifyUpdate();
    }

    public async Task OnTitleUpdated(string title)
    {
        await EnsureInitialized();

        Session = Session! with {
            Title = title
        };

        NotifyUpdate();
    }

    void IDisposable.Dispose()
    {
        LeaveAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }
}
