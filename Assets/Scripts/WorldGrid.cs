using Core.Extensions;
using Core.Patterns;
using UnityEngine;
using System.Collections.Generic;

public class WorldGrid : MonoSingleton<WorldGrid>
{
    public enum CellType
    {
        CITY,
        PLAIN,
        WATER,
        RIVER,
        ROAD,
        BRIDGE,
    }

    public struct Cell
    {
        public CellType   Type;
        public Vector2Int Position;
        public float      Height;
        public POIData    POI;
        public bool       IsOccupied;

        public bool Is(params CellType[] _cellTypes)
        {
            foreach (var type in _cellTypes)
                if (Type == type)
                    return true;

            return false;
        }
    }

    public int       size = 256;
    public Transform centerMarker;
    public Transform bedrockTransform;

    private Vector3 CenterPosition => new(size / 2f, 0, size  / 2f);
    private Vector3 BedrockScale   => new(size / 10f, 1, size / 10f);

    public class NearCityData
    {
        public string     Name;
        public Vector2Int CityPos;
        public float      Distance;
    }

    public List<NearCityData> NearCities = new();

    public Cell[,] Cells;

    private static Cell[] _TileBuffer = new Cell[256];

    public static int    TileBufferCount { get; private set; }
    public static Cell[] TileBuffer      => _TileBuffer;

    private void Start()
    {
        centerMarker.position       = CenterPosition;
        bedrockTransform.localScale = BedrockScale;
        bedrockTransform.position   = CenterPosition.WithY(-1f);
    }

    public void InitGrid()
    {
        Cells = new Cell[size, size];

        centerMarker.position       = CenterPosition;
        bedrockTransform.localScale = BedrockScale;
        bedrockTransform.position   = CenterPosition.WithY(-1f);
    }

    public Cell? GetCell(Vector2Int _pos)
    {
        return IsInBounds(_pos) ? Cells[_pos.x, _pos.y] : null;
    }

    public Vector3 CellToWorld(Vector2Int _pos)
    {
        var cell = GetCell(_pos);
        return new Vector3(_pos.x + 0.5f, cell?.Height ?? 0, _pos.y + 0.5f);
    }

    public void UpdateCell(Vector2Int _pos, Cell _cell)
    {
        if (!IsInBounds(_pos)) return;

        Cells[_pos.x, _pos.y] = _cell;
    }

    public Cell[] GetTilesInRadius(Vector2Int _center, float _radius)
    {
        FillTileBuffer(_center, _radius);

        var result = new Cell[TileBufferCount];
        System.Array.Copy(_TileBuffer, result, TileBufferCount);
        return result;
    }

    public void FillTileBuffer(Vector2Int _center, float _radius)
    {
        TileBufferCount = 0;
        var radiusSq   = _radius * _radius;
        var radiusCeil = Mathf.CeilToInt(_radius);

        for (var dx = -radiusCeil; dx <= radiusCeil; dx++)
        {
            for (var dy = -radiusCeil; dy <= radiusCeil; dy++)
            {
                var px = _center.x + dx;
                var py = _center.y + dy;

                if (px < 0 || px >= size || py < 0 || py >= size) continue;
                if (dx * dx + dy * dy > radiusSq) continue;

                if (TileBufferCount >= _TileBuffer.Length)
                    System.Array.Resize(ref _TileBuffer, _TileBuffer.Length * 2);

                _TileBuffer[TileBufferCount++] = Cells[px, py];
            }
        }
    }

    public bool IsInBounds(Vector2Int _pos)
    {
        return _pos.x >= 0 && _pos.x < size && _pos.y >= 0 && _pos.y < size;
    }

    public bool IsCellEmpty(Vector2Int _pos)
    {
        var cell = GetCell(_pos);
        return cell is { IsOccupied: false };
    }

    public void SetCellOccupied(Vector2Int _pos)
    {
        if (!IsInBounds(_pos)) return;

        var cell = Cells[_pos.x, _pos.y];
        cell.IsOccupied       = true;
        Cells[_pos.x, _pos.y] = cell;
    }

    public Cell[] GetNeighbors(Vector2Int _pos)
    {
        var neighbors = new List<Cell>();

        var directions = new[]
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        foreach (var dir in directions)
        {
            var neighborPos = _pos + dir;
            if (IsInBounds(neighborPos))
                neighbors.Add(Cells[neighborPos.x, neighborPos.y]);
        }

        return neighbors.ToArray();
    }
}