using System;
using System.Collections.Generic;
using System.Linq;
using DungeonHunter.Core;
using Godot;

namespace DungeonHunter.Game;

public sealed class World
{
    public const int StoneRadius = 3;

    private readonly AStarGrid2D _pathfinder = new();
    private readonly List<Vector2I> _walkableCells = new();
    private readonly List<Noise> _heroNoises = new();

    public World(LevelData level, int seed)
    {
        Level = level;
        Rng = new Random(seed);
        Hero = new Hero { Position = level.HeroStart };
        OpenDoors = new HashSet<Vector2I>(level.OpenDoors);
        var id = 0;
        foreach (var spawn in level.Monsters)
        {
            var type = MonsterType.Get(spawn.Kind);
            Monsters.Add(new Monster
            {
                Id = id++,
                Type = type,
                Behavior = spawn.Behavior,
                Home = spawn.Position,
                Position = spawn.Position,
                Hp = type.Hp,
            });
        }
        _pathfinder.Region = new Rect2I(0, 0, level.Width, level.Height);
        _pathfinder.CellSize = Vector2.One;
        _pathfinder.DiagonalMode = AStarGrid2D.DiagonalModeEnum.OnlyIfNoObstacles;
        _pathfinder.DefaultComputeHeuristic = AStarGrid2D.Heuristic.Octile;
        _pathfinder.DefaultEstimateHeuristic = AStarGrid2D.Heuristic.Octile;
        _pathfinder.Update();
        for (var y = 0; y < level.Height; y++)
        {
            for (var x = 0; x < level.Width; x++)
            {
                var cell = new Vector2I(x, y);
                if (level.Terrain[x, y] == Terrain.Wall)
                {
                    _pathfinder.SetPointSolid(cell, true);
                }
                else if (level.Terrain[x, y] != Terrain.Door)
                {
                    _walkableCells.Add(cell);
                }
            }
        }
        UpdateVisibility();
    }

    public LevelData Level { get; }
    public Random Rng { get; }
    public Hero Hero { get; }
    public List<Monster> Monsters { get; } = new();
    public HashSet<Vector2I> OpenDoors { get; }
    public Dictionary<Vector2I, int> ArrowsOnGround { get; } = new();
    public Dictionary<Vector2I, bool> RememberedDoors { get; } = new();
    public Dictionary<Vector2I, int> RememberedArrows { get; } = new();
    public HashSet<Vector2I> Lit { get; } = new();
    public HashSet<Vector2I> Seen { get; } = new();
    public List<GameEvent> Events { get; } = new();
    public int Turn { get; private set; }
    public bool Won { get; private set; }
    public bool Lost => Hero.IsDead;
    public bool Over => Won || Lost;

    public IEnumerable<Monster> LivingMonsters => Monsters.Where(m => !m.IsDead);

    public bool InBounds(Vector2I c) => c.X >= 0 && c.Y >= 0 && c.X < Level.Width && c.Y < Level.Height;

    public Terrain TerrainAt(Vector2I c) => InBounds(c) ? Level.Terrain[c.X, c.Y] : Terrain.Wall;

    public bool IsWall(Vector2I c) => TerrainAt(c) == Terrain.Wall;

    public bool IsDoor(Vector2I c) => TerrainAt(c) == Terrain.Door;

    public bool IsClosedDoor(Vector2I c) => IsDoor(c) && !OpenDoors.Contains(c);

    public bool BlocksSight(Vector2I c) => IsWall(c) || IsClosedDoor(c);

    public bool IsPassable(Vector2I c) => !BlocksSight(c);

    public Monster MonsterAt(Vector2I c) => Monsters.FirstOrDefault(m => !m.IsDead && m.Position == c);

    public bool IsOccupied(Vector2I c) => Hero.Position == c || MonsterAt(c) != null;

    public bool IsMonsterVisible(Monster m) => !m.IsDead && Lit.Contains(m.Position);

    public bool CornerFree(Vector2I from, Vector2I direction)
    {
        if (direction.X == 0 || direction.Y == 0)
        {
            return true;
        }
        return IsPassable(from + new Vector2I(direction.X, 0)) && IsPassable(from + new Vector2I(0, direction.Y));
    }

    public bool CanStep(Vector2I from, Vector2I direction)
    {
        var to = from + direction;
        return InBounds(to) && IsPassable(to) && CornerFree(from, direction) && !IsOccupied(to);
    }

