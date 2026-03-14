using System.Collections.Generic;
using UnityEngine;

public static class Pathfinding
{
    private const float _SQRT2 = 1.41421356f;

    public static List<Vector2Int> FindPath(Vector2Int                  _start, Vector2Int _end, WorldGrid _grid,
                                            RoadCostCalculator.CostFunc _costFunc         = null,
                                            bool                        _allowOutOfBounds = false)
    {
        var openSet = new SortedSet<(float f, int id, Vector2Int pos)>(
            Comparer<(float f, int id, Vector2Int pos)>.Create((_a, _b) =>
            {
                var cmp = _a.f.CompareTo(_b.f);
                return cmp != 0 ? cmp : _a.id.CompareTo(_b.id);
            }));

        var cameFrom  = new Dictionary<Vector2Int, Vector2Int>();
        var gScore    = new Dictionary<Vector2Int, float>();
        var closed    = new HashSet<Vector2Int>();
        var idCounter = 0;

        gScore[_start] = 0f;
        openSet.Add((Heuristic(_start, _end), idCounter++, _start));

        while (openSet.Count > 0)
        {
            var (_, _, current) = openSet.Min;
            openSet.Remove(openSet.Min);

            if (current == _end)
                return ReconstructPath(cameFrom, current);

            if (!closed.Add(current)) continue;

            for (var i = 0; i < _DIRECTIONS.Length; i++)
            {
                var neighborPos = current + _DIRECTIONS[i];

                if (closed.Contains(neighborPos)) continue;

                if (!CanTraverse(neighborPos, _grid, _allowOutOfBounds))
                    continue;

                var stepCost = i < 4 ? 1f : _SQRT2;

                var moveCost = _costFunc?.Invoke(current, neighborPos, _grid) * stepCost ?? stepCost;

                var tentativeG = gScore[current] + moveCost;

                if (gScore.TryGetValue(neighborPos, out var existingG) && tentativeG >= existingG)
                    continue;

                cameFrom[neighborPos] = current;
                gScore[neighborPos]   = tentativeG;

                var f = tentativeG + Heuristic(neighborPos, _end);
                openSet.Add((f, idCounter++, neighborPos));
            }
        }

        return null;
    }

    private static readonly Vector2Int[] _DIRECTIONS =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
        new(1, 1), new(1, -1), new(-1, 1), new(-1, -1),
    };

    private static float Heuristic(Vector2Int _a, Vector2Int _b)
    {
        var dx = Mathf.Abs(_a.x - _b.x);
        var dy = Mathf.Abs(_a.y - _b.y);
        return Mathf.Max(dx, dy) + (_SQRT2 - 1f) * Mathf.Min(dx, dy);
    }

    private static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> _cameFrom, Vector2Int _current)
    {
        var path = new List<Vector2Int> { _current };
        while (_cameFrom.ContainsKey(_current))
        {
            _current = _cameFrom[_current];
            path.Add(_current);
        }

        path.Reverse();
        return path;
    }
    
    public static bool CanTraverse(Vector2Int _pos, WorldGrid _grid, bool _allowOutOfBounds)
    {
        return _grid.IsInBounds(_pos) || _allowOutOfBounds;
    }
}