using GdUnit4;
using Godot;
using NeonWarfare.Scenes.Screen.ServerHud;
using NeonWarfare.Scripts.GlobalServices.ServerLog;
using Serilog.Events;
using Serilog.Parsing;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Screen.ServerHud;

[TestSuite]
public class ServerLogViewTests
{
    private const string Time = "12:30:15.250";
    private static readonly DateTimeOffset Timestamp = new(2026, 1, 1, 12, 30, 15, 250, TimeSpan.Zero);

    [TestCase]
    [RequireGodotRuntime]
    public void Refresh_ShowsTimeLevelSourceMessageAndException_TextAsIs()
    {
        ServerLogService log = new();
        RichTextLabel label = AutoFree(new RichTextLabel())!;
        log.Emit(Event(LogEventLevel.Information, "hi [b]there[/b]", source: "Chat"));
        log.Emit(Event(LogEventLevel.Error, "boom", exception: new InvalidOperationException("oops")));

        new ServerLogView(label, log).Refresh();

        AssertThat(label.GetParsedText()).IsEqual(
            $"{Time} INF Chat: hi [b]there[/b]\n" +
            $"{Time} ERR boom\n" +
            "System.InvalidOperationException: oops");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Refresh_AppendsOnlyTheNewEntries()
    {
        ServerLogService log = new();
        RichTextLabel label = AutoFree(new RichTextLabel())!;
        ServerLogView view = new(label, log);
        log.Emit(Event(LogEventLevel.Information, "first"));
        view.Refresh();
        log.Emit(Event(LogEventLevel.Warning, "second"));

        view.Refresh();
        view.Refresh();

        AssertThat(label.GetParsedText()).IsEqual($"{Time} INF first\n{Time} WRN second");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Refresh_OverCapacity_RemovesTheOldestEntryWithAllItsLines()
    {
        ServerLogService log = new();
        RichTextLabel label = AutoFree(new RichTextLabel())!;
        ServerLogView view = new(label, log);
        log.Emit(Event(LogEventLevel.Error, "oldest", exception: new InvalidOperationException("oops")));
        view.Refresh();
        for (int i = 1; i < ServerLogService.Capacity; i++)
        {
            log.Emit(Event(LogEventLevel.Information, $"line {i}"));
        }
        view.Refresh();
        log.Emit(Event(LogEventLevel.Information, "newest"));

        view.Refresh();

        string[] lines = label.GetParsedText().Split('\n');
        AssertThat(label.GetParagraphCount()).IsEqual(ServerLogService.Capacity);
        AssertThat(lines.Length).IsEqual(ServerLogService.Capacity);
        AssertThat(lines.First()).IsEqual($"{Time} INF line 1");
        AssertThat(lines.Last()).IsEqual($"{Time} INF newest");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Refresh_FarBehind_ShowsOnlyTheKeptEntries()
    {
        ServerLogService log = new();
        RichTextLabel label = AutoFree(new RichTextLabel())!;
        ServerLogView view = new(label, log);
        log.Emit(Event(LogEventLevel.Error, "oldest", exception: new InvalidOperationException("oops")));
        view.Refresh();
        for (int i = 0; i < ServerLogService.Capacity + 3; i++)
        {
            log.Emit(Event(LogEventLevel.Information, $"line {i}"));
        }

        view.Refresh();

        string[] lines = label.GetParsedText().Split('\n');
        AssertThat(label.GetParagraphCount()).IsEqual(ServerLogService.Capacity);
        AssertThat(lines.First()).IsEqual($"{Time} INF line 3");
        AssertThat(lines.Last()).IsEqual($"{Time} INF line {ServerLogService.Capacity + 2}");
    }

    private static LogEvent Event(
        LogEventLevel level,
        string text,
        string? source = null,
        Exception? exception = null)
    {
        LogEventProperty[] properties = source == null
            ? []
            : [new LogEventProperty("ShortSourceContext", new ScalarValue(source))];
        return new LogEvent(Timestamp, level, exception, new MessageTemplateParser().Parse(text), properties);
    }
}
