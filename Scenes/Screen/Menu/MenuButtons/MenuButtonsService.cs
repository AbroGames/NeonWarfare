using System;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;
using KludgeBox.Net;

namespace NeoVector;

public static class MenuButtonsService
{
    public static void ChangeMenuFromButtonClick(PackedScene menuChangeTo)
    {
        Root.Instance.MainSceneContainer.GetCurrentStoredNode<MainMenuMainScene>().ChangeMenu(menuChangeTo);
    }
    public static void ShutDown()
    {
        Root.Instance.GetTree().Quit();
    }
}