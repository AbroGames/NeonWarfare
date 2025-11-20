using Godot;
using System;
using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.KludgeBox.Godot.Services;

public partial class AimLaser : Sprite2D
{
    public static bool GlobalLaserVisibility { get; set; } = true;
    
    public static void ToggleGlobalLaserVisibility() => GlobalLaserVisibility = !GlobalLaserVisibility;
    
    private bool _laserVisibility;

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed(Keys.Flashlight))
        {
            ToggleGlobalLaserVisibility();
            Audio2D.PlayUiSound(Sfx.Flashlight);
        }
        
        if (_laserVisibility != GlobalLaserVisibility)
        {
            _laserVisibility = GlobalLaserVisibility;
            Visible = _laserVisibility;
        }
    }
}
