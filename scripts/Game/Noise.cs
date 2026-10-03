using Godot;

namespace DungeonHunter.Game;

public enum NoiseLevel
{
    None,
    Light,
    Loud,
    Huge,
}

public readonly record struct Noise(Vector2I Position, NoiseLevel Level, string Sound, bool IsLight = false);

public static class NoiseRules
{
    public static int Range(NoiseLevel level) => level switch
    {
        NoiseLevel.Light => 3,
        NoiseLevel.Loud => 10,
        NoiseLevel.Huge => int.MaxValue,
        _ => -1,
    };

    public static string Label(NoiseLevel level) => level switch
    {
        NoiseLevel.Light => "léger",
        NoiseLevel.Loud => "fort",
        NoiseLevel.Huge => "énorme",
        _ => "aucun",
    };
}
