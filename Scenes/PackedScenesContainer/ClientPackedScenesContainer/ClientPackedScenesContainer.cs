using System.Collections.Generic;
using Godot;
using KludgeBox;

namespace NeonWarfare.Scenes.PackedScenesContainer.ClientPackedScenesContainer;

public partial class ClientPackedScenesContainer : Node 
{
    /*
     * EFFECTS
     */
    [ExportGroup("Effects")]
    [Export] [NotNull] public PackedScene DeathFx { get; private set; }
    [Export] [NotNull] public PackedScene DebrisFx { get; private set; }
    [Export] [NotNull] public PackedScene BulletHitFx { get; private set; }
    [Export] [NotNull] public PackedScene SpawnFx { get; private set; }
    [Export] [NotNull] public PackedScene FloatingLabel { get; private set; }
    
    /*
     * MAIN SCENES
     */
    [ExportGroup("Main Scenes")]
    [Export] [NotNull] public PackedScene BattleWorldMainScene { get; private set; }
    [Export] [NotNull] public PackedScene SafeWorldMainScene { get; private set; }
    
    /*
     * SCREEN
     */
    [ExportGroup("Screen")]
    [Export] [NotNull] public PackedScene MainMenuPackedScene { get; private set; }
    
    [Export] [NotNull] public PackedScene MainMenu { get; private set; }
    [Export] [NotNull] public PackedScene SettingsMenu { get; private set; }
    [Export] [NotNull] public PackedScene PlayerSettingsMenu { get; private set; }
    [Export] [NotNull] public PackedScene GraphicSettingsMenu { get; private set; }
    [Export] [NotNull] public PackedScene SoundSettingsMenu { get; private set; }
    [Export] [NotNull] public PackedScene ConnectToServerMenu { get; private set; }
    [Export] [NotNull] public PackedScene CreateServerMenu { get; private set; }
    
    [Export] [NotNull] public PackedScene LoadingScreenCanvas { get; private set; }
    
    [Export] [NotNull] public PackedScene SafeHud { get; private set; }
    [Export] [NotNull] public PackedScene BattleHud { get; private set; }
    
    [Export] [NotNull] public PackedScene Overlay { get; private set; }
    [Export] [NotNull] public PackedScene HitOverlay { get; private set; }
    
    /*
     * WORLD
     */
    [ExportGroup("World")]
    [Export] [NotNull] public PackedScene SafeWorld { get; private set; }
    [Export] [NotNull] public PackedScene BattleWorld { get; private set; }
    [Export] [NotNull] public PackedScene Background { get; private set; }
    
    [Export] [NotNull] public PackedScene Player { get; private set; }
    [Export] [NotNull] public PackedScene Ally { get; private set; }
    [Export] [NotNull] public PackedScene Enemy { get; private set; }
    
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }
}
