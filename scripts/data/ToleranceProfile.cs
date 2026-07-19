using Godot;

namespace GlassesBar;

[GlobalClass]
public partial class ToleranceProfile : Resource
{
    [Export(PropertyHint.Range, "0,1,0.01")]
    public double AmountToleranceRatio { get; set; } = 0.1;

    [Export] public bool EnableQuantityScoring { get; set; }
}

