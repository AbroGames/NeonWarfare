using System.Collections.Generic;
using GodotBox.Godot;
using NeonWarfare.Scenes.World.Infra.Composition;

namespace NeonWarfare.Scenes.World.Infra.Hud;

/// <summary>
/// One-shot HUD notices of the current frame: only a Presentation posts, from an event handler; a widget reads in
/// <c>_Process</c>. The frame number clears the mailbox, so it needs no owner that empties it.
/// </summary>
/// <remarks>
/// Godot runs the event handlers in <c>multiplayer.poll()</c> (a remote client) or in a physics step (the host's
/// loopback), both before <c>_Process</c> of the same frame number. A post after <c>_Process</c> (a timer,
/// <c>CallDeferred</c>) is lost, and a read in <c>_PhysicsProcess</c> sees nothing.
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
