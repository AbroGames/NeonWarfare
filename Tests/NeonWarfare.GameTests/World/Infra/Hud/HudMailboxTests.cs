using GdUnit4;
using NeonWarfare.GameTests.World.Fixtures;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Infra.Hud;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.World.Infra.Hud;

// The frame comes from a counter the test advances: real engine frames would make the clearing timing-dependent
[TestSuite]
public class HudMailboxTests
{
    private ManualFrameProvider _frames = null!;
    private HudMailbox _mailbox = null!;

    [BeforeTest]
    public void SetUp()
    {
        _frames = new ManualFrameProvider();
        _mailbox = new HudMailbox(_frames);
    }

    [TestCase]
    public void Read_SeveralPostsInOneFrame_ReturnsAllInPostOrder()
    {
        _mailbox.Post(new Toast("first"));
        _mailbox.Post(new Toast("second"));
        _mailbox.Post(new Toast("third"));

        AssertThat(_mailbox.Read<Toast>())
            .ContainsExactly(new Toast("first"), new Toast("second"), new Toast("third"));
    }

    [TestCase]
    public void Read_ReturnsOnlyTheRequestedTypeAndRemovesNothing()
    {
        _mailbox.Post(new Toast("first"));
        _mailbox.Post(new Flash());
        _mailbox.Post(new Toast("second"));

        AssertThat(_mailbox.Read<Toast>()).ContainsExactly(new Toast("first"), new Toast("second"));
        AssertThat(_mailbox.Read<Flash>()).ContainsExactly(new Flash());
        AssertThat(_mailbox.Read<Toast>()).ContainsExactly(new Toast("first"), new Toast("second"));
    }

    [TestCase]
    public void Read_InLaterFrameWithoutPost_ReturnsNothing()
    {
        _mailbox.Post(new Toast("old"));

        _frames.Frame++;

        AssertThat(_mailbox.Read<Toast>()).IsEmpty();
    }

    [TestCase]
    public void Post_InNewFrame_DropsThePreviousFrame()
    {
        _mailbox.Post(new Toast("old"));
        _mailbox.Post(new Flash());

        _frames.Frame++;
        _mailbox.Post(new Toast("new"));

        AssertThat(_mailbox.Read<Toast>()).ContainsExactly(new Toast("new"));
        AssertThat(_mailbox.Read<Flash>()).IsEmpty();
    }

    private record Toast(string Text) : Notice;

    private record Flash : Notice;
}
