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
        References.Instance.WorldContainer.ChangeStoredNode(_newWorldScene.Instantiate() as Node2D);
        References.Instance.MenuContainer.ClearStoredNode();
    }

}