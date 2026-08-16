using Godot;
using GodotBox;
using KludgeBox.DI.Requests.NotNullCheck;
using GameCheckedAbstractStorage = NeonWarfare.Scenes.Misc.GameCheckedAbstractStorage;

namespace NeonWarfare.Scenes.Screen.Menu;

public partial class ContextStorage : GameCheckedAbstractStorage
{
    [Export] [NotNull] public PackedScene MainContext { get; private set; }
    [Export] [NotNull] public PackedScene SettingsContext { get; private set; }
    [Export] [NotNull] public PackedScene ConnectionContext { get; private set; }
}