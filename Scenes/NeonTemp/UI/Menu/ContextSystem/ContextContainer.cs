using Godot;
using KludgeBox.DI.Requests.LoggerInjection;
using Serilog;

namespace Kludgeful.Main.ContextSystem;

public partial class ContextContainer : Control
{
    [Logger] private ILogger _logger;
    
    public IContext RootContext { get; private set; }
    public IContext CurrentContext { get; private set; }

    public override void _Ready()
    {
        Di.Process(this);
    }

    public void SetRootContext(IContext context)
    {
        if (CurrentContext is not null)
        {
            _logger.Error("Attempt to set root context more than once for {containerPath}", GetPath());
            return;
        }
        
        CurrentContext = context;
        RootContext = context;
        AddChild(context as Node);
        SetupContext(context);
    }

    public void PushContext(IContext nextContext)
    {
        var parentContext = CurrentContext;
        if (!nextContext.IsTop)
        {
            _logger.Error("Attempt to push next context that contains child context at {containerPath}", GetPath());
            return;
        }
        
        for (var ctx = parentContext; ctx != null; ctx = ctx.Parent)
        {
            if (ctx == nextContext)
            {
                _logger.Error("Loop detected: trying to push a context that already exists in the chain at {containerPath}", GetPath());
                return;
            }
        }
        
        if (CurrentContext is Node currentContextNode)
        {
            RemoveChild(currentContextNode);
            parentContext.PushChild(nextContext);
            parentContext.OnHidden(nextContext);
            nextContext.SetParent(parentContext);
            
            AddChild(nextContext as Node);
            SetupContext(nextContext);
            nextContext.OnShown(parentContext);
            CurrentContext = nextContext;
        }
        else
        {
            _logger.Error("Current context somehow is not a Node at {containerPath}", GetPath());
        }
    }

    public IContext PopContext()
    {
        if (CurrentContext.IsRoot)
        {
            _logger.Warning("Attempt to pop root context at {containerPath}", GetPath());
            return CurrentContext;
        }
        
        var parentContext = CurrentContext.Parent;
        var childContext = CurrentContext;
        
        RemoveChild(childContext as Node);
        childContext.OnHidden(parentContext);
        
        AddChild(parentContext as Node);
        parentContext.OnShown(childContext);
        childContext.Close();
        CurrentContext = parentContext;
        
        return parentContext;
    }

    private void SetupContext(IContext context)
    {
        context.Setup(() => PopContext(), PushContext);
    }
}