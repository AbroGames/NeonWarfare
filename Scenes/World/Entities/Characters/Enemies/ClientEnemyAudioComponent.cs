using System;
using Godot;
using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Godot.Services;
using NeonWarfare.Scripts.Utils.Cooldown;

namespace NeonWarfare.Scenes.World.Entities.Characters.Enemies;

public partial class ClientEnemyAudioComponent : ClientEnemyComponentBase
{
    private bool _isActive;

    private AutoCooldown _tryToPlayVoiceCooldown;
    private EnemyInfoStorage.ClientEnemyAudioProfile _audioProfile;
    protected override void Initialize()
    {
        Parent = GetParent<ClientEnemy>();
        _audioProfile = Parent.EnemyTemplate.AudioProfile;
        if (_audioProfile is null)
            return;
        
        _isActive = true;
        _tryToPlayVoiceCooldown = new AutoCooldown(_audioProfile.VoicePeriod);
        _tryToPlayVoiceCooldown.Update(Rand.Range(_audioProfile.VoicePeriod));

        Parent.TreeExiting += PlayDeathSound;
        _tryToPlayVoiceCooldown.ActionWhenReady += PlayVoice;
        
        TryToPlaySound(_audioProfile.SpawnVoice);
    }

    public override void _Process(double delta)
    {
        _tryToPlayVoiceCooldown?.Update(delta);
    }

    private void PlayVoice()
    {
        if(_audioProfile.CanDoVoice(Parent))
            TryToPlaySound(_audioProfile.NormalVoice);
    }

    private void PlaySpawnVoice()
    {
        TryToPlaySound(_audioProfile.SpawnVoice);
    }
    
    private void PlayDeathSound()
    {
        TryToPlaySound(_audioProfile.DeathSfx);
        TryToPlaySound(_audioProfile.DeathVoice);
    }

    private void TryToPlaySound(Func<PlaybackOptions> pathProvider)
    {
        PlaybackOptions playbackOptions = pathProvider?.Invoke();
        if (playbackOptions is null) return;

        var path = playbackOptions.Path;
        var volume = playbackOptions.Volume;
        
        var audio = Audio2D.PlaySoundAt(path, Parent.GlobalPosition, volume, Parent.GetParent());
        audio.PanningStrength = playbackOptions.PanningStrength;
        audio.MaxDistance = playbackOptions.MaxDistance;
        audio.Attenuation = playbackOptions.Attenuation;
    }
}