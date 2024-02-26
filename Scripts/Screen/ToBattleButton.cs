using Godot;

namespace AbroDraft;

public partial class ToBattleButton : Godot.Button
{
    [Export] private PackedScene _battleMainScene;
	
    public override void _Ready()
    {
        Pressed += OnClick;
    }

    private void OnClick()
    {
        Root.Instance.Game.MainSceneContainer.ChangeStoredNode(_battleMainScene.Instantiate());
    }

}