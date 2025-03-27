using Godot;
using System;
using System.Text;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scripts.KludgeBox.Core;

public partial class AchievementContainer : VBoxContainer
{
    [Export] [NotNull] private TextureRect _iconRect { get; set; }
    [Export] [NotNull] private TextureRect _iconVignetteRect { get; set; }
    [Export] [NotNull] private ColorRect _backgroundRect { get; set; }
    [Export] [NotNull] private ColorRect _iconDarkenerRect { get; set; }
    [Export] [NotNull] private Label _nameLabel { get; set; }
    [Export] private Label _descriptionLabel { get; set; }

    private Color _initialColor;
    private Color _disabledColor;
    private AchievementData _achievement;
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }

    public void InitializeFrom(AchievementData achievement)
    {
        _achievement = achievement;
        _initialColor = _backgroundRect.Color;
        _disabledColor = _initialColor with { V = _initialColor.V * 0.5f, S = _initialColor.S * 0.5f };
        
        _iconRect.Texture = _achievement.Icon;
        var id = _achievement.Id;
        if (ClientRoot.Instance.IsAchievementUnlocked(id))
        {
            Activate();
        }
        else
        {
            Deactivate();
        }
    }

    private void Activate()
    {
        UpdateTexts(true);
        SetShaderStrength(_iconRect, 0);
        _iconDarkenerRect.Visible = false;
        _iconVignetteRect.Visible = false;
        _nameLabel.LabelSettings.FontColor = _achievement.Color;
        _backgroundRect.Color = _initialColor;
        _iconRect.Visible = true;
    }
    private void Deactivate()
    {
        UpdateTexts(false);
        SetShaderStrength(_iconRect, 1);
        _iconDarkenerRect.Visible = true;
        _iconVignetteRect.Visible = true;
        _nameLabel.LabelSettings.FontColor = _achievement.Color.Grayscale();
        _backgroundRect.Color = _disabledColor;
        _iconRect.Visible = !_achievement.IsHidden;
    }

    private static void SetShaderStrength(CanvasItem canvasItem, Variant value)
    {
        var material = canvasItem.Material as ShaderMaterial;
        material!.SetShaderParameter("strength", value);
    }

    private void UpdateTexts(bool isActive)
    {
        _nameLabel.Text = ProcessText(_achievement.Name, _achievement.IsHidden && !isActive);
        _descriptionLabel.Text = ProcessText(_achievement.Description, _achievement.IsHidden && !isActive);
    }

    private string ProcessText(string text, bool isHidden)
    {
        var sb = new StringBuilder();
        char replacer = '\u2588';

        foreach (char c in text)
        {
            char newChar = c;
            if (isHidden)
            {
                if (c != ' ' && c != '\t' && c != '\n' && c != '\r' && c != '\f')
                {
                    newChar = replacer;
                }
            }
            sb.Append(newChar);
        }
        
        return sb.ToString();
    }
}
