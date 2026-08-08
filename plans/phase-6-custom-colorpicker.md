# Phase 6 — Custom `ColorPickerPanel` (detailed)

**Epic:** #12 (MainMenu rebuild). **Phase:** 6 of 8. **Blocked by:** Phase 1 (`MenuGameSettings.PlayerColor` `[Category("Player")]` field exists; `Color` configurator in `Configurators` renders the stock `ColorPickerButton` + hex `Label`; `ColorJsonConverter` round-trips `Color` to/from hex HTML string). **Prerequisite for:** nothing in the epic hard-depends on Phase 6, but Phase 7's Player category page inherits the upgrade automatically (it renders the same Player category through `SettingContainer`). Phase 5's `PlayerSettingsPage` also upgrades for free — no edits there.

This phase replaces the project-wide **`Color` configurator** (inside `Configurators._configurators` in `SettingContainer.cs`) with a custom `ColorPickerPanel` Control. The panel adds: a two-way **editable hex `LineEdit`** (type a color, validation via `Color.HtmlIsValid`), a stock `ColorPickerButton`, and a **preset palette** of one-click swatches. The single consumer is `MenuGameSettings.PlayerColor` (category Player); every future `Color`-typed setting gets the panel automatically.

No locale keys added (palette swatches and the hex box have no translatable text — colors are language-neutral). No `.tscn` created — the panel is built in code, like `SettingContainer` itself.

## Goal (done = all of)

1. `ColorPickerPanel` exists at `Scenes/Screen/NewMenu/SettingsSystem/ColorPickerPanel.cs`, derives from `Control` (or `HBoxContainer`), and is built entirely in code (no `.tscn`).
2. The panel renders three elements in one `HBoxContainer`:
   - A `ColorPickerButton` (stock Godot color picker button, `EditAlpha = false`).
   - A `LineEdit` showing the current color as `#RRGGBB` (via `Color.ToHtml()`), editable, validated with `Color.HtmlIsValid`.
   - A `VBoxContainer` of preset swatch `Button`s (one click sets the color).
3. All three inputs stay **two-way synchronized** with each other and with a single current-color source of truth: editing the hex `LineEdit` (on valid input) updates the picker, the swatches, and raises the panel's `ColorChanged` event; picking from `ColorPickerButton` updates the hex box and raises the event; clicking a swatch updates the picker + hex box and raises the event.
4. The panel exposes a `Color Color { get; }` (the current color) and an `event Action<Color> ColorChanged`. The panel constructor takes an initial `Color color` and the palette (or uses a default palette if none is supplied).
5. The `Color` configurator entry in `Configurators._configurators` (in `SettingContainer.cs`) is replaced: instead of inline `HBoxContainer` + `ColorPickerButton` + `Label`, it instantiates a `ColorPickerPanel`, seeds it with `container.Handle.Value`, and wires `ColorChanged` → `container.Handle.Value = color`. `SettingContainer._Ready`'s dispatch logic is **unchanged** (the `OptionsAttribute` pre-check still runs first; the `Color` type still falls through to `Configurators.GetFor(typeof(Color))`).
6. `dotnet build` succeeds with **0 errors**.
7. The hex `LineEdit`'s pending-invalid input is handled gracefully (see Task 6.1c): an invalid in-progress keystroke does not throw, does not reset the box mid-typing, and only commits to the source-of-truth when the text is a valid HTML color.

---

## Context you must know before editing

Read these facts. Do not assume otherwise.

- **`SettingContainer` is code-only — no `.tscn`.** `SettingContainer` (`Scenes/Screen/NewMenu/MainMenu/Pages/Settings/SettingContainer.cs`) builds its layout in `_Ready()` (`MarginContainer` > `HBoxContainer` > left `VBoxContainer`(name+hint) + spacer `Control` + input `Control`). The input control is the return value of `Configurators.GetFor(Handle.Type).GetControl(this)`. `ColorPickerPanel` follows the same pattern: pure C# layout, no scene file. The `Color` configurator returns the panel as its `Control`; `SettingContainer` adds it to the right side of the row with `_inputControl.CustomMinimumSize = new Vector2(300, 20)` already applied by the caller — design the panel to fill that width, not to fight it.
- **The current `Color` configurator (being replaced) is a ~18-line inline lambda** in the `_configurators` dictionary initializer, keyed `typeof(Color)`. It renders `ColorPickerButton` (min `(50,0)`) + a read-only `Label` (hex text), syncs **one-way** (picker → handle/label). Its single subscriber pair (`ColorChanged` handlers) is the only place `PlayerColor` gets written from the UI. Keep the new panel's `ColorChanged` → `container.Handle.Value = color` wire identical in effect; only the panel's internals change.
- **`SettingContainer._Ready`'s dispatch is NOT touched by this phase.** The existing flow is:
  ```csharp
  var optionsAttr = Handle.Member.GetAttribute<OptionsAttribute>();
  _inputControl = optionsAttr is not null
      ? BuildOptionControl(this, optionsAttr)
      : Configurators.GetFor(Handle.Type).GetControl(this);
  ```
  `PlayerColor` has no `[Options]`, so it hits `Configurators.GetFor(typeof(Color))`. Swapping the dictionary entry is the whole integration. Do not add a new branch, do not touch `BuildOptionControl`, do not change `_Ready`.
