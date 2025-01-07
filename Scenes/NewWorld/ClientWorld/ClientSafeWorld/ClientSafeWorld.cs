namespace NeonWarfare;

public partial class ClientSafeWorld : ClientWorld
{
	
	public override void _Ready()
	{
		base._Ready();
		PlaySafeMusic();
	}
	
	private void PlaySafeMusic()
	{
		var music = Audio2D.PlayMusic(Music.MainBgm, 0.75f);
		music.Finished += PlaySafeMusic;
	}
}