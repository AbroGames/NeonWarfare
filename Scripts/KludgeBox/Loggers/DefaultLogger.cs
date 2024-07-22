using System;
using System.Text;
using Godot;

namespace KludgeBox.Loggers;

internal class DefaultLogger : ILogger
{
    public void Debug(object msg = null)
    {
        Print(msg, "gray");
    }

    public void Info(object msg = null)
    {
        Print(msg);
    }

    public void Warning(object msg = null, Exception exception = null)
    {
        Print(msg, "yellow", exception, pushWarning: true);
    }

    public void Error(object msg = null, Exception exception = null)
    {
        Print(msg, "orange", exception, pushError: true);
    }

    public void Critical(object msg = null, Exception exception = null)
    {
        Print(msg, "red", exception, pushError: true);
    }

    private void Print(object msg = null, string color = null, Exception exception = null, bool pushWarning = false, bool pushError = false)
    {
        if (msg is null && exception is null)
        {
            GD.Print();
            return;
        }
        
        var sb = new StringBuilder();
        sb.Append(msg ?? "");
        sb.Append(msg is null || exception is null ? "" : "\n");
        sb.Append(exception?.ToString() ?? "");

        if (color == "" || color == "white" || color is null)
        {
            GD.PrintRich($"{sb}");
            return;
        }
        
        GD.PrintRich($"[color={color}]{sb}[/color]");
        //if(pushWarning) GD.PushWarning(sb.ToString());
        if(pushError) GD.PushError(sb.ToString());
    }
}