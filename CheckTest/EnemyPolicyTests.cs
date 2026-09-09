using System;

internal static class EnemyPolicyTests
{
    private sealed class Target
    {
        public bool InRange;
        public int? Path;
        public int Health = 20;
    }

    public static int Run()
    {
        int passed = 0;
        void Check(string name, bool condition)
        {
            if (!condition) throw new Exception("FAIL Enemy policy: " + name);
            passed++;
            Console.WriteLine("PASS Enemy policy: " + name);
        }
        Target Pick(Target preferred, params Target[] alive) =>
            EnemyTurnPolicy.ChooseTarget(alive, preferred, t => t.InRange, t => t.Path, t => t.Health);

        var deadTarget = new Target();
        var reachable = new Target { Path = 4 };
        Check("Dead/removed target replaced by living reachable player", Pick(deadTarget, reachable) == reachable);
        var blocked = new Target { Path = null, Health = 1 };
        Check("Blocked low-HP target does not prevent acting on another player", Pick(blocked, blocked, reachable) == reachable);
        var nearby = new Target { InRange = true };
        Check("Attackable player preferred over walking toward old target", Pick(reachable, reachable, nearby) == nearby);
        Check("Valid in-range intent is preserved", Pick(nearby, nearby, new Target { InRange = true, Health = 1 }) == nearby);
        var otherPath = new Target { Path = 2 };
        Check("Reachable locked target preserved if nobody is in range", Pick(reachable, reachable, otherPath) == reachable);
        Check("No intent/new enemy chooses reachable target", Pick(null, blocked, reachable) == reachable);
        Check("All paths blocked returns no target for explicit wait reason", Pick(blocked, blocked) == null);
        Check("No living players returns no target", Pick(deadTarget) == null);
        Check("Shortest path selected without valid intent", Pick(null, reachable, otherPath) == otherPath);
        return passed;
    }
}

