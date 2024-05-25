using Microsoft.AspNetCore.SignalR;

namespace PlanningPoker.Server;

#pragma warning disable CS4014 // Task.Run fire-and-forget
public class SessionHub(IStore store) : Hub<ISessionHubClient>, ISessionHub {
    public async Task AddPointAsync(string sessionId, string point) {
        Task.Run(async () => await store.AddPointAsync(sessionId, point));

        await Clients.Groups(sessionId).OnPointAdded(point);
    }

    public async Task<Session> ConnectToSessionAsync(string sessionId) {
        if (await store.GetSessionAsync(sessionId) is not Session session) {
            throw new InvalidOperationException($"Session {sessionId} does not exist.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId.ToString());

        return session;
    }

    public async Task<string> CreateSessionAsync(string title, IEnumerable<string> points) {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));

        var newGuid = await store.CreateSessionAsync(title, points);

        await Groups.AddToGroupAsync(Context.ConnectionId, newGuid.ToString());

        return newGuid;
    }

    public async Task<string> JoinSessionAsync(string sessionId, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        if (!await store.ExistsSessionAsync(sessionId)) {
            throw new InvalidOperationException($"Session {sessionId} does not exist.");
        }

        Task.Run(async () => await store.CreateParticipantAsync(sessionId, Context.ConnectionId, name));

        await Clients.Group(sessionId.ToString()).OnParticipantAdded(Context.ConnectionId, name);
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId.ToString());

        return Context.ConnectionId;
    }

    public async Task DisconnectFromSessionAsync(string sessionId) {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId.ToString());

        Task.Run(async () => await store.DeleteParticipantAsync(sessionId, Context.ConnectionId));

        await Clients.OthersInGroup(sessionId.ToString()).OnParticipantRemoved(Context.ConnectionId);
    }

    public async Task RemovePointAsync(string sessionId, string point) {
        Task.Run(async () => await store.RemovePointAsync(sessionId, point));

        await Clients.Groups(sessionId).OnPointRemoved(point);
    }

    public async Task SendStarToParticipantAsync(string sessionId, string participantId) {
        Task.Run(async () => await store.IncrementParticipantStarsAsync(sessionId, participantId));

        await Clients.Group(sessionId.ToString()).OnStarSentToParticipant(participantId);
    }

    public async Task UpdateParticipantPointsAsync(string sessionId, string points) {
        Task.Run(async () => await store.UpdateParticipantPointsAsync(sessionId, Context.ConnectionId, points));

        await Clients.OthersInGroup(sessionId.ToString()).OnParticipantPointsUpdated(Context.ConnectionId, points);
    }

    public async Task UpdateSessionStateAsync(string sessionId, State state) {
        Task.Run(async () => await store.UpdateSessionStateAsync(sessionId, state));

        if (state == State.Hidden) {
            Task.Run(async () => await store.UpdateAllParticipantPointsAsync(sessionId));
        }

        await Clients.Group(sessionId.ToString()).OnStateUpdated(state);
    }

    public async Task UpdateSessionTitleAsync(string sessionId, string title) {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));

        Task.Run(async () => await store.UpdateSessionTitleAsync(sessionId, title));
        
        await Clients.OthersInGroup(sessionId.ToString()).OnTitleUpdated(title);
    }

    public async Task UpdateParticipantNameAsync(string sessionId, string name) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        Task.Run(async () => await store.UpdateParticipantNameAsync(sessionId, Context.ConnectionId, name));

        await Clients.OthersInGroup(sessionId.ToString()).OnParticipantNameUpdated(Context.ConnectionId, name);
    }
}
#pragma warning restore CS4014