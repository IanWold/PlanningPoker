namespace PlanningPoker;

public record Effect(EffectType Type, string SenderParticipantId, string TargetParticipantId);