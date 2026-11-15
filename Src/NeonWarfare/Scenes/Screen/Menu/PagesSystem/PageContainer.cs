using Godot;
using KludgeBox.Logging;
using Serilog;

namespace NeonWarfare.Scenes.Screen.Menu.PagesSystem;

public partial class PageContainer : Control
{
    private readonly ILogger _log = LogFactory.GetForStatic<PageContainer>();
    
    public IPage RootPage { get; private set; }
    public IPage CurrentPage { get; private set; }

    public void SetRootPage(IPage page)
    {
        if (CurrentPage is not null)
        {
            _log.Error("Attempt to set root context more than once for {containerPath}", GetPath());
            return;
        }
        
        CurrentPage = page;
        RootPage = page;
        SetupPage(page);
        AddChild(page as Node);
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
                _log.Error(
                    "Loop detected: trying to push a context that already exists in the chain at {containerPath}",
                    GetPath());
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

    public override void _Notification(int id)
    {
        if (id == NotificationPredelete) FreeHiddenPages();
    }

    /// <summary>
    /// Only <see cref="CurrentPage"/> is a child of the container: <see cref="PushPage"/> takes the pages under it
    /// out of the tree, so freeing the container does not reach them, and they would leak with every
    /// MainMenu → Game transition and on quit.
    /// </summary>
    private void FreeHiddenPages()
    {
        IPage page = CurrentPage?.Parent;
        while (page is not null)
        {
            IPage parent = page.Parent;
            (page as Node)?.Free();
            page = parent;
        }
    }
}