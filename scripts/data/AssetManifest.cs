using Godot;
using Godot.Collections;

namespace GlassesBar;

[GlobalClass]
public partial class AssetManifest : Resource
{
    [Export] public Array<AssetEntry> Entries { get; set; } = new();

    public AssetEntry? Find(StringName id)
    {
        foreach (var entry in Entries)
        {
            if (entry.Id == id)
                return entry;
        }

        return null;
    }
}
