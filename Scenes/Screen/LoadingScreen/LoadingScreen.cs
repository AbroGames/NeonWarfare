using System;
using Godot;
using KludgeBox.DI.Requests.ChildInjection;

namespace NeonWarfare.Scenes.Screen.LoadingScreen;

public partial class LoadingScreen : CanvasLayer
{
    [Child] public LoadingAnimHandle LoadingHandle { get; private set; }
    [Child] public Label LoadingLabel { get; private set; }
    [Child] public Button CancelButton { get; private set; }

    private Action _cancelAction;

    public LoadingScreen InitPreReady()
    {
        Di.Process(this);
        CancelButton.Pressed += OnCancelButtonPressed;
        CancelButton.Visible = false;
        return this;
    }

    public override void _Ready()
    {
        SetLayer(Int32.MaxValue);
    }

    public void SetText(string loadingText)
    {
        LoadingLabel.Text = loadingText;
    }

    public void SetCancelAction(Action cancelAction)
    {
        _cancelAction = cancelAction;
        CancelButton.Visible = cancelAction != null;
    }

    private void OnCancelButtonPressed()
    {
        _cancelAction?.Invoke();
    }
}