    public bool HasLineOfSight(Vector2I from, Vector2I to) => ClearLine(from, to) || ClearLine(to, from);

    private bool ClearLine(Vector2I from, Vector2I to)
    {
        var x0 = from.X;
        var y0 = from.Y;
        var dx = Math.Abs(to.X - x0);
        var dy = -Math.Abs(to.Y - y0);
        var sx = x0 < to.X ? 1 : -1;
        var sy = y0 < to.Y ? 1 : -1;
        var err = dx + dy;
        while (true)
        {
            if (x0 == to.X && y0 == to.Y)
            {
                return true;
            }
            var e2 = 2 * err;
            if (e2 >= dy)
            {
                err += dy;
                x0 += sx;
            }
            if (e2 <= dx)
            {
                err += dx;
                y0 += sy;
            }
            var cell = new Vector2I(x0, y0);
            if (cell != to && BlocksSight(cell))
            {
                return false;
            }
        }
    }

    public void UpdateVisibility()
    {
        Lit.Clear();
        if (Hero.StoneCarried)
        {
            AddLight(Hero.Position, Hero.StoneLit ? StoneRadius : 1);
        }
        else
        {
            AddLight(Hero.Position, 1);
            AddLight(Hero.StonePosition, StoneRadius);
        }
        Seen.UnionWith(Lit);
        foreach (var cell in Lit)
        {
            if (IsDoor(cell))
            {
                foreach (var c in DoorCells(cell))
                {
                    RememberedDoors[c] = OpenDoors.Contains(c);
                }
            }
            if (ArrowsOnGround.TryGetValue(cell, out var count))
            {
                RememberedArrows[cell] = count;
            }
            else
            {
                RememberedArrows.Remove(cell);
            }
        }
    }

    private void AddLight(Vector2I source, int radius)
    {
        for (var dy = -radius; dy <= radius; dy++)
        {
            for (var dx = -radius; dx <= radius; dx++)
            {
                var cell = source + new Vector2I(dx, dy);
                if (!InBounds(cell))
                {
                    continue;
                }
                var inside = radius == 1 || dx * dx + dy * dy <= radius * radius;
                if (inside && HasLineOfSight(source, cell))
                {
                    Lit.Add(cell);
                }
            }
        }
    }

    public bool MonsterSeesHero(Monster m)
    {
        var range = m.Type.SightRange;
        return Grid.DistanceSquared(m.Position, Hero.Position) <= range * range && HasLineOfSight(m.Position, Hero.Position);
    }

    public bool TryMove(Vector2I direction)
    {
        var to = Hero.Position + direction;
        var diagonal = !Grid.IsOrthogonal(direction);
        if (!InBounds(to))
        {
            return false;
        }
        if (diagonal && !CornerFree(Hero.Position, direction))
        {
            return Refuse("Impossible de passer en diagonale par un coin de mur.");
        }
        var monster = MonsterAt(to);
        if (monster != null)
        {
            if (Hero.Equipped != Weapon.Dagger)
            {
                return Refuse("Trop près pour l'arc : prenez la dague (touche 1).");
            }
            if (diagonal)
            {
                return Refuse("La dague frappe dans les 4 directions seulement.");
            }
            MeleeAttack(monster);
            return EndHeroTurn();
        }
        if (IsClosedDoor(to))
        {
            if (diagonal)
            {
                return Refuse("Une porte s'ouvre de face, pas en diagonale.");
            }
            SetDoor(to, true);
            HeroNoise(to, NoiseLevel.Light, "Grinc");
            return EndHeroTurn();
        }
        if (!IsPassable(to))
        {
            return Refuse("Un mur bloque le passage.");
        }
        var from = Hero.Position;
        Hero.Position = to;
        Events.Add(new GameEvent(GameEventKind.HeroMoved, From: from, To: to));
        HeroNoise(to, NoiseLevel.Light, "Tap");
        if (TerrainAt(to) == Terrain.Stairs)
        {
            Won = true;
        }
        return EndHeroTurn();
    }

