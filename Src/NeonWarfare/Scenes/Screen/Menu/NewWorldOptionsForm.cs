using Godot;
using NeonWarfare.Scenes.Worlds.Features.Options;

namespace NeonWarfare.Scenes.Screen.Menu;

/// <summary>The world options controls of the new-game pages.</summary>
public static class NewWorldOptionsForm
{
    public static WorldOptions Read(CheckBox allBuildingsUnlocked, SpinBox friendlyFireMultiplier)
    {
        return new WorldOptions(
            new FixedWorldOptions(allBuildingsUnlocked: allBuildingsUnlocked.ButtonPressed),
            new AdjustableWorldOptions(friendlyFireMultiplier: friendlyFireMultiplier.Value));
    }
}
