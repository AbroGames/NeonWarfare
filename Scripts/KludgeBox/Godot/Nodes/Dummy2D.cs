using Godot;

namespace NeonWarfare.Scripts.KludgeBox.Godot.Nodes;

/// <summary>
/// Custom 2D node class for a dummy object with optional despawn functionality.
/// </summary>
public partial class Dummy2D : Node2D
{
    /// <summary>
    /// Gets or sets a value indicating whether this dummy object should be despawned when empty.
    /// </summary>
    public bool Despawn { get; set; } = true;

    /// <summary>
    /// Gets a value indicating whether this is the first process loop after instantiation.
    /// </summary>
    public bool First { get; private set; } = true;

    /// <summary>
    /// Process function called every frame with the elapsed time since the last frame.
    /// </summary>
    /// <param name="delta">The time elapsed since the last frame.</param>
    public override void _Process(double delta)
    {
        // If despawn is enabled, and this is not the first process loop, and there are no child nodes,
        // queue the object for freeing.
        if (Despawn && !First && GetChildCount() == 0)
        {
            QueueFree();
        }

        // Set the 'First' flag to false to indicate that the first process loop has completed.
        First = false;
    }

    /// <summary>
    /// Prevents the dummy object from being despawned when empty.
    /// </summary>
    public void KeepAlive()
    {
        Despawn = false;
    }

    /// <summary>
    /// Allows the dummy object to be despawned when empty.
    /// </summary>
    public void AllowDespawn()
    {
        Despawn = true;
    }
}