    public bool TryAttack(Vector2I direction)
    {
        if (Hero.Equipped == Weapon.Bow)
        {
            return TryShoot(direction);
        }
        if (!Grid.IsOrthogonal(direction))
        {
            return Refuse("La dague frappe dans les 4 directions seulement.");
        }
        var target = Hero.Position + direction;
        var monster = MonsterAt(target);
        if (monster != null)
        {
            MeleeAttack(monster);
            return EndHeroTurn();
        }
        Events.Add(new GameEvent(GameEventKind.HeroAttacked, From: Hero.Position, To: target));
        if (BlocksSight(target))
        {
            Message("Vous frappez la paroi.");
            HeroNoise(target, NoiseLevel.Loud, "Bang");
        }
        else
        {
            Message("Vous frappez dans le vide.");
            HeroNoise(Hero.Position, NoiseLevel.Light, "Fft");
        }
        return EndHeroTurn();
    }

    public bool TryShoot(Vector2I direction) => TryShootAt(Hero.Position + direction * Hero.BowMaxRange);

    public List<Vector2I> ArrowPath(Vector2I target)
    {
        var path = new List<Vector2I>();
        var delta = target - Hero.Position;
        var steps = Math.Max(Math.Abs(delta.X), Math.Abs(delta.Y));
        if (steps == 0)
        {
            return path;
        }
        var far = Hero.Position + new Vector2I(
            (int)Math.Round(delta.X * (double)Hero.BowMaxRange / steps),
            (int)Math.Round(delta.Y * (double)Hero.BowMaxRange / steps));
        var previous = Hero.Position;
        foreach (var cell in Line(Hero.Position, far))
        {
            if (cell == Hero.Position)
            {
                continue;
            }
            if (BlocksSight(cell) || !CornerFree(previous, cell - previous))
            {
                break;
            }
            path.Add(cell);
            if (path.Count >= Hero.BowMaxRange)
            {
                break;
            }
            previous = cell;
        }
        return path;
    }

    public bool TryShootAt(Vector2I target)
    {
        if (Hero.Equipped != Weapon.Bow)
        {
            return Refuse("L'arc n'est pas équipé (touche 2).");
        }
        if (Hero.Arrows <= 0)
        {
            return Refuse("Plus de flèches.");
        }
        if (target == Hero.Position)
        {
            return Refuse("Choisissez une direction de tir.");
        }
        var path = ArrowPath(target);
        if (path.Count > 0 && MonsterAt(path[0]) != null)
        {
            return Refuse("Trop près pour l'arc : il faut au moins 2 cases. Prenez la dague (touche 1).");
        }
        Hero.Arrows--;
        Events.Add(new GameEvent(GameEventKind.HeroShot, From: Hero.Position, To: target));
        if (path.Count == 0)
        {
            AddArrows(Hero.Position, 1);
            Message("La flèche heurte la paroi et tombe à vos pieds.");
            HeroNoise(Hero.Position, NoiseLevel.Light, "Twang");
            HeroNoise(Hero.Position + StepToward(target - Hero.Position), NoiseLevel.Loud, "Tac");
            return EndHeroTurn();
        }
        var landing = path[^1];
        Monster hit = null;
        foreach (var cell in path)
        {
            hit = MonsterAt(cell);
            if (hit != null)
            {
                landing = cell;
                break;
            }
        }
        Events.Add(new GameEvent(GameEventKind.Arrow, hit?.Id ?? -1, Hero.Position, landing));
        HeroNoise(Hero.Position, NoiseLevel.Light, "Twang");
        if (hit != null)
        {
            hit.ArrowsStuck++;
            Message($"La flèche touche le {hit.Type.DisplayName.ToLowerInvariant()}.");
            Damage(hit, 1);
            HeroNoise(hit.Position, NoiseLevel.Loud, "Tchak");
        }
        else
        {
            AddArrows(landing, 1);
            Message("La flèche ne touche rien et retombe au sol.");
            HeroNoise(landing, NoiseLevel.Light, "Tac");
        }
        return EndHeroTurn();
    }

    private static Vector2I StepToward(Vector2I delta) => new(Math.Sign(delta.X), Math.Sign(delta.Y));

