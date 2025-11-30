using Godot;
using KludgeBox.DI.Requests.ChildInjection;
using NeonWarfare.Scenes.Screen.MainMenu.Pages;
using NodeContainer = NeonWarfare.Scenes.KludgeBox.NodeContainer;

namespace NeonWarfare.Scenes.Screen.MainMenu;

public partial class MainMenu : Node2D
{
    [Child] public NodeContainer BackgroundContainer { get; private set; }
    [Child] public NodeContainer MenuContainer { get; private set; }
    [Child] public MainMenuPackedScenes PackedScenes { get; private set; }
    
    public override void _Ready()
    {
        Di.Process(this);

        ChangeMenuPage(PackedScenes.Main);
    }
    
    public Node ChangeMenuPage(PackedScene newMenuPageScene)
    {
        MainMenuPage newMenuPage = newMenuPageScene.Instantiate<MainMenuPage>();
        newMenuPage.InitPreReady(ChangeMenuPage, PackedScenes);
        return MenuContainer.ChangeStoredNode(newMenuPage);
    }
}