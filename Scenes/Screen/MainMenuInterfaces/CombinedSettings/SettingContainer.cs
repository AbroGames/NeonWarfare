using Godot;
using System;
using NeonWarfare.Scenes.Game.ClientGame.ClientSettings.SettingTypes;

public partial class SettingContainer : VBoxContainer
{
    [Export] public Label SettingNameLabel { get; private set; }
    [Export] public Label SettingDescriptionLabel { get; private set; }
    [Export] public MarginContainer SettingControlsContainer { get; private set; }
    [Export] public Panel DividePanel { get; private set; }
    [Export] public ColorRect BackgroundColor { get; private set; }
    public Setting Setting { get; private set; }
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
    }

    public void AssignSetting(Setting setting)
    {
        Setting ??= setting;
        SettingNameLabel.Text = Setting.Name;
		
        if (Setting.Description is not null)
        {
            SettingDescriptionLabel.Text = Setting.Description;
        }
        else
        {
            SettingDescriptionLabel.Visible = false;
        }
    }
}
