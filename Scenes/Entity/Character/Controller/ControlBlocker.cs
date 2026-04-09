using System;
using MessagePack;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller;

[MessagePackObject(AllowPrivate = true)]
public partial class ControlBlocker : IEquatable<ControlBlocker>
{
    public static readonly ControlBlocker MenuIsOpen = new(true, true, true);
    public static readonly ControlBlocker ChatIsOpen = new(true, true, true);
    public static readonly ControlBlocker CharacterIsDead = new(true, true, true);
    public static readonly ControlBlocker CharacterIsStunned = new(true, true, true);
    public static readonly ControlBlocker CharacterIsRooted = new(true, false, false);
    public static readonly ControlBlocker CharacterIsSilenced = new(false, false, true);
        
    [Key(1)] public readonly bool BlockMovement;
    [Key(2)] public readonly bool BlockRotating;
    [Key(3)] public readonly bool BlockSkills;
        
    private ControlBlocker(bool blockMovement, bool blockRotating, bool blockSkills)
    {
        BlockMovement = blockMovement;
        BlockRotating = blockRotating;
        BlockSkills = blockSkills;
    }

    public bool Equals(ControlBlocker other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return BlockMovement == other.BlockMovement &&
               BlockRotating == other.BlockRotating &&
               BlockSkills == other.BlockSkills;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as ControlBlocker);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(BlockMovement, BlockRotating, BlockSkills);
    }

    public static bool operator ==(ControlBlocker left, ControlBlocker right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(ControlBlocker left, ControlBlocker right)
    {
        return !(left == right);
    }
}