    private static IEnumerable<Vector2I> Line(Vector2I from, Vector2I to)
    {
        var x0 = from.X;
        var y0 = from.Y;
        var dx = Math.Abs(to.X - x0);
        var dy = -Math.Abs(to.Y - y0);
        var sx = x0 < to.X ? 1 : -1;
        var sy = y0 < to.Y ? 1 : -1;
        var err = dx + dy;
        while (true)
        {
            yield return new Vector2I(x0, y0);
            if (x0 == to.X && y0 == to.Y)
            {
                yield break;
            }
            var e2 = 2 * err;
            if (e2 >= dy)
            {
                err += dy;
                x0 += sx;
            }
            if (e2 <= dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    public bool Equip(Weapon weapon)
    {
        if (Hero.Equipped == weapon)
        {
            return false;
        }
        Hero.Equipped = weapon;
        Message(weapon == Weapon.Bow ? "Vous prenez l'arc." : "Vous prenez la dague.");
        HeroNoise(Hero.Position, NoiseLevel.Light, "Clic");
        return EndHeroTurn();
    }

    public bool ToggleStone()
    {
        if (!Hero.StoneCarried)
        {
            return Refuse("La pierre de lumière est au sol (R pour la rappeler).");
        }
        Hero.StoneLit = !Hero.StoneLit;
        Message(Hero.StoneLit ? "La pierre s'illumine." : "La pierre s'éteint.");
        HeroNoise(Hero.Position, NoiseLevel.Light, "Fsss");
        return EndHeroTurn();
    }

    public List<Vector2I> StoneTargets()
    {
        var targets = new List<Vector2I> { Hero.Position };
        if (!Hero.StoneCarried)
        {
            return targets;
        }
        foreach (var direction in Grid.AllDirections)
        {
            for (var i = 1; i <= Hero.ThrowMaxRange; i++)
            {
                var cell = Hero.Position + direction * i;
                if (!InBounds(cell) || KnownObstacle(cell))
                {
                    break;
                }
                targets.Add(cell);
            }
        }
        return targets;
    }

    private bool KnownObstacle(Vector2I cell)
    {
        if (!Seen.Contains(cell))
        {
            return false;
        }
        if (IsWall(cell))
        {
            return true;
        }
        return IsDoor(cell) && DoorRemembered(cell, out var open) && !open;
    }

    public bool ThrowStoneAt(Vector2I target)
    {
        var delta = target - Hero.Position;
        var distance = Grid.Chebyshev(target, Hero.Position);
        if (distance > 0 && (delta.X != 0 && delta.Y != 0 && Math.Abs(delta.X) != Math.Abs(delta.Y)))
        {
            return Refuse("La pierre se lance en ligne droite ou en diagonale.");
        }
        var direction = distance == 0 ? Vector2I.Zero : new Vector2I(Math.Sign(delta.X), Math.Sign(delta.Y));
        return ThrowStone(direction, distance);
    }

    public bool ThrowStone(Vector2I direction, int distance)
    {
        if (!Hero.StoneCarried)
        {
            return Refuse("Vous n'avez pas la pierre de lumière.");
        }
        distance = Math.Clamp(distance, 0, Hero.ThrowMaxRange);
        var landing = Hero.Position;
        for (var i = 1; i <= distance; i++)
        {
            var cell = Hero.Position + direction * i;
            if (BlocksSight(cell) || MonsterAt(cell) != null || !CornerFree(cell - direction, direction))
            {
                break;
            }
            landing = cell;
        }
        Hero.StoneCarried = false;
        Hero.StonePosition = landing;
        Events.Add(new GameEvent(GameEventKind.StoneThrown, From: Hero.Position, To: landing));
        var shortBy = distance - Grid.Chebyshev(landing, Hero.Position);
        Message(distance == 0 ? "Vous posez la pierre à vos pieds." : shortBy > 0 ? "La pierre heurte un obstacle et retombe avant la case visée." : "Vous lancez la pierre de lumière.");
        HeroNoise(landing, NoiseLevel.Light, "Tic");
        return EndHeroTurn();
    }

    public bool RecallStone()
    {
        if (Hero.StoneCarried)
        {
            return Refuse("Vous avez déjà la pierre.");
        }
        var from = Hero.StonePosition;
        Hero.StoneCarried = true;
        Events.Add(new GameEvent(GameEventKind.StoneThrown, From: from, To: Hero.Position));
        Message(Grid.Chebyshev(from, Hero.Position) <= 1 ? "Vous ramassez la pierre de lumière." : "La pierre revient dans votre main.");
        HeroNoise(Hero.Position, NoiseLevel.Light, "Vziou");
        return EndHeroTurn();
    }

    public bool PickUpArrows() => PickUpArrows(Hero.Position);

    public bool CanReachArrows(Vector2I cell) =>
        ArrowsOnGround.ContainsKey(cell)
        && Grid.Chebyshev(cell, Hero.Position) <= 1
        && (cell == Hero.Position || CornerFree(Hero.Position, cell - Hero.Position));

    public bool PickUpArrows(Vector2I cell)
    {
        if (!CanReachArrows(cell))
        {
            return Refuse("Rien à ramasser ici.");
        }
        var count = ArrowsOnGround[cell];
        Hero.Arrows += count;
        ArrowsOnGround.Remove(cell);
        Message(count > 1 ? $"Vous ramassez {count} flèches." : "Vous ramassez une flèche.");
        HeroNoise(Hero.Position, NoiseLevel.Light, "Frr");
        return EndHeroTurn();
    }

    public List<Vector2I> InteractionTargets()
    {
        var targets = new List<Vector2I>();
        foreach (var cell in ArrowsOnGround.Keys.Where(CanReachArrows))
        {
            targets.Add(cell);
        }
        foreach (var direction in Grid.Orthogonal)
        {
            var cell = Hero.Position + direction;
            if (IsDoor(cell) && OpenDoors.Contains(cell) && DoorIsFree(cell) && !targets.Contains(cell))
            {
                targets.Add(cell);
            }
        }
        if (!Hero.StoneCarried && !targets.Contains(Hero.StonePosition))
        {
            targets.Add(Hero.StonePosition);
        }
        return targets;
    }

    public bool Interact(Vector2I cell)
    {
        if (!Hero.StoneCarried && cell == Hero.StonePosition)
        {
            return RecallStone();
        }
        if (CanReachArrows(cell))
        {
            return PickUpArrows(cell);
        }
        var direction = cell - Hero.Position;
        if (Grid.IsOrthogonal(direction) && Grid.Chebyshev(cell, Hero.Position) == 1 && IsDoor(cell) && OpenDoors.Contains(cell))
        {
            return TryCloseDoor(direction);
        }
        return Refuse("Rien à faire ici.");
    }

    public bool TryCloseDoor(Vector2I direction)
    {
        if (!Grid.IsOrthogonal(direction))
        {
            return Refuse("Choisissez une porte en face de vous.");
        }
        var cell = Hero.Position + direction;
        if (!IsDoor(cell) || !OpenDoors.Contains(cell))
        {
            return Refuse("Pas de porte ouverte de ce côté.");
        }
        if (!DoorIsFree(cell))
        {
            return Refuse("Quelque chose bloque la porte.");
        }
        SetDoor(cell, false);
        HeroNoise(cell, NoiseLevel.Light, "Clac");
        return EndHeroTurn();
    }

    public bool Wait()
    {
        Message("Vous attendez.");
        return EndHeroTurn();
    }

    private void MeleeAttack(Monster monster)
    {
        Events.Add(new GameEvent(GameEventKind.HeroAttacked, From: Hero.Position, To: monster.Position));
        Damage(monster, 1);
        HeroNoise(monster.Position, NoiseLevel.Loud, "Clang");
    }

    private void Damage(Monster monster, int amount)
    {
        monster.Hp -= amount;
        Events.Add(new GameEvent(GameEventKind.MonsterHurt, monster.Id, To: monster.Position));
        if (!monster.IsDead)
        {
            return;
        }
        Events.Add(new GameEvent(GameEventKind.MonsterDied, monster.Id, To: monster.Position));
        Message($"{monster.Type.DisplayName} est vaincu.");
        if (monster.ArrowsStuck > 0)
        {
            AddArrows(monster.Position, monster.ArrowsStuck);
            monster.ArrowsStuck = 0;
        }
    }

    private void AddArrows(Vector2I cell, int count)
    {
        ArrowsOnGround[cell] = ArrowsOnGround.GetValueOrDefault(cell) + count;
    }

    private void HeroNoise(Vector2I position, NoiseLevel level, string sound)
    {
        _heroNoises.Add(new Noise(position, level, sound));
    }

    private bool Refuse(string message)
    {
        Message(message);
        return false;
    }

    public void Message(string text) => Events.Add(new GameEvent(GameEventKind.Message, Text: text));

    private bool EndHeroTurn()
    {
        Turn++;
        if (!Won)
        {
            foreach (var monster in Monsters.Where(m => !m.IsDead).ToList())
            {
                MonsterBrain.Act(this, monster, _heroNoises.Concat(LightLures()).ToList());
                if (Hero.IsDead)
                {
                    break;
                }
            }
        }
        _heroNoises.Clear();
        UpdateVisibility();
        if (Hero.IsDead)
        {
            Events.Add(new GameEvent(GameEventKind.HeroDied));
        }
        return true;
    }

    public void MonsterNoise(Vector2I position, NoiseLevel level, string sound)
    {
        if (Grid.Chebyshev(position, Hero.Position) <= NoiseRules.Range(level) && !Lit.Contains(position))
        {
            Events.Add(new GameEvent(GameEventKind.Sound, To: position, Text: sound));
        }
    }

    public void HurtHero(Monster attacker)
    {
        Hero.Hp = Math.Max(0, Hero.Hp - attacker.Type.Damage);
        Events.Add(new GameEvent(GameEventKind.MonsterAttacked, attacker.Id, attacker.Position, Hero.Position));
        Events.Add(new GameEvent(GameEventKind.HeroHurt, To: Hero.Position));
        Message($"{attacker.Type.DisplayName} vous frappe !");
    }

    public void MoveMonster(Monster monster, Vector2I to)
    {
        var from = monster.Position;
        monster.Position = to;
        if (IsDoor(from) && !IsDoor(to) && monster.State == MonsterState.Normal)
        {
            monster.DoorToClose = from;
        }
        Events.Add(new GameEvent(GameEventKind.MonsterMoved, monster.Id, from, to));
        MonsterNoise(to, NoiseLevel.Light, "Tap");
    }

    public void OpenDoorByMonster(Vector2I cell)
    {
        SetDoor(cell, true);
        MonsterNoise(cell, NoiseLevel.Light, "Grinc");
    }

    public void CloseDoorByMonster(Vector2I cell)
    {
        SetDoor(cell, false);
        MonsterNoise(cell, NoiseLevel.Light, "Clac");
    }

    public IEnumerable<Vector2I> DoorCells(Vector2I cell)
    {
        yield return cell;
        if (Level.DoorPartners.TryGetValue(cell, out var partner))
        {
            yield return partner;
        }
    }

    public bool DoorIsFree(Vector2I cell) => DoorCells(cell).All(c => !IsOccupied(c) && !ArrowsOnGround.ContainsKey(c));

    private void SetDoor(Vector2I cell, bool open)
    {
        foreach (var c in DoorCells(cell))
        {
            if (open)
            {
                OpenDoors.Add(c);
            }
            else
            {
                OpenDoors.Remove(c);
            }
        }
        Events.Add(new GameEvent(GameEventKind.DoorChanged, To: cell));
    }

    public IEnumerable<Noise> LightLures()
    {
        if (!Hero.StoneCarried)
        {
            yield return new Noise(Hero.StonePosition, NoiseLevel.Loud, "Lumière", true);
        }
        else if (Hero.StoneLit)
        {
            yield return new Noise(Hero.Position, NoiseLevel.Loud, "Lumière", true);
        }
    }

    public bool DoorRemembered(Vector2I cell, out bool open)
    {
        var lit = DoorCells(cell).Any(Lit.Contains);
        if (lit)
        {
            open = OpenDoors.Contains(cell);
            return true;
        }
        return RememberedDoors.TryGetValue(cell, out open);
    }

    public Vector2I RandomWalkableCell() => _walkableCells[Rng.Next(_walkableCells.Count)];

    public List<Vector2I> FindPath(Monster monster, Vector2I target)
    {
        var blocked = new List<Vector2I>();
        foreach (var other in Monsters)
        {
            if (other != monster && !other.IsDead && other.Position != target && !_pathfinder.IsPointSolid(other.Position))
            {
                _pathfinder.SetPointSolid(other.Position, true);
                blocked.Add(other.Position);
            }
        }
        var path = new List<Vector2I>();
        if (InBounds(target) && !_pathfinder.IsPointSolid(target))
        {
            foreach (var cell in _pathfinder.GetIdPath(monster.Position, target))
            {
                path.Add(cell);
            }
        }
        foreach (var cell in blocked)
        {
            _pathfinder.SetPointSolid(cell, false);
        }
        return path;
    }
}
