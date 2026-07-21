using System;
using Godot;
using GlassesBar.Domain;

namespace GlassesBar;

public partial class MyopiaEffectController : Node
{
    [Signal] public delegate void MyopiaChangedEventHandler(float degrees, float blurRadius);

    [Export(PropertyHint.Range, "0,1000,5")] public float InitialMyopiaDegrees { get; set; } = 50f;

    public float MyopiaDegrees { get; private set; }
    public float BlurRadius { get; private set; }

    private ShaderMaterial _material = null!;

    public override void _Ready()
    {
        var blur = GetNode<ColorRect>("../RealityEffects/RealityBlur");
        _material = (ShaderMaterial)blur.Material;
        GameSession.Instance.DayChanged += OnDayChanged;
        SetMyopiaDegrees(MyopiaProgression.DegreesForDay(GameSession.Instance.CurrentDay), false);
    }

    public void SetMyopiaDegrees(float degrees, bool announce = true)
    {
        MyopiaDegrees = Math.Clamp(degrees, 0f, 1000f);
        BlurRadius = DegreesToBlurRadius(MyopiaDegrees);
        _material.SetShaderParameter("blur_radius", BlurRadius);
        _material.SetShaderParameter("myopia_strength", Math.Clamp(MyopiaDegrees / 500f, 0f, 1f));
        EmitSignal(SignalName.MyopiaChanged, MyopiaDegrees, BlurRadius);

        if (announce)
            GameSession.Instance.EmitSignal(GameSession.SignalName.StatusMessage,
                $"现实世界近视已调为 {MyopiaDegrees:0} 度。摘镜时生效。");
    }

    public static float DegreesToBlurRadius(float degrees)
    {
        if (degrees <= 0f)
            return 0f;
        return Math.Clamp(0.65f + degrees / 38f, 0f, 6f);
    }

    private void OnDayChanged(int day) => SetMyopiaDegrees(MyopiaProgression.DegreesForDay(day), false);
}
