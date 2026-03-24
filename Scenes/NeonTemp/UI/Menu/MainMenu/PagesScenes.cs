using Godot;
using System;
using KludgeBox.DI.Requests.NotNullCheck;
using Kludgeful.Main;
using NeonWarfare.Scenes.KludgeBox;

public partial class PagesScenes : CheckedAbstractStorage
{
	[Export] [NotNull] public PackedScene MainPage { get; private set; }
	[Export] [NotNull] public PackedScene SettingsPage { get; private set; }
	[Export] [NotNull] public PackedScene ConnectionPage { get; private set; }
	[Export] [NotNull] public PackedScene CreateServer { get; private set; }
}
