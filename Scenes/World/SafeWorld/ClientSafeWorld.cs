namespace NeonWarfare;

public partial class ClientSafeWorld : World
{
	public SafeHud SafeHud { get; set; }
	
	public override void _Ready()
	{
		base._Ready();
		PlaySafeMusic(); //TODO to music service (safe music service)
	}
	
	private void PlaySafeMusic()
	{
		var music = Audio2D.PlayMusic(Music.MainBgm, 0.75f);
		music.Finished += PlaySafeMusic;
	}
}