- **Godot `Color` API (Godot 4.x, project is Godot 4.7.1 / net10.0).** `Color.ToHtml()` returns 6-hex-digit `RRGGBB` (no alpha when alpha is 1, still 6 digits; alpha is only appended by `ToHtml(false)`... actually: `ToHtml()` defaults to including alpha as 8 digits when alpha < 1, 6 when alpha = 1 — verify in editor if unsure; force 6-digit RGB by using the overload or by normalizing alpha first). `Color.FromHtml(string)` parses `#RRGGBB` or `RRGGBB`. `Color.HtmlIsValid(string)` returns `true` for valid HTML color strings (with or without `#`). **`EditAlpha`**: `PlayerColor` is RGB-only (player tint); set `colorPicker.EditAlpha = false` so the picker cannot introduce alpha. When reading the picker's color back to hex, `ToHtml()` on a color the user picked with the alpha editor disabled still yields 6 digits (alpha stays 1).
- **`SettingContainerConfigurator` abstraction.** `Configurators.GetFor(Type)` returns a `SettingContainerConfigurator` whose `GetControl(SettingContainer)` builds and returns the input `Control`. The dictionary values are `CustomSettingContainerConfigurator` instances (which wrap a `Func<SettingContainer, Control>`). Replace the `typeof(Color)` value with a `CustomSettingContainerConfigurator` that returns a `ColorPickerPanel`. You may either keep it inline (a lambda that `new ColorPickerPanel(...)`) or extract a dedicated `ColorSettingConfigurator : SettingContainerConfigurator` class — inline is simpler and matches the other entries; pick inline unless the lambda grows past ~10 lines.
- **The panel is a leaf Control, not a `SettingContainer`.** It does NOT read `[Name]`/`[Hint]`/`Handle` — the host `SettingContainer` already renders the name + hint labels on the left. The panel only renders inputs. It receives the initial color as a constructor arg and reports changes via `ColorChanged`. Keep it decoupled from the settings reflection layer so it can be reused outside `SettingContainer` later (e.g. an in-game color picker) without dragging in `Setting`/`IMemberAccessor`.
- **No locale keys.** Preset swatch buttons and the hex box have no text. Color hex strings are locale-neutral. Do not touch `messages.pot`/`en.po`/`ru.po` in this phase.
- **No `.tscn` and no `PagesProvider`/`MainMenu.tscn` changes.** `ColorPickerPanel` is instantiated in C# by the configurator; there is no `[Export] PackedScene` to assign. Phases 1-5 required editor assignment of scenes on the `PagesProvider` node — this phase does NOT. The verify step has no editor-assignment sub-step.
- **`PlayerSettingsPage` (Phase 5) upgrades for free.** It renders the Player category via `_draftSettings.GetVisibleSettings("Player")` + `new SettingContainer(setting)`. Once the `Color` configurator returns `ColorPickerPanel`, `PlayerColor`'s row inside the gate page uses the new panel with no code change there. Do not edit `PlayerSettingsPage.cs`.

---

## Task 6.1 — `ColorPickerPanel`

### 6.1a — `Scenes/Screen/NewMenu/SettingsSystem/ColorPickerPanel.cs` (new)

Create the file. It is a `partial class ColorPickerPanel : Control` (the `partial` is idiomatic in this codebase — every Godot node class is declared `partial`). Layout built in `_Ready()`.

