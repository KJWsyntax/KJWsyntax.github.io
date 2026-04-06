using System;
using System.Collections.Generic;

namespace CreatureSim;

// ─────────────────────────────────────────
// ABSTRACT BASE CLASS
// ─────────────────────────────────────────

public abstract class Creature
{
    public string Name       { get; protected set; }
    public int    Health     { get; protected set; }
    public int    MaxHealth  { get; protected set; }
    public int    AttackPower { get; protected set; }
    public int    ScoreValue { get; protected set; }   // points awarded on defeat

    protected Creature(string name, int health, int attackPower, int scoreValue)
    {
        Name        = name;
        Health      = health;
        MaxHealth   = health;
        AttackPower = attackPower;
        ScoreValue  = scoreValue;
    }

    public int TakeDamage(int amount)
    {
        if (amount < 0) amount = 0;
        Health -= amount;
        if (Health < 0) Health = 0;
        return Health;
    }

    public bool IsAlive() => Health > 0;

    // Health bar display helper
    public string HealthBar()
    {
        int filled = (int)Math.Round((double)Health / MaxHealth * 10);
        return "[" + new string('█', filled) + new string('░', 10 - filled) + "]";
    }

    public abstract int Attack(Player player);
}

// ─────────────────────────────────────────
// CREATURE SUBCLASSES
// ─────────────────────────────────────────

public class Goblin : Creature
{
    public Goblin() : base("Goblin", health: 35, attackPower: 8, scoreValue: 100) { }

    public override int Attack(Player player)
    {
        int damage = AttackPower;
        player.TakeDamage(damage);
        return damage;
    }
}

public class Troll : Creature
{
    public Troll() : base("Troll", health: 60, attackPower: 10, scoreValue: 250) { }

    public override int Attack(Player player)
    {
        // Troll gets enraged when health drops below 30
        bool enraged = Health <= 30;
        int damage   = enraged ? AttackPower + 5 : AttackPower;
        player.TakeDamage(damage);
        return damage;
    }
}

public class Dragon : Creature
{
    public Dragon() : base("Dragon", health: 120, attackPower: 15, scoreValue: 500) { }

    public override int Attack(Player player)
    {
        // Dragon breathes fire — heavy damage
        int damage = AttackPower * 2;
        player.TakeDamage(damage);
        return damage;
    }
}

// ─────────────────────────────────────────
// PLAYER CLASS
// ─────────────────────────────────────────

public class Player
{
    public string Name     { get; private set; }
    public int    Health   { get; private set; }
    public int    MaxHealth { get; private set; }
    public int    Mana     { get; private set; }
    public int    MaxMana  { get; private set; }
    public int    Strength { get; private set; }
    public int    Score    { get; private set; }       // cumulative score

    private readonly List<string> _inventory = new();

    public Player(string name, int health, int mana, int strength, IEnumerable<string> startingItems)
    {
        Name      = string.IsNullOrWhiteSpace(name) ? "Hero" : name.Trim();
        Health    = health;
        MaxHealth = health;
        Mana      = mana;
        MaxMana   = mana;
        Strength  = strength;
        Score     = 0;

        _inventory.AddRange(startingItems);
    }

    public IReadOnlyList<string> Inventory => _inventory.AsReadOnly();

    public bool IsAlive() => Health > 0;

    public void AddScore(int points)
    {
        if (points > 0) Score += points;
    }

    // ── Combat ──

    public int TakeDamage(int amount)
    {
        if (amount < 0) amount = 0;
        Health -= amount;
        if (Health < 0) Health = 0;
        return Health;
    }

    public int Heal(int amount)
    {
        if (amount < 0) amount = 0;
        Health += amount;
        if (Health > MaxHealth) Health = MaxHealth;  // BUG FIX: cap at max health
        return Health;
    }

    public int Attack(Creature creature)
    {
        int damage = Strength;
        creature.TakeDamage(damage);
        return damage;
    }

    // ── Special Abilities ──

