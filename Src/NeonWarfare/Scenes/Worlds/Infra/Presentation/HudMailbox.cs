using System.Collections.Generic;
using GodotBox.Godot;
using NeonWarfare.Scenes.Worlds.Infra.Composition;

namespace NeonWarfare.Scenes.Worlds.Infra.Presentation;

/// <summary>
/// One-frame HUD notices. A Presentation posts them from an event handler, and a widget reads them in
/// <c>_Process</c>.
/// A new frame number clears the mailbox, so nobody has to empty it.
/// </summary>
/// <remarks>
/// The event handlers run before <c>_Process</c> of the same frame, in <c>multiplayer.poll()</c> on a remote client
/// and in the physics step on the host. A post after <c>_Process</c>, from a timer or <c>CallDeferred</c>, is
/// therefore lost, and a read in <c>_PhysicsProcess</c> sees nothing.
/// </remarks>
[Presentation]
public class HudMailbox(FrameProvider frames)
{
    private readonly List<Notice> _notices = [];
    private ulong _frame;

    public void Post(Notice notice)
    {
        DropPreviousFrame();
        _notices.Add(notice);
    }

    /// <returns>The notices of type <typeparamref name="T"/> posted in this frame, in post order.</returns>
    public IReadOnlyList<T> Read<T>() where T : Notice
    {
        DropPreviousFrame();

        List<T> found = null;
        foreach (Notice notice in _notices)
        {
            if (notice is T typed)
            {
                (found ??= []).Add(typed);
            }
        }
        return found ?? (IReadOnlyList<T>) [];
    }
    
    private void DropPreviousFrame()
    {
        ulong now = frames.GetProcessFrames();
        if (now == _frame) return;

        _notices.Clear();
        _frame = now;
    }
}
