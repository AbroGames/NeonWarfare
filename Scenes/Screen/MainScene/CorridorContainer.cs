using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Core;

public partial class CorridorContainer : Node3D
{
    [Export] [NotNull] private PackedScene _corridorSegmentScene {get; set;}
    private int _corridorSegmentsCount = 5;
    private float _maxAllowedSegmentPosition = 10; // 10 метров за камерой
    private float _segmentLength = 7; // длина одного сегмента - 7 метров
    private float _coridorSpeed = 0.2f;
    
    
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }

    public override void _Process(double delta)
    {
        var segments = GetCorridorSegments();
        foreach (var segment in segments)
        {
            segment.Position += new Vector3(0, 0, (float)(_coridorSpeed * delta));
        }
        
        BuildCorridorSegments();
        TrimCorridorSegments();
    }

    private void BuildCorridorSegments()
    {
        var segments = GetCorridorSegments();
        if (segments.Count < _corridorSegmentsCount)
        {
            var newSegment = _corridorSegmentScene.Instantiate() as Node3D;
            newSegment!.Position = GetFarthestSegment().Position + new Vector3(0, 0, -_segmentLength);
            AddChild(newSegment);
        }
    }

    private void TrimCorridorSegments()
    {
        var lastSegment = GetClosestSegment();
        if (lastSegment.Position.Z > _maxAllowedSegmentPosition)
        {
            lastSegment.QueueFree();
        }
    }

    private List<Node3D> GetCorridorSegments()
    {
        return GetChildren().OfType<Node3D>().ToList();
    }
    
    private Node3D GetClosestSegment()
    {
        return GetCorridorSegments().OrderBy(segment => segment.Position.Z).Last();
    }

    private Node3D GetFarthestSegment()
    {
        return GetCorridorSegments().OrderBy(segment => segment.Position.Z).First();
    }
}
