using Godot;

namespace AbroDraft.Scripts.Screen;

public partial class ToBattleButton : Godot.Button
{
    [Export] private PackedScene _battleMainScene;
	
    public override void _Ready()
    {
        Pressed += OnClick;
    }

    private void OnClick()
    {
        Scenes.Root.Root.Instance.Game.MainSceneContainer.ChangeStoredNode(_battleMainScene.Instantiate());
    }

}