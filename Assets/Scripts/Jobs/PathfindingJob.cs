using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public struct PathRequest
{
    public int2 Start;
    public int2 End;

    public float NoiseOffsetX;
    public float NoiseOffsetY;
}

[BurstCompile]
public struct PathfindingProcessJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<GridJobUtilities.JobCellData> GridCells;
    [ReadOnly] public int                                       GridSize;
    [ReadOnly] public NativeArray<PathRequest>                  Requests;

    [ReadOnly] public float WaterPenalty;
    [ReadOnly] public float ElevationMultiplier;
    [ReadOnly] public float OccupiedPenalty;
    [ReadOnly] public float TerrainNoiseStrength;
    [ReadOnly] public float TerrainNoiseScale;

    public const int MAX_PATH_LENGTH = 1024;

    [NativeDisableParallelForRestriction] public NativeArray<int2> AllPaths;

    public NativeArray<int> PathLengths;

    private const float _SQRT2    = 1.41421356f;
    private const int   _MAX_ITER = 50000;

    public void Execute(int _index)
    {
        var request = Requests[_index];
        var start   = request.Start;
        var end     = request.End;

        if (start.Equals(end))
        {
            AddPoint(_index, 0, start);
            PathLengths[_index] = 1;
            return;
        }

        var openSet  = new NativeList<int>(256, Allocator.Temp);
        var closed   = new NativeHashSet<int>(1024, Allocator.Temp);
        var cameFrom = new NativeHashMap<int, int>(1024, Allocator.Temp);
        var gScore   = new NativeHashMap<int, float>(1024, Allocator.Temp);
        var fScore   = new NativeHashMap<int, float>(1024, Allocator.Temp);

        var startIdx = ToIndex(start);
        var endIdx   = ToIndex(end);

        openSet.Add(startIdx);
        gScore[startIdx] = 0f;
        fScore[startIdx] = OctileHeuristic(start, end);

        var pathFound  = false;
        var iterations = 0;

        while (openSet.Length > 0 && iterations < _MAX_ITER)
        {
            iterations++;

            var bestOpenIdx = 0;
            var bestF       = float.MaxValue;

            for (var i = 0; i < openSet.Length; i++)
            {
                var node = openSet[i];
                if (!fScore.TryGetValue(node, out var f) || !(f < bestF)) continue;
                bestF       = f;
                bestOpenIdx = i;
            }

            var currentIdx = openSet[bestOpenIdx];
            openSet.RemoveAtSwapBack(bestOpenIdx);

            if (currentIdx == endIdx)
            {
                pathFound = true;
                break;
            }

            if (!closed.Add(currentIdx)) continue;

            var currentPos = ToPos(currentIdx);

            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    var neighborPos = currentPos + new int2(dx, dy);

                    if (!IsInBounds(neighborPos)) continue;

                    var neighborIdx = ToIndex(neighborPos);

                    if (closed.Contains(neighborIdx)) continue;

                    var isDiagonal = dx != 0 && dy != 0;
                    var stepCost   = isDiagonal ? _SQRT2 : 1f;

                    var terrainCost = CalculateCost(currentPos, neighborPos, request);
                    if (terrainCost >= 9999f) continue;

                    var tentativeG = gScore[currentIdx] + stepCost * terrainCost;

                    if (gScore.TryGetValue(neighborIdx, out var existingG) && tentativeG >= existingG)
                        continue;

                    cameFrom[neighborIdx] = currentIdx;
                    gScore[neighborIdx]   = tentativeG;
                    fScore[neighborIdx]   = tentativeG + OctileHeuristic(neighborPos, end);

                    if (!openSet.Contains(neighborIdx))
                        openSet.Add(neighborIdx);
                }
            }
        }

        if (pathFound)
        {
            var path  = new NativeList<int2>(Allocator.Temp);
            var track = endIdx;
            path.Add(ToPos(track));

            while (cameFrom.ContainsKey(track))
            {
                track = cameFrom[track];
                path.Add(ToPos(track));
                if (track == startIdx) break;
            }

            var length = math.min(path.Length, MAX_PATH_LENGTH);
            PathLengths[_index] = length;

            for (var i = 0; i < length; i++)
                AddPoint(_index, i, path[length - 1 - i]);

            path.Dispose();
        }
        else
            PathLengths[_index] = 0;

        openSet.Dispose();
        closed.Dispose();
        cameFrom.Dispose();
        gScore.Dispose();
        fScore.Dispose();
    }

    private void AddPoint(int _pathIndex, int _pointIndex, int2 _point) => AllPaths[_pathIndex * MAX_PATH_LENGTH + _pointIndex] = _point;

    private        int  ToIndex(int2   _pos) => _pos.y * GridSize + _pos.x;
    private        int2 ToPos(int      _idx) => new(_idx % GridSize, _idx / GridSize);
    private        bool IsInBounds(int2 _pos) => _pos.x >= 0 && _pos.x < GridSize && _pos.y >= 0 && _pos.y < GridSize;

    private static float OctileHeuristic(int2 _a, int2 _b)
    {
        var dx = math.abs(_a.x - _b.x);
        var dy = math.abs(_a.y - _b.y);
        return math.max(dx, dy) + (_SQRT2 - 1f) * math.min(dx, dy);
    }

    private float CalculateCost(int2 _from, int2 _to, PathRequest _req)
    {
        var cell = GridCells[ToIndex(_to)];

        if (cell.Type is WorldGrid.CellType.WATER or WorldGrid.CellType.RIVER)
            return WaterPenalty > 0 ? WaterPenalty : 9999f;

        if (cell.IsOccupied || cell.HasPoi)
            return OccupiedPenalty > 0 ? OccupiedPenalty : 9999f;

        var fromCell = GridCells[ToIndex(_from)];
        var elevCost = math.abs(cell.Height - fromCell.Height) * ElevationMultiplier;

        var nx = _to.x * TerrainNoiseScale + _req.NoiseOffsetX;
        var ny = _to.y * TerrainNoiseScale + _req.NoiseOffsetY;

        var noiseVal = noise.cnoise(new float2(nx, ny)) * TerrainNoiseStrength;

        var baseCost = 1.0f;
        if (cell.Type is WorldGrid.CellType.ROAD or WorldGrid.CellType.BRIDGE)
            baseCost = 0.5f;

        return math.max(0.1f, baseCost + elevCost + noiseVal);
    }
}