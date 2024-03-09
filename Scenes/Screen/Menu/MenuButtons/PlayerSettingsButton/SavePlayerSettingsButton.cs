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
            EventBus.Publish(new SavePlayerSettingButtonClickEvent(this));
        };
    }
}