using System;

public static class CardRules
{
    public static bool IsValidTarget(CardDefinition card, bool sameCharacter, bool sameTeam, int distance)
    {
        if (card == null || distance < 0 || distance > card.Range) return false;
        switch (card.Target)
        {
            case CardTarget.Self: return sameCharacter;
            case CardTarget.Ally: return sameTeam;
            case CardTarget.Enemy: return !sameTeam;
            default: return false;
        }
    }

    public static void Apply(CardEffect effect, CharacterStats caster, CharacterStats target, Action<int> draw, string source = null)
    {
        if (caster == null || !caster.IsAlive) return;
        if (effect.kind == CardEffectKind.Draw) { draw?.Invoke(effect.amount); return; }
        if (target == null || !target.IsAlive) return;
        switch (effect.kind)
        {
            case CardEffectKind.Damage: target.TakeDamage(caster.CalculateAttack(effect.amount), false, source); break;
            case CardEffectKind.Block: target.AddBlock(effect.amount); break;
            case CardEffectKind.Heal: target.Heal(effect.amount); break;
            case CardEffectKind.Status: target.ApplyStatus(effect.status, effect.amount, effect.duration); break;
        }
    }
}
