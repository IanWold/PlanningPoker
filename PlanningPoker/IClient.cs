namespace PlanningPoker;

/// <summary>
/// The client contract
/// </summary>
public interface IClient {
    /// <summary>
    /// When a new participant has joined the session.
    /// </summary>
    /// <param name="participantId">The id of the new participant</param>
    /// <param name="name">The name of the new participant</param>
    /// <seealso cref="IServer.JoinSessionAsync(string, string)"/>
    Task OnParticipantAdded(string participantId, string name);

    /// <summary>
    /// When a participant has updated their name.
    /// </summary>
    /// <param name="participantId">The id of the updated participant</param>
    /// <param name="name">The new name</param>
    /// <seealso cref="IServer.UpdateParticipantNameAsync(string, string)"/>
    Task OnParticipantNameUpdated(string participantId, string name);

    /// <summary>
    /// When a participant has updated which point option they have selected.
    /// </summary>
    /// <param name="participantId">The id of the updated participant</param>
    /// <param name="points">The selected point option</param>
    /// <seealso cref="IServer.UpdateParticipantPointsAsync(string, string)"/>
    Task OnParticipantPointsUpdated(string participantId, string points);

    /// <summary>
    /// When a participant leaves the session.
    /// </summary>
    /// <param name="participantId">The id of the removed participant</param>
    /// <seealso cref="IServer.DisconnectFromSessionAsync(string)"/>S
    Task OnParticipantRemoved(string participantId);

    /// <summary>
    /// When a new point option has been added to the session.
    /// </summary>
    /// <param name="point">The point option added</param>
    /// <param name="actingParticipantId">The id of the participant who made the update.</param>
    /// <seealso cref="IServer.AddPointAsync(string, string)"/>
    Task OnPointAdded(string point, string actingParticipantId);

    /// <summary>
    /// When a point option has been removed from a session.
    /// </summary>
    /// <param name="point">The ponit option removed</param>
    /// <param name="actingParticipantId">The id of the participant who made the update.</param>
    /// <seealso cref="IServer.RemovePointAsync(string, string)"/>
    Task OnPointRemoved(string point, string actingParticipantId);

    /// <summary>
    /// When a star has been "sent" to a participant.
    /// </summary>
    /// <param name="participantId">The participant to whom the star has been "sent"</param>
    /// <seealso cref="IServer.SendStarToParticipantAsync(string, string)"/>
    Task OnStarSentToParticipant(string participantId);

    /// <summary>
    /// When the session state (hidden/revealed) has been changed, hiding or showing all selected point options for all participants.
    /// </summary>
    /// <param name="state">The state to which the session has been updated</param>
    /// <param name="actingParticipantId">The id of the participant who made the update.</param>
    /// <seealso cref="IServer.UpdateSessionStateAsync(string, State)"/>
    Task OnStateUpdated(State state, string actingParticipantId);

    /// <summary>
    /// When the title of the sesion has been updated.
    /// </summary>
    /// <param name="title">The new title of the session</param>
    /// <param name="actingParticipantId">The id of the participant who made the update.</param>
    /// <seealso cref="IServer.UpdateSessionTitleAsync(string, string)"/>
    Task OnTitleUpdated(string title, string actingParticipantId);
}
