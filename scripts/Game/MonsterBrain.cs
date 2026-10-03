using System.Collections.Generic;
using System.Linq;
using DungeonHunter.Core;
using Godot;

namespace DungeonHunter.Game;

public static class MonsterBrain
{
    private const int RestRadius = 5;
    private const int LingerRadius = 5;

    public static void Act(World world, Monster monster, IReadOnlyList<Noise> heroNoises)
    {
        if (TryCloseDoorBehind(world, monster))
        {
            return;
        }
        if (world.MonsterSeesHero(monster))
        {
            monster.State = MonsterState.Alert;
            monster.LastSeenHero = world.Hero.Position;
            monster.Lingering = false;
            Chase(world, monster);
            return;
        }

        if (monster.State == MonsterState.Alert)
        {
            monster.State = MonsterState.Search;
            monster.Target = monster.LastSeenHero;
            monster.Lingering = false;
        }

        if (monster.State != MonsterState.Search)
        {
            var heard = PickNoise(world, monster, heroNoises);
            if (heard.HasValue)
            {
                if (monster.State == MonsterState.Hunt)
                {
                    monster.ReturnToHunt = true;
                }
                else if (monster.State != MonsterState.Investigate)
                {
                    monster.ReturnToHunt = false;
                }
                monster.State = MonsterState.Investigate;
                monster.Target = heard.Value.Position;
                monster.Lingering = false;
            }
        }

        switch (monster.State)
        {
            case MonsterState.Investigate:
            case MonsterState.Search:
                Investigate(world, monster);
                break;
            case MonsterState.Hunt:
                Wander(world, monster);
                break;
            default:
                ActNormally(world, monster);
                break;
        }
    }

    private static bool TryCloseDoorBehind(World world, Monster monster)
    {
        if (monster.DoorToClose is not { } door)
        {
            return false;
        }
        monster.DoorToClose = null;
        if (monster.State != MonsterState.Normal || world.MonsterSeesHero(monster) || !world.OpenDoors.Contains(door) || !world.DoorIsFree(door))
        {
            return false;
        }
        if (!world.DoorCells(door).Any(c => Grid.IsOrthogonallyAdjacent(c, monster.Position)))
        {
            return false;
        }
        world.CloseDoorByMonster(door);
        return true;
    }

    private static Noise? PickNoise(World world, Monster monster, IReadOnlyList<Noise> noises)
    {
        var audible = noises
            .Where(n => n.Level != NoiseLevel.None && Grid.Chebyshev(n.Position, monster.Position) <= NoiseRules.Range(n.Level))
            .Where(n => !n.IsLight || world.HasLineOfSight(monster.Position, n.Position))
            .ToList();
        if (audible.Count == 0)
        {
            return null;
        }
        var loudest = audible.Max(n => n.Level);
        var candidates = audible.Where(n => n.Level == loudest).ToList();
        return candidates[world.Rng.Next(candidates.Count)];
    }

    private static void Chase(World world, Monster monster)
    {
        var hero = world.Hero.Position;
        if (Grid.Chebyshev(monster.Position, hero) == 1 && world.CornerFree(monster.Position, hero - monster.Position))
        {
            world.HurtHero(monster);
            return;
        }
        if (Grid.Chebyshev(monster.Position, hero) == 1)
        {
            var options = new[] { new Vector2I(monster.Position.X, hero.Y), new Vector2I(hero.X, monster.Position.Y) };
            foreach (var option in options.OrderBy(_ => world.Rng.Next()))
            {
                if (TryStepInto(world, monster, option))
                {
                    return;
                }
            }
            return;
        }
        StepToward(world, monster, hero);
    }

    private static void Investigate(World world, Monster monster)
    {
        if (!monster.Lingering)
        {
            var target = monster.Target ?? monster.Position;
            if (monster.Position == target || !StepToward(world, monster, target))
            {
                monster.Lingering = true;
                monster.LingerTurns = world.Rng.Next(3, 7);
                monster.LingerCenter = monster.Position;
            }
            return;
        }

        monster.LingerTurns--;
        if (world.Rng.NextDouble() < 0.6)
        {
            RandomStep(world, monster, monster.LingerCenter, LingerRadius);
        }
        if (monster.LingerTurns > 0)
        {
            return;
        }

        monster.Lingering = false;
        if (monster.State == MonsterState.Search || monster.ReturnToHunt)
        {
            monster.State = MonsterState.Hunt;
            monster.Target = null;
            return;
        }
        monster.State = MonsterState.Normal;
        monster.Target = monster.Behavior == MonsterBehavior.Explorer ? null : monster.Home;
    }

    private static void ActNormally(World world, Monster monster)
    {
        switch (monster.Behavior)
        {
            case MonsterBehavior.Guard:
                if (monster.Position != monster.Home)
                {
                    StepToward(world, monster, monster.Home);
                }
                break;
            case MonsterBehavior.Rest:
                if (Grid.Chebyshev(monster.Position, monster.Home) > RestRadius)
                {
                    StepToward(world, monster, monster.Home);
                }
                else if (world.Rng.NextDouble() < 0.4)
                {
                    RandomStep(world, monster, monster.Home, RestRadius);
                }
                break;
            default:
                Wander(world, monster);
                break;
        }
    }

    private static void Wander(World world, Monster monster)
    {
        if (monster.Target == null || monster.Target == monster.Position)
        {
            monster.Target = world.RandomWalkableCell();
        }
        if (!StepToward(world, monster, monster.Target.Value))
        {
            monster.Target = null;
        }
    }

    private static bool StepToward(World world, Monster monster, Vector2I target)
    {
        var path = world.FindPath(monster, target);
        if (path.Count < 2)
        {
            return false;
        }
        TryStepInto(world, monster, path[1]);
        return true;
    }

    private static bool TryStepInto(World world, Monster monster, Vector2I cell)
    {
        var direction = cell - monster.Position;
        if (world.IsWall(cell) || world.IsOccupied(cell))
        {
            return false;
        }
        if (world.IsClosedDoor(cell))
        {
            world.OpenDoorByMonster(cell);
            return true;
        }
        if (!world.CornerFree(monster.Position, direction))
        {
            return false;
        }
        world.MoveMonster(monster, cell);
        return true;
    }

    private static void RandomStep(World world, Monster monster, Vector2I center, int radius)
    {
        var options = Grid.AllDirections
            .Where(d => world.CanStep(monster.Position, d) && Grid.Chebyshev(monster.Position + d, center) <= radius)
            .ToList();
        if (options.Count > 0)
        {
            world.MoveMonster(monster, monster.Position + options[world.Rng.Next(options.Count)]);
        }
    }
}
