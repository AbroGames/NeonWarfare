using Godot;
using NeonWarfare.Scripts.Content.Skills;
using NeonWarfare.Scripts.KludgeBox.Core;

namespace NeonWarfare.Scenes.Screen.BattleHud;

public partial class Icon : Control
{
	[Export] [NotNull] public Label KeyLabel { get; private set; }
	[Export] [NotNull] public TextureRect IconImage { get; private set; }
	[Export] [NotNull] public TextureRect Overlay { get; private set; }
	[Export] [NotNull] public TextureRect GlowRect { get; private set; }
	[Export] [NotNull] public ColorRect CooldownOverlay { get; private set; }
	[Export] [NotNull] public ColorRect KeyBackground { get; private set; }
	
	public ClientPlayerSkillHandle SkillHandle { get; private set; }

	public void SetHandle(ClientPlayerSkillHandle skillHandle)
	{
		SkillHandle = skillHandle;
		IconImage.Texture = SkillHandle.GetIcon();
		KeyLabel.Text = skillHandle.Key;
		if (KeyLabel.Text.Trim() == "")
		{
			KeyBackground.Visible = false;
		}
	}
	
	public double Progress
	{
		get => _process;
		set
		{
			((ShaderMaterial)CooldownOverlay.Material).SetShaderParameter("Progress", value);
			_process = value;
		}
	}

	public bool IsActive
	{
		get => !Overlay.Visible;
		set => Overlay.Visible = !value;
	}

	
	private double _process;
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
	}
	
	public override void _Process(double delta)
	{
		if(SkillHandle is null)
			return;

		Progress = SkillHandle.GetCooldownProgress();
		
		if (SkillHandle.CanUse() && !GlowRect.Visible)
			GlowRect.Visible = true;
		
		if (!SkillHandle.CanUse() && GlowRect.Visible)
			GlowRect.Visible = false;
	}
}
