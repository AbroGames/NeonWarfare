using System;
using System.Text;
using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class Console : CanvasLayer, ILogger
{
    //[Export] [NotNull] public RichTextLabel TextLabel { get; private set; }
    [Export] [NotNull] public VBoxContainer MessagesContainer { get; private set; }
    [Export] [NotNull] public ScrollContainer ScrollContainer { get; private set; }
    [Export] public int MessagesCount { get; set; } = 1000;
    [Export] public int FontSize { get; set; } = 10;
    
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }

    public void Debug(object msg = null)
    {
        Print(msg, Colors.Gray);
    }

    public void Info(object msg = null)
    {
        Print(msg, Colors.White);
    }

    public void Warning(object msg = null, Exception exception = null)
    {
        Print(msg, Colors.Yellow, exception);
    }

    public void Error(object msg = null, Exception exception = null)
    {
        Print(msg, Colors.Red, exception);
    }

    public void Critical(object msg = null, Exception exception = null)
    {
        Print(msg, Colors.DarkRed, exception);
    }

    private void Print(object msg, Color color, Exception exception = null)
    {
        if(msg is null && exception is null)
        {
            AddMessage("", Col());
            return;
        }
        
        var sb = new StringBuilder();
        sb.Append(msg ?? "");
        sb.Append(msg is null || exception is null ? "" : "\n");
        sb.Append(exception?.StackTrace ?? "");
        
        AddMessage(sb.ToString(), color);
    }

    private void AddMessage(string text, Color color)
    {
        var scroll = ScrollContainer.GetVScrollBar().Value;
        var maxScroll = ScrollContainer.GetVScrollBar().MaxValue;
        
        var label = new Label
        {
            Text = text
        };
        label.Modulate = color;
        label.LabelSettings = new();
        label.LabelSettings.FontSize = FontSize;
        MessagesContainer.AddChild(label);

        if (maxScroll != 0)
        {
            var scrollPercent = scroll / maxScroll;
            if (scrollPercent is >= 0.98 or 0.0)
            {
                // Scroll to bottom
                ScrollContainer.ScrollVertical = (int)ScrollContainer.GetVScrollBar().MaxValue;
            }
        }

        if (MessagesContainer.GetChildCount() > MessagesCount)
        {
            MessagesContainer.GetChild(0).QueueFree();
        }
    }
    
    /*private void Print(object msg, Color color, Exception exception = null)
    {
        if(msg is null && exception is null)
        {
            TextLabel.AppendText($"\n");
            return;
        }
        
        var sb = new StringBuilder();
        sb.Append(msg ?? "");
        sb.Append(msg is null || exception is null ? "" : "\n");
        sb.Append(exception?.StackTrace ?? "");
        
        TextLabel.PushColor(color);
        TextLabel.AppendText($"\n{sb}");
        TextLabel.ScrollToLine(TextLabel.GetLineCount());
        
    }*/
}
