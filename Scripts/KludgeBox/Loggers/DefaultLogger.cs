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

    public void Warning(object msg = null)
    {
        Print(msg, "yellow");
    }

    public void Error(object msg = null)
    {
        Print(msg, "orange");
    }

    public void Critical(object msg = null)
    {
        Print(msg, "red");
    }

    private void Print(object msg = null, string color = null)
    {
        if (msg is null)
        {
            GD.Print();
            return;
        }

        if (color == "" || color == "white" || color is null)
        {
            GD.PrintRich($"{msg}");
            return;
        }
        
        GD.PrintRich($"[color={color}]{msg}[/color]");
    }
}