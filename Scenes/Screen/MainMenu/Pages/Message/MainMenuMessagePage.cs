using Godot;
using KludgeBox.DI.Requests.ChildInjection;

namespace NeonWarfare.Scenes.Screen.MainMenu.Pages.Message;

public partial class MainMenuMessagePage : MainMenuPage
{
    
    [Child] public Label MessageLabel { get; private set; }
    [Child] public Button OkButton { get; private set; }
    
    public override void _Ready()
    {
        Di.Process(this);

        OkButton.Pressed += () => ChangeMenuPage(PackedScenes.Main);
    }
}