using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.KludgeBox.Godot.Services;

namespace NeonWarfare.Scenes.World.SafeWorld.ClientSafeWorld;

public partial class ClientSafeWorld
{
	
	private void PlaySafeMusic()
	{
		/*var music = Audio2D.PlayMusic(Music.MainBgm, 0.75f);
		music.Finished += PlaySafeMusic;*/
		var playlistHandle = Audio2D.PlayMusicSequence(
			playlist: [Music.MainBgm], 
			volume: 0.75f,
			loop: true,
			target: this);
	}
}
