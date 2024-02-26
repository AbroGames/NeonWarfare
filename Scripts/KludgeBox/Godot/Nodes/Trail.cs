using System.Collections.Generic;
using Godot;

namespace KludgeBox.Godot;

/// <summary>
/// This node is slow AF
/// Its even slower than ExperimentalTrailLine
/// </summary>
[GlobalClass]
public partial class Trail : Node2D
{
	/// <summary>
	/// Node2D to which Trail should be applied.
	/// </summary>
	[Export]
	public Node2D Target { get; set; }

	/// <summary>
	/// Amount of segments in the trail. Bigger values working better on high speed, while smaller values better for slow moving
	/// without abrupt change of direction.
	/// </summary>
	[Export]
	public int SegmentsCount {get; set; } = 40;

	/// <summary>
	/// Trail width at the start.
	/// </summary>
	[Export]
	public real StartWidth = 0f;

	/// <summary>
	/// Trail width at the end.
	/// </summary>
	[Export]
	public real EndWidth = 0f;

	/// <summary>
	/// How many seconds will the trail last.
	/// </summary>
	[Export]
	public real Length = 1;

	/// <summary>
	/// Trail color at the start.
	/// </summary>
	[Export]
	public Color StartColor = new Color(1, 1, 1);

	/// <summary>
	/// Trail color at the end.
	/// </summary>
	[Export]
	public Color EndColor = new Color(1, 1, 1);

	/// <summary>
	/// Trail alpha at the start.
	/// </summary>
	[Export(PropertyHint.Range, "0, 1, 0.05")]
	public float StartAlpha = 1f;

	/// <summary>
	/// Trail alpha at the end.
	/// </summary>
	[Export(PropertyHint.Range, "0, 1, 0.05")]
	public float EndAlpha = 1f;

	/// <summary>
	/// Sets the color of the trail. Setting the color will update both the start and end color values.
	/// </summary>
	public Color Color
	{
		set
		{
			StartColor = value;
			EndColor = value;
		}
	}

	/// <summary>
	/// Sets the alpha value of the trail. Setting the alpha will update both the start and end alpha values.
	/// </summary>
	public float Alpha
	{
		set
		{
			StartAlpha = value;
			EndAlpha = value;
		}
	}

	/// <summary>
	/// Sets the width of the trail. Setting the width will update both the start and end width values.
	/// </summary>
	public real Width
	{
		set
		{
			StartWidth = value;
			EndWidth = value;
		}
	}

	// Actual segments spawning delay will dependon your FPS.
	private double TimeBetweenSpawns => Length / SegmentsCount;

	// List of active segments
	private List<Segment> segments = new List<Segment>();

	// Last created segment. End of this segment is always following the target.
	private Segment currentSegment = null;

	private double _timeThreshold = 0;

	public override void _Ready()
	{
		Target = GetParent<Node2D>();
		Reset();
	}

	public override void _Process(double delta)
	{
		// Reset pos to (0, 0)
		GlobalPosition = Vec();

		// Accumulate some time
		_timeThreshold += delta;

		// Add new segment if needed
		if (_timeThreshold >= TimeBetweenSpawns)
		{
			_timeThreshold = 0;
			SpawnSegment();
		}

		// Current segment's end must always be at the target location
		currentSegment.SetEndPos(Target.Position);

		// Remove all finished segments
		segments.RemoveAll(s => s.Finished);

		// Update all segments
		foreach (var segment in segments)
			segment.Update(delta);
	}

	// Creates new segment
	private void SpawnSegment()
	{
		currentSegment = new Segment(this, currentSegment);
		segments.Add(currentSegment);
		AddChild(currentSegment.polygon);
	}

	// Free and remove all active segments
	public void Reset()
	{
		foreach (var line in segments)
		{
			line.QueueFree();
		}
		segments.Clear();
		currentSegment?.QueueFree();

		currentSegment = new Segment(this, null);
		currentSegment.startPos = Target.Position;
		currentSegment.endPos = Target.Position;
		segments.Add(currentSegment);
	}




	/// <summary>
	/// Internal class that represents a trail segment
	/// </summary>
	private class Segment
	{
		// Processing/Deletion flag
		public bool Finished => timeToLive <= 0 || polygon == null;

		// Start pos is the end pos of the previous segment
		public Vector2 StartPos => previous is null ? startPos : previous.endPos;
		public Vector2 EndPos => endPos;

		// Used to calculate the segment edge
		public Vector2 EdgeNorm => (EndPos - StartPos).Orthogonal().Normalized();

		// Ending edge is being calculated
		public Vector2[] EndingEdge => new[] { EndPos + EdgeNorm * WidthAtEnd / 2, EndPos - EdgeNorm * WidthAtEnd / 2 };
		// Starting edge is just an ending edge of the previous segment
		public Vector2[] StartingEdge => previous is null ? new[] { StartPos + EdgeNorm * WidthAtStart / 2, StartPos - EdgeNorm * WidthAtStart / 2 } : previous.EndingEdge;
		

		// These properties returns widths at the start and the end of segment
		public real WidthAtEnd => Mathf.Lerp(endWidth, startWidth, (timeToLive / startingTimeToLive));
		public real WidthAtStart => previous is null ? WidthAtEnd : previous.WidthAtEnd;

		// Polygon used to draw the segment
		public Polygon2D polygon = new Polygon2D();
		// Previous segment ref
		public Segment previous = null;
		// Tail instance this segment belongs to
		public Trail parentTrail;

		public real startWidth;
		public real endWidth;

		public real timeToLive;
		public real startingTimeToLive;

		public Vector2 endPos;
		public Vector2 startPos;


		public Segment(Trail trail, Segment prev = null)
		{
			// Setup references
			previous = prev;
			parentTrail = trail;

			// get TTL for the segment
			timeToLive = trail.Length;
			startingTimeToLive = timeToLive;

			// Get width
			startWidth = trail.StartWidth;
			endWidth = trail.EndWidth;

			// Set static starting position
			startPos = prev is null ? parentTrail.Target.GlobalPosition : prev.endPos;

			// Placeholder polygon
			polygon.Polygon = new[] { startPos, startPos, startPos, startPos };
		}

		// Must be called from the _Process() method
		public void Update(double dt)
		{
			// Update time to live of the segment
			var ttlPart = (real)(timeToLive / startingTimeToLive);
			timeToLive -= (real)dt;

			// Remove the Polygon2D after finishing
			if (Finished)
			{
				QueueFree();
				return;
			}

			// Update it's color and alpha
			polygon.Color = parentTrail.EndColor.Lerp(parentTrail.StartColor, ttlPart);
			polygon.Color = polygon.Color with { A = (float)Mathf.Lerp(parentTrail.EndAlpha, parentTrail.StartAlpha, ttlPart) };

			// Get 4 points of the segment's edge
			var unsortedPoints = new[] {
				EndingEdge[0],
				EndingEdge[1],
				StartingEdge[1],
				StartingEdge[0]
			};

			// Geometry2D.ConvexHull is required, since crossing edges results in invisible polygon
			polygon.Polygon = Geometry2D.ConvexHull(unsortedPoints);
		}

		public void SetEndPos(Vector2 end)
		{
			endPos = end;
		}

		public void QueueFree()
		{
			polygon.QueueFree();
		}
	}

}