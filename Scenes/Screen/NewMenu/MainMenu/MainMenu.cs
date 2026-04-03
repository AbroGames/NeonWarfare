using Godot;
using KludgeBox.DI.Requests;
using KludgeBox.DI.Requests.ChildInjection;
using NeonWarfare.Scenes.Screen.NewMenu.PagesSystem;

namespace NeonWarfare.Scenes.Screen.NewMenu.MainMenu;

public partial class MainMenu : Node
{
    public IPage CurrentPage => _pageContainer.CurrentPage;
    [Child(By.Type)] private PageContainer _pageContainer { get; set; }
    [Child(By.Type)] public PagesProvider PagesProvider { get; private set; }

    public override void _Ready()
    {
        Di.Process(this);
        _pageContainer.SetRootPage(PagesProvider.MainPageScene.Instantiate<MainMenuPage>().WithAvailablePages(PagesProvider));
    }
    
    public void PushPage(IPage page) => _pageContainer.PushPage(page);
    public IPage PopPage() => _pageContainer.PopPage();
}