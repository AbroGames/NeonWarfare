using System.Collections.Generic;
using System.Linq;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller;

public class ControlBlockerHandler
{
    public class ControlBlocker
    {
        public static readonly ControlBlocker CharacterIsDead =  new(true, true, true);
        public static readonly ControlBlocker MenuIsOpen =  new(true, true, true);
        public static readonly ControlBlocker ChatIsOpen =  new(true, true, true);
        public static readonly ControlBlocker ButtonIsHover =  new(false, true, false);
        
        public readonly bool BlockKeyboardKey;
        public readonly bool BlockMouseKey;
        public readonly bool BlockMousePosition;

        private ControlBlocker(bool blockKeyboardKey, bool blockMouseKey, bool blockMousePosition)
        {
            BlockKeyboardKey = blockKeyboardKey;
            BlockMouseKey = blockMouseKey;
            BlockMousePosition = blockMousePosition;
        }
    }

    private readonly List<ControlBlocker> _currentBlockers = new();

    public void AddBlock(ControlBlocker controlBlocker)
    {
        _currentBlockers.Add(controlBlocker);
    }

    public void RemoveBlock(ControlBlocker controlBlocker)
    {
        _currentBlockers.Remove(controlBlocker);
    }

    public bool IsKeyboardKeyBlocked()
    {
        return _currentBlockers.Any(b => b.BlockKeyboardKey);
    }
    
    public bool IsMouseKeyBlocked()
    {
        return _currentBlockers.Any(b => b.BlockMouseKey);
    }
    
    public bool IsMousePositionBlocked()
    {
        return _currentBlockers.Any(b => b.BlockMousePosition);
    }
}