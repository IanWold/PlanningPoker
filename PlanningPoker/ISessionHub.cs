namespace PlanningPoker;

public interface ISessionHub {
    /// <summary>
    /// Adds a point option to the session, which then becomes available for all participants to select as their estimate during a pointing round.
    /// </summary>
    /// <param name="sessionId">The session to update</param>
    /// <param name="point">The point option to add</param>
    /// <seealso cref="ISessionHubClient.OnPointAdded(string, string)"/>
    Task AddPointAsync(string sessionId, string point);

    /// <summary>
    /// Connects a client to the server and ensures the client receives messages pertinent to the session.
    /// This is called every time a client connects or reconnects to the server, participating in a session..
    /// </summary>
    /// <param name="sessionId">The session to connect to</param>
    /// <returns>The current state of the session</returns>
    /// <exception cref="InvalidOperationException">When <paramref name="sessionId"/> does not exist</exception>
    Task<Session> ConnectToSessionAsync(string sessionId);

    /// <summary>
    /// Creates a new session, connecting the client to it
    /// </summary>
    /// <param name="title">The title of the new session</param>
    /// <param name="points">The set of point options to create with the session</param>
    /// <returns>The id of the new session</returns>
    /// <exception cref="ArgumentException">When <paramref name="title"/> is null or whitespace</exception>
    Task<string> CreateSessionAsync(string title, IEnumerable<string> points);

    /// <summary>
    /// Adds the connected client as a participant in the session.
    /// This is only called once per client, at the beginning of their participation in the session.
    /// </summary>
    /// <param name="sessionId">The session to join</param>
    /// <param name="name">The name of the participant</param>
    /// <exception cref="ArgumentException">When <paramref name="name"/> is null or whitespace</exception>
    /// <exception cref="InvalidOperationException">When <paramref name="sessionId"/> does not exist</exception>
    /// <seealso cref="ISessionHubClient.OnParticipantAdded(string, string)"/>
    Task JoinSessionAsync(string sessionId, string name);

    /// <summary>
    /// Disconnects a client from the server and removes the client as a participant frmo the session.
    /// </summary>
    /// <param name="sessionId">The session from which the client is disconnecting</param>
    /// <seealso cref="ISessionHubClient.OnParticipantRemoved(string)"/>
    Task DisconnectFromSessionAsync(string sessionId);

    /// <summary>
    /// Removes a point option from the session, which then removes it from being available for any participants to select as their estimate during a pointing round.
    /// </summary>
    /// <param name="sessionId">The session to update</param>
    /// <param name="point">The point option to remove</param>
    /// <seealso cref="ISessionHubClient.OnPointRemoved(string, string)"/>
    Task RemovePointAsync(string sessionId, string point);

    /// <summary>
    /// "Sends" a star to another participant, incrementing the target participant's star count by one.
    /// </summary>
    /// <param name="sessionId">The session to update</param>
    /// <param name="participantId">The participant to whom the star will be "sent"</param>
    /// <seealso cref="ISessionHubClient.OnStarSentToParticipant(string)"/>
    Task SendStarToParticipantAsync(string sessionId, string participantId);

    /// <summary>
    /// Updates which point option is selected for the client.
    /// </summary>
    /// <param name="sessionId">The session to update</param>
    /// <param name="points">The point option to select for the client</param>
    /// <seealso cref="ISessionHubClient.OnParticipantPointsUpdated(string, string)"/>
    Task UpdateParticipantPointsAsync(string sessionId, string points);

    /// <summary>
    /// Updates the state (hidden/revealed) for the session, hiding or showing all selected point options for all participants.
    /// </summary>
    /// <param name="sessionId">The session to update</param>
    /// <param name="state">The state to which the session will be updated</param>
    /// <seealso cref="ISessionHubClient.OnStateUpdated(State, string)"/>
    Task UpdateSessionStateAsync(string sessionId, State state);

    /// <summary>
    /// Updates the title of the session
    /// </summary>
    /// <param name="sessionId">The session to update</param>
    /// <param name="title">The new session title</param>
    /// <exception cref="ArgumentException">When <paramref name="title"/> is null or whitespace</exception>
    /// <seealso cref="ISessionHubClient.OnTitleUpdated(string, string)"/>
    Task UpdateSessionTitleAsync(string sessionId, string title);

    /// <summary>
    /// Updates the participant name for the client
    /// </summary>
    /// <param name="sessionId">The session to update</param>
    /// <param name="name">The new participant name</param>
    /// <exception cref="ArgumentException">When <paramref name="name"/> i snull or whitespace</exception>
    /// <seealso cref="ISessionHubClient.OnParticipantNameUpdated(string, string)"/>
    Task UpdateParticipantNameAsync(string sessionId, string name);
}
