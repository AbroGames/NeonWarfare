using KludgeBox;
using KludgeBox.Events;

namespace NeoVector;

[GameService]
public class SafeWorldService
{
    
    [EventListener]
    public void OnSafeWorldReadyEvent(SafeWorldReadyEvent safeWorldReadyEvent)
    {
        SafeWorld safeWorld = safeWorldReadyEvent.SafeWorld;
        Root.Instance.CurrentWorld = safeWorld;
        
        PlaySafeMusic(); //TODO to music service (safe music service)
    }
    
    [EventListener]
    public void OnSafeWorldProcessEvent(SafeWorldProcessEvent safeWorldProcessEvent) { }
    
    public void PlaySafeMusic()
    {
        var music = Audio2D.PlayMusic(Music.MainBgm, 0.75f);
        music.Finished += PlaySafeMusic;
    }
}