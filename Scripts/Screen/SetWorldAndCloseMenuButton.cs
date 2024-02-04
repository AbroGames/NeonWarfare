using Godot;
using System;

public partial class SetWorldAndCloseMenuButton : Godot.Button
{
    [Export] private PackedScene _newWorldMainScene;
	
    public override void _Ready()
    {
        Pressed += OnClick;
    }

    private void OnClick()
    {
        Root.Instance.Game.MainSceneContainer.ChangeStoredNode(_newWorldMainScene.Instantiate());
    }

}