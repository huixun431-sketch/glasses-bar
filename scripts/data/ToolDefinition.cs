using Godot;

namespace GlassesBar;

[GlobalClass]
public partial class ToolDefinition : Resource
{
    [Export] public StringName Id { get; set; } = new();
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export] public InteractionKind Interaction { get; set; } = InteractionKind.Operate;
    [Export] public bool RequiresContinuousInput { get; set; } = true;
}

