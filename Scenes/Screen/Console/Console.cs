using System;
using System.Text;
using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class Console : CanvasLayer, ILogger
{
    [Export] [NotNull] public RichTextLabel TextLabel { get; private set; }
    
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
    }
}
