using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Godot.Services;

namespace NeonWarfare.Scenes.World.BattleWorld.ClientBattleWorld;

public partial class ClientBattleWorld
{

	private void PlayBattleMusic()
	{
		if (Rand.Chance(0.5))
		{
			PlayBattleMusic1();
		}
		else
		{
			PlayBattleMusic2();
		}
	}
	
	private void PlayBattleMusic1()
	{
		var music = Audio2D.PlayMusic(Music.WorldBgm1, 0.5f);
		music.Finished += PlayBattleMusic2;
	}
    
	private void PlayBattleMusic2()
	{
		var music = Audio2D.PlayMusic(Music.WorldBgm2, 0.5f);
		music.Finished += PlayBattleMusic1;
	}
}
