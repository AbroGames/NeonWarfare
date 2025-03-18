using Godot;
using System;

public partial class SettingsGroupPanel : PanelContainer
{
    [Export] public ColorRect BackgroundColor { get; private set; }
    [Export] public VBoxContainer SettingsContainer { get; private set; }
}
