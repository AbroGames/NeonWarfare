using System;
using Godot;
using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Godot.Services;
using NeonWarfare.Scripts.Utils.Cooldown;

namespace NeonWarfare.Scenes.World.Entities.Characters.Enemies;

public partial class ClientEnemyAudioComponent : Node
{
    private ClientEnemy _parent;

    private bool _isActive;

    private AutoCooldown _tryToPlayVoiceCooldown;
    private EnemyInfoStorage.ClientEnemyAudioProfile _audioProfile;
    public override void _Ready()
    {
        _parent = GetParent<ClientEnemy>();
        _audioProfile = _parent.EnemyTemplate.AudioProfile;
        if (_audioProfile is null)
            return;
        
        _isActive = true;
        _tryToPlayVoiceCooldown = new AutoCooldown(_audioProfile.VoicePeriod);
        _tryToPlayVoiceCooldown.Update(Rand.Range(_audioProfile.VoicePeriod));

        _parent.TreeExiting += PlayDeathSound;
        _tryToPlayVoiceCooldown.ActionWhenReady += PlayVoice;
        
        TryToPlaySound(_audioProfile.SpawnVoice);
    }

    public override void _Process(double delta)
    {
        _tryToPlayVoiceCooldown?.Update(delta);
    }

    private void PlayVoice()
    {
        if(_audioProfile.CanDoVoice(_parent))
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
        
        var audio = Audio2D.PlaySoundAt(path, _parent.GlobalPosition, volume, _parent.GetParent());
        audio.PanningStrength = playbackOptions.PanningStrength;
        audio.MaxDistance = playbackOptions.MaxDistance;
        audio.Attenuation = playbackOptions.Attenuation;
    }
}