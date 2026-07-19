using Godot;

namespace GlassesBar;

[GlobalClass]
public partial class AssetEntry : Resource
{
    [Export] public StringName Id { get; set; } = new();
    [Export(PropertyHint.File, "*.glb")]
    public string ModelPath { get; set; } = string.Empty;
    [Export] public bool IsPlaceholder { get; set; } = true;
}

