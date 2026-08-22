using Godot;
using KludgeBox.DI.Requests.ChildInjection;

namespace NeonWarfare.Scenes.Screen.Menu.MainMenu.Pages.Message;

public partial class MessagePage : MainMenuPage
{
    [Child] public Label MessageLabel { get; private set; }
    [Child] public Button OkButton { get; private set; }

    private string _message;

    public override void _Ready()
    {
        Di.Process(this);

        MessageLabel.Text = _message;
        OkButton.Pressed += GoBack;
    }

    /// <summary>Called by <see cref="PagesProvider.PrepareMessagePage"/>
    /// before the page is added to the tree.</summary>
    public void Configure(string message)
    {
        _message = message;
    }
}