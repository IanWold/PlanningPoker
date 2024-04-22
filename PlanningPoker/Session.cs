namespace PlanningPoker;

public record Session(
    string Title,
    IEnumerable<Participant> Participants,
    State State,
    IEnumerable<string> Points
);
