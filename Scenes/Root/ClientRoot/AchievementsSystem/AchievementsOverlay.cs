using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Godot.Services;

public partial class AchievementsOverlay : CanvasLayer
{
    [Export] [NotNull] private PackedScene AchievementScene { get; set; }
    [Export] [NotNull] private Control OverlayRoot { get; set; }
    public double AnimationTime { get; set; } = 0.5;
    public double WaitTime { get; set; } = 5;
    
    private Queue<AchievementData> _unlockQueue = new Queue<AchievementData>();
    private bool CanShow { get; set; } = true;

    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }

    public void ShowAchievement(AchievementData achievement)
    {
        if(_unlockQueue.Any(queuedAchievement => queuedAchievement.Id == achievement.Id))
            return;
        
        _unlockQueue.Enqueue(achievement);
    }

    public override void _Process(double delta)
    {
        if (!CanShow) return;
        if (_unlockQueue.TryDequeue(out var achievement))
        {
            RenderAchievement(achievement);
        }
    }

    private void RenderAchievement(AchievementData achievement)
    {
        var screenSize = OverlayRoot.Size;
        var achievementPanel = AchievementScene.Instantiate<AchievementPanel>();
        achievementPanel.InitializeFrom(achievement);
        
        OverlayRoot.AddChild(achievementPanel);
        var initialPosition = GetViewport().GetVisibleRect().Size - achievementPanel.Size with { X = 0 };
        var endPosition   = GetViewport().GetVisibleRect().Size - achievementPanel.Size;
        achievementPanel.Position = initialPosition;
        
        var tween = achievementPanel.CreateTween();
        Audio2D.PlayUiSound(Sfx.UiAchievement);
        CanShow = false;
        tween.TweenProperty(achievementPanel, "position", endPosition, AnimationTime)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        tween.TweenInterval(WaitTime);
        tween.TweenCallback(Callable.From(() =>
        {
            achievementPanel.Position = GetViewport().GetVisibleRect().Size - achievementPanel.Size;
            var nextTween = achievementPanel.CreateTween();
            nextTween.TweenProperty(achievementPanel, "position", GetViewport().GetVisibleRect().Size + new Vector2(achievementPanel.Size.X, -achievementPanel.Size.Y), AnimationTime)
                .SetEase(Tween.EaseType.In)
                .SetTrans(Tween.TransitionType.Cubic);
            nextTween.TweenCallback(Callable.From(() =>
            {
                achievementPanel.QueueFree();
                CanShow = true;
            }));
            nextTween.Play();
        }));

        tween.Play();
    }
}
