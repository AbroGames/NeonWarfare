using Godot;
using KludgeBox.DI.Requests;
using KludgeBox.DI.Requests.ChildInjection;
using NeonWarfare.Scenes.NeonTemp.UI.Menu.PagesSystem;

namespace NeonWarfare.Scenes.NeonTemp.UI.Menu.MainMenu;

public partial class MainMenu : Node
{
    [Child(By.Type)] public PageContainer PageContainer { get; private set; }
    [Child(By.Type)] public PagesScenes PagesScenes { get; private set; }

    public override void _Ready()
    {
        Di.Process(this);
        PageContainer.SetRootPage(PagesScenes.MainPage.Instantiate<MainMenuPage>().WithAvailablePages(PagesScenes));
    }
}