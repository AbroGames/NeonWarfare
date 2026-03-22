using Godot;
using Kludgeful.Main;

namespace ReVectorSurvivors.States.MainMenuState;

public partial class ContextStorage : CheckedAbstractStorage
{
    [Export] public PackedScene MainContext { get; private set; }
    [Export] public PackedScene SettingsContext { get; private set; }
    [Export] public PackedScene ConnectionContext { get; private set; }
}