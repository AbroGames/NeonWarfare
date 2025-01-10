namespace NeonWarfare;

public partial class ClientSafeWorld : ClientWorld
{
	
	private void PlaySafeMusic()
	{
		var music = Audio2D.PlayMusic(Music.MainBgm, 0.75f);
		music.Finished += PlaySafeMusic; //TODO бессконечная рекурсия?
	}
}