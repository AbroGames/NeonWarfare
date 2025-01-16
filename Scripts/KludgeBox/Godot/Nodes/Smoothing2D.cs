using System.Runtime.CompilerServices;
using Godot;

namespace NeonWarfare.Scripts.KludgeBox.Godot.Nodes;

[GlobalClass]
public partial class Smoothing2D : Node2D
{
    [Export]
    public NodePath TargetPath
    {
        get => _targetPath;
        set
        {
            _targetPath = value;
            if (IsInsideTree())
            {
                FindTarget();
            }
        }
    }

    private NodePath _targetPath;

    private Node2D _target;
    private bool _flip;

    private Transform2D _currentTransform = new();
    private Transform2D _previousTransform = new();

    private const int SF_ENABLED = 1 << 0;
    private const int SF_GLOBAL_IN = 1 << 1;
    private const int SF_GLOBAL_OUT = 1 << 2;
    private const int SF_TOP_LEVEL = 1 << 3;
    private const int SF_INVISIBLE = 1 << 4;

    private int _flags = SF_ENABLED | SF_GLOBAL_IN | SF_GLOBAL_OUT;

    [Export(PropertyHint.Flags, "enabled, global in, global out, top level")]
    public int Flags
    {
        get => _flags;
        set
        {
            _flags = value;
            SetProcessing();
        }
    }

    #region Public

    public void Teleport()
    {
        RefreshTransform();
        _previousTransform = _currentTransform;
        _Process(0);
    }

    public void SetEnabled(bool enabled)
    {
        ChangeFlags(SF_ENABLED, enabled);
        SetProcessing();
    }

    public bool IsEnabled()
    {
        return TestFlags(SF_ENABLED);
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        ProcessPriority = 100;
        Engine.PhysicsJitterFix = 0;
        TopLevel = TestFlags(SF_TOP_LEVEL);
        Teleport();
    }

    /// <inheritdoc />
    public override void _EnterTree()
    {
        FindTarget();
    }

    /// <inheritdoc />
    public override void _Notification(int what)
    {
        if (what == NotificationVisibilityChanged)
        {
            ChangeFlags(SF_INVISIBLE, !IsVisibleInTree());
            SetProcessing();
        }
    }

    /// <inheritdoc />
    public override void _PhysicsProcess(double delta)
    {
        RefreshTransform();
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        var fraction = (real)Engine.GetPhysicsInterpolationFraction();
        var transform = new Transform2D();

        
        if (_flip)
        {
            transform = _currentTransform;
        }
        else
        {
            transform.Origin = _previousTransform.Origin.Lerp(_currentTransform.Origin, fraction);
            transform.X = _previousTransform.X.Lerp(_currentTransform.X, fraction);
            transform.Y = _previousTransform.Y.Lerp(_currentTransform.Y, fraction);
        }

        if (TestFlags(SF_GLOBAL_OUT) && !TestFlags(SF_TOP_LEVEL))
        {
            GlobalTransform = transform;
        }
        else
        {
            Transform = transform;
        }
    }

    #endregion

    #region Private


    private void FindTarget()
    {
        _target = null;
        if (TargetPath is null || TargetPath.IsEmpty)
        {
            var parent = GetParent();
            if (parent.IsValid() && parent is Node2D node2D)
            {
                _target = node2D;
                return;
            }
        }

        var tar = GetNode(TargetPath);

        if (!tar.IsValid())
        {
            Log.Error($"SmoothingNode2D : Target {TargetPath} not found");
            return;
        }

        if (tar is not Node2D tar2d)
        {
            Log.Error($"SmoothingNode2D : Target {TargetPath} is not Node2D");
            TargetPath = new NodePath();
            return;
        }

        _target = tar2d;
    }

    private void SetProcessing()
    {
        var enable = TestFlags(SF_ENABLED);
        if (TestFlags(SF_INVISIBLE))
            enable = false;

        SetProcess(enable);
        SetPhysicsProcess(enable);
        TopLevel = TestFlags(SF_TOP_LEVEL);
    }

    public void RefreshTransform()
    {
        if (!HasTarget()) return;

        _previousTransform = _currentTransform;

        if (TestFlags(SF_GLOBAL_IN))
        {
            _currentTransform = _target.GlobalTransform;
        }
        else
        {
            _currentTransform = _target.Transform;
        }

        _flip = false;

        if (DeterminantSign(_previousTransform) != DeterminantSign(_currentTransform))
        {
            _flip = true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool DeterminantSign(Transform2D transform)
    {
        return transform.X.X * transform.Y.Y - transform.X.Y * transform.Y.X >= 0;
    }

    private bool HasTarget()
    {
        if (_target.IsValid()) return true;

        return false;
    }

    private void SetFlags(int flags)
    {
        Flags |= flags;
    }

    private void ClearFlags(int flags)
    {
        Flags &= ~flags;
    }

    private bool TestFlags(int flags)
    {
        return (Flags & flags) == flags;
    }

    private void ChangeFlags(int flags, bool set)
    {
        if (set)
        {
            SetFlags(flags);
        }
        else
        {
            ClearFlags(flags);
        }
    }

#endregion
    
}
