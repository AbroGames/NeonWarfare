using Godot;
using KludgeBox;

namespace NeonWarfare.LoadingScreen;

public partial class LoadingScreen : CanvasLayer
{
    [Export] [NotNull] public LoadingAnimHandle LoadingAnimHandle { get; private set; }
    [Export] [NotNull] public Label LoadingLabel { get; private set; }

    public void SetUpperText(string loadingText)
    {
        LoadingLabel.Text = loadingText.ToUpper();
    }

    public static LoadingScreen Create(string text = null)
    {
        LoadingScreen loadingScreen = ClientRoot.Instance.PackedScenes.Client.Screens.LoadingScreenCanvas.Instantiate<LoadingScreen>();
        if (text != null)
        {
            loadingScreen.SetUpperText(text);
        }

        return loadingScreen;
    }
}