using System;
using Godot;

namespace Kludgeful.Main.ContextSystem;

public abstract partial class Page : Control, IPage
{
    public IPage Parent { get; private set; }
    public IPage Child { get; private set; }
    
    public bool IsTop => Child is null;
    public bool IsRoot => Parent is null;

    protected Action<IPage> GoNext { get; set; }
    protected Action GoBack { get; set; }

    public void PushChild(IPage next)
    {
        if (Child is not null)
            return;
        
        Child = next;
    }

    public void SetParent(IPage parent)
    {
        if (Parent is not null)
            return;
        
        Parent = parent;
    }

    public virtual void OnHidden(IPage nextToShow){}

    public virtual void OnShown(IPage previousSeen){}
    public void Setup(Action goBack, Action<IPage> next)
    {
        GoBack = goBack;
        GoNext = next;
    }

    public void Close()
    {
        if (IsRoot)
        {
            // Cannot close root context. Delete container instead.
            return;
        }

        Child?.Close();
    
        if (IsTop)
        {
            Pop();
        }

        QueueFree();
    }

    private void Pop()
    {
        if (Parent.Child == this && Parent is Page nextContextNode)
        {
            nextContextNode.Child = null;
        }
    }
}