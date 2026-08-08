# Phase 1 — Settings System Foundation (detailed)

**Epic:** #12 (MainMenu rebuild). **Phase:** 1 of 8. **Prerequisite for:** Phases 5, 6, 7.

This plan adds the data fields, reflection plumbing, and a dropdown UI configurator that later phases (5/7) build the pages on top of. No UI pages are created here. No files are deleted here.

## Goal (done = all of)

1. `GameSettings` record carries 7 new fields.
2. `MenuGameSettings` carries the same 7 fields with `[Name]`/`[Hint]`/`[Category]` (and `[Range]`/`[Options]`/`[Hide]` where relevant).
3. `[Category]` and `[Options]` attributes exist.
4. `MenuGameSettings.GetVisibleSettings(string category)` + `SetVisibleSettings(list, category)` overloads exist. Parameterless overloads still work unchanged.
5. `SettingContainer` renders a `[Options]` field as a Godot `OptionButton`; non-`[Options]` fields still render exactly as before.
6. `MenuGameSettingsService.Convert` round-trips all 7 new fields both directions.
7. `MenuGameSettingsService.ApplySettings` applies fullscreen / window size / bus volumes as runtime side-effects.
8. New translation keys present in `messages.pot`, `en.po`, `ru.po`.
9. `dotnet build` succeeds with **0 errors**.

---

## Context you must know before editing

Read these facts. Do not assume otherwise.

- **Two layers, two namespaces.**
  - Persistence record `GameSettings`: `Scripts/Service/Settings/GameSettings.cs`, namespace `NeonWarfare.Scripts.Service.Settings`. It is a **positional record** (`public record GameSettings(...)`). Adding fields = add to the record header AND to `GetDefault()`.
  - Menu model `MenuGameSettings`: a `partial class` split across **two** files in `Scenes/Screen/NewMenu/SettingsSystem/` — `MenuGameSettings.cs` (fields, ctor, `Validate()`) and `GameSettingsBase.cs` (serialization + reflection, despite the misleading filename; **it is not a base class**). Namespace `NeonWarfare.Scenes.Screen.NewMenu.SettingsSystem`.
- **Field-name mismatch already exists:** record has `PlayerNick`, menu has `PlayerName`. `MenuGameSettingsService.Convert` remaps by hand. You must preserve this.
- **`Locale` is intentionally absent from `MenuGameSettings`.** Do NOT add it in Phase 1.
- **Attributes** (`Scenes/Screen/NewMenu/SettingsSystem/SettingAttributes.cs`), all `AttributeTargets.Property | AttributeTargets.Field`:
  - `NameAttribute(string name)`
  - `HintAttribute(string hint)`
  - `RangeAttribute(double min, double max)`
  - `StepAttribute(double step)`
  - `HideAttribute()` — parameterless.
  - There is **no** `[Category]` and **no** `[Options]`. You add them.
- **Reflection/dispatch facts:**
  - `Setting` (record, `Setting.cs`) exposes `Member` (an `IMemberAccessor`), `Type`, `Value` (mutable `{ get; set; }`), `Name`, `Hint`, `Target`.
  - `IMemberAccessor` extends `IBaseMemberInfo`, which provides `GetAttribute<TAttribute>()`, `HasAttribute<T>()`, `TryGetAttribute<T>(out T)`, and `Member` (the `System.Reflection.MemberInfo`).
  - `VisibleAccessors` (in `GameSettingsBase.cs`, inside the `file static class GameSettingsInternals`) is **cached statically**. Attributes are read at scan time — adding `[Category]` to existing properties is fine; just don't expect new properties to appear if added at runtime (not our case).
  - `Configurators.GetFor(Type)` is **exact-Type** lookup; missing types throw `KeyNotFoundException`.
  - `SettingContainer._Ready()` (file `Scenes/Screen/NewMenu/MainMenu/Pages/Settings/SettingContainer.cs`) currently dispatches purely by type at this line:
    ```csharp
    _inputControl = Configurators.GetFor(Handle.Type).GetControl(this);
    ```
    You will branch on `[Options]` **before** this line.
