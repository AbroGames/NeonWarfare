using Godot;
using KludgeBox;

namespace NeonWarfare;

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
            ClientRoot.Instance.PlayerSettings.PlayerName = newNickname;
            ClientRoot.Instance.PlayerSettings.PlayerColor = newColor;
            SettingsService.PlayerSettingsSave();
        };
    }
}