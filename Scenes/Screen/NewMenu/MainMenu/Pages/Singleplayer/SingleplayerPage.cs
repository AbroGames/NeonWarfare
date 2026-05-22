using System;
using System.Linq;
using Godot;
using KludgeBox.DI.Requests.ChildInjection;

namespace NeonWarfare.Scenes.Screen.NewMenu.MainMenu.Pages.Singleplayer;

public partial class SingleplayerPage : MainMenuPage
{
    [Child] public Button StartButton { get; private set; }
    [Child] public Button CancelButton { get; private set; }
    [Child] public LineEdit SaveNameLineEdit { get; private set; }
    [Child] public VBoxContainer SavesListContainer { get; private set; }
    [Child] public TabContainer TabContainer { get; private set; }
    [Child] public HBoxContainer SaveNameContainer { get; private set; }
    
    private string _selectedSaveName;

    private const int NewGameTabId = 0;
    private const int LoadGameTabId = 1;
    
    public override void _Ready()
    {
        Di.Process(this);
        
        StartButton.Pressed += OnStart;
        CancelButton.Pressed += OnCancel;
        TabContainer.TabChanged += OnSwitchingTabs;

        _selectedSaveName = Services.SaveLoad.GetAllSaveFiles().FirstOrDefault().FileName ?? String.Empty;
        SaveNameLineEdit.Text = _selectedSaveName;
        
        PopulateSavesList();
        if (!String.IsNullOrWhiteSpace(_selectedSaveName))
        {
            TabContainer.SetCurrentTab(LoadGameTabId);
        }
        else
        {
            TabContainer.SetCurrentTab(NewGameTabId);
        }

        TabContainer.DeselectEnabled = false;
    }

    private void PopulateSavesList()
    {
        var saves = Services.SaveLoad.GetAllSaveFiles();
        foreach (var save in saves)
        {
            var button = new Button();
            button.Text = $"{save.FileName} ({DateTimeOffset.FromUnixTimeSeconds((long)save.ModifiedTime).ToLocalTime():yyyy-MM-dd HH:mm})";
            button.Pressed += () => SaveNameLineEdit.Text = save.FileName;
            SavesListContainer.AddChild(button);
        }
    }

    private void OnSwitchingTabs(long tabId)
    {
        // Save name input stays visible for both new game and load game flows.
        SaveNameContainer.Show();
        if (tabId == LoadGameTabId && !String.IsNullOrWhiteSpace(_selectedSaveName))
        {
            SaveNameLineEdit.Text = _selectedSaveName;
        }
    }

    private void OnStart()
    {
        string saveFileName = String.IsNullOrWhiteSpace(SaveNameLineEdit.Text)
            ? Services.SaveLoad.GenNewSaveFileName()
            : SaveNameLineEdit.Text.Trim();
        Services.MainScene.StartSingleplayerGame(saveFileName);
    }
    
    private void OnCancel()
    {
        GoBack();
    }
}