using Microsoft.AspNetCore.SignalR;

namespace PlanningPoker.Server;

public class SessionHub(IStore store) : Hub<ISessionHubClient>, ISessionHub {
    public class UserIdProvider : IUserIdProvider {
        public string? GetUserId(HubConnectionContext connection) {
            var participantId = connection.GetHttpContext()?.Request.Query["participantId"].ToString()?.Trim();

            if (string.IsNullOrEmpty(participantId)) {
                connection.Abort();
            }

            return participantId;
        }
    }

    public async Task AddPointAsync(string sessionId, string point) {
        store.AddPointAsync(sessionId, point).Forget();

        await Clients.Groups(sessionId).OnPointAdded(point, Context.UserIdentifier!);
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

    public async Task JoinSessionAsync(string sessionId, string name) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        if (!await store.ExistsSessionAsync(sessionId)) {
            throw new InvalidOperationException($"Session {sessionId} does not exist.");
        }

        store.CreateParticipantAsync(sessionId, Context.UserIdentifier!, name).Forget();

        await Clients.Group(sessionId.ToString()).OnParticipantAdded(Context.UserIdentifier!, name);
    }

    public async Task DisconnectFromSessionAsync(string sessionId) {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId.ToString());

        store.DeleteParticipantAsync(sessionId, Context.UserIdentifier!).Forget();

        await Clients.OthersInGroup(sessionId.ToString()).OnParticipantRemoved(Context.UserIdentifier!);
    }

    public async Task RemovePointAsync(string sessionId, string point) {
        store.RemovePointAsync(sessionId, point).Forget();

        await Clients.Groups(sessionId).OnPointRemoved(point, Context.UserIdentifier!);
    }

    public async Task SendStarToParticipantAsync(string sessionId, string participantId) {
        store.IncrementParticipantStarsAsync(sessionId, participantId).Forget();

        await Clients.Group(sessionId.ToString()).OnStarSentToParticipant(participantId);
    }

    public async Task UpdateParticipantPointsAsync(string sessionId, string points) {
        store.UpdateParticipantPointsAsync(sessionId, Context.UserIdentifier!, points).Forget();

        await Clients.OthersInGroup(sessionId.ToString()).OnParticipantPointsUpdated(Context.UserIdentifier!, points);
    }

    public async Task UpdateSessionStateAsync(string sessionId, State state) {
        store.UpdateSessionStateAsync(sessionId, state).Forget();

        if (state == State.Hidden) {
            store.UpdateAllParticipantPointsAsync(sessionId).Forget();
        }

        await Clients.Group(sessionId.ToString()).OnStateUpdated(state, Context.UserIdentifier!);
    }

    public async Task UpdateSessionTitleAsync(string sessionId, string title) {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));

        store.UpdateSessionTitleAsync(sessionId, title).Forget();
        
        await Clients.OthersInGroup(sessionId.ToString()).OnTitleUpdated(title, Context.UserIdentifier!);
    }

    public async Task UpdateParticipantNameAsync(string sessionId, string name) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        store.UpdateParticipantNameAsync(sessionId, Context.UserIdentifier!, name).Forget();

        await Clients.OthersInGroup(sessionId.ToString()).OnParticipantNameUpdated(Context.UserIdentifier!, name);
    }
}
