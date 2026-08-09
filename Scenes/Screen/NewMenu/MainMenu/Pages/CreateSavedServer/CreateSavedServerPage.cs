using System;
using System.Linq;
using Godot;
using KludgeBox.DI.Requests.ChildInjection;

namespace NeonWarfare.Scenes.Screen.NewMenu.MainMenu.Pages.CreateSavedServer;

public partial class CreateSavedServerPage : MainMenuPage
{
    [Child] public SpinBox PortSpinBox { get; private set; }
    [Child] public LineEdit SearchLineEdit { get; private set; }
    [Child] public VBoxContainer SavesListContainer { get; private set; }
    [Child] public CheckButton IsDedicatedCheckButton { get; private set; }
    [Child] public Button CreateServerButton { get; private set; }
    [Child] public Button CancelButton { get; private set; }

    private string _selectedSaveFileName;

    public override void _Ready()
    {
        Di.Process(this);

        CreateServerButton.Pressed += OnCreate;
        CancelButton.Pressed += () => GoBack();
        SearchLineEdit.TextChanged += OnSearchChanged;

        PortSpinBox.Value = Consts.DefaultPort;
        IsDedicatedCheckButton.ButtonPressed = false;
        SearchLineEdit.Text = String.Empty;

        _selectedSaveFileName = Services.SaveLoad.GetAllSaveFiles().FirstOrDefault().FileName ?? String.Empty;
        PopulateSavesList(SearchLineEdit.Text);
    }

    private void PopulateSavesList(string filter)
    {
        foreach (var child in SavesListContainer.GetChildren())
        {
            child.QueueFree();
        }

        var saves = Services.SaveLoad.GetAllSaveFiles();
        string filterLower = (filter ?? String.Empty).Trim().ToLowerInvariant();

        foreach (var save in saves)
        {
            if (!String.IsNullOrEmpty(filterLower)
                && !save.FileName.ToLowerInvariant().Contains(filterLower))
            {
                continue;
            }

            var button = new Button();
            button.Text = $"{save.FileName} ({DateTimeOffset.FromUnixTimeSeconds((long)save.ModifiedTime).ToLocalTime():yyyy-MM-dd HH:mm})";
            button.Pressed += () =>
            {
                _selectedSaveFileName = save.FileName;
            };
            if (save.FileName == _selectedSaveFileName)
            {
                button.ButtonPressed = true;
            }
            SavesListContainer.AddChild(button);
        }
    }

    private void OnSearchChanged(string newText)
    {
        PopulateSavesList(newText);
    }

    private void OnCreate()
    {
        if (String.IsNullOrWhiteSpace(_selectedSaveFileName))
        {
            GoNext(PagesProvider.PrepareMessagePage(Tr("CREATE_SAVED_SERVER_MENU__NO_SAVE_SELECTED_ERROR")));
            return;
        }

        int port = (int) PortSpinBox.Value;
        bool isDedicated = IsDedicatedCheckButton.ButtonPressed;
        TryStartGame(() => Services.MainScene.HostMultiplayerGameAsClient(_selectedSaveFileName, port, isDedicated));
    }
}
