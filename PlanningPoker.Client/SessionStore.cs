namespace PlanningPoker.Client;

public class SessionStore {
    public event EventHandler? Changed;

    #region State

    public string ParticipantId { get; } = Guid.NewGuid().ToString();

    public string? SessionId { get; private set; }

    public Session? Session { get; private set; }

    public string SessionUrl { get; private set; } = string.Empty;

    public Participant? Self =>
        Session?.Participants?.FirstOrDefault(p => p.ParticipantId == ParticipantId);

    public IEnumerable<Participant> Others =>
        Session?.Participants?.Where(p => p.ParticipantId != ParticipantId) ?? [];

    public bool ShowShareNotification { get; private set; }

    public bool IsReconnecting { get; private set; }

    #endregion

    #region Setters

    public void ResetSession(string sessionId, string encryptionKey, Session session, bool showShareNotification = false) {
        SessionId = sessionId;
        Session = session;
        ShowShareNotification = ShowShareNotification || showShareNotification;
        SessionUrl=$"https://freeplanningpoker.io/session/{sessionId}#key={encryptionKey}";
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetReconnecting(bool isReconnecting) =>
        IsReconnecting = isReconnecting;

    public void SetSession(Session session) {
        Session = session;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Clear() {
        SessionId = null;
        Session = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetShowShareNotification(bool showShareNotification) {
        ShowShareNotification = showShareNotification;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void AddParticipant(Participant participant) {
        Session = Session! with { Participants = [.. Session!.Participants, participant] };
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateParticipant(string? participantId, Func<Participant, Participant> update) {
        Session = Session! with {
            Participants = [.. Session!.Participants.Select(p => p.ParticipantId == participantId ? update(p) : p)]
        };
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveParticipant(string participantId) {
        Session = Session! with { Participants = [.. Session!.Participants.Where(p => p.ParticipantId != participantId)] };
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void AddPoint(string point) {
        Session = Session! with { Points = [.. Session!.Points, point] };
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void RemovePoint(string point) {
        Session = Session! with { Points = [.. Session!.Points.Except([point])] };
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetState(State state) {
        Session = Session! with {
            State = state,
            Participants =
                state == State.Revealed
                ? Session!.Participants
                : [.. Session!.Participants.Select(p => p with { Points = "" })]
        };
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetTitle(string title) {
        Session = Session! with { Title = title };
        Changed?.Invoke(this, EventArgs.Empty);
    }

    #endregion
}
