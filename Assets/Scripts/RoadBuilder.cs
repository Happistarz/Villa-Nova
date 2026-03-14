using System.Collections.Generic;
using UnityEngine;

public static class RoadBuilder
{

    public static void BuildFromGraph(WorldGrid _grid, RoadGraph.Graph _graph, RoadSettings _settings)
    {
        foreach (var edge in _graph.Edges)
        {
            var from = _graph.Nodes[edge.FromIndex].Position;
            var to   = _graph.Nodes[edge.ToIndex].Position;

            var perEdgeSettings = _settings;
            perEdgeSettings.noiseOffsetX = Random.Range(0f, 1000f);
            perEdgeSettings.noiseOffsetY = Random.Range(0f, 1000f);

            if (edge.Type == RoadGraph.EdgeType.SECONDARY)
                perEdgeSettings.roadWidth = Mathf.Max(1, perEdgeSettings.roadWidth - 1);

            var path = FindRoadPath(from, to, _grid, perEdgeSettings);

            if (path == null)
            {
                var fromNode = _graph.Nodes[edge.FromIndex];
                var toNode   = _graph.Nodes[edge.ToIndex];
                Debug.LogWarning($"[RoadBuilder] No path: {fromNode.Label} → {toNode.Label}");
                continue;
            }

            path = MathHelper.SmoothPath(path);
            StampRoad(path, perEdgeSettings.roadWidth, _grid, perEdgeSettings.maxBridgeLength);
        }
    }

    public static void BuildExternalRoads(WorldGrid    _grid, List<WorldGrid.NearCityData> _nearCities,
                                          RoadSettings _settings)
    {
        if (_nearCities == null || _nearCities.Count == 0) return;

        var cityCenter = new Vector2Int(_grid.size / 2, _grid.size / 2);

        foreach (var nearCity in _nearCities)
        {
            var perRoadSettings = _settings;
            perRoadSettings.noiseOffsetX = Random.Range(0f, 1000f);
            perRoadSettings.noiseOffsetY = Random.Range(0f, 1000f);

            var clampedTarget = ClampToGrid(nearCity.CityPos, _grid);
            var path          = FindRoadPath(cityCenter, clampedTarget, _grid, perRoadSettings);

            if (path == null)
            {
                Debug.LogWarning($"[RoadBuilder] No path found to near city at {nearCity.CityPos}");
                continue;
            }

            path = MathHelper.SmoothPath(path);
            StampRoad(path, perRoadSettings.roadWidth, _grid, perRoadSettings.maxBridgeLength);
        }
    }

    public static void BuildInternalRoads(WorldGrid        _grid,         Vector2Int   _cityCenter,
                                          List<Vector2Int> _poiPositions, RoadSettings _settings)
    {
        foreach (var poi in _poiPositions)
        {
            var perRoadSettings = _settings;
            perRoadSettings.noiseOffsetX = Random.Range(0f, 1000f);
            perRoadSettings.noiseOffsetY = Random.Range(0f, 1000f);

            var clampedPoi = ClampToGrid(poi, _grid);
            var path       = FindRoadPath(_cityCenter, clampedPoi, _grid, perRoadSettings);

            if (path == null)
            {
                Debug.LogWarning($"[RoadBuilder] No path found to POI at {poi}");
                continue;
            }

            path = MathHelper.SmoothPath(path);
            StampRoad(path, perRoadSettings.roadWidth, _grid, perRoadSettings.maxBridgeLength);
        }
    }

    private static List<Vector2Int> FindRoadPath(Vector2Int _start, Vector2Int   _end,
                                                 WorldGrid  _grid,  RoadSettings _settings)
    {
        var costFunc = RoadCostCalculator.BuildCostFunction(_settings);

        return Pathfinding.FindPath(_start, _end, _grid, costFunc, _settings.allowOutOfBounds);
    }

    public static void StampRoad(List<Vector2Int> _path, int _width, WorldGrid _grid, int _maxBridgeLength)
    {
        var bridgeCells = new HashSet<Vector2Int>();
        CollectBridgeCells(_path, _grid, _maxBridgeLength, bridgeCells);

        var half = _width / 2;

        foreach (var center in _path)
        {
            for (var dx = -half; dx <= half; dx++)
            {
                for (var dy = -half; dy <= half; dy++)
                {
                    var pos = new Vector2Int(center.x + dx, center.y + dy);
                    if (!_grid.IsInBounds(pos)) continue;

                    var cell = _grid.Cells[pos.x, pos.y];

                    if (cell.Type is WorldGrid.CellType.WATER or WorldGrid.CellType.RIVER
                        && bridgeCells.Contains(center))
                    {
                        cell.Type = WorldGrid.CellType.BRIDGE;
                        _grid.UpdateCell(pos, cell);
                        continue;
                    }

                    if (!CanPlaceRoad(pos, _grid)) continue;

                    cell.Type = WorldGrid.CellType.ROAD;
                    _grid.UpdateCell(pos, cell);
                }
            }
        }
    }

    private static void CollectBridgeCells(List<Vector2Int> _path,            WorldGrid           _grid,
                                           int              _maxBridgeLength, HashSet<Vector2Int> _bridgeCells)
    {
        var waterRun = new List<Vector2Int>();

        foreach (var pos in _path)
        {
            if (!_grid.IsInBounds(pos))
            {
                FlushWaterRun(waterRun, _maxBridgeLength, _bridgeCells);
                continue;
            }

            var cell = _grid.Cells[pos.x, pos.y];

            if (cell.Type is WorldGrid.CellType.WATER or WorldGrid.CellType.RIVER)
                waterRun.Add(pos);
            else
                FlushWaterRun(waterRun, _maxBridgeLength, _bridgeCells);
        }

        FlushWaterRun(waterRun, _maxBridgeLength, _bridgeCells);
    }

    private static void FlushWaterRun(List<Vector2Int>    _waterRun, int _maxLength,
                                      HashSet<Vector2Int> _bridgeCells)
    {
        if (_waterRun.Count > 0 && _waterRun.Count <= _maxLength)
        {
            foreach (var pos in _waterRun)
                _bridgeCells.Add(pos);
        }

        _waterRun.Clear();
    }
    
    public static bool CanPlaceRoad(Vector2Int _pos, WorldGrid _grid)
    {
        if (!_grid.IsInBounds(_pos)) return false;

        var cell = _grid.Cells[_pos.x, _pos.y];

        if (!cell.Is(WorldGrid.CellType.PLAIN)) return false;
        if (cell.IsOccupied) return false;
        return !cell.POI;
    }
    
    public static Vector2Int ClampToGrid(Vector2Int _pos, WorldGrid _grid)
    {
        return new Vector2Int(
            Mathf.Clamp(_pos.x, 0, _grid.size - 1),
            Mathf.Clamp(_pos.y, 0, _grid.size - 1)
        );
    }
}