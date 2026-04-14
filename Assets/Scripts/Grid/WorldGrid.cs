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
        HOUSE
    }

    /// <summary>
    /// Single cell on the grid. Stores terrain type, height, POI and road information
    /// </summary>
    public struct Cell
    {
        public CellType Type;
        public float    Height;
        public POIData  POI;
        public bool     IsOccupied;

        // Urbanity level from 0 to 1 (rural to urban)
        public float UrbanityLevel;

        // For road cells, indicates the tier of the road (HIGHWAY, MAIN, ALLEY...)
        public int RoadTier;

        /// Shorthand for checking if the cell is a specific type or one of multiple types
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

    /// <summary>
    /// Data for a city visible on the map border
    /// </summary>
    public class NearCityData
    {
        public string     Name;
        public Vector2Int CityPos;
        public float      Distance;
    }

    public List<NearCityData> NearCities = new();

    public Cell[,] Cells;

    // Shared buffer for storing cells within a radius during generation and validation
    private static Cell[] _TileBuffer = new Cell[256];

    /// Number of valid cells currently in the tile buffer after calling FillTileBuffer
    public static int TileBufferCount { get; private set; }

    public static Cell[] TileBuffer => _TileBuffer;

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

    /// <summary>
    /// Fills the shared tile buffer with cells inside the given radius
    /// </summary>
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
}