using Godot;
using KludgeBox.DI.Requests.NotNullCheck;
using NeonWarfare.Scenes.KludgeBox;

namespace NeonWarfare.Scenes.NeonTemp.UI.Menu;

public partial class ContextStorage : CheckedAbstractStorage
{
    [Export] [NotNull] public PackedScene MainContext { get; private set; }
    [Export] [NotNull] public PackedScene SettingsContext { get; private set; }
    [Export] [NotNull] public PackedScene ConnectionContext { get; private set; }
}