    /// <summary>
    /// Power Strike: costs 15 mana, deals 2.5x normal damage.
    /// Returns damage dealt, or -1 if not enough mana.
    /// </summary>
    public int PowerStrike(Creature creature)
    {
        const int manaCost = 15;
        if (Mana < manaCost) return -1;          // not enough mana
        Mana -= manaCost;
        int damage = (int)(Strength * 2.5);
        creature.TakeDamage(damage);
        return damage;
    }

    /// <summary>
    /// Fireball: costs 25 mana, deals 40 fixed magic damage.
    /// Returns damage dealt, or -1 if not enough mana.
    /// </summary>
    public int Fireball(Creature creature)
    {
        const int manaCost = 25;
        if (Mana < manaCost) return -1;
        Mana -= manaCost;
        const int damage = 40;
        creature.TakeDamage(damage);
        return damage;
    }

    // ── Inventory ──

    public bool AddItem(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName)) return false;
        _inventory.Add(itemName.Trim());
        return true;
    }

    public bool RemoveItemAt(int index)
    {
        if (index < 0 || index >= _inventory.Count) return false;
        _inventory.RemoveAt(index);
        return true;
    }

    // Overload: use by index
    public bool UseItem(int index)
    {
        if (index < 0 || index >= _inventory.Count) return false;
        string item = _inventory[index];
        bool used   = UseItem(item);
        if (used) _inventory.RemoveAt(index);
        return used;
    }

    // Overload: use by name
    public bool UseItem(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName)) return false;

        if (itemName.Equals("Health Potion", StringComparison.OrdinalIgnoreCase))
        {
            Heal(25);
            return true;
        }
        if (itemName.Equals("Mana Crystal", StringComparison.OrdinalIgnoreCase))
        {
            Mana += 15;
            if (Mana > MaxMana) Mana = MaxMana;   // BUG FIX: cap at max mana
            return true;
        }
        if (itemName.Equals("Bomb", StringComparison.OrdinalIgnoreCase))
        {
            return true;   // bomb handled in Program (needs enemy reference)
        }
        return false;
    }

    // ── Display ──

    public string HealthBar()
    {
        int filled = (int)Math.Round((double)Health / MaxHealth * 10);
        return "[" + new string('█', filled) + new string('░', 10 - filled) + "]";
    }

    public string ManaBar()
    {
        int filled = (int)Math.Round((double)Mana / MaxMana * 10);
        return "[" + new string('▓', filled) + new string('░', 10 - filled) + "]";
    }

    public void PrintStats()
    {
        Console.WriteLine($"\n─── {Name}'s Stats ───────────────────");
        Console.WriteLine($"  HP    {HealthBar()} {Health}/{MaxHealth}");
        Console.WriteLine($"  Mana  {ManaBar()} {Mana}/{MaxMana}");
        Console.WriteLine($"  STR   {Strength}");
        Console.WriteLine($"  Score {Score} pts");
        Console.WriteLine("─────────────────────────────────────");
    }

    public void PrintInventory()
    {
        Console.WriteLine("\n─── Inventory ───────────────────────");
        if (_inventory.Count == 0)
        {
            Console.WriteLine("  Empty.");
        }
        else
        {
            for (int i = 0; i < _inventory.Count; i++)
                Console.WriteLine($"  {i}: {_inventory[i]}");
        }
        Console.WriteLine("─────────────────────────────────────");
    }
}

// ─────────────────────────────────────────
// PROGRAM — MAIN GAME LOOP
// ─────────────────────────────────────────

