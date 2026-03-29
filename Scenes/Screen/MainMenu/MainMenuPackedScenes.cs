using Godot;
using KludgeBox.DI.Requests.NotNullCheck;
using NeonWarfare.Scenes.KludgeBox;

namespace NeonWarfare.Scenes.Screen.MainMenu;

public partial class MainMenuPackedScenes : CheckedAbstractStorage
{
    
    [Export] [NotNull] public PackedScene Main { get; private set; }
    [Export] [NotNull] public PackedScene StartSingleplayer { get; private set; }
    [Export] [NotNull] public PackedScene CreateServer { get; private set; }
    [Export] [NotNull] public PackedScene ConnectToServer { get; private set; }
    [Export] [NotNull] public PackedScene Settings { get; private set; }
    [Export] [NotNull] public PackedScene Message { get; private set; }
}