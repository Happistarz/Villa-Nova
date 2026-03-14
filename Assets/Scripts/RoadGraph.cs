using System.Collections.Generic;
using UnityEngine;

public static class RoadGraph
{
    public enum EdgeType
    {
        EXTERNAL,
        MAIN,
        SECONDARY,
    }

    public struct Node
    {
        public          Vector2Int Position;
        public readonly string     Label;

        public Node(Vector2Int _pos, string _label)
        {
            Position = _pos;
            Label    = _label;
        }
    }

    public struct Edge
    {
        public int      FromIndex;
        public int      ToIndex;
        public EdgeType Type;
        public int      Priority;
    }

    public struct Graph
    {
        public List<Node> Nodes;
        public List<Edge> Edges;
    }

    public static Graph Build(WorldGrid                    _grid, Vector2Int _cityCenter,
                              IReadOnlyList<Vector2Int>    _poiPositions,
                              List<WorldGrid.NearCityData> _nearCities)
    {
        var graph = new Graph
        {
            Nodes = new List<Node>(),
            Edges = new List<Edge>(),
        };

        var centerIdx = AddNode(ref graph, _cityCenter, "Center");

        if (_nearCities != null)
        {
            for (var i = 0; i < _nearCities.Count; i++)
            {
                var nc  = _nearCities[i];
                var pos = RoadBuilder.ClampToGrid(nc.CityPos, _grid);
                var idx = AddNode(ref graph, pos, nc.Name ?? $"NearCity {i}");

                AddEdge(ref graph, idx, centerIdx, EdgeType.EXTERNAL, _priority: 0);
            }
        }

        var poiIndices = new List<int>();
        if (_poiPositions != null)
        {
            for (var i = 0; i < _poiPositions.Count; i++)
            {
                var idx = AddNode(ref graph, _poiPositions[i], $"POI {i}");
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

    private static int AddNode(ref Graph _graph, Vector2Int _pos, string _label)
    {
        _graph.Nodes.Add(new Node(_pos, _label));
        return _graph.Nodes.Count - 1;
    }

    private static void AddEdge(ref Graph _graph, int _from, int _to, EdgeType _type, int _priority)
    {
        _graph.Edges.Add(new Edge
        {
            FromIndex = _from,
            ToIndex   = _to,
            Type      = _type,
            Priority  = _priority,
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