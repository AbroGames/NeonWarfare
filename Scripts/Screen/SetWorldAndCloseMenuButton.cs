using Godot;
using System;

public partial class SetWorldAndCloseMenuButton : Godot.Button
{
    [Export] private PackedScene _newWorldScene;
	
    public override void _Ready()
    {
        Pressed += OnClick;
    }

    private void OnClick()
    {
        var instatinatedNode = _newWorldScene.Instantiate();
        var world = instatinatedNode as Node2D;
        Root.Instance.Game.WorldContainer.ChangeStoredNode(world);
        Root.Instance.Game.MenuContainer.ClearStoredNode();
    }

}