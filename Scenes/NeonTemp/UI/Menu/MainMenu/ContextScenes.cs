using Godot;
using System;
using Kludgeful.Main;

public partial class ContextScenes : CheckedAbstractStorage
{
	[Export] public PackedScene MainContext { get; private set; }
	[Export] public PackedScene SettingsContext { get; private set; }
	[Export] public PackedScene ConnectionContext { get; private set; }
}