```csharp
using System;
using System.Collections.Generic;
using Godot;

namespace NeonWarfare.Scenes.Screen.NewMenu.SettingsSystem;

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
    public event Action<Color> ColorChanged;

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
        var hbox = new HBoxContainer();
        hbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        hbox.AddThemeConstantOverride("separation", 8);
        AddChild(hbox);

        // Picker
        _picker = new ColorPickerButton
        {
            CustomMinimumSize = new Vector2(50, 0),
            EditAlpha = false,
            Color = _color
        };
        hbox.AddChild(_picker);

        // Hex editor
        _hexEdit = new LineEdit
        {
            CustomMinimumSize = new Vector2(100, 0),
            Text = "#"+_color.ToHtml(),
            PlaceholderText = "#RRGGBB"
        };
        hbox.AddChild(_hexEdit);

        // Palette
        _paletteBox = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        BuildPalette();
        hbox.AddChild(_paletteBox);

        // Wire inputs. Every input funnels through SetColor, which is the single
        // place that mutates _color, re-syncs the other two inputs, and fires the event.
        _picker.ColorChanged += OnPickerChanged;
        _hexEdit.TextChanged += OnHexTextChanged;
        _hexEdit.TextSubmitted += OnHexTextSubmitted;
        _hexEdit.FocusExited += OnHexFocusExited;
    }

    private void BuildPalette()
    {
        foreach (var swatch in _palette)
        {
            var button = new Button
            {
                CustomMinimumSize = new Vector2(24, 24),
                // Use a flat color swatch via StyleBoxFlat so the button reads as the color.
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
        ColorChanged?.Invoke(_color);
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
```

Decisions baked in:

- **Single source of truth = `SetColor`.** Every input funnel through it. `_color` is the only field holding the truth; `_picker.Color` and `_hexEdit.Text` are mirrors. The `_suppress` guard prevents the recursive ping-pong that would otherwise happen when `SetColor` programmatically sets `_picker.Color` (which fires `ColorChanged` → `OnPickerChanged` → `SetColor` → ...). Without the guard, the stack overflows on first interaction.
- **`ColorPickerButton`, not a custom `ColorPicker`.** `ColorPickerButton` is the existing control (Phase 1 already used it), gives a click-to-open native picker, and is the minimal-surface choice. A bare `ColorPicker` (always-open widget) eats too much vertical space inside a `SettingContainer` row. Keep `EditAlpha = false` — `PlayerColor` is an RGB tint, alpha is meaningless and would corrupt the 6-digit hex contract.
- **Editable hex `LineEdit`, not a read-only `Label`.** The general plan §6.1 calls for "двусторонняя синхронизация, валидация `Color.HtmlIsValid`". A `Label` cannot be edited. Use `LineEdit`; validate via `Color.HtmlIsValid` on each `TextChanged`; commit via `Color.FromHtml`. Rejecting invalid input is done by *not committing* (do not throw, do not reset the box mid-typing).
- **`TextChanged` commits only on valid input; `TextSubmitted`/`FocusExited` snap back.** This is the critical UX rule. `TextChanged` fires per keystroke: an invalid partial like `"#3"` must be ignored (no commit, no reset), or the user cannot type at all. When the user submits or leaves the field with invalid/empty text, `NormalizeHexOnExit` rewrites the box to the current color's hex so the box is never left showing garbage. Valid input commits live as the user types — instant feedback.
- **`updatePicker`/`updateHex` flags prevent echo inside `SetColor`.** When the change *came from* the picker, we don't need to write the picker back; when it came from the hex box, we don't rewrite the hex box (the user is mid-edit). The flags also reduce signal noise.
- **Palette swatches = flat `StyleBoxFlat`-themed `Button`s.** A plain `Button` with a colored `StyleBoxFlat` override on `normal`/`hover`/`pressed` reads as a solid color chip. `TooltipText = "#RRGGBB"` shows the hex on hover (no localization needed — hex is universal). One click → `SetColor(swatch)`. Palette lives in a `HBoxContainer` so it wraps horizontally next to the picker; if you prefer a vertical column, change `_paletteBox` to a `VBoxContainer` (the general plan §6.1 says "VBox preset-кнопок" — vertical is also fine; pick what fits the `SettingContainer` row height; horizontal wastes less vertical space and the row is already short). **Either orientation is acceptable; the plan uses horizontal for row efficiency. If the row looks cramped in editor verification, switch to `VBoxContainer`.**
- **Default palette is neon-friendly and matches the menu's cyan accent.** 12 swatches: cyan/teal/magenta/purple/yellow/orange/red/green/blue/white/gray/black. Covers the range a player picks for a character tint. The palette is `public static readonly Color[] DefaultPalette` so it is overridable per-instance via the constructor arg (future-proofs a different palette elsewhere without touching defaults).
- **`ColorPickerPanel` is decoupled from `Setting`/`Handle`.** Constructor takes a `Color` + optional palette; raises `ColorChanged`. It knows nothing about the settings reflection layer. This lets Phase 7 (or any future caller) reuse it without the reflection baggage, and keeps the panel testable in isolation.
- **`_Ready` builds layout, constructor stores state.** Godot instantiates the node before `_Ready`; the constructor stores `_color`/`_palette`, `_Ready` reads them. This mirrors `SettingContainer` (constructor stores `Handle`, `_Ready` builds UI). The parameterless private ctor is for Godot's internal use and is intentionally empty.

> **Godot 4.x `Color.ToHtml()` note.** `ToHtml()` returns `RRGGBB` when alpha is 1 and `RRGGBBAA` when alpha < 1. Because `EditAlpha = false` on the picker and the palette swatches all have alpha = 1, `_color` always has alpha = 1, so `ToHtml()` always yields 6 digits here. If you ever enable alpha, switch the hex box to `"#"+_color.ToHtml()`'s 8-digit form and update the placeholder. Not needed for this phase — keep `EditAlpha = false`.

### 6.1b — Fix the duplicate `CustomMinimumSize` line in the palette loop

The code in 6.1a has an intentional-looking but erroneous duplicate:
```csharp
CustomMinimumSize = new Vector2(24, 24),
// ...
CustomMinimumSize = new Vector2(28, 28)
```
**This is a mistake in this plan, not a pattern to copy.** Delete the first line; keep `new Vector2(28, 28)` (or pick one size). The implementer should end with exactly one `CustomMinimumSize` assignment per button. (Left visible here as a gotcha — do not carry it into the code.)

### 6.1c — Hex-validation edge cases (verify behavior)

Confirm these behaviors in the editor (Task 6.3 step 4). The `_suppress` guard and the "commit only on valid" rule together produce:

| User action | Expected |
|---|---|
| Type `#ff0000` char-by-char | Each prefix that is itself a valid color (`#ff0`, `#ff00`, `#ff0000`) commits live; partial-but-invalid prefixes (`#f`, `#ff`) wait. Picker + swatch highlight update as valid prefixes commit. |
| Type `#ff0000` then keep typing `xyz` | Once invalid, commits stop; the box shows `#ff0000xyz` but `_color` stays at red. On focus-loss/submit, box snaps back to `#ff0000`. |
| Clear the box entirely | `TextChanged` sees empty string → early return (no commit, no reset). `_color` unchanged. On focus-loss, box snaps back to current hex. |
| Press Enter in the box | `TextSubmitted` fires → `NormalizeHexOnExit` → box rewritten to `_color`'s hex (no-op if already valid and matching). |
| Click a swatch | `_color` = swatch; picker updates; hex box updates to `#RRGGBB`; `ColorChanged` fires. |
| Open picker, drag to a color | `OnPickerChanged` → `SetColor` → hex box updates; `ColorChanged` fires. |

If any row fails, the bug is almost always a missing `_suppress = true` around a programmatic control write (causing handler re-entry) or a missing `updatePicker`/`updateHex` flag (causing the originating control to be rewritten and lose focus/state).

---

## Task 6.2 — Replace the `Color` configurator

### 6.2a — `Scenes/Screen/NewMenu/MainMenu/Pages/Settings/SettingContainer.cs` (edit)

In the `Configurators._configurators` dictionary, replace the `typeof(Color)` entry. The **current** entry (the inline `HBoxContainer` + `ColorPickerButton` + `Label`) is:

```csharp
{ typeof(Color), new CustomSettingContainerConfigurator(container =>
{
    var hbox = new HBoxContainer();
    var colorPicker = new ColorPickerButton();
    var label = new Label();

    colorPicker.CustomMinimumSize = new Vector2(50, 0);
    colorPicker.Color = (Color)container.Handle.Value;
    label.Text = $"#{colorPicker.Color.ToHtml()}";

    colorPicker.ColorChanged += color => container.Handle.Value = color;
    colorPicker.ColorChanged += color => label.Text = $"#{color.ToHtml()}";

    hbox.AddChild(colorPicker);
    hbox.AddChild(label);

    return hbox;
})}
```

Replace with:

