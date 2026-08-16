using System;
using System.Collections.Generic;
using Godot;

namespace NeonWarfare.Scenes.Screen.Menu.SettingsSystem;

public partial class ColorPickerPanel : Control
{
    /// <summary>
    /// Default preset palette — neon-friendly swatches matching the menu's visual language.
    /// Cyan, teal, magenta, purple, yellow, orange, red, green, blue, white, gray, black.
    /// </summary>
    public static readonly Color[] DefaultPalette =
    {
        new("00e5ff"), // cyan   (menu accent)
        new("00b8a9"), // teal
        new("ff00e5"), // magenta
        new("9d00ff"), // purple
        new("ffe500"), // yellow
        new("ff8c00"), // orange
        new("ff3b3b"), // red
        new("3bff6b"), // green
        new("3b6bff"), // blue
        new("ffffff"), // white
        new("888888"), // gray
        new("000000"), // black
    };

    private ColorPickerButton _picker;
    private LineEdit _hexEdit;
    private Container _paletteBox;
    private IReadOnlyList<Color> _palette;

    private Color _color;
    private bool _suppress; // guards against recursive sync (programmatic update firing handlers)

    /// <summary>Current color. Read-only from the outside; mutate via the inputs.</summary>
    public Color Color => _color;

    /// <summary>Raised whenever the current color changes (user or programmatic).</summary>
    public event Action<Color> ColorChangedEvent;

    /// <param name="color">Initial color.</param>
    /// <param name="palette">Preset swatches; null falls back to <see cref="DefaultPalette"/>.</param>
    public ColorPickerPanel(Color color, IReadOnlyList<Color> palette = null)
    {
        _color = color;
        _palette = palette ?? DefaultPalette;
    }

    private ColorPickerPanel() { } // for Godot; not used

    public override void _Ready()
    {
        var vbox = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        vbox.AddThemeConstantOverride("separation", 4);
        AddChild(vbox);

        // Top row: picker + hex editor
        var topRow = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin
        };
        topRow.AddThemeConstantOverride("separation", 8);
        vbox.AddChild(topRow);

        // Picker
        _picker = new ColorPickerButton
        {
            CustomMinimumSize = new Vector2(50, 0),
            EditAlpha = false,
            Color = _color
        };
        topRow.AddChild(_picker);

        // Hex editor
        _hexEdit = new LineEdit
        {
            CustomMinimumSize = new Vector2(100, 0),
            Text = "#"+_color.ToHtml(),
            PlaceholderText = "#RRGGBB"
        };
        topRow.AddChild(_hexEdit);

        // Palette: grid wraps swatches onto new rows instead of overflowing.
        _paletteBox = new GridContainer
        {
            Columns = 6,
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin
        };
        _paletteBox.AddThemeConstantOverride("h_separation", 4);
        _paletteBox.AddThemeConstantOverride("v_separation", 4);
        BuildPalette();
        vbox.AddChild(_paletteBox);

        // Wire inputs. Every input funnels through SetColor, which is the single
        // place that mutates _color, re-syncs the other two inputs, and fires the event.
        _picker.ColorChanged += OnPickerChanged;
        _hexEdit.TextChanged += OnHexTextChanged;
        _hexEdit.TextSubmitted += OnHexTextSubmitted;
        _hexEdit.FocusExited += OnHexFocusExited;

        // Control does not aggregate children min-size; set explicitly so the host
        // SettingContainer row grows tall enough for the palette instead of clipping it.
        // top row (~28) + vbox sep (4) + 2 swatch rows (28 each) + grid sep (4) ~= 92.
        CustomMinimumSize = new Vector2(CustomMinimumSize.X, 92);
    }

    private void BuildPalette()
    {
        foreach (var swatch in _palette)
        {
            var button = new Button
            {
                CustomMinimumSize = new Vector2(28, 28)
            };
            var style = new StyleBoxFlat();
            style.BgColor = swatch;
            style.SetBorderWidthAll(1);
            style.BorderColor = new Color(1, 1, 1, 0.25f);
            style.SetContentMarginAll(2);
            button.AddThemeStyleboxOverride("normal", style);
            button.AddThemeStyleboxOverride("hover", style);
            button.AddThemeStyleboxOverride("pressed", style);
            button.TooltipText = "#"+swatch.ToHtml();
            button.Pressed += () => SetColor(swatch);
            _paletteBox.AddChild(button);
        }
    }

    // ---- single source of truth: SetColor ----

    private void SetColor(Color color, bool updatePicker = true, bool updateHex = true)
    {
        if (_suppress) return;
        _color = color;
        _suppress = true;
        try
        {
            if (updatePicker) _picker.Color = _color;
            if (updateHex) _hexEdit.Text = "#"+_color.ToHtml();
        }
        finally
        {
            _suppress = false;
        }
        ColorChangedEvent?.Invoke(_color);
    }

    // ---- input handlers: each normalizes then funnels to SetColor ----

    private void OnPickerChanged(Color color)
    {
        if (_suppress) return;
        SetColor(color, updatePicker: false, updateHex: true);
    }

    private void OnHexTextChanged(string text)
    {
        // Fires on every keystroke. Only commit when valid — do NOT touch the box
        // mid-typing (no resetting to #), or the user cannot type. Invalid in-progress
        // input (e.g. "#3", "#ff") simply waits for more input.
        string trimmed = text.Trim();
        if (trimmed.Length == 0) return;
        if (!Color.HtmlIsValid(trimmed)) return;
        SetColor(Color.FromHtml(trimmed), updatePicker: true, updateHex: false);
    }

    private void OnHexTextSubmitted(string text)
    {
        NormalizeHexOnExit();
    }

    private void OnHexFocusExited()
    {
        NormalizeHexOnExit();
    }

    private void NormalizeHexOnExit()
    {
        // On submit/focus-loss: if invalid or empty, snap the box back to the current color.
        _suppress = true;
        try { _hexEdit.Text = "#"+_color.ToHtml(); }
        finally { _suppress = false; }
    }
}
