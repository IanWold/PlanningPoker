namespace PlanningPoker.Client;

public static class EffectTypeExtensions
{
    public static string GetIcon(this EffectType effect) => effect switch {
        EffectType.Flip => "swap-vertical",
        EffectType.Beer => "beer",
        EffectType.Dice => "dice",
        _ => throw new ArgumentException(null, nameof(effect))
    };

    public static string GetVerb(this EffectType effect) => effect switch {
        EffectType.Flip => "flipped",
        EffectType.Beer => "bought a beer for",
        EffectType.Dice => "rolled dice for",
        _ => throw new ArgumentException(null, nameof(effect))
    };
}
