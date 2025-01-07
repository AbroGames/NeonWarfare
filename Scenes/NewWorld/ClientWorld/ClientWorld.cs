using System;
using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class ClientWorld : Node2D
{
    public Floor Floor { get; set; }
    
    public NetworkEntityManager NetworkEntityManager { get; private set; } = new();
    public Player Player { get; private set; } //TODO ClientPlayer

    public override void _EnterTree()
    {
        Floor = new Floor();
        Floor.Texture = ImageTexture.CreateFromImage(Image.LoadFromFile(Sprites.Floor));
        AddChild(Floor);
    }

    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }
    
    public Player CreateAndAddPlayer(ClientPlayerProfile playerProfile)
    {
        if (Player != null)
        {
            throw new ArgumentException("Player already exists.");
        }

        Player player = ClientRoot.Instance.PackedScenes.Player.Instantiate<Player>(); //TODO ClientPlayer special ~~constructor~~ static builder, based on playerProfile
        Player = player;
        AddChild(player);
        return player;
    }

    public void RemovePlayer()
    {
        Player.QueueFree();
        Player = null;
    }
}