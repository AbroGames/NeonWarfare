using System;
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

        _selectedSaveName = ""; //TODO: А почему не просто самый последний по времени сейв? Они отсортированы по времени уже. Было так: Services.GameSettings.GetSettings().LastGame.SaveName ?? String.Empty;
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
            button.Text = save.FileName; //TODO: Отображать и время изменения файла
            button.Pressed += () => SaveNameLineEdit.Text = save.FileName;
            SavesListContainer.AddChild(button);
        }
    }

    private void OnSwitchingTabs(long tabId)
    {
        // NOTE: Пока что мы просто меняем видимость поля ввода имени сохранения. В будущем, если захотим сразу давать имена новым сохранениям, вернем поле на постоянку.
        if (tabId == NewGameTabId)
        {
            SaveNameContainer.Hide();
            _selectedSaveName = SaveNameLineEdit.Text;
            SaveNameLineEdit.Text  = String.Empty;
        }
        else if (tabId == LoadGameTabId)
        {
            SaveNameContainer.Show();
            SaveNameLineEdit.Text = _selectedSaveName;
        }
    }

    private void OnStart()
    {
        string saveFileName = !String.IsNullOrWhiteSpace(SaveNameLineEdit.Text) ? SaveNameLineEdit.Text : null;
        // TODO: СЖИЖЕНЫИ
        //Services.GameSettings.SetLastGame(new GameSettings.ResumableGame(GameSettings.ResumableGame.ResumableType.RunSingleplayer, saveFileName, null?, null?, null?));
        //Services.MainScene.StartSingleplayerGame(saveFileName);
    }
    
    private void OnCancel()
    {
        GoBack();
    }
}