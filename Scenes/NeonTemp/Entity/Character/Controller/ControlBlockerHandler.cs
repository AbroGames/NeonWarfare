using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller;

public class ControlBlockerHandler
{

    private readonly List<ControlBlocker> _currentBlockers = new();

    public void AddBlock(ControlBlocker controlBlocker)
    {
        _currentBlockers.Add(controlBlocker);
    }

    public void RemoveBlock(ControlBlocker controlBlocker)
    {
        _currentBlockers.Remove(controlBlocker);
    }

    public bool IsMovementBlocked()
    {
        return _currentBlockers.Any(b => b.BlockMovement);
    }
    
    public bool IsRotatingBlocked()
    {
        return _currentBlockers.Any(b => b.BlockRotating);
    }
    
    public bool IsSkillsBlocked()
    {
        return _currentBlockers.Any(b => b.BlockSkills);
    }
}