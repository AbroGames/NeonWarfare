using System;
using System.Collections;
using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using NeonWarfare.Utils.Cooldown;

namespace NeonWarfare;

public partial class ClientPlayer : ClientAlly 
{
    
    public ClientPlayerProfile PlayerProfile { get; private set; }
    
    public void InitOnProfile(ClientPlayerProfile playerProfile)
    {
        base.InitOnProfile(playerProfile);
        PlayerProfile = playerProfile;
    }
    
    //TODO del
    private AutoCooldown log;
    public override void _Ready()
    {
        base._Ready();
        log = new AutoCooldown(5, true, Log);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        log.Update(delta);
    }

    private void Log()
    {
        ClientWorld world = ClientRoot.Instance.Game.World;
        ClientGame game = ClientRoot.Instance.Game;

        string str;
        str = $"Game.ally id: {getStr(game.AllyProfiles.Select(ally => ally.PeerId).ToArray())}; Game.Player id: {game.PlayerProfile.PeerId}";
        global::KludgeBox.Log.Info(str);
        str = $"Game.ally link: {getStr(game.AllyProfiles.Select(ally => ally.Ally.Nid).ToArray())}; Game.Player link: {game.PlayerProfile.Player.Nid}";
        global::KludgeBox.Log.Info(str);
        str = $"World.ally link: {getStr(world.Allies.Select(ally => ally.Nid).ToArray())}; World.Player link: {world.Player.Nid}";
        global::KludgeBox.Log.Info(str);
        str = $"World.ally id: {getStr(world.Allies.Select(ally => ally.AllyProfile.PeerId).ToArray())}; World.Player id: {world.Player.PlayerProfile.PeerId}";
        global::KludgeBox.Log.Info(str);
    }

    private string getStr(long[] objets)
    {
        return "[" + String.Join(", ", objets) + "]";
    }
}