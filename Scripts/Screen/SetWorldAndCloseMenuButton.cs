using Godot;

namespace AbroDraft.Scripts.Screen;

public partial class SetWorldAndCloseMenuButton : Godot.Button
{
    [Export] private PackedScene _newWorldMainScene;
	
    public override void _Ready()
    {
        Pressed += OnClick;
    }

    private void OnClick()
    {
        Scenes.Root.Root.Instance.Game.MainSceneContainer.ChangeStoredNode(_newWorldMainScene.Instantiate());
    }

}