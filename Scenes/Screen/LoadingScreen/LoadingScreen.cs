using Godot;
using NeonWarfare.Scripts.KludgeBox.Core;

namespace NeonWarfare.Scenes.Screen.LoadingScreen;

public partial class LoadingScreen : CanvasLayer
{
    [Export] [NotNull] public LoadingAnimHandle LoadingAnimHandle { get; private set; }
    [Export] [NotNull] public Label LoadingLabel { get; private set; }

    public void SetUpperText(string loadingText)
    {
        LoadingLabel.Text = loadingText.ToUpper();
    }
}
