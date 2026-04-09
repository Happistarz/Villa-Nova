using System.Collections.Generic;
using UnityEngine;

public static class RoadGraph
{
    public enum EdgeType
    {
        EXTERNAL,
        MAIN,
        SECONDARY,
        ALLEY
    }

    /// <summary>
    /// Represents a node in the road graph, which can be a city center, a point of interest (POI), or a nearby city
    /// </summary>
    public struct Node
    {
        public Vector2Int Position;

        public Node(Vector2Int _pos)
        {
            Position = _pos;
        }
    }

    /// <summary>
    /// Represents a directed edge in the road graph, connecting two nodes with a specific type and priority
    /// </summary>
    public struct Edge
    {
        public int      FromIndex;
        public int      ToIndex;
        public EdgeType Type;
        public int      Priority;
    }

    /// <summary>
    /// Represents the entire road graph, containing a list of nodes and edges that define the connections between them
    /// </summary>
    public struct Graph
    {
        public List<Node> Nodes;
        public List<Edge> Edges;
    }

    /// <summary>
    /// Builds a road graph based on the world grid, city center, points of interest, and nearby cities
    /// The graph will have nodes for the city center, each POI, and each nearby city, with edges connecting them according to their types and priorities
    /// </summary>
    public static Graph Build(WorldGrid                    _grid, Vector2Int _cityCenter,
                              IReadOnlyList<Vector2Int>    _poiPositions,
                              List<WorldGrid.NearCityData> _nearCities)
    {
        var graph = new Graph
        {
            Nodes = new List<Node>(),
            Edges = new List<Edge>()
        };

        var centerIdx = AddNode(ref graph, _cityCenter);

        if (_nearCities != null)
        {
            for (var i = 0; i < _nearCities.Count; i++)
            {
                var nc       = _nearCities[i];
                var clamped  = RoadBuilder.ClampToGrid(nc.CityPos, _grid);
                var walkable = FindNearestWalkable(_grid, clamped);
                var idx      = AddNode(ref graph, walkable);

                AddEdge(ref graph, idx, centerIdx, EdgeType.EXTERNAL, _priority: 0);
            }
        }

        var poiIndices = new List<int>();
        if (_poiPositions != null)
        {
            for (var i = 0; i < _poiPositions.Count; i++)
            {
                var idx = AddNode(ref graph, _poiPositions[i]);
                poiIndices.Add(idx);
            }
        }

        for (var i = 0; i < poiIndices.Count; i++)
        {
            if (i == 0)
                AddEdge(ref graph, poiIndices[i], centerIdx, EdgeType.MAIN, _priority: 1);
            else
            {
                var nearestIdx = FindNearestNode(graph, poiIndices[i], centerIdx, poiIndices, i);
                AddEdge(ref graph, poiIndices[i], nearestIdx, EdgeType.SECONDARY, _priority: 2);
            }
        }

        graph.Edges.Sort((_a, _b) => _a.Priority.CompareTo(_b.Priority));

        return graph;
    }

    private static int AddNode(ref Graph _graph, Vector2Int _pos)
    {
        _graph.Nodes.Add(new Node(_pos));
        return _graph.Nodes.Count - 1;
    }

    /// <summary>
    /// Spirals outward from a position to find the nearest cell that is not water/river and is in bounds
    /// Returns the original position if nothing better is found within a reasonable radius
    /// </summary>
    private static Vector2Int FindNearestWalkable(WorldGrid _grid, Vector2Int _pos)
    {
        if (_grid.IsInBounds(_pos))
        {
            var cell = _grid.Cells[_pos.x, _pos.y];
            if (!cell.Is(WorldGrid.CellType.WATER, WorldGrid.CellType.RIVER))
                return _pos;
        }

        const int MAX_SEARCH = 30;
        for (var r = 1; r <= MAX_SEARCH; r++)
        {
            for (var dx = -r; dx <= r; dx++)
            {
                for (var dy = -r; dy <= r; dy++)
                {
                    if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue; // only perimeter

                    var candidate = new Vector2Int(_pos.x + dx, _pos.y + dy);
                    if (!_grid.IsInBounds(candidate)) continue;

                    var c = _grid.Cells[candidate.x, candidate.y];
                    if (!c.Is(WorldGrid.CellType.WATER, WorldGrid.CellType.RIVER))
                        return candidate;
                }
            }
        }

        return _pos;
    }

    private static void AddEdge(ref Graph _graph, int _from, int _to, EdgeType _type, int _priority)
    {
        _graph.Edges.Add(new Edge
        {
            FromIndex = _from,
            ToIndex   = _to,
            Type      = _type,
            Priority  = _priority
        });
    }

    private static int FindNearestNode(Graph     _graph,      int _poiIdx, int _centerIdx,
                                       List<int> _poiIndices, int _upToExclusive)
    {
        var poiPos     = _graph.Nodes[_poiIdx].Position;
        var bestIdx    = _centerIdx;
        var bestDistSq = (poiPos - _graph.Nodes[_centerIdx].Position).sqrMagnitude;

        for (var i = 0; i < _upToExclusive; i++)
        {
            var otherPos = _graph.Nodes[_poiIndices[i]].Position;
            var distSq   = (poiPos - otherPos).sqrMagnitude;
            if (distSq >= bestDistSq) continue;

            bestDistSq = distSq;
            bestIdx    = _poiIndices[i];
        }

        return bestIdx;
    }
}