- **JSON:** menu layer uses a custom `JsonSerializerOptions` (indented, with `ColorJsonConverter`). Record (persistence) layer uses default options. You do not change serialization options in Phase 1; new simple fields (int/bool/string) serialize fine in both.
- **Locale convention:** `SECTION__KEY`. This section is **`SETTING_MENU__`** (singular `SETTING`, not `SETTINGS`). Every new key must go into **all three** files: `Assets/Locales/messages.pot` (msgstr empty), `Assets/Locales/en.po`, `Assets/Locales/ru.po`. `Tr()` returns the input string unchanged when no translation is found — this is relied upon for `[Options]` raw values.

---

## Task 1.1 — Extend `GameSettings` record

**File:** `Scripts/Service/Settings/GameSettings.cs`

### 1.1a — Add 7 positional fields to the record header

Append these after the existing `bool AutoSaveEnabled` (keep the existing 5 fields and their order **unchanged** above them):

```csharp
bool PlayerSettingsAcknowledged,
int MasterVolume,
int SoundsVolume,
int MusicVolume,
bool Fullscreen,
string Resolution,
string InterfaceSize
```

### 1.1b — Update `GetDefault()`

Add matching named arguments after `AutoSaveEnabled: true`:

```csharp
PlayerSettingsAcknowledged: false,
MasterVolume: 100,
SoundsVolume: 100,
MusicVolume: 100,
Fullscreen: false,
Resolution: "1280x720",
InterfaceSize: "Medium"
```

### 1.1c — Find and fix every `new GameSettings(...)` call

The record is positional, so every direct construction must pass the new fields. Run:

```bash
grep -rn "new GameSettings(" --include=*.cs .
```

Expected hits and required action:
- `Scripts/Service/Settings/MenuGameSettingsService.cs` → `Convert(MenuGameSettings)` → update in Task 1.5.
- `Scripts/Service/Settings/GameSettings.cs` → `GetDefault()` → already updated in 1.1b.
- Any **test** or other file → pass sensible values (use the same defaults as `GetDefault()`). If unsure, ask; do not guess silently.

`GameSettingsService` uses `_settings with { ... }` (not `new`), so it is unaffected — new fields carry over via `with`.

---

## Task 1.2 — Add `[Category]` and `[Options]` attributes

**File:** `Scenes/Screen/NewMenu/SettingsSystem/SettingAttributes.cs`

Add these two attribute classes (same file, same namespace `NeonWarfare.Scenes.Screen.NewMenu.SettingsSystem`, same `AttributeTargets.Property | AttributeTargets.Field` usage as the others):

```csharp
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class CategoryAttribute : Attribute
{
    public string Category { get; }
    public CategoryAttribute(string category)
    {
        Category = category;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class OptionsAttribute : Attribute
{
    public string[] Options { get; }
    public OptionsAttribute(params string[] options)
    {
        Options = options;
    }
}
```

`params string[]` lets you write `[Options("Small", "Medium", "Large")]`.

---

## Task 1.3 — Extend `MenuGameSettings` fields (the fields partial)

**File:** `Scenes/Screen/NewMenu/SettingsSystem/MenuGameSettings.cs`

### 1.3a — Add `[Category]` to existing properties

The existing 4 visible properties currently have no category. Without a category they will disappear from every categorized view, so **every visible property must get a `[Category]`**. Apply:

- `PlayerName` → add `[Category("Player")]`
- `PlayerColor` → add `[Category("Player")]`
- `PlayerUid` → add `[Category("Player")]`
- `AutoSaveEnabled` → add `[Category("Player")]`

Put `[Category]` on its own line above `[Name]`. Example of the resulting shape:

```csharp
[Category("Player")]
[Name("SETTING_MENU__NICK")]
[Hint("SETTING_MENU__NICK_HINT")]
public string PlayerName { get; set; } = GameSettings.GetDefault().PlayerNick;
```

### 1.3b — Add the 7 new properties

Add them grouped by category. Use the **exact** locale keys shown (they are created in Task 1.7). Defaults come from `GameSettings.GetDefault()`.

**Player — the gate flag (HIDDEN, not user-editable):**

```csharp
[Category("Player")]
[Hide]
public bool PlayerSettingsAcknowledged { get; set; } = GameSettings.GetDefault().PlayerSettingsAcknowledged;
```

`[Hide]` removes it from `VisibleAccessors` entirely, so no UI row is rendered and no category filtering is needed for it.

