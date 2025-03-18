namespace NeonWarfare.Scenes.Game.ClientGame.ClientSettings;

public enum SettingType
{
    Unknown,
    
    // Checkbox
    Boolean,
    
    // Number field with up/down buttons or range slider
    Number,
    
    // Text field
    String,
    
    // Some kind of color selection
    Color,
    
    Group
}

public enum NumberInputType
{
    TextField,
    SpinBox,
    Slider
}
