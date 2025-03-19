using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.Screen.BattleHud;
using NeonWarfare.Scenes.Screen.Components.TwoColoredBar;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scripts.Content.Skills;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Core;

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
	
	
	private readonly Stopwatch _physicsStopwatch = new();
	private readonly Queue<double> _deltas = new();
	private bool _isPlayerInitialized;
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
		
		InitHudForPlayer();
		
		HpBar.CurrentUpperValue = (float) player.Hp;
		double hpBarValueDelta = Mathf.Clamp(HpBar.CurrentLowerValue - HpBar.CurrentUpperValue, 
			0, Math.Max(HpBar.MaxValue - HpBar.CurrentUpperValue, 0));
		double hpBarValueDeltaDecrease = HpBar.MaxValue * 0.25 * delta;
		HpBar.CurrentLowerValue = (float) (player.Hp + hpBarValueDelta - hpBarValueDeltaDecrease);
        
		HpBar.MaxValue = (float) player.MaxHp;
		HpBar.Label.Text = $"Health: {player.Hp:N0} / {player.MaxHp:N0}";
		Fps.Text = $"FPS: {Engine.GetFramesPerSecond():N0}";
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
