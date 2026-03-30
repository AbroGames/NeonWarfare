using Godot;
using KludgeBox.DI.Requests.LoggerInjection;
using Serilog;

namespace NeonWarfare.Scenes.Screen.NewMenu.PagesSystem;

public partial class PageContainer : Control
{
    [Logger] private ILogger _log;
    
    public IPage RootPage { get; private set; }
    public IPage CurrentPage { get; private set; }

    public override void _Ready()
    {
        Di.Process(this);
    }

    public void SetRootPage(IPage page)
    {
        if (CurrentPage is not null)
        {
            _log.Error("Attempt to set root context more than once for {containerPath}", GetPath());
            return;
        }
        
        CurrentPage = page;
        RootPage = page;
        AddChild(page as Node);
        SetupPage(page);
    }

    public void PushPage(IPage nextPage)
    {
        var parentContext = CurrentPage;
        if (!nextPage.IsTop)
        {
            _log.Error("Attempt to push next context that contains child context at {containerPath}", GetPath());
            return;
        }
        
        for (var ctx = parentContext; ctx != null; ctx = ctx.Parent)
        {
            if (ctx == nextPage)
            {
                _log.Error("Loop detected: trying to push a context that already exists in the chain at {containerPath}", GetPath());
                return;
            }
        }
        
        if (CurrentPage is Node currentContextNode)
        {
            RemoveChild(currentContextNode);
            parentContext.PushChild(nextPage);
            parentContext.OnHidden(nextPage);
            nextPage.SetParent(parentContext);
            
            SetupPage(nextPage);
            AddChild(nextPage as Node);
            nextPage.OnShown(parentContext);
            CurrentPage = nextPage;
        }
        else
        {
            _log.Error("Current context somehow is not a Node at {containerPath}", GetPath());
        }
    }

    public IPage PopPage()
    {
        if (CurrentPage.IsRoot)
        {
            _log.Warning("Attempt to pop root context at {containerPath}", GetPath());
            return CurrentPage;
        }
        
        var parentContext = CurrentPage.Parent;
        var childContext = CurrentPage;
        
        RemoveChild(childContext as Node);
        childContext.OnHidden(parentContext);
        
        AddChild(parentContext as Node);
        parentContext.OnShown(childContext);
        childContext.Close();
        CurrentPage = parentContext;
        
        return parentContext;
    }

    private void SetupPage(IPage page)
    {
        page.Setup(() => PopPage(), PushPage);
    }
}