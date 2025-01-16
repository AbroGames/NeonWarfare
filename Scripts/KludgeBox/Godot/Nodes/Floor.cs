using System.Collections.Generic;
using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Scheduling;

namespace NeonWarfare.Scripts.KludgeBox.Godot.Nodes;

[GlobalClass]
public partial class Floor : Node2D
{
	/// <summary>
	/// Used for terrain generation on-the-fly
	/// </summary>
	[Export] [NotNull] public Camera2D Camera { get; set; }
	[Export] [NotNull] public Texture2D Texture { get; set; }

	private Cooldown _checksCooldown = new Cooldown(0.25, CooldownMode.Single);

	// Why TF I'm doing this?
	private List<Tile> _tiles = new List<Tile>();
	private Dictionary<Vector2I, Tile> _grid = new();

	public override void _Ready()
	{
		_checksCooldown.Ready += () =>
		{
			if(Camera.IsValid())
				CheckTiles();
		};
	}

	public void ForceCheck()
	{
		CheckTiles();
	}

	public override void _Process(double delta)
	{
		_checksCooldown.Update(delta);
	}



	private void CheckTiles()
	{
		_checksCooldown.Use();
		var floorRadius = Camera.GetRadius() * 8;

		var width = Texture.GetWidth();
		var height = Texture.GetHeight();

		var gridSize = Mathf.Ceil(floorRadius / Mathf.Max(width, height));
		var gridRadius = (int)gridSize / 2;

		var currentTile = WorldToGrid(Camera.GlobalPosition, Texture.GetSize());

		for (int w = -gridRadius; w <= gridRadius; w++)
		{
			for (int h = -gridRadius; h <= gridRadius; h++)
			{
				Tile tile;
				var gridPos = currentTile + VecI(w,h);
				if (!_grid.TryGetValue(gridPos, out tile))
				{
					tile = new Tile(this, gridPos);
					_grid[gridPos] = tile;
					AddChild(tile);
				}
			}
		}

		var markedForRemoval = _tiles.Where(t=>IsTooFar(t, floorRadius));
		foreach (var tile in markedForRemoval)
		{
			tile.QueueFree();
			_tiles.Remove(tile);
		}

		foreach ((var pos, var tile) in _grid)
		{
			if (markedForRemoval.Contains(tile))
				_grid.Remove(pos);
		}
		
		this.ToBackground();
	}


	public bool IsTooFar(Tile tile, double distance)
	{
		return tile.Position.DistanceTo(Camera.Position) > distance;
	}


	public Vector2 GridToWorld(Vector2I gridPos, Vector2 tileSize)
	{
		return Vec(
			gridPos.X * tileSize.X,
			gridPos.Y * tileSize.Y);
	}


	public Vector2I WorldToGrid(Vector2 pos, Vector2 tileSize)
	{
		return VecI(
			(int)(pos.X/tileSize.X),
			(int)(pos.Y/tileSize.Y));
	}
}
