using Godot;
using System;
using NeonWarfare.Scripts.KludgeBox.Core;

public partial class DeathOverlay : Control
{
    [Export] [NotNull] public Control GrayscaleShader { get; private set; }
    [Export] [NotNull] public Control Vignette { get; private set; }

    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }

    public void SetStrength(float strength)
    {
        Vignette.Modulate = Vignette.Modulate with { A = strength };
        var material = GrayscaleShader.Material as ShaderMaterial;
        material!.SetShaderParameter("strength", strength);
    }
}
