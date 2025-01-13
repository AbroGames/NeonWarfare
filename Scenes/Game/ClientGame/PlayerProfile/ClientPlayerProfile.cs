using System;
using NeonWarfare;

public class ClientPlayerProfile
{
    public long Id { get; private set; }
    public long UserId { get; set; }
    public string Nickname { get; set; } = "";
    public bool IsAdmin { get; set; } = false;

    public ClientMyPlayer MyPlayer => ClientRoot.Instance.Game.World.MyPlayer;
    //TODO может быть хранить всех союзников тоже? Тогда создание ServerPlayer, ClientPlayer и ClientMyPlayer будут схожи

    public ClientPlayerProfile(long id)
    {
        Id = id;
    }
}