class Program
{
    static void Main(string[] args)
    {
        Console.Clear();
        PrintBanner();

        Console.Write("Enter your character name: ");
        string playerName = Console.ReadLine() ?? "Hero";

        Player player = new Player(
            name:          playerName,
            health:        100,
            mana:          50,
            strength:      12,
            startingItems: new[] { "Rusty Sword", "Health Potion", "Mana Crystal", "Bomb" }
        );

        List<Creature> enemies = new() { new Goblin(), new Troll(), new Dragon() };
        int enemyIndex = 0;
        Creature enemy = enemies[enemyIndex];

        Console.WriteLine($"\nWelcome, {player.Name}! A wild {enemy.Name} appears!\n");
        PrintCombatants(player, enemy);

        bool running = true;

        while (running)
        {
            // ── Death checks ──
            if (!player.IsAlive())
            {
                Console.WriteLine("\n💀 You have fallen. Game Over.");
                Console.WriteLine($"   Final Score: {player.Score} pts");
                break;
            }

            if (!enemy.IsAlive())
            {
                int earned = enemy.ScoreValue;
                player.AddScore(earned);
                Console.WriteLine($"\n⚔️  You defeated the {enemy.Name}! (+{earned} pts | Total: {player.Score})");

                // Bonus heal between fights
                player.Heal(10);
                Console.WriteLine($"   You catch your breath and recover 10 HP. ({player.Health}/{player.MaxHealth})");

                enemyIndex++;
                if (enemyIndex >= enemies.Count)
                {
                    Console.WriteLine("\n🏆 You cleared all enemies. VICTORY!");
                    Console.WriteLine($"   Final Score: {player.Score} pts");
                    break;
                }

                enemy = enemies[enemyIndex];
                Console.WriteLine($"\n⚠️  A new enemy appears: {enemy.Name}!\n");
                PrintCombatants(player, enemy);
            }

            // ── Combat menu ──
            Console.WriteLine("\n═══ COMBAT ═══════════════════════════");
            Console.WriteLine("  1) Attack");
            Console.WriteLine("  2) Special Ability  (Mana required)");
            Console.WriteLine("  3) Use Item");
            Console.WriteLine("  4) View Stats");
            Console.WriteLine("  5) View Inventory");
            Console.WriteLine("  6) Run Away");
            Console.Write("Choose (1-6): ");

            string input = Console.ReadLine() ?? "";

            if (!int.TryParse(input, out int choice))   // BUG FIX: use TryParse not try/catch
            {
                Console.WriteLine("Please enter a number between 1 and 6.");
                continue;
            }

            switch (choice)
            {
                // ── Attack ──
                case 1:
                {
                    int dmg = player.Attack(enemy);
                    Console.WriteLine($"\n⚔️  You hit {enemy.Name} for {dmg} damage.  {enemy.Name} {enemy.HealthBar()} {enemy.Health}/{enemy.MaxHealth}");

                    if (enemy.IsAlive())
                    {
                        int enemyDmg = enemy.Attack(player);
                        Console.WriteLine($"💥  {enemy.Name} hits you for {enemyDmg}.  You {player.HealthBar()} {player.Health}/{player.MaxHealth}");
                    }
                    break;
                }

                // ── Special Ability ──
                case 2:
                {
                    Console.WriteLine("\n─── Special Abilities ───────────────");
                    Console.WriteLine($"  1) Power Strike  (15 mana) — 2.5x damage");
                    Console.WriteLine($"  2) Fireball      (25 mana) — 40 magic damage");
                    Console.WriteLine($"  Current Mana: {player.Mana}/{player.MaxMana}");
                    Console.Write("Choose ability (1-2): ");

                    string abilityInput = Console.ReadLine() ?? "";
                    if (!int.TryParse(abilityInput, out int abilityChoice))
                    {
                        Console.WriteLine("Invalid choice.");
                        break;
                    }

                    int spellDmg = abilityChoice switch
                    {
                        1 => player.PowerStrike(enemy),
                        2 => player.Fireball(enemy),
                        _ => -2   // invalid choice sentinel
                    };

                    if (spellDmg == -2)
                    {
                        Console.WriteLine("Invalid ability choice.");
                        break;
                    }
                    if (spellDmg == -1)
                    {
                        Console.WriteLine("❌ Not enough mana!");
                        break;
                    }

                    string abilityName = abilityChoice == 1 ? "Power Strike" : "Fireball";
                    Console.WriteLine($"\n✨ {abilityName}! You deal {spellDmg} damage to {enemy.Name}.  {enemy.Name} {enemy.HealthBar()} {enemy.Health}/{enemy.MaxHealth}");
                    Console.WriteLine($"   Mana remaining: {player.Mana}/{player.MaxMana}");

                    if (enemy.IsAlive())
                    {
                        int enemyDmg = enemy.Attack(player);
                        Console.WriteLine($"💥  {enemy.Name} hits you for {enemyDmg}.  You {player.HealthBar()} {player.Health}/{player.MaxHealth}");
                    }
                    break;
                }

                // ── Use Item ──
                case 3:
                {
                    player.PrintInventory();
                    if (player.Inventory.Count == 0) break;

                    Console.Write("Enter item index to use: ");
                    string itemInput = Console.ReadLine() ?? "";

                    if (!int.TryParse(itemInput, out int itemIndex))   // BUG FIX: TryParse
                    {
                        Console.WriteLine("Invalid index. Enter a whole number.");
                        break;
                    }

                    if (itemIndex < 0 || itemIndex >= player.Inventory.Count)
                    {
                        Console.WriteLine("That index is out of range.");
                        break;
                    }

                    // Handle bomb separately — needs enemy reference
                    if (player.Inventory[itemIndex].Equals("Bomb", StringComparison.OrdinalIgnoreCase))
                    {
                        player.UseItem(itemIndex);
                        int bombDmg = 30;
                        enemy.TakeDamage(bombDmg);
                        Console.WriteLine($"\n💣 BOOM! Bomb deals {bombDmg} to {enemy.Name}.  {enemy.Name} {enemy.HealthBar()} {enemy.Health}/{enemy.MaxHealth}");
                    }
                    else
                    {
                        bool used = player.UseItem(itemIndex);
                        Console.WriteLine(used
                            ? $"✅ Item used.  HP: {player.Health}/{player.MaxHealth}  Mana: {player.Mana}/{player.MaxMana}"
                            : "❌ Could not use that item.");
                    }

                    // Enemy turn after item use
                    if (enemy.IsAlive())
                    {
                        int enemyDmg = enemy.Attack(player);
                        Console.WriteLine($"💥  {enemy.Name} hits you for {enemyDmg}.  You {player.HealthBar()} {player.Health}/{player.MaxHealth}");
                    }
                    break;
                }

                case 4:
                    player.PrintStats();
                    Console.WriteLine($"\n  {enemy.Name} {enemy.HealthBar()} {enemy.Health}/{enemy.MaxHealth}");
                    break;

                case 5:
                    player.PrintInventory();
                    break;

                case 6:
                    Console.WriteLine($"\n🏃 You ran away! Final Score: {player.Score} pts");
                    running = false;
                    break;

                default:
                    Console.WriteLine("Choose a valid option (1-6).");
                    break;
            }
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    // ── Helpers ──

    static void PrintBanner()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(@"
  ██████╗██████╗ ███████╗ █████╗ ████████╗██╗   ██╗██████╗ ███████╗
 ██╔════╝██╔══██╗██╔════╝██╔══██╗╚══██╔══╝██║   ██║██╔══██╗██╔════╝
 ██║     ██████╔╝█████╗  ███████║   ██║   ██║   ██║██████╔╝█████╗  
 ██║     ██╔══██╗██╔══╝  ██╔══██║   ██║   ██║   ██║██╔══██╗██╔══╝  
 ╚██████╗██║  ██║███████╗██║  ██║   ██║   ╚██████╔╝██║  ██║███████╗
  ╚═════╝╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝  ╚═╝    ╚═════╝ ╚═╝  ╚═╝╚══════╝
          S I M U L A T O R  —  Can you defeat the Dragon?
        ");
        Console.ResetColor();
    }

    static void PrintCombatants(Player player, Creature enemy)
    {
        Console.WriteLine($"  {player.Name,-15} {player.HealthBar()} {player.Health}/{player.MaxHealth} HP");
        Console.WriteLine($"  {enemy.Name,-15} {enemy.HealthBar()} {enemy.Health}/{enemy.MaxHealth} HP");
    }
}