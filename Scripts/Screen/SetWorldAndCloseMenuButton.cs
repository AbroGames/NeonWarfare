using Godot;
using System;

public partial class SetWorldAndCloseMenuButton : Godot.Button
{
    [Export] private PackedScene _newWorldScene;
    [Export] private PackedScene _hud;
	
    public override void _Ready()
    {
        Pressed += OnClick;
    }

    private void OnClick()
    {
        World world = _newWorldScene.Instantiate<World>();
        Hud hud = _hud.Instantiate<Hud>();
        hud.World = world;
        
        Root.Instance.Game.WorldContainer.ChangeStoredNode(world);
        Root.Instance.Game.HudContainer.ChangeStoredNode(hud);
        Root.Instance.Game.MenuContainer.ClearStoredNode();
    }

}