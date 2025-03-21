using Godot;
using System;
using NeonWarfare.Scenes.Game.ClientGame.PlayerProfile;
using NeonWarfare.Scenes.Screen;

public partial class Message : RichTextLabel
{
    private const double Lifespan = 10;
    private double _timeToLife = Lifespan;
    private bool _isVisibilityForced;
    public bool ShouldBeVisible => _isVisibilityForced || _timeToLife > 0;
    public bool IsExpired => _timeToLife <= 0;
    
    public void InitMessage(ChatMessage chatMessage)
    {
        var sender = chatMessage.SenderInfo;
        Text = $"[color={sender.SenderColor.ToHtml(false)}]{sender.SenderName}[/color]: {chatMessage.MessageText}";
    }

    public void InitMessageRaw(string rawMessage)
    {
        Text = rawMessage;
    }

    public void ForceExpiration()
    {
        _timeToLife = 0;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_timeToLife > 0)
        {
            _timeToLife -= delta;
        }
        
        UpdateVisibility();
    }

    public void SetForcedVisibility(bool isVisible)
    {
        _isVisibilityForced = isVisible;
    }

    private void UpdateVisibility()
    {
        if (Visible && !ShouldBeVisible)
        {
            Visible = false;
        }
        else if (!Visible && ShouldBeVisible)
        {
            Visible = true;
        }
    }
}
