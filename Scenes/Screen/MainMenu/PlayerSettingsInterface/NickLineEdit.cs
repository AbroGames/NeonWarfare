using Godot;

namespace NeonWarfare;

public partial class NickLineEdit : LineEdit
{
    public override void _Ready()
    {
        Text = ClientRoot.Instance.PlayerSettings.PlayerName;
    }
}