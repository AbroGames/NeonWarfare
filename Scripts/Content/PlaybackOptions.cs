namespace NeonWarfare.Scripts.Content;

public record PlaybackOptions(string Path, 
    float Volume,
    float MaxDistance = 3000f,
    float PanningStrength = 2f,
    float Attenuation = 1f,
    float PitchScale = 1f
);