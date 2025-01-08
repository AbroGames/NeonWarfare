using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Networking;
using NeonWarfare;

public partial class ClientGame
{
    public ClientPlayerProfile PlayerProfile { get; private set; }

    public void AddPlayerProfile(long id)
    {
        if (PlayerProfile != null)
        {
            throw new ArgumentException("Player already exists.");
        }
        
        PlayerProfile = new ClientPlayerProfile(id);
    }

}