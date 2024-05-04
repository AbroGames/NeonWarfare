using Godot;

namespace NeonWarfare;

public partial class NickLineEdit : LineEdit
{
    public override void _Ready()
    {
        Text = Root.Instance.PlayerSettings.PlayerName;
    }
}