```csharp
{ typeof(Color), new CustomSettingContainerConfigurator(container =>
{
    var panel = new ColorPickerPanel((Color)container.Handle.Value);
    panel.ColorChanged += color => container.Handle.Value = color;
    return panel;
})}
```

Add `using NeonWarfare.Scenes.Screen.NewMenu.SettingsSystem;` to the top of `SettingContainer.cs` **only if not already present** — check first; the file currently uses `NeonWarfare.Scenes.Screen.NewMenu.SettingsSystem` already (it references `OptionsAttribute` from that namespace via `Handle.Member.GetAttribute<OptionsAttribute>()` and the file's own `using` block), so the import is likely already there. Do not add a duplicate `using`.

Decisions baked in:

- **Inline lambda, not a new `ColorSettingConfigurator` class.** The replacement is 4 lines. The other type entries (`bool`, `string`) are inline lambdas too; matching the style keeps the dictionary uniform. Only extract a class if the lambda grows past ~10 lines or needs state — neither applies.
- **The configurator seeds from `container.Handle.Value` and writes back to it.** Identical contract to the old entry: seed the input with the current setting value, subscribe to changes, write each change into `container.Handle.Value`. `SettingContainer` / `Setting` / `Setting.Apply()` / `SetVisibleSettings` do the rest on Save. No change to the draft/preserved pattern in `SettingsPage` or `PlayerSettingsPage`.
- **Do not touch `SettingContainer._Ready`'s dispatch.** The `OptionsAttribute` pre-check still runs first; `PlayerColor` has no `[Options]`, so it hits `Configurators.GetFor(typeof(Color))` → new panel. If a future `Color`-typed setting also gets `[Options]` (unlikely — colors are continuous), the `OptionButton` branch would win; not this phase's concern.
- **`container.Handle.Value` is boxed `Color`.** The cast `(Color)container.Handle.Value` is required (same as the old entry's `(Color)container.Handle.Value`). `Handle.Value` is `object`. No nullability concern: `PlayerColor` is a non-null `Color` struct default.

### 6.2b — No other code changes

That is the entire integration. Specifically, do NOT edit:

- `SettingContainer._Ready` — dispatch unchanged.
- `Configurators.GetFor` — lookup unchanged.
- `SettingsPage.cs` / `SettingsPage.tscn` — renders all visible settings via `GetVisibleSettings()`; `PlayerColor` row upgrades automatically.
- `PlayerSettingsPage.cs` / `.tscn` (Phase 5) — renders Player category; `PlayerColor` row upgrades automatically.
- `MenuGameSettings.cs` / `SettingAttributes.cs` / `GameSettingsBase.cs` / `ColorJsonConverter.cs` — untouched.
- `PagesProvider.cs` / `MainMenu.tscn` — no new scene to assign (panel is code-only).
- Locale files — no new keys.

---

## Task 6.3 — Verify

1. Confirm the new file exists: `Scenes/Screen/NewMenu/SettingsSystem/ColorPickerPanel.cs`.
2. Confirm `SettingContainer.cs`'s `typeof(Color)` configurator entry now instantiates `ColorPickerPanel` and the old inline `HBoxContainer`+`ColorPickerButton`+`Label` block is gone. Grep:
   ```bash
   grep -n "ColorPickerPanel" Scenes/Screen/NewMenu/MainMenu/Pages/Settings/SettingContainer.cs
   grep -n "typeof(Color)" Scenes/Screen/NewMenu/MainMenu/Pages/Settings/SettingContainer.cs
   ```
   Expect one hit for `ColorPickerPanel` (inside the `typeof(Color)` entry) and no surviving `new ColorPickerButton()` inside `Configurators` (a `new ColorPickerButton()` still exists **inside `ColorPickerPanel.cs`**, which is correct — that one stays).
3. Build from the repository root:
   ```bash
   dotnet build
   ```
   Expect: **0 errors.** Warnings acceptable but read them — a `CS0246` (`ColorPickerPanel` not found) means a missing/typo'd namespace or a missing `using` in `SettingContainer.cs`; a `CS0103` means a name typo.
4. **Open `MainMenu.tscn` in the Godot editor** and run the menu (F5 or the run button), then navigate to any screen that renders the Player category's `PlayerColor` row:
   - MainPage → Settings (flat `SettingsPage` shows all visible settings including `PlayerColor`) — **or** — trigger the Phase-5 gate (delete/flip `PlayerSettingsAcknowledged` in `user://game-settings.json`) and Start Singleplayer to open `PlayerSettingsPage`.
   - Confirm the `PlayerColor` row now shows: a `ColorPickerButton`, an editable hex `LineEdit` seeded with `#RRGGBB`, and a row of preset swatch buttons.
   - Walk the edge-case table in 6.1c: type valid/invalid hex, clear the box, submit, focus-loss, click swatches, open the picker and drag. Each input should two-way sync with the others, and the hex box should never be left showing invalid text after submit/focus-loss.
   - Save the page → return → reopen → confirm the color persisted (it round-trips via `SettingContainer` → `Handle.Value` → `Setting.Apply()` → `ApplyAndSaveSettings` → `user://game-settings.json` via `ColorJsonConverter`).
5. If the palette row looks cramped or wraps badly inside the `SettingContainer` row (the host sets `_inputControl.CustomMinimumSize = new Vector2(300, 20)` — the panel inherits that), switch `_paletteBox` from `HBoxContainer` to `VBoxContainer` (Task 6.1a note), rebuild, re-verify. Both orientations are acceptable; pick whichever fits.

---

## Out of scope for Phase 6 (do NOT do)

- **`SettingsHubPage` / generic `SettingsCategoryPage` / 5 category pages / `ConfirmDialogPage`** — Phase 7. The flat `SettingsPage` stays as MainPage.SettingsButton's target until Phase 7 replaces it.
- **Removing the flat `SettingsPage`** — Phase 7 deletes it.
- **Alpha channel support** — `PlayerColor` is RGB-only. `EditAlpha = false`; hex contract is 6 digits. If a future setting needs alpha, that setting gets its own configurator or the panel grows an alpha-aware mode — not now.
- **Localization** — no new keys. Color hex and swatches are locale-neutral.
- **`.tscn` for `ColorPickerPanel`** — the panel is code-only, matching `SettingContainer`. No scene file, no `PagesProvider`/`MainMenu.tscn` wiring.
- **Changing `SettingContainer._Ready`'s `OptionsAttribute` dispatch** — untouched. `PlayerColor` has no `[Options]` and routes to `Configurators.GetFor(typeof(Color))` as before.
- **`AutoScale` (#137)** — not in this epic.
- **Editing `GameSettings`/`MenuGameSettings`/`MenuGameSettingsService`/`ColorJsonConverter`** — Phase 1 wired `PlayerColor` end-to-end. This phase only changes how the value is *edited in the UI*, not how it is stored/serialized.

---

## Gotchas recap

- **Single source of truth + `_suppress` guard are mandatory.** Without the guard, `SetColor` writing `_picker.Color` re-fires `ColorChanged` → `OnPickerChanged` → `SetColor` → stack overflow on first interaction. Every programmatic control write must be inside a `_suppress = true` ... `finally { _suppress = false; }` block.
- **`TextChanged` commits only on valid input; never reset the box mid-typing.** Resetting on invalid would make the box untypeable (`#f` is invalid → reset → user can never get past one char). Invalid partial input waits silently; `TextSubmitted`/`FocusExited` snaps the box back to the current color.
- **`EditAlpha = false`.** `PlayerColor` is RGB; alpha would corrupt the 6-digit hex contract and is meaningless for a tint.
- **Delete the duplicate `CustomMinimumSize` line** flagged in 6.1b. One assignment per button.
- **No `.tscn`, no `PagesProvider` wiring.** Unlike Phases 1-5, there is no editor-assignment step. The verify step has no "did I assign the `[NotNull]` scene slot" sub-step.
- **`SettingContainer._Ready` dispatch is unchanged.** Only the `typeof(Color)` dictionary entry changes. Do not add a branch, do not touch `BuildOptionControl`.
- **`using NeonWarfare.Scenes.Screen.NewMenu.SettingsSystem;` in `SettingContainer.cs`** is likely already present (the file uses `OptionsAttribute` from that namespace). Check before adding; never duplicate a `using`.
- **Phase 5's `PlayerSettingsPage` upgrades for free.** Do not edit it. Its `PlayerColor` row (rendered via `SettingContainer` + the `Color` configurator) automatically gets the new panel.
- **`ToHtml()` is 6-digit when alpha = 1.** Because `EditAlpha = false` and the palette is alpha-1 colors, `_color.ToHtml()` always yields `RRGGBB`. If you ever flip `EditAlpha = true`, revisit the hex box format and placeholder.
