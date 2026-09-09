using System;
using System.Collections.Generic;

public enum StatusKind { Poison, Weak, Vulnerable, Strength }

public readonly struct DamageReport
{
    public string Source { get; }
    public int HealthLost { get; }
    public int BlockAbsorbed { get; }
    public int RemainingHealth { get; }
    public DamageReport(string source, int healthLost, int blockAbsorbed, int remainingHealth)
    { Source = source; HealthLost = healthLost; BlockAbsorbed = blockAbsorbed; RemainingHealth = remainingHealth; }
}

/// <summary>Independent of Unity. Durations tick at the END of the affected team's turn.</summary>
public sealed class CharacterStats
{
    private sealed class Status { public int Amount; public int Turns; }
    private readonly Dictionary<StatusKind, Status> statuses = new Dictionary<StatusKind, Status>();
    public int MaxHealth { get; }
    public int Health { get; private set; }
    public int Block { get; private set; }
    public bool IsAlive => Health > 0;
    public event Action Changed;
    public event Action<DamageReport> DamageTaken;

    public CharacterStats(int maxHealth)
    {
        if (maxHealth < 1) throw new ArgumentOutOfRangeException(nameof(maxHealth));
        MaxHealth = Health = maxHealth;
    }

    public int StatusAmount(StatusKind kind) => statuses.TryGetValue(kind, out var status) ? status.Amount : 0;
    public int StatusTurns(StatusKind kind) => statuses.TryGetValue(kind, out var status) ? status.Turns : 0;

    public void ApplyStatus(StatusKind kind, int amount, int turns)
    {
        if (!Enum.IsDefined(typeof(StatusKind), kind) || amount < 1 || turns < 1)
            throw new ArgumentOutOfRangeException(nameof(amount));
        if (!IsAlive) return;
        if (!statuses.TryGetValue(kind, out var status)) statuses[kind] = status = new Status();
        status.Amount = (int)Math.Min(100000, (long)status.Amount + amount);
        status.Turns = Math.Max(status.Turns, turns);
        Changed?.Invoke();
    }

    public int CalculateAttack(int baseDamage)
    {
        long value = Math.Max(0, (long)baseDamage + StatusAmount(StatusKind.Strength));
        if (StatusAmount(StatusKind.Weak) > 0) value = value * 3 / 4;
        return (int)Math.Min(int.MaxValue, value);
    }

    public void TakeDamage(int amount, bool direct = false, string source = null)
    {
        if (!IsAlive || amount <= 0) return;
        long damage = amount;
        int oldHealth = Health;
        int absorbed = 0;
        if (!direct && StatusAmount(StatusKind.Vulnerable) > 0) damage = damage * 3 / 2;
        if (!direct)
        {
            absorbed = (int)Math.Min(Block, damage);
            Block -= absorbed;
            damage -= absorbed;
        }
        Health = (int)Math.Max(0, Health - damage);
        DamageTaken?.Invoke(new DamageReport(source ?? "Nguồn không xác định", oldHealth - Health, absorbed, Health));
        Changed?.Invoke();
    }

    public void Heal(int amount)
    {
        if (!IsAlive || amount <= 0) return; // Healing is not resurrection.
        Health = (int)Math.Min(MaxHealth, (long)Health + amount);
        Changed?.Invoke();
    }

    public void AddBlock(int amount)
    {
        if (!IsAlive || amount <= 0) return;
        Block = (int)Math.Min(int.MaxValue, (long)Block + amount);
        Changed?.Invoke();
    }

    public void BeginTurn()
    {
        Block = 0;
        Changed?.Invoke();
    }

    public void EndTurn()
    {
        TakeDamage(StatusAmount(StatusKind.Poison), true, "Poison");
        foreach (var kind in new List<StatusKind>(statuses.Keys))
            if (--statuses[kind].Turns <= 0) statuses.Remove(kind);
        Changed?.Invoke();
    }
}