**Audio (int 0–100, slider via `[Range]`):**

```csharp
[Category("Audio")]
[Name("SETTING_MENU__MASTER_VOLUME")]
[Hint("SETTING_MENU__MASTER_VOLUME_HINT")]
[Range(0, 100)]
public int MasterVolume { get; set; } = GameSettings.GetDefault().MasterVolume;

[Category("Audio")]
[Name("SETTING_MENU__SOUNDS_VOLUME")]
[Hint("SETTING_MENU__SOUNDS_VOLUME_HINT")]
[Range(0, 100)]
public int SoundsVolume { get; set; } = GameSettings.GetDefault().SoundsVolume;

[Category("Audio")]
[Name("SETTING_MENU__MUSIC_VOLUME")]
[Hint("SETTING_MENU__MUSIC_VOLUME_HINT")]
[Range(0, 100)]
public int MusicVolume { get; set; } = GameSettings.GetDefault().MusicVolume;
```

**Graphics:**

```csharp
[Category("Graphics")]
[Name("SETTING_MENU__FULLSCREEN")]
[Hint("SETTING_MENU__FULLSCREEN_HINT")]
public bool Fullscreen { get; set; } = GameSettings.GetDefault().Fullscreen;

[Category("Graphics")]
[Name("SETTING_MENU__RESOLUTION")]
[Hint("SETTING_MENU__RESOLUTION_HINT")]
[Options("1280x720", "1366x768", "1600x900", "1920x1080", "2560x1440", "3840x2160")]
public string Resolution { get; set; } = GameSettings.GetDefault().Resolution;
```

**Interface:**

```csharp
[Category("Interface")]
[Name("SETTING_MENU__INTERFACE_SIZE")]
[Hint("SETTING_MENU__INTERFACE_SIZE_HINT")]
[Options("Small", "Medium", "Large")]
public string InterfaceSize { get; set; } = GameSettings.GetDefault().InterfaceSize;
```

> **Design decision (read this).** The master plan worded `[Options]` as "translation keys". Here we instead pass **raw values** (`"1280x720"`, `"Medium"`, …). The stored value **and** the displayed text are the same raw string; `Tr()` is applied for display but is a no-op when no translation key matches, so untranslated literals render verbatim. This keeps one source of truth (no key↔value mapping table) and is correct for self-describing values like resolutions. If localization of "Small/Medium/Large" is later required, add a separate display-label mechanism in a later phase — do not block Phase 1 on it.

### 1.3c — Update the parameterized constructor and `Validate()`

The existing constructor takes 4 named params. **Extend it** with the 7 new params in the same order as the record, assigning each to its property. Keep the parameterless constructor as-is. Example skeleton (fill all 7):

```csharp
public MenuGameSettings(
    string playerName, Color playerColor, string playerUid, bool autoSaveEnabled,
    bool playerSettingsAcknowledged, int masterVolume, int soundsVolume, int musicVolume,
    bool fullscreen, string resolution, string interfaceSize)
{
    PlayerName = playerName;
    PlayerColor = playerColor;
    PlayerUid = playerUid;
    AutoSaveEnabled = autoSaveEnabled;
    PlayerSettingsAcknowledged = playerSettingsAcknowledged;
    MasterVolume = masterVolume;
    SoundsVolume = soundsVolume;
    MusicVolume = musicVolume;
    Fullscreen = fullscreen;
    Resolution = resolution;
    InterfaceSize = interfaceSize;
}
```

Extend `Validate()` with null-coalesces for the two new string fields (numbers/bools need none):

```csharp
public void Validate()
{
    PlayerName ??= GameSettings.GetDefault().PlayerNick;
    Resolution ??= GameSettings.GetDefault().Resolution;
    InterfaceSize ??= GameSettings.GetDefault().InterfaceSize;
}
```

---

## Task 1.4 — Category-filtered `GetVisibleSettings` / `SetVisibleSettings`

**File:** `Scenes/Screen/NewMenu/SettingsSystem/GameSettingsBase.cs` (the reflection partial of `MenuGameSettings`).

Add these two overloads. Place them next to the existing parameterless `GetVisibleSettings()` / `SetVisibleSettings()`. **Do not** modify the existing parameterless versions — later code and the current `SettingsPage` rely on them meaning "all visible".

