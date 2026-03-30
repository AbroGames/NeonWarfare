using Godot;
using System;
using KludgeBox.DI.Requests.ChildInjection;
using NeonWarfare.Scenes.Screen.NewMenu.MainMenu;

public partial class MessagePage : MainMenuPage
{
    [Child] public Label MessageLabel { get; private set; }
    [Child] public Button OkButton { get; private set; }
    
    public override void _Ready()
    {
        Di.Process(this);

        OkButton.Pressed += GoBack;
    }
}
