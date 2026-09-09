using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;

internal static class Program
{
    private static int passed;
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); }
    private static void Test(string name, Action body)
    { body(); passed++; Console.WriteLine("PASS " + name); }
    private static void Reject(Action body)
    {
        try { body(); } catch (ArgumentException) { return; }
        throw new Exception("Expected validation failure.");
    }

    public static int Main()
    {
        try
        {
            GameDatabase db = GameDatabase.CreateDefault();
            passed += EnemyPolicyTests.Run();
            Test("Duplicate definitions and invalid effects rejected", () =>
            {
                Reject(() => new GameDatabase(new[] { db.Get("strike"), db.Get("strike") }));
                Reject(() => new CardDefinition("bad", "bad", -1, 0, CardTarget.Self, new[] { new CardEffect(CardEffectKind.Heal, 1) }));
                Reject(() => new CardDefinition("bad", "bad", 1, 0, CardTarget.Self, new[] { new CardEffect(CardEffectKind.Status, 1, StatusKind.Weak, 0) }));
            });
            Test("Three-card initial draw, energy and per-copy identity", () =>
            {
                var deck = new DeckState(db.Resolve(GameDatabase.DefaultDeck()), 1);
                deck.BeginTurn(3, 3);
                Check(deck.Hand.Count == 3 && deck.Energy == 3 && deck.DrawPile.Count == 8, "Start-turn budget.");
                var duplicateDeck = new DeckState(new[] { db.Get("strike"), db.Get("strike") }, 1);
                duplicateDeck.Draw(2);
                Check(!ReferenceEquals(duplicateDeck.Hand[0], duplicateDeck.Hand[1]), "Copies must have unique identity.");
            });
            Test("Consumed card cannot be replayed and foreign card cannot be used", () =>
            {
                var deck = new DeckState(new[] { db.Get("strike") }, 1);
                deck.BeginTurn(3, 1);
                var card = deck.Hand[0];
                Check(deck.TryConsume(card), "Play should succeed.");
                Check(!deck.TryConsume(card) && deck.Energy == 2, "Double consumption.");
                Check(!deck.TryConsume(new CardInstance(db.Get("strike"))), "Foreign instance.");
            });
            Test("Insufficient energy does not mutate piles", () =>
            {
                var deck = new DeckState(new[] { db.Get("strike") }, 1);
                deck.BeginTurn(0, 1);
                Check(!deck.TryConsume(deck.Hand[0]) && deck.Hand.Count == 1 && deck.DiscardPile.Count == 0, "Failed play mutated deck.");
            });
            Test("Unused cards retained and used cards recycled, including former exhaust cards", () =>
            {
                var deck = new DeckState(new[] { db.Get("mend"), db.Get("strike") }, 1, 2);
                deck.BeginTurn(3, 10);
                Check(deck.Hand.Count == 2, "Hand cap.");
                var mend = deck.Hand.First(c => c.Definition.Id == "mend");
                var strike = deck.Hand.First(c => c.Definition.Id == "strike");
                deck.TryConsume(mend);
                Check(deck.DrawPile.Contains(mend) && deck.Hand.Contains(strike), "Recycle must preserve identity.");
                deck.EndTurn();
                Check(deck.Hand.Count == 1 && deck.Energy == 0, "Keep unused hand.");
                deck.BeginTurn(3, 10);
                Check(deck.Hand.Count == 2 && deck.Hand.Contains(mend) && deck.Hand.Contains(strike), "Recycled card was not drawn.");
                deck.TryConsume(strike);
                Check(deck.Draw(10) == 1 && deck.Hand.Contains(strike) && deck.ExhaustPile.Count == 0, "Ordinary card did not return.");
            });
            Test("Damage report includes actual HP loss, block absorption and source", () =>
            {
                var stats = new CharacterStats(10);
                DamageReport report = default;
                stats.DamageTaken += value => report = value;
                stats.AddBlock(5);
                stats.TakeDamage(3, false, "Melee");
                Check(report.Source == "Melee" && report.HealthLost == 0 && report.BlockAbsorbed == 3 && report.RemainingHealth == 10, "Blocked hit report.");
                stats.TakeDamage(100, false, "Strike");
                Check(report.HealthLost == 10 && report.BlockAbsorbed == 2 && report.RemainingHealth == 0, "Overkill should report real HP lost.");
                var poisoned = new CharacterStats(10);
                poisoned.DamageTaken += value => report = value;
                poisoned.ApplyStatus(StatusKind.Poison, 2, 1);
                poisoned.EndTurn();
                Check(report.Source == "Poison" && report.HealthLost == 2, "Poison source missing.");
            });
            Test("Supply occurs only after two complete rounds and never twice per round", () =>
            {
                var schedule = new CardSupplySchedule();
                var deck = new DeckState(db.Resolve(GameDatabase.DefaultDeck()), 1);
                for (int round = 1; round <= 5; round++)
                {
                    deck.BeginTurn(3, schedule.GetDrawCount(round));
                    Check(deck.Hand.Count == 3 + (round - 1) / 2, "Wrong draw cadence.");
                    Check(schedule.GetDrawCount(round) == 0, "Duplicate grant in same round.");
                    deck.EndTurn();
                }
                Check(schedule.GetDrawCount(2) == 0, "Old round granted cards.");
            });
            Test("Empty draw and discard terminate; seeded shuffle repeats", () =>
            {
                var a = new DeckState(db.Resolve(GameDatabase.DefaultDeck()), 45, 30);
                var b = new DeckState(db.Resolve(GameDatabase.DefaultDeck()), 45, 30);
                a.Draw(100); b.Draw(100);
                Check(a.Hand.Count == 11 && a.Draw(100) == 0, "Empty deck draw.");
                Check(a.Hand.Select(c => c.Definition.Id).SequenceEqual(b.Hand.Select(c => c.Definition.Id)), "Seed mismatch.");
            });
            Test("Pile conservation across 100 turns", () =>
            {
                var deck = new DeckState(db.Resolve(GameDatabase.DefaultDeck()), 7);
                for (int round = 0; round < 100; round++)
                {
                    deck.BeginTurn(3, 5);
                    foreach (var card in deck.Hand.ToArray()) deck.TryConsume(card);
                    deck.EndTurn();
                    var all = deck.DrawPile.Concat(deck.Hand).Concat(deck.DiscardPile).Concat(deck.ExhaustPile).ToArray();
                    Check(all.Length == 11 && all.Distinct().Count() == 11, "Lost or duplicated copy.");
                }
            });
            Test("Block absorbs hits, expires on own turn, healing clamps", () =>
            {
                var stats = new CharacterStats(20);
                stats.AddBlock(5); stats.TakeDamage(8);
                Check(stats.Health == 17 && stats.Block == 0, "Block calculation.");
                stats.AddBlock(10); stats.BeginTurn();
                Check(stats.Block == 0, "Block did not expire.");
                stats.Heal(100); Check(stats.Health == 20, "Overheal.");
                stats.TakeDamage(100); stats.Heal(100);
                Check(!stats.IsAlive && stats.Health == 0, "Healing revived dead character.");
            });
            Test("Strength and Weak affect outgoing damage; Vulnerable incoming", () =>
            {
                var attacker = new CharacterStats(20);
                var target = new CharacterStats(20);
                attacker.ApplyStatus(StatusKind.Strength, 2, 2);
                attacker.ApplyStatus(StatusKind.Weak, 1, 1);
                target.ApplyStatus(StatusKind.Vulnerable, 1, 2);
                target.AddBlock(2);
                target.TakeDamage(attacker.CalculateAttack(6));
                Check(target.Health == 13, "Expected floor((6+2)*.75)*1.5-2 = 7.");
                attacker.EndTurn();
                Check(attacker.CalculateAttack(6) == 8, "Weak expiry.");
            });
            Test("Poison bypasses block, ticks exactly its duration", () =>
            {
                var stats = new CharacterStats(20);
                stats.AddBlock(10);
                stats.ApplyStatus(StatusKind.Poison, 2, 3);
                stats.EndTurn(); stats.EndTurn(); stats.EndTurn(); stats.EndTurn();
                Check(stats.Health == 14 && stats.Block == 10 && stats.StatusAmount(StatusKind.Poison) == 0, "Poison duration.");
            });
            Test("Status stacking adds potency and refreshes longest duration", () =>
            {
                var stats = new CharacterStats(20);
                stats.ApplyStatus(StatusKind.Poison, 2, 3);
                stats.ApplyStatus(StatusKind.Poison, 1, 1);
                Check(stats.StatusAmount(StatusKind.Poison) == 3 && stats.StatusTurns(StatusKind.Poison) == 3, "Stack policy.");
            });
            Test("Target validation rejects wrong team and out-of-range", () =>
            {
                Check(!CardRules.IsValidTarget(db.Get("strike"), false, true, 1), "Friendly damage target.");
                Check(!CardRules.IsValidTarget(db.Get("strike"), false, false, 4), "Out of range.");
                Check(CardRules.IsValidTarget(db.Get("strike"), false, false, 3), "Range boundary.");
                Check(!CardRules.IsValidTarget(db.Get("guard"), false, true, 0), "Self card on ally.");
                Check(CardRules.IsValidTarget(db.Get("mend"), true, true, 0), "Heal self.");
            });
            Test("All five effect kinds execute through shared combat rules", () =>
            {
                var caster = new CharacterStats(20); var target = new CharacterStats(20);
                int drawn = 0;
                CardRules.Apply(new CardEffect(CardEffectKind.Block, 5), caster, target, null);
                CardRules.Apply(new CardEffect(CardEffectKind.Damage, 8), caster, target, null);
                CardRules.Apply(new CardEffect(CardEffectKind.Heal, 2), caster, target, null);
                CardRules.Apply(new CardEffect(CardEffectKind.Status, 2, StatusKind.Poison, 3), caster, target, null);
                CardRules.Apply(new CardEffect(CardEffectKind.Draw, 2), caster, target, n => drawn += n);
                Check(target.Health == 19 && target.StatusAmount(StatusKind.Poison) == 2 && drawn == 2, "Effect execution.");
            });
            Test("Progress validates versions, IDs, counts and copy isolation", () =>
            {
                var data = new PlayerProgress { deck = GameDatabase.DefaultDeck() };
                Check(data.IsValid(db), "Default progress.");
                var copy = data.Copy(); copy.deck.Clear();
                Check(data.deck.Count == 11 && !copy.IsValid(db), "Copy exposed original.");
                data.version = 2; Check(!data.IsValid(db), "Unknown schema.");
                data.version = 1; data.deck.Add("missing"); Check(!data.IsValid(db), "Unknown ID.");
            });
            Test("Save round-trip, atomic replacement and corrupted-save rejection", () =>
            {
                string directory = Path.Combine(Path.GetTempPath(), "turngame-test-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, "save.json");
                var options = new JsonSerializerOptions { IncludeFields = true };
                var store = new ProgressStore(path, db, p => JsonSerializer.Serialize(p, options), text =>
                {
                    try { return JsonSerializer.Deserialize<PlayerProgress>(text, options); }
                    catch (JsonException e) { throw new FormatException("Malformed JSON", e); }
                });
                Check(!store.TryLoad(out _, out var error) && error == null, "Missing save should be normal.");
                var data = new PlayerProgress { deck = GameDatabase.DefaultDeck(), battlesWon = 2, stage = 3 };
                Check(store.TrySave(data, out error), error);
                Check(store.TryLoad(out var loaded, out error) && loaded.battlesWon == 2 && loaded.deck.SequenceEqual(data.deck), "Round trip.");
                data.stage = 4; Check(store.TrySave(data, out error) && File.Exists(path + ".bak"), "Atomic replace/backup.");
                Check(store.TryLoad(out loaded, out error) && loaded.stage == 4, "Replacement contents.");
                File.WriteAllText(path, "{broken");
                Check(!store.TryLoad(out _, out error) && error != null, "Corrupted save accepted.");
                data.deck.Add("unknown");
                Check(!store.TrySave(data, out _) && File.ReadAllText(path) == "{broken", "Invalid save replaced original.");
                // Only remove the individually named files created by this test.
                File.Delete(path); File.Delete(path + ".bak"); Directory.Delete(directory);
            });
            Console.WriteLine($"{passed} tests passed.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
