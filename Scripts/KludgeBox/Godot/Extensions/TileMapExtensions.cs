using System;
using System.Collections.Generic;
using Godot;

namespace KludgeBox.Godot;

public static class TileMapExtensions
{
    public static List<Vector2[]> GetPolygons(this TileMap map, Vector2I tile)
    {
        if (map.TileSet.TileShape is not TileSet.TileShapeEnum.Square) 
            throw new InvalidOperationException("Polygons can be built only for square or rectangular tiles");
        
        var cells = map.GetUsedCellsById(0, 0, tile);
        var polygons = new List<Vector2[]>();
        var tileSize = map.TileSet.TileSize;

        var tileWidth = tileSize.X;
        var tileHeight = tileSize.Y;
        
        foreach (var (x, y) in cells)
        {
            var cellRect = new Rect2I(VecI(x * tileWidth, y * tileHeight), tileSize);
            var cellPoly = cellRect.ToPolygon().ToVector2Array();
			
            polygons.Add(cellPoly);
        }

        return polygons;
    }
}