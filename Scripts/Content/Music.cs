using KludgeBox.Collections;

namespace Game.Content;

public static class Music
{
    public const string SoundsDir = "res://Assets/Audio/Music";
    public static RandomPicker<string> WorldBgm { get; } = new RandomPicker<string>(
        $"{SoundsDir}/bgm1.mp3"
    );
}