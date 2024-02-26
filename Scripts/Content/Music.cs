using KludgeBox.Collections;

namespace NeoVector;

public static class Music
{
    public const string SoundsDir = "res://Assets/Audio/Music";
    
    public static RandomPicker<string> WorldBgm1 { get; } = new RandomPicker<string>(
        $"{SoundsDir}/bgm1.mp3"
    );
    public static RandomPicker<string> WorldBgm2 { get; } = new RandomPicker<string>(
        $"{SoundsDir}/bgm2.mp3"
    );
    public static RandomPicker<string> MainBgm { get; } = new RandomPicker<string>(
        $"{SoundsDir}/main_bgm.mp3"
    );
}