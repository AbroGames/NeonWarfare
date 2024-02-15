using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace KludgeBox.Structs;

/// <summary>
/// Represents a cluster of intersecting circles in 2D space.
/// </summary>
public class CirclesCluster
{
	/// <summary>
	/// Gets the list of circles in the cluster.
	/// </summary>
	public List<Circle> Circles { get; private set; } = new List<Circle>();

	/// <summary>
	/// Calculates and returns the circumcircle that encloses all the circles in the cluster.
	/// </summary>
	/// <remarks>
	/// The circumcircle is the smallest circle2F that completely contains all the circles in the cluster.
	/// If the cluster is empty, the returned circle2F has a center position of (0, 0) and a radius of 0.
	/// </remarks>
	public Circle Circumcircle
	{
		get
		{
			// If the cluster is empty, return a circle2F with position (0, 0) and radius 0.
			if (Circles.Count == 0)
				return new Circle(Vector2.Zero, 0);

			if(Circles.Count == 1)
				return Circles.First();

			// Find the minimum and maximum coordinates of the cluster's circles
			real minX = real.MaxValue;
			real minY = real.MaxValue;
			real maxX = real.MinValue;
			real maxY = real.MinValue;

			foreach (Circle circle in Circles)
			{
				minX = Math.Min(minX, circle.Position.X - circle.Radius);
				minY = Math.Min(minY, circle.Position.Y - circle.Radius);
				maxX = Math.Max(maxX, circle.Position.X + circle.Radius);
				maxY = Math.Max(maxY, circle.Position.Y + circle.Radius);
			}

			// Calculate the center position and radius of the circumcircle
			Vector2 center = new Vector2((minX + maxX) / 2, (minY + maxY) / 2);
			real radius = Mathf.Max(maxX - center.X, maxY - center.Y);

			return new Circle(center, radius);
		}
	}

	public int Count => Circles.Count;

	/// <summary>
	/// Calculates and returns an approximate center of the cluster.
	/// </summary>
	/// <remarks>
	/// The approximate center is computed as the average position of all the circles in the cluster.
	/// If the cluster is empty, the returned vector has a position of (0, 0).
	/// </remarks>
	public Vector2 ApproxCenter
	{
		get
		{
			if (Circles.Count == 0)
				return Vector2.Zero;

			// Calculate the average center position of the circles in the cluster
			Vector2 sum = new Vector2(0, 0);
			foreach (Circle circle in Circles)
			{
				sum.X += circle.Position.X;
				sum.Y += circle.Position.Y;
			}

			return new Vector2(sum.X / Circles.Count, sum.Y / Circles.Count);
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CirclesCluster"/> class.
	/// </summary>
	/// <param name="circles">The list of circles to form the cluster.</param>
	public CirclesCluster(List<Circle> circles)
	{
		Circles = circles;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CirclesCluster"/> class.
	/// </summary>
	public CirclesCluster() { }

	/// <summary>
	/// Adds a circle2F to the cluster.
	/// </summary>
	/// <param name="circle">The circle2F to add to the cluster.</param>
	public void Add(Circle circle)
	{
		Circles.Add(circle);
	}

	/// <summary>
	/// Finds closest point laying on the whole cluster perimeter.
	/// </summary>
	/// <param name="start">Any vector outside the cluster.</param>
	/// <returns>Closest point laying on the perimeter of the cluster.</returns>
	public Vector2 CastOnPerimeter(Vector2 start)
	{
		if (Circles.Count == 0)
			return Vector2.Zero;

		// Find the closest point on each circle2F's perimeter to the ray's start position
		List<Vector2> closestPoints = new List<Vector2>();
		foreach (Circle circle in Circles)
		{
			Vector2 centerToStart = start - circle.Position;
			Vector2 closestPointOnPerimeter = circle.Position + (centerToStart).Normalized() * circle.Radius;
			closestPoints.Add(closestPointOnPerimeter);
		}

		// Find the closest point among all the closest points
		Vector2 closestPerimeterPoint = closestPoints[0];
		double closestDistanceSquared = (closestPerimeterPoint - start).LengthSquared();
		for (int i = 1; i < closestPoints.Count; i++)
		{
			double distanceSquared = (closestPoints[i] - start).LengthSquared();
			if (distanceSquared < closestDistanceSquared)
			{
				closestPerimeterPoint = closestPoints[i];
				closestDistanceSquared = distanceSquared;
			}
		}

		return closestPerimeterPoint;
	}

	/// <summary>
	/// Splits a list of circles into clusters of intersecting circles. Every cluster contains at least one circle2F.
	/// </summary>
	/// <param name="circles">The list of circles to be clustered.</param>
	/// <returns>A list of circles clusters.</returns>
	public static List<CirclesCluster> FindClusters(IEnumerable<Circle> circles)
	{
		List<CirclesCluster> clusters = new List<CirclesCluster>();

		foreach (Circle circle in circles)
		{
			// Check if the current circle2F belongs to any existing cluster
			bool addedToCluster = false;
			foreach (CirclesCluster cluster in clusters)
			{
				if (cluster.Circles.Exists(c => c.IntersectsWith(circle)))
				{
					cluster.Add(circle);
					addedToCluster = true;
					break;
				}
			}

			// If the circle2F doesn't belong to any existing cluster, create a new cluster for it
			if (!addedToCluster)
			{
				CirclesCluster newCluster = new CirclesCluster();
				newCluster.Add(circle);
				clusters.Add(newCluster);
			}
		}

		return clusters;
	}
}