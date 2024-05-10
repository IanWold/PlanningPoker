namespace PlanningPoker;

public static class EffectTypeExtensions
{
    public static int GetCost(this EffectType effect) => effect switch {
        EffectType.Flip => 3,
        EffectType.Beer => 5,
        EffectType.Dice => 5,
        _ => throw new ArgumentException(null, nameof(effect))
    };
}