```csharp
public IReadOnlyList<Setting> GetVisibleSettings(string category)
{
    return GetVisibleSettings()
        .Where(setting => setting.Member.GetAttribute<CategoryAttribute>()?.Category == category)
        .ToList();
}

public void SetVisibleSettings(IReadOnlyList<Setting> settings, string category)
{
    // The list passed in is already category-filtered; applying is the same as the
    // parameterless version. The category param exists for API symmetry/call-site clarity.
    SetVisibleSettings(settings);
}
```

`setting.Member` is an `IMemberAccessor` which has `GetAttribute<T>()` via `IBaseMemberInfo`. The file already has `using System.Linq;`.

---

## Task 1.5 — `SettingContainer` renders `[Options]` as an `OptionButton`

**File:** `Scenes/Screen/NewMenu/MainMenu/Pages/Settings/SettingContainer.cs`

### 1.5a — Branch on `[Options]` before type dispatch

In `_Ready()`, replace this single line:

```csharp
_inputControl = Configurators.GetFor(Handle.Type).GetControl(this);
```

with:

```csharp
var optionsAttr = Handle.Member.GetAttribute<OptionsAttribute>();
_inputControl = optionsAttr is not null
    ? BuildOptionControl(this, optionsAttr.Options)
    : Configurators.GetFor(Handle.Type).GetControl(this);
```

`Handle.Member` is `IMemberAccessor` (exposes `GetAttribute<T>()`). The file already `using`s the settings namespace (it references `Configurators`, `SettingContainerConfigurator`, etc.); confirm `OptionsAttribute` resolves — it is in `NeonWarfare.Scenes.Screen.NewMenu.SettingsSystem`, and `SettingContainer.cs` already has `using NeonWarfare.Scenes.Screen.NewMenu.SettingsSystem;`.

### 1.5b — Add the `BuildOptionControl` helper

Add this `private static` method on the `SettingContainer` class:

```csharp
private static Control BuildOptionControl(SettingContainer container, string[] options)
{
    var optionButton = new OptionButton();
    var currentValue = container.Handle.Value?.ToString() ?? "";

    for (int i = 0; i < options.Length; i++)
    {
        optionButton.AddItem(Tr(options[i]));
        if (options[i] == currentValue)
        {
            optionButton.Select(i);
        }
    }

    // If the current value is not among the options, force it to the first option so the
    // control and the stored value stay consistent on first interaction.
    if (optionButton.Selected == -1 && options.Length > 0)
    {
        optionButton.Select(0);
        container.Handle.Value = options[0];
    }

    optionButton.ItemSelected += index => container.Handle.Value = options[(int)index];
    return optionButton;
}
```

Notes:
- `SettingContainer` extends `PanelContainer` (a `Node`/`CanvasItem`), so the inherited `Tr(string)` is available — no qualifier needed.
- `ItemSelected` passes `long`; cast to `int` before indexing.
- The input control is still sized by the existing `_inputControl.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;` / `CustomMinimumSize = new Vector2(300, 20);` lines that follow in `_Ready()`. Do not remove them.

---

## Task 1.6 — `MenuGameSettingsService`: Convert both ways + runtime side-effects

**File:** `Scripts/Service/Settings/MenuGameSettingsService.cs`

Add `using Godot;` at the top (needed for `DisplayServer`, `AudioServer`, `Mathf`).

### 1.6a — `Convert(GameSettings)` → `MenuGameSettings`

Pass the 7 new fields through (the constructor was extended in 1.3c). Keep the existing `PlayerName ↔ PlayerNick` remap and the existing fields exactly:

```csharp
private MenuGameSettings Convert(GameSettings gameSettings)
{
    return new MenuGameSettings(
        playerName: gameSettings.PlayerNick,
        playerColor: gameSettings.PlayerColor,
        playerUid: gameSettings.PlayerUid,
        autoSaveEnabled: gameSettings.AutoSaveEnabled,
        playerSettingsAcknowledged: gameSettings.PlayerSettingsAcknowledged,
        masterVolume: gameSettings.MasterVolume,
        soundsVolume: gameSettings.SoundsVolume,
        musicVolume: gameSettings.MusicVolume,
        fullscreen: gameSettings.Fullscreen,
        resolution: gameSettings.Resolution,
        interfaceSize: gameSettings.InterfaceSize
    );
}
```

