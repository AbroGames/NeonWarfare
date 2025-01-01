﻿using NeonWarfare;

public class ServerPlayerProfile
{
    public long Id { get; private set; }
    public long UserId { get; set; }
    public string Nickname { get; set; } = "";
    public bool IsAdmin { get; set; } = false;

    public Player Player => ServerRoot.Instance.Game.World.PlayerById[Id];

    public ServerPlayerProfile(long id)
    {
        Id = id;
    }
}