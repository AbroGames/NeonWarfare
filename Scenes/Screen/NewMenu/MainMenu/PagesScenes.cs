using Godot;
using KludgeBox.DI.Requests.NotNullCheck;
using NeonWarfare.Scenes.KludgeBox;

namespace NeonWarfare.Scenes.NeonTemp.UI.Menu.MainMenu;

public partial class PagesScenes : CheckedAbstractStorage
{
	[Export] [NotNull] public PackedScene MainPage { get; private set; }
	[Export] [NotNull] public PackedScene SettingsPage { get; private set; }
	[Export] [NotNull] public PackedScene ConnectionPage { get; private set; }
	[Export] [NotNull] public PackedScene CreateServer { get; private set; }
}