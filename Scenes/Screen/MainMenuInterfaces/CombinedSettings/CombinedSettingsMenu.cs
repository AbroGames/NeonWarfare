using Godot;
using System;
using System.Globalization;
using System.Linq;
using NeonWarfare.Scenes.Game.ClientGame.ClientSettings;
using NeonWarfare.Scenes.Game.ClientGame.ClientSettings.SettingTypes;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.Screen.MainMenuInterfaces;
using NeonWarfare.Scripts.KludgeBox.Core;

public partial class CombinedSettingsMenu : Control
{
    [Export] [NotNull] private PackedScene _settingContainerScene { get; set; }
    [Export] [NotNull] private PackedScene _settingGroupPanelScene { get; set; }
    [Export] [NotNull] private VBoxContainer _settingsContainer { get; set; }
    [Export] [NotNull] private Button _returnButton { get; set; }

    [Export] private float _settingGroupSpacing { get; set; } = 15;
    
    private SettingsGroupPanel _settingsGroupPanel;

    private Control GetActiveSettingsContainer()
    {
	    return _settingsGroupPanel is not null ? _settingsGroupPanel.SettingsContainer : _settingsContainer;
    }
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		var settingsList = ClientRoot.Instance.Settings.GetSettings().OrderBy(setting => setting.Accessor.Member.MetadataToken);
		_returnButton.Pressed += () =>
		{
			MenuService.ChangeMenuFromButtonClick(ClientRoot.Instance.PackedScenes.MainMenu);
		};
		
		foreach (var setting in settingsList)
		{
			var settingControl = CreateSettingContainer(setting);
			GetActiveSettingsContainer().AddChild(settingControl);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private Control CreateSettingContainer(Setting setting)
	{
		var container = _settingContainerScene.Instantiate<SettingContainer>();
		container.AssignSetting(setting);
		Control settingControl = null;
		
		if (setting is NumericSetting numericSetting)
		{
			settingControl = CreateNumericSettingControl(numericSetting);
		}

		if (setting is BooleanSetting booleanSetting)
		{
			settingControl = CreateBooleanSettingControl(booleanSetting);
		}

		if (setting is StringSetting stringSetting)
		{
			settingControl = CreateStringSettingControl(stringSetting);
		}

		if (setting is ColorSetting colorSetting)
		{
			settingControl = CreateColorSettingControl(colorSetting);
		}

		if (setting is SettingGroupSetting settingGroupSetting)
		{
			var panel = _settingGroupPanelScene.Instantiate() as SettingsGroupPanel;
			_settingsContainer.AddChild(panel);
			_settingsGroupPanel = panel;
			
			container.SettingControlsContainer.QueueFree();
			var spacingNode = new Control();
			spacingNode.CustomMinimumSize = new Vector2(0, _settingGroupSpacing);
			_settingsContainer.AddChild(spacingNode);
			//container.MoveChild(spacingNode, 0);
			//container.DividePanel.Visible = true;
			container.BackgroundColor.Color = new Color(0, 0.2f, 0.19f, 0.7f);
			settingControl = new Control();
			container.SettingNameLabel.LabelSettings = (LabelSettings)container.SettingNameLabel.LabelSettings.Duplicate();
			container.SettingNameLabel.LabelSettings.FontSize = (int)(container.SettingNameLabel.LabelSettings.FontSize * 1.5);
		}
		
		container.SettingControlsContainer.AddChild(settingControl);

		return container;
	}
	

	private static ColorPickerButton CreateColorSettingControl(ColorSetting setting)
	{
		var baseValue = (Color)setting.GetValue();
		var colorPicker = new ColorPickerButton();
		colorPicker.Color = baseValue;
		colorPicker.CustomMinimumSize = new Vector2(100, 30);
		colorPicker.SetAnchorsPreset(LayoutPreset.CenterLeft);
		colorPicker.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;

		setting.ValueValidationFailed += (info) =>
		{
			if (((Color)info.ProvidedValue).A < 0.5
			    || ((Color)info.ProvidedValue).V < 0.3)
				ClientRoot.Instance.UnlockAchievement(AchievementIds.NinjaAchievement);
		};
		
		colorPicker.ColorChanged += (Color color) =>
		{
			setting.SetValue(color);
			Settings.InvokeChanged(ClientRoot.Instance.Settings);
		};
		
		return colorPicker;
	}

	private static CheckBox CreateBooleanSettingControl(BooleanSetting setting)
	{
		var checkBox = new CheckBox();
		checkBox.ButtonPressed = (bool)setting.GetValue();
		checkBox.Pressed += () =>
		{
			setting.SetValue(checkBox.ButtonPressed);
			Settings.InvokeChanged(ClientRoot.Instance.Settings);
		};
		
		return checkBox;
	}

	private static Control CreateStringSettingControl(StringSetting setting)
	{
		var baseValue = setting.GetValue().ToString();
		Control settingControl = null;
		
		if (setting.HasOptions)
		{
			var optionButton = new OptionButton();
			optionButton.AllowReselect = true;

			foreach (var option in setting.Options)
			{
				optionButton.AddItem(option);
			}
			
			optionButton.Text = baseValue ?? "";
			if (String.IsNullOrWhiteSpace(optionButton.Text))
			{
				optionButton.Select(0);
			}

			optionButton.ItemSelected += (index) =>
			{
				var value = optionButton.GetItemText((int)index);
				optionButton.Text = value;
				setting.SetValue(value);
				Settings.InvokeChanged(ClientRoot.Instance.Settings);
			};
			
			settingControl = optionButton;
		}
		else
		{
			var textField = new LineEdit();
			textField.Text = baseValue;

			textField.TextChanged += (string text) =>
			{
				setting.SetValue(text);
				Settings.InvokeChanged(ClientRoot.Instance.Settings);
			};
			
			settingControl = textField;
		}
		
		return settingControl;
	}

	private static Control CreateNumericSettingControl(NumericSetting setting)
	{
		var numericInputType = setting.NumberInputType;
		var baseValue = Convert.ToDouble(setting.GetValue());
		Control settingControl = null;
		
		if (numericInputType is NumberInputType.TextField)
		{
			var textField = new LineEdit();
			textField.Text = baseValue.ToString(CultureInfo.InvariantCulture);

			textField.TextChanged += (string text) =>
			{
				if (Double.TryParse(text, out var value))
				{
					setting.SetValue(value);
					Settings.InvokeChanged(ClientRoot.Instance.Settings);
				}
			};
			
			settingControl = textField;
		}

		if (numericInputType is NumberInputType.SpinBox)
		{
			var spinBox = new SpinBox();
			spinBox.MaxValue = setting.MaxValue;
			spinBox.MinValue = setting.MinValue;
			spinBox.Step = setting.Step;
			spinBox.CustomArrowStep = setting.Step;
			spinBox.Value = baseValue;

			spinBox.ValueChanged += (double value) =>
			{
				setting.SetValue(value);
				Settings.InvokeChanged(ClientRoot.Instance.Settings);
			};
			
			settingControl = spinBox;
		}

		if (numericInputType is NumberInputType.Slider)
		{
			var slider = new HSlider();
			slider.MinValue = setting.MinValue;
			slider.MaxValue = setting.MaxValue;
			slider.Step = setting.Step;
			slider.Value = baseValue;
			slider.ValueChanged += (double value) =>
			{
				setting.SetValue(value);
				Settings.InvokeChanged(ClientRoot.Instance.Settings);
			};
			
			settingControl = slider;
		}

		return settingControl;
	}
}
