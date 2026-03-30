using Godot;
using System;
using KludgeBox.DI.Requests.ChildInjection;
using NeonWarfare.Scenes.Screen.NewMenu.MainMenu;

public partial class SingleplayerPage : MainMenuPage
{
    [Child] public Button StartButton { get; private set; }
    [Child] public Button CancelButton { get; private set; }
    [Child] public LineEdit SaveNameLineEdit { get; private set; }
    [Child] public VBoxContainer SavesListContainer { get; private set; }
    
    public override void _Ready()
    {
        Di.Process(this);
        
        StartButton.Pressed += OnStart;
        CancelButton.Pressed += OnCancel;

        SaveNameLineEdit.Text = Services.GameSettings.Settings.LastSingleplayerSaveName ?? "";
        PopulateSavesList();
    }

    private void PopulateSavesList()
    {
        var saves = Services.SaveLoad.GetAllSaveFiles();
        foreach (var save in saves)
        {
            var button = new Button();
            button.Text = save;
            button.Pressed += () => SaveNameLineEdit.Text = save;
            SavesListContainer.AddChild(button);
        }
    }

    private void OnStart()
    {
        string saveFileName = !String.IsNullOrWhiteSpace(SaveNameLineEdit.Text) ? SaveNameLineEdit.Text : null;
        Services.GameSettings.PreserveSingleplayerGame(saveFileName);
        Services.MainScene.StartSingleplayerGame(saveFileName);
    }
    
    private void OnCancel()
    {
        GoBack();
    }
}


