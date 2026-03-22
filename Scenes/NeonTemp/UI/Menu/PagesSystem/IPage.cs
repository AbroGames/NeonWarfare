using System;

namespace Kludgeful.Main.ContextSystem;

public interface IPage
{
    IPage Parent { get; }
    IPage Child { get; }
    bool IsTop => Child is null;
    bool IsRoot => Parent is null;

    IPage GetRoot()
    {
        if (IsRoot) return this;
        return Parent.GetRoot();
    }

    IPage GetTop()
    {
        if (IsTop) return this;
        return Child.GetTop();
    }

    void PushChild(IPage next);
    void SetParent(IPage parent);

    void OnHidden(IPage nextToShow);
    void OnShown(IPage previousSeen);

    void Setup(Action goBack, Action<IPage> next);
    
    void Close();
}