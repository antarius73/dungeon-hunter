using System.Collections.Generic;
using Godot;

namespace DungeonHunter.Core;

public static class TileAtlas
{
    private const string SheetPath = "res://assets/tileset/dungeon_tileset.png";
    private static Texture2D _sheet;
    private static readonly Dictionary<string, Texture2D> Textures = new();
    private static readonly Dictionary<string, SpriteFrames> Frames = new();
    private static readonly Dictionary<Rect2I, int> OpaqueTops = new();
    private static Image _image;

    public static Texture2D Sheet => _sheet ??= GD.Load<Texture2D>(SheetPath);

    public static int OpaqueTop(Texture2D texture)
    {
        if (texture is not AtlasTexture atlas)
        {
            return 0;
        }
        var region = (Rect2I)atlas.Region;
        if (OpaqueTops.TryGetValue(region, out var top))
        {
            return top;
        }
        if (_image == null)
        {
            _image = Sheet.GetImage();
            if (_image.IsCompressed())
            {
                _image.Decompress();
            }
        }
        top = _image.GetRegion(region).GetUsedRect().Position.Y;
        OpaqueTops[region] = top;
        return top;
    }

    public static bool Has(string name) => TileRegions.All.ContainsKey(name);

    public static Rect2I Region(string name) => TileRegions.All[name];

    public static Texture2D Get(string name)
    {
        if (Textures.TryGetValue(name, out var texture))
        {
            return texture;
        }
        texture = new AtlasTexture { Atlas = Sheet, Region = Region(name) };
        Textures[name] = texture;
        return texture;
    }

    public static SpriteFrames BuildFrames(string baseName)
    {
        if (Frames.TryGetValue(baseName, out var cached))
        {
            return cached;
        }
        var frames = new SpriteFrames();
        frames.RemoveAnimation("default");
        AddAnimation(frames, "idle", $"{baseName}_idle_anim", true);
        AddAnimation(frames, "run", $"{baseName}_run_anim", true);
        AddAnimation(frames, "hit", $"{baseName}_hit_anim", false);
        if (!frames.HasAnimation("idle"))
        {
            AddAnimation(frames, "idle", $"{baseName}_anim", true);
        }
        if (!frames.HasAnimation("run"))
        {
            AddAnimation(frames, "run", $"{baseName}_anim", true);
        }
        if (!frames.HasAnimation("idle"))
        {
            AddAnimation(frames, "idle", baseName, true);
        }
        Frames[baseName] = frames;
        return frames;
    }

    private static void AddAnimation(SpriteFrames frames, string animation, string prefix, bool loop)
    {
        var names = new List<string>();
        for (var i = 0; Has($"{prefix}_f{i}"); i++)
        {
            names.Add($"{prefix}_f{i}");
        }
        if (names.Count == 0 && Has(prefix))
        {
            names.Add(prefix);
        }
        if (names.Count == 0)
        {
            return;
        }
        frames.AddAnimation(animation);
        frames.SetAnimationLoopMode(animation, loop ? SpriteFrames.LoopMode.Linear : SpriteFrames.LoopMode.None);
        frames.SetAnimationSpeed(animation, 8);
        foreach (var name in names)
        {
            frames.AddFrame(animation, Get(name));
        }
    }
}
