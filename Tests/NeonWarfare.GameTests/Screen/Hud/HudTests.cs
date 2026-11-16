using GdUnit4;
using HudScreen = NeonWarfare.Scenes.Screen.Hud.Hud;
using NeonWarfare.Scenes.World.Features.Chat;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Screen.Hud;

// The key comes back in brackets, so a line shows which key it was translated with
[TestSuite]
public class HudTests
{
    private const long Now = 1_700_000_000;

    [TestCase]
    public void FormatChatLine_PlayerMessage_ShowsNickAndText()
    {
        var entry = new ChatPresentation.PlayerMessageEntry(Now, "uid", "Alice", "hello");

        AssertThat(HudScreen.FormatChatLine(entry, Translate)).IsEqual("[Alice]: hello");
    }

    [TestCase]
    public void FormatChatLine_ServerMessage_ShowsTranslatedServerNickAndText()
    {
        var entry = new ChatPresentation.ServerTextEntry(Now, "pong");

        AssertThat(HudScreen.FormatChatLine(entry, Translate)).IsEqual("[<HUD__CHAT_SERVER_NICK>]: pong");
    }

    [TestCase]
    public void FormatChatLine_LocalizedServerMessage_ShowsServerNickAndTranslationWithArgs()
    {
        var entry = new ChatPresentation.LocalizedServerEntry(Now, "SAVED", ["my save"]);

        AssertThat(HudScreen.FormatChatLine(entry, TranslateWithArgs))
            .IsEqual("[server]: saved as 'my save'");
    }

    // A translation of another version may expect an argument the server did not send
    [TestCase]
    public void FormatChatLine_LocalizedServerMessageMissingArg_ShowsKeyAndArgs()
    {
        var entry = new ChatPresentation.LocalizedServerEntry(Now, "SAVED", []);

        AssertThat(HudScreen.FormatChatLine(entry, TranslateWithArgs)).IsEqual("[server]: SAVED");
    }

    // A server of another version may send a key this client has no translation for
    [TestCase]
    public void FormatChatLine_LocalizedServerMessageUnknownKey_ShowsKeyAndArgs()
    {
        var entry = new ChatPresentation.LocalizedServerEntry(Now, "UNKNOWN", ["my save"]);

        AssertThat(HudScreen.FormatChatLine(entry, TranslateWithArgs)).IsEqual("[server]: UNKNOWN my save");
    }

    [TestCase]
    public void LocalizedServerEntry_EqualArgs_AreEqual()
    {
        AssertThat(new ChatPresentation.LocalizedServerEntry(Now, "KEY", ["a", "b"]))
            .IsEqual(new ChatPresentation.LocalizedServerEntry(Now, "KEY", ["a", "b"]))
            .IsNotEqual(new ChatPresentation.LocalizedServerEntry(Now, "KEY", ["a", "c"]));
    }

    [TestCase]
    public void FormatChatLine_UnknownEntry_Throws()
    {
        AssertThrown(() => HudScreen.FormatChatLine(new UnknownEntry(), Translate))
            .IsInstanceOf<ArgumentOutOfRangeException>();
    }

    private static string Translate(string key) => $"<{key}>";

    private static string TranslateWithArgs(string key) => key switch
    {
        "HUD__CHAT_SERVER_NICK" => "server",
        "SAVED" => "saved as '{0}'",
        _ => key
    };

    private record UnknownEntry() : ChatPresentation.ChatEntry(Now);
}
