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
    
    
}