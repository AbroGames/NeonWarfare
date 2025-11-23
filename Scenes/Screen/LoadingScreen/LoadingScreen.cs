using System;
using Godot;
using KludgeBox.DI.Requests.ChildInjection;
using KludgeBox.DI.Requests.NotNullCheck;

namespace NeonWarfare.Scenes.Screen.LoadingScreen;

public partial class LoadingScreen : CanvasLayer
{
    [Child] public LoadingAnimHandle LoadingHandle { get; private set; }
    [Child] public Label LoadingLabel { get; private set; }

    public LoadingScreen InitPreReady()
    {
        Di.Process(this);
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
}
