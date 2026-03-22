using System;

namespace Kludgeful.Main.ContextSystem;

public interface IContext
{
    IContext Parent { get; }
    IContext Child { get; }
    bool IsTop => Child is null;
    bool IsRoot => Parent is null;

    IContext GetRoot()
    {
        if (IsRoot) return this;
        return Parent.GetRoot();
    }

    IContext GetTop()
    {
        if (IsTop) return this;
        return Child.GetTop();
    }

    void PushChild(IContext next);
    void SetParent(IContext parent);

    void OnHidden(IContext nextToShow);
    void OnShown(IContext previousSeen);

    void Setup(Action goBack, Action<IContext> next);
    
    void Close();
}