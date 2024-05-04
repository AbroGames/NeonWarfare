using Godot;
using KludgeBox;
using KludgeBox.Events.Global;

namespace NeoVector;

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
            Root.Instance.PlayerSettings.PlayerName = newNickname;
            Root.Instance.PlayerSettings.PlayerColor = newColor;
            SettingsService.PlayerSettingsSave();
        };
    }
}