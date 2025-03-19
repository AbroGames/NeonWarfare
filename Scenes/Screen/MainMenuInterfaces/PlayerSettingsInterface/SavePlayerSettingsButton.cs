using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.Utils.PlayerSettings;

namespace NeonWarfare.Scenes.Screen.MainMenuInterfaces.PlayerSettingsInterface;

public partial class SavePlayerSettingsButton : Button
{
    [Export] [NotNull] public LineEdit NickLineEdit { get; private set; }
    [Export] [NotNull] public ColorRect ColorRect { get; private set; }
    
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
        Pressed += () =>
        {
            string newNickname = NickLineEdit.Text;
            Color newColor = ColorRect.Color;
            //ClientRoot.Instance.PlayerSettings.PlayerName = newNickname;
            //ClientRoot.Instance.PlayerSettings.PlayerColor = newColor;
            //PlayerSettingsService.SaveSettings(ClientRoot.Instance.PlayerSettings);
        };
    }
}
