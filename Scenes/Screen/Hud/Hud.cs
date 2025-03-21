using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.Screen.BattleHud;
using NeonWarfare.Scenes.Screen.Components.TwoColoredBar;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Entities.Characters;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.Content.Skills;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Godot.Services;

namespace NeonWarfare.Scenes.Screen;

public partial class Hud : Control
{
	[ExportGroup("Bars")]
	[Export] [NotNull] public TwoColoredBar HpBar { get; private set; }
	
	[ExportGroup("FPS & TPS")]
	[Export] [NotNull] public Label Fps { get; private set; }
	[Export] [NotNull] public Label Tps { get; private set; }
	
	[ExportGroup("Bottom part")]
	[Export] [NotNull] public HBoxContainer SkillsContainer { get; private set; }
	
	[ExportGroup("General")]
	[Export] [NotNull] public DeathOverlay DeathOverlay { get; private set; }
	[Export] [NotNull] public Control DamageOverlay { get; private set; }
	[Export] [NotNull] public ChatContainer ChatContainer { get; private set; }
	
	
	private readonly Stopwatch _physicsStopwatch = new();
	private readonly Queue<double> _deltas = new();
	private bool _isPlayerInitialized;
	private float _deathOverlayAnimationTime = 2f;
	private float _deathEffectStrength;
	private float _previousDeathEffectStrength = -1;
	
	private float _damageOverlayFadeTime = 0.3f;
	private float _damageEffectStrength;

	private float[] _deathEqGains = [-6f, 3f, 2f, 0.1f, -1.1f, -8.4f, -12.1f, -14.5f, -16f, -20f];
	public virtual ClientPlayer GetCurrentPlayer()
	{
		return GetCurrentWorld().Player;
	}

	public virtual ClientWorld GetCurrentWorld()
	{
		throw new NotSupportedException();
	}
	
	public override void _Process(double delta)
	{
		Fps.Text = $"FPS: {Engine.GetFramesPerSecond():N0}";
		
		ClientPlayer player = GetCurrentPlayer();
		if (player is null) return;
		player.PlayerTakingDamage += OnPlayerTakingDamage;
		
		InitHudForPlayer();
		
		HpBar.CurrentUpperValue = (float) player.Hp;
		double hpBarValueDelta = Mathf.Clamp(HpBar.CurrentLowerValue - HpBar.CurrentUpperValue, 
			0, Math.Max(HpBar.MaxValue - HpBar.CurrentUpperValue, 0));
		double hpBarValueDeltaDecrease = HpBar.MaxValue * 0.25 * delta;
		HpBar.CurrentLowerValue = (float) (player.Hp + hpBarValueDelta - hpBarValueDeltaDecrease);
        
		HpBar.MaxValue = (float) player.MaxHp;
		HpBar.Label.Text = $"Health: {player.Hp:N0} / {player.MaxHp:N0}";
		Fps.Text = $"FPS: {Engine.GetFramesPerSecond():N0}";

		if (player.IsDead)
		{
			_deathEffectStrength += (float)delta / _deathOverlayAnimationTime;
		}
		else
		{
			_deathEffectStrength -= (float)delta / _deathOverlayAnimationTime;
		}
		
		_deathEffectStrength = Math.Clamp(_deathEffectStrength, 0, 1);
		
		// ReSharper disable once CompareOfFloatsByEqualityOperator
		if (_deathEffectStrength != _previousDeathEffectStrength)
		{
			DeathOverlay.SetStrength(_deathEffectStrength);
			AdjustSoundMuffleStrength(_deathEffectStrength);
		}
		
		_previousDeathEffectStrength = _deathEffectStrength;
		
		_damageEffectStrength -= (float)delta / _damageOverlayFadeTime;
		_damageEffectStrength = Math.Clamp(_damageEffectStrength, 0, 1);
		DamageOverlay.Modulate = DamageOverlay.Modulate with { A = _damageEffectStrength };
	}
	
	public override void _PhysicsProcess(double delta)
	{
		var realDelta = _physicsStopwatch.Elapsed.TotalSeconds;
        
		_deltas.Enqueue(realDelta);
		if (_deltas.Count >= 120)
		{
			var tps = _deltas.Average();
			Tps.Text = $"TPS: {1/tps:N0}";
			_deltas.Dequeue();
		}
		_physicsStopwatch.Restart();
	}

	public override void _ExitTree()
	{
		AdjustSoundMuffleStrength(0);
	}

	public override void _Input(InputEvent @event)
	{
		
	}

	private void OnPlayerTakingDamage(ClientCharacter.SC_DamageCharacterPacket damageCharacterPacket)
	{
		Callable.From(() =>
		{
			var player = GetCurrentPlayer();
			return _damageEffectStrength = 1 - (float)(player.Hp / player.MaxHp);
		}).CallDeferred();
	}
	
	private void AdjustSoundMuffleStrength(float strenght)
	{
		const float additionalStrenght = 2f;
		var effect = AudioServer.GetBusEffect(Audio2D.MasterIndex, 0);
		if (effect is AudioEffectEQ10 eq)
		{
			for (int i = 0; i < _deathEqGains.Length; i++)
			{
				eq.SetBandGainDb(i, _deathEqGains[i] * strenght * additionalStrenght);
			}
		}
	}

	private void InitHudForPlayer()
	{
		if(_isPlayerInitialized)
			return;
		
		_isPlayerInitialized = true;
		
		var skills = GetCurrentPlayer().GetSkills();
		foreach (var skillHandle in skills)
		{
			var icon = CreateIconForSkill(skillHandle);
			SkillsContainer.AddChild(icon);
		}
	}

	private Control CreateIconForSkill(ClientPlayerSkillHandle skillHandle)
	{
		var icon = ClientRoot.Instance.PackedScenes.SkillIcon.Instantiate() as Icon;
		icon.SetHandle(skillHandle);
		
		return icon;
	}
}
