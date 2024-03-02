using Godot;

namespace KludgeBox.Events.Global;

public partial class ToBattleButton : Button
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