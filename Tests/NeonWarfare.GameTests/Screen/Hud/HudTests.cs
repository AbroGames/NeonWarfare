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
    public void FormatChatLine_PlayerJoined_ShowsNickAndTranslatedFact()
    {
        var entry = new ChatPresentation.PlayerJoinedEntry(Now, "uid", "Alice");

        AssertThat(HudScreen.FormatChatLine(entry, Translate)).IsEqual("Alice <HUD__CHAT_PLAYER_JOINED>");
    }

    [TestCase]
    public void FormatChatLine_PlayerLeft_ShowsNickAndTranslatedFact()
    {
        var entry = new ChatPresentation.PlayerLeftEntry(Now, "uid", "Alice");

        AssertThat(HudScreen.FormatChatLine(entry, Translate)).IsEqual("Alice <HUD__CHAT_PLAYER_LEFT>");
    }

    [TestCase]
    public void FormatChatLine_UnknownEntry_Throws()
    {
        AssertThrown(() => HudScreen.FormatChatLine(new UnknownEntry(), Translate))
            .IsInstanceOf<ArgumentOutOfRangeException>();
    }

    private static string Translate(string key) => $"<{key}>";

    private record UnknownEntry() : ChatPresentation.ChatEntry(Now);
}
