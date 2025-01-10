using System.Collections.Generic;
using Godot;
using KludgeBox;
using KludgeBox.Events.Global;

namespace NeonWarfare;

public partial class ClientBattleWorld : ClientWorld
{
	
	public override void _Ready()
	{
		base._Ready();
        PlayBattleMusic();
	}
}