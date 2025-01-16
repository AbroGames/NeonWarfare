namespace NeonWarfare.Scenes.World.SafeWorld.ClientSafeWorld;

public partial class ClientSafeWorld
{
	
	private void PlaySafeMusic()
	{
		var music = Audio2D.PlayMusic(Music.MainBgm, 0.75f);
		music.Finished += PlaySafeMusic; //TODO бессконечная рекурсия?
	}
}
