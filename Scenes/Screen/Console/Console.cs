using Godot;
using System;
using KludgeBox;

namespace NeoVector;

public partial class Console : CanvasLayer, ILogger
{
    [Export] [NotNull] public RichTextLabel TextLabel { get; private set; }
    
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
        Log.AddLogger(this);
        Log.Info("Added builtin logger");
        Log.Debug("Added builtin logger");
        Log.Warning("Added builtin logger");
        Log.Error("Added builtin logger");
        Log.Critical("Added builtin logger");
    }

    public void Debug(object msg = null)
    {
        Print(msg, Colors.Gray);
    }

    public void Info(object msg = null)
    {
        Print(msg, Colors.White);
    }

    public void Warning(object msg = null)
    {
        Print(msg, Colors.Yellow);
    }

    public void Error(object msg = null)
    {
        Print(msg, Colors.Red);
    }

    public void Critical(object msg = null)
    {
        Print(msg, Colors.DarkRed);
    }
    
    private void Print(object msg, Color color)
    {
        TextLabel.PushColor(color);
        TextLabel.AppendText($"\n{msg}");
    }
}
