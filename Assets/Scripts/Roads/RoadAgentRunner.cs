using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RoadAgentRunner
{
    /// <summary>
    /// Internal state struct for a road agent: position, direction, steps remaining, road type and branching parameters.
    /// </summary>
    private struct AgentState
    {
        public Vector2Int         Position;
        public Vector2Int         Direction;
        public int                StepsRemaining;
        public RoadGraph.EdgeType RoadType;
        public int                RoadWidth;
        public float              BranchProbability;
        public int                MinBranchSteps;
    }

    private static readonly Vector2Int[] _CARDINAL =
    {
        Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
    };

    public static IEnumerator Run(WorldGrid _grid, RoadAgentConfig _config,
                                  List<Vector2Int> _spawnCells, int _seed)
    {
        if (_spawnCells == null || _spawnCells.Count == 0 || !_config)
            yield break;

        var rng         = new System.Random(_seed);
        var noiseOffset = (float)(rng.NextDouble() * 1000.0);
        var queue       = new Queue<AgentState>();
        var spawnCount  = Mathf.Min(_config.maxAgents, _spawnCells.Count);

        for (var i = 0; i < spawnCount; i++)
        {
            queue.Enqueue(new AgentState
            {
                Position          = _spawnCells[rng.Next(0, _spawnCells.Count)],
                Direction         = _CARDINAL[rng.Next(0, 4)],
                StepsRemaining    = rng.Next(_config.minSteps, _config.maxSteps + 1),
                RoadType          = _config.roadType,
                RoadWidth         = _config.roadWidth,
                BranchProbability = _config.branchProbability,
                MinBranchSteps    = _config.minBranchSteps
            });
        }

        var stampCount = 0;
        var agentCount = 0;
        var maxAgents  = _config.maxAgents * 4;

        while (queue.Count > 0 && agentCount < maxAgents)
        {
            var agent = queue.Dequeue();
            agentCount++;

            for (var step = 0; step < agent.StepsRemaining; step++)
            {
                if (step > 0 && step % 5 == 0)
                {
                    var n = Mathf.PerlinNoise(
                        agent.Position.x * _config.dirNoiseScale + noiseOffset,
                        agent.Position.y * _config.dirNoiseScale + noiseOffset);

                    if (n > 1f - _config.dirNoiseStrength / 45f)
                        agent.Direction = Perpendicular(agent.Direction, rng.Next(0, 2) == 0);
                }

                var next = agent.Position + agent.Direction;

                if (!_grid.IsInBounds(next)) break;

                var cell = _grid.Cells[next.x, next.y];

                if (cell.Is(WorldGrid.CellType.WATER, WorldGrid.CellType.RIVER)) break;
                if (cell.UrbanityLevel < _config.minUrbanity) break;
                if (cell.Is(WorldGrid.CellType.ROAD) && step > 2) break;

                if (RoadBuilder.CanPlaceRoad(next, _grid))
                {
                    StampAgentCell(_grid, next, agent.RoadType, agent.RoadWidth);
                    stampCount++;
                }

                agent.Position = next;

                if (step >= agent.MinBranchSteps &&
                    rng.NextDouble() < agent.BranchProbability &&
                    agentCount + queue.Count < maxAgents)
                {
                    var branchSteps = rng.Next(
                        agent.MinBranchSteps,
                        Mathf.Max(agent.MinBranchSteps + 1, agent.StepsRemaining - step));

                    queue.Enqueue(new AgentState
                    {
                        Position          = agent.Position,
                        Direction         = Perpendicular(agent.Direction, rng.Next(0, 2) == 0),
                        StepsRemaining    = branchSteps,
                        RoadType          = RoadGraph.EdgeType.ALLEY,
                        RoadWidth         = 1,
                        BranchProbability = agent.BranchProbability * 0.5f,
                        MinBranchSteps    = agent.MinBranchSteps
                    });
                }

                if (stampCount % 40 == 0)
                    yield return null;
            }
        }
    }

    private static void StampAgentCell(WorldGrid _grid, Vector2Int _pos,
                                       RoadGraph.EdgeType _type, int _width)
    {
        var half = _width / 2;

        for (var dx = -half; dx <= half; dx++)
        {
            for (var dy = -half; dy <= half; dy++)
            {
                var pos = new Vector2Int(_pos.x + dx, _pos.y + dy);
                if (!RoadBuilder.CanPlaceRoad(pos, _grid)) continue;

                var cell = _grid.Cells[pos.x, pos.y];
                if (cell.RoadTier > 0 && cell.RoadTier < (byte)_type) continue;

                cell.Type     = WorldGrid.CellType.ROAD;
                cell.RoadTier = (byte)_type;
                _grid.UpdateCell(pos, cell);
            }
        }
    }

    private static Vector2Int Perpendicular(Vector2Int _dir, bool _left)
    {
        return _left
            ? new Vector2Int(-_dir.y, _dir.x)
            : new Vector2Int(_dir.y, -_dir.x);
    }
}
