using GdUnit4;
using NeonWarfare.Scripts.GlobalServices.ServerLog;
using Serilog.Events;
using Serilog.Parsing;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Scripts.GlobalServices.ServerLog;

[TestSuite]
public class ServerLogServiceTests
{
    private ServerLogService _service = null!;

    [BeforeTest]
    public void SetUp()
    {
        _service = new ServerLogService();
    }

    [TestCase]
    public void Emit_OverCapacity_KeepsTheLastInOrder()
    {
        for (int i = 0; i < ServerLogService.Capacity + 2; i++)
        {
            _service.Emit(Event($"line {i}"));
        }

        string[] messages = _service.ReadSince(0).Entries.Select(entry => entry.Message).ToArray();
        AssertThat(messages.Length).IsEqual(ServerLogService.Capacity);
        AssertThat(messages[0]).IsEqual("line 2");
        AssertThat(messages[^1]).IsEqual($"line {ServerLogService.Capacity + 1}");
    }

    [TestCase]
    public void ReadSince_ReturnsOnlyTheNewerEntriesAndTheVersion()
    {
        _service.Emit(Event("first"));
        long version = _service.ReadSince(0).Version;
        _service.Emit(Event("second"));
        _service.Emit(Event("third"));

        (long newVersion, IReadOnlyList<ServerLogEntry> entries) = _service.ReadSince(version);

        AssertThat(version).IsEqual(1);
        AssertThat(newVersion).IsEqual(3);
        AssertThat(entries.Select(entry => entry.Message).ToArray()).IsEqual(new[] { "second", "third" });
        AssertThat(_service.ReadSince(newVersion).Entries.Count).IsEqual(0);
    }

    [TestCase]
    public void Emit_RendersTheMessageWithItsProperties_KeepsLevelSourceAndException()
    {
        InvalidOperationException exception = new("boom");
        LogEvent logEvent = new(
            DateTimeOffset.Now,
            LogEventLevel.Warning,
            exception,
            new MessageTemplateParser().Parse("Player {nick} sent {count} [items]"),
            [
                new LogEventProperty("nick", new ScalarValue("Neo")),
                new LogEventProperty("count", new ScalarValue(3)),
                new LogEventProperty("ShortSourceContext", new ScalarValue("ChatSimulation"))
            ]);

        _service.Emit(logEvent);

        ServerLogEntry entry = _service.ReadSince(0).Entries.Single();
        AssertThat(entry.Message).IsEqual("Player Neo sent 3 [items]");
        AssertThat(entry.Level).IsEqual(LogEventLevel.Warning);
        AssertThat(entry.Source).IsEqual("ChatSimulation");
        AssertThat(entry.Exception).IsEqual(exception.ToString());
        AssertThat(entry.Timestamp).IsEqual(logEvent.Timestamp);
    }

    [TestCase]
    public void ReadSince_ReturnsASnapshot()
    {
        _service.Emit(Event("first"));
        IReadOnlyList<ServerLogEntry> snapshot = _service.ReadSince(0).Entries;

        _service.Emit(Event("second"));

        AssertThat(snapshot.Count).IsEqual(1);
    }

    private static LogEvent Event(string text)
    {
        return new LogEvent(
            DateTimeOffset.Now,
            LogEventLevel.Information,
            null,
            new MessageTemplateParser().Parse(text),
            []);
    }
}