### 1.6b — `Convert(MenuGameSettings)` → `GameSettings`

The record is positional; pass all 12 fields. Preserve the existing behavior where `Locale` is taken from the currently-loaded settings (the menu does not edit it):

```csharp
private GameSettings Convert(MenuGameSettings menuGameSettings)
{
    return new GameSettings(
        PlayerUid: menuGameSettings.PlayerUid,
        PlayerNick: menuGameSettings.PlayerName,
        PlayerColor: menuGameSettings.PlayerColor,
        Locale: Services.GameSettings.GetSettings().Locale,
        AutoSaveEnabled: menuGameSettings.AutoSaveEnabled,
        PlayerSettingsAcknowledged: menuGameSettings.PlayerSettingsAcknowledged,
        MasterVolume: menuGameSettings.MasterVolume,
        SoundsVolume: menuGameSettings.SoundsVolume,
        MusicVolume: menuGameSettings.MusicVolume,
        Fullscreen: menuGameSettings.Fullscreen,
        Resolution: menuGameSettings.Resolution,
        InterfaceSize: menuGameSettings.InterfaceSize
    );
}
```

> **Field order in the record matters.** Make sure the order of named arguments matches the order of fields declared in the `GameSettings` record header (Task 1.1a). Named arguments protect you if order is wrong, but keep them in declaration order for readability.

### 1.6c — `ApplySettings`: add runtime side-effects

After the existing `Services.I18N.SetCurrentLocale(gameSettings.Locale);` line, call a new helper:

```csharp
ApplyRuntimeSettings(gameSettings);
```

Add the helper and its two private statics:

```csharp
private void ApplyRuntimeSettings(GameSettings gameSettings)
{
    // Fullscreen toggle
    var mode = gameSettings.Fullscreen
        ? DisplayServer.WindowMode.Fullscreen
        : DisplayServer.WindowMode.Windowed;
    DisplayServer.WindowSetMode(mode);

    // Window size from "WxH" (only applies in windowed mode; harmless otherwise)
    if (TryParseResolution(gameSettings.Resolution, out int width, out int height))
    {
        DisplayServer.WindowSetSize(width, height);
    }

    // Bus volumes. "Master" always exists by default; "Sounds"/"Music" may not until
    // created in a later phase — guard with GetBusIndex >= 0.
    SetBusVolume("Master", gameSettings.MasterVolume);
    SetBusVolume("Master", gameSettings.MasterVolume); // see note below; replace with real buses when they exist
    SetBusVolume("Sounds", gameSettings.SoundsVolume);
    SetBusVolume("Music", gameSettings.MusicVolume);
}

private static bool TryParseResolution(string resolution, out int width, out int height)
{
    width = 0;
    height = 0;
    if (string.IsNullOrWhiteSpace(resolution)) return false;
    var parts = resolution.Split('x');
    if (parts.Length != 2) return false;
    return int.TryParse(parts[0], out width) && int.TryParse(parts[1], out height) && width > 0 && height > 0;
}

private static void SetBusVolume(string busName, int volume0to100)
{
    int busIndex = AudioServer.GetBusIndex(busName);
    if (busIndex < 0) return; // bus doesn't exist yet — silently skip

    if (volume0to100 <= 0)
    {
        AudioServer.SetBusMute(busIndex, true);
        return;
    }

    AudioServer.SetBusMute(busIndex, false);
    AudioServer.SetBusVolumeDb(busIndex, (float)Mathf.LinearToDb(volume0to100 / 100.0));
}
```

> **Remove the accidental duplicate** `SetBusVolume("Master", ...)` line shown twice above — that is a copy-paste hazard warning, not intended code. The final method must call `Master`/`Sounds`/`Music` exactly once each.

`Mathf.LinearToDb` is `Godot.Mathf.LinearToDb`. `DisplayServer` and `AudioServer` are static Godot classes. All resolve once `using Godot;` is present.

> **Startup wiring is NOT in Phase 1 scope.** `ApplySettings` only runs when a page saves/cancels. Applying saved settings on game launch is a later concern — do not add it here.

---

## Task 1.7 — Translation keys

Add **every** key below to **all three** files: `Assets/Locales/messages.pot`, `Assets/Locales/en.po`, `Assets/Locales/ru.po`. Follow the existing format exactly (blank line between entries). In `messages.pot` the `msgstr` is always empty.

| msgid | en msgstr | ru msgstr |
|---|---|---|
| `SETTING_MENU__MASTER_VOLUME` | `Master volume:` | `Общая громкость:` |
| `SETTING_MENU__MASTER_VOLUME_HINT` | `Master volume for all audio` | `Общая громкость всего звука` |
| `SETTING_MENU__SOUNDS_VOLUME` | `Sounds volume:` | `Громкость звуков:` |
| `SETTING_MENU__SOUNDS_VOLUME_HINT` | `Volume of gameplay sound effects` | `Громкость звуковых эффектов` |
| `SETTING_MENU__MUSIC_VOLUME` | `Music volume:` | `Громкость музыки:` |
| `SETTING_MENU__MUSIC_VOLUME_HINT` | `Volume of background music` | `Громкость фоновой музыки` |
| `SETTING_MENU__FULLSCREEN` | `Fullscreen:` | `Полноэкранный режим:` |
| `SETTING_MENU__FULLSCREEN_HINT` | `Run the game in fullscreen` | `Запускать игру в полноэкранном режиме` |
| `SETTING_MENU__RESOLUTION` | `Resolution:` | `Разрешение:` |
| `SETTING_MENU__RESOLUTION_HINT` | `Screen resolution (windowed mode)` | `Разрешение экрана (оконный режим)` |
| `SETTING_MENU__INTERFACE_SIZE` | `Interface size:` | `Размер интерфейса:` |
| `SETTING_MENU__INTERFACE_SIZE_HINT` | `Scale of UI elements` | `Масштаб элементов интерфейса` |

Place each entry near the existing `SETTING_MENU__*` block (after `SETTING_MENU__SAVE_BUTTON` is fine) to keep related keys together. Do **not** add a key for `PlayerSettingsAcknowledged` — it is `[Hide]`.

---

## Task 1.8 — Verify

1. Re-run the `new GameSettings(` grep from 1.1c and confirm every hit compiles (all 12 fields supplied where positional, or via `with`).
2. Build from the repository root:
   ```bash
   dotnet build
   ```
   Expect: **0 errors**. Warnings are acceptable but read them — a `CS0618`/obsolete or `CS0162`/unreachable-code warning usually means a typo in an API name (e.g. wrong `DisplayServer` method).
3. Sanity-check reflection: after a successful build, the implementer is **not** required to run the game in Phase 1, but should confirm no compile-time dependency on a bus named "Sounds"/"Music" exists (the guard `busIndex < 0` makes absence safe at runtime).

---

## Out of scope for Phase 1 (do NOT do)

- Creating any page under `Pages/` (that is Phase 3/5/7).
- Building `SettingsHubPage` or category pages (Phase 7).
- The `KnownServersService` (Phase 2).
- The custom `ColorPickerPanel` (Phase 6) — leave the existing Color configurator as-is.
- Removing `SettingsPage` or `ConnectPage` (Phase 4/7).
- Wiring `ApplySettings` to run on startup.
- Actually creating audio buses named "Sounds"/"Music" in the project — guarded skip is enough for now.

---

## Gotchas recap

- `GameSettingsBase.cs` is a **partial**, not a base class. Edit it as part of `MenuGameSettings`.
- `PlayerNick` (record) vs `PlayerName` (menu) — keep the remap in `Convert`.
- `Locale` is deliberately not in the menu — keep pulling it from `Services.GameSettings.GetSettings().Locale` on the menu→record path.
- `VisibleAccessors` is cached statically; this is fine because all our attribute changes are on properties present at first scan.
- `Configurators.GetFor(Type)` is exact-Type only — that's exactly why `[Options]` is handled in `SettingContainer._Ready()` **before** type dispatch, not as a new dictionary entry.
- Positional record field **order** must stay consistent across the record header, `GetDefault()`, and every `new GameSettings(...)`.
- `[Category]` is mandatory on every visible property; an un-categorized visible property vanishes from all categorized views (Phases 7 will rely on this).
- `Tr()` is identity for untranslated strings — that is why raw `[Options]` values display correctly without keys.
