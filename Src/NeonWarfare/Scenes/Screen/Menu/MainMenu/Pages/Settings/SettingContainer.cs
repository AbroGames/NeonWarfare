using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using NeonWarfare.Scenes.Screen.Menu.SettingsSystem;

namespace NeonWarfare.Scenes.Screen.Menu.MainMenu.Pages.Settings;

public partial class SettingContainer : PanelContainer
{
    public Setting Handle { get; }
    
    private HBoxContainer _mainHbox;
    private VBoxContainer _infoVbox;
    private Label _nameLabel;
    private Label _hintLabel;
    private Control _inputControl;

    public SettingContainer(Setting handle)
    {
        Handle = handle;
    }

    private SettingContainer()
    {
    }

    public override void _Ready()
    {
        var margin = new MarginContainer();
        var spaceControl = new Control();
        _mainHbox = new HBoxContainer();
        _nameLabel = new Label();
        _hintLabel = new Label();
        var settings = new LabelSettings();
        _infoVbox = new VBoxContainer();

        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        margin.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        spaceControl.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _mainHbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        
        margin.AddThemeConstantOverride("margin_top", 5);
        margin.AddThemeConstantOverride("margin_bottom", 5);
        margin.AddThemeConstantOverride("margin_left", 15);
        margin.AddThemeConstantOverride("margin_right", 15);

        settings.FontSize = 12;
        settings.FontColor = Colors.Gray.Darkened(0.2f);
        _hintLabel.LabelSettings = settings;
        
        margin.AddChild(_mainHbox);
        _mainHbox.AddChild(_infoVbox);
        _infoVbox.AddChild(_nameLabel);
        _infoVbox.AddChild(_hintLabel);
        _mainHbox.AddChild(spaceControl);
        AddChild(margin);
        
        _nameLabel.Text = Handle.Name;
        _hintLabel.Text = Handle.Hint;
        _hintLabel.Visible = Handle.Hint is not null;
        var optionsAttr = Handle.Member.GetAttribute<OptionsAttribute>();
        _inputControl = optionsAttr is not null
            ? BuildOptionControl(this, optionsAttr)
            : Configurators.GetFor(Handle.Type).GetControl(this);
        _inputControl.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
        // Width 300 keeps inputs aligned; respect the control's own min height
        // (e.g. ColorPickerPanel's multi-row palette needs more than 20px).
        _inputControl.CustomMinimumSize = new Vector2(300, _inputControl.CustomMinimumSize.Y);
        _mainHbox.AddChild(_inputControl);
        if (Handle.Hint is not null)
        {
            _nameLabel.TooltipText = Handle.Hint;
            _nameLabel.MouseFilter = MouseFilterEnum.Stop;
        }
    }

    private static Control BuildOptionControl(SettingContainer container, OptionsAttribute optionsAttr)
    {
        if (optionsAttr is null) return null;

        string[] labels = optionsAttr.Options;
        object[] values = container.Handle.Member.GetAttribute<OptionValuesAttribute>()?.Values ?? labels;
        if (values.Length != labels.Length || values.Any(value => value.GetType() != container.Handle.Type))
        {
            throw new InvalidOperationException(
                $"The option values of {container.Handle.Member.Member.Name} do not match its labels or its type.");
        }

        var optionButton = new OptionButton();
        foreach (string label in labels)
        {
            optionButton.AddItem(container.Tr(label));
        }
        // A value not among the options, from a hand-edited file, selects nothing and stays until an item is picked.
        optionButton.Select(Array.FindIndex(values, value => Equals(value, container.Handle.Value)));

        optionButton.ItemSelected += index => container.Handle.Value = values[(int)index];
        return optionButton;
    }
}

public abstract class SettingContainerConfigurator
{
    public abstract Control GetControl(SettingContainer settingContainer);
}

public class NumericSetting
{
    public static SettingContainerConfigurator CreateConfigurator<TNumber>()
        where TNumber : struct, IConvertible
    {
        return new CustomSettingContainerConfigurator(container =>
        {
            var stepAttribute = container.Handle.Member.GetAttribute<StepAttribute>();

            if (container.Handle.Member.TryGetAttribute<RangeAttribute>(out var rangeAttribute))
            {
                var slider = new HSlider();

                if (stepAttribute is not null)
                    slider.Step = stepAttribute.Step;

                slider.MinValue = Convert.ToDouble(rangeAttribute.Min);
                slider.MaxValue = Convert.ToDouble(rangeAttribute.Max);
                slider.Value = Convert.ToDouble(container.Handle.Value);

                slider.ValueChanged += value =>
                    container.Handle.Value = (TNumber)Convert.ChangeType(value, typeof(TNumber));

                return slider;
            }
            else
            {
                var spinBox = new SpinBox();

                if (stepAttribute is not null)
                    spinBox.Step = stepAttribute.Step;

                spinBox.Value = Convert.ToDouble(container.Handle.Value);

                spinBox.ValueChanged += value =>
                    container.Handle.Value = (TNumber)Convert.ChangeType(value, typeof(TNumber));

                return spinBox;
            }
        });
    }
}

public class CustomSettingContainerConfigurator : SettingContainerConfigurator
{
    private readonly Func<SettingContainer, Control> _configureAction;

    public CustomSettingContainerConfigurator(Func<SettingContainer, Control> configureAction)
    {
        _configureAction = configureAction;
    }
    public override Control GetControl(SettingContainer container)
    {
        return _configureAction(container);
    }
}

public static class Configurators
{
    private static readonly Dictionary<Type, SettingContainerConfigurator> _configurators = new()
    {
        { typeof(bool), new CustomSettingContainerConfigurator(container =>
        {
            var checkbox = new CheckBox();
            checkbox.ButtonPressed = (bool)container.Handle.Value;
            checkbox.Toggled += state => container.Handle.Value = state;
            return checkbox;
        })},
        
        { typeof(int), NumericSetting.CreateConfigurator<int>() },
        
        { typeof(long), NumericSetting.CreateConfigurator<long>()},
        
        { typeof(float), NumericSetting.CreateConfigurator<float>()},
        
        { typeof(double), NumericSetting.CreateConfigurator<double>()},
        
        { typeof(string), new CustomSettingContainerConfigurator(container =>
        {
            var input = new LineEdit();
            input.Text = container.Handle.Value.ToString();
            input.TextChanged += text => container.Handle.Value = text;
            return input;
        })},
        
        { typeof(Color), new CustomSettingContainerConfigurator(container =>
        {
            var panel = new ColorPickerPanel((Color)container.Handle.Value);
            panel.ColorChanged += color => container.Handle.Value = color;
            return panel;
        })}
    };
    
    
    static Configurators()
    {
        
    }
    
    public static SettingContainerConfigurator GetFor<T>()
    {
        return GetFor(typeof(T));
    }

    public static SettingContainerConfigurator GetFor(Type type)
    {
        if (_configurators.TryGetValue(type, out var configurator))
        {
            return configurator;
        }
        
        throw new KeyNotFoundException($"No configurator found for type {type}");
    }
}