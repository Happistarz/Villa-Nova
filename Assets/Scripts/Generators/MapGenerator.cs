using System;
using System.Collections;
using Core.Extensions;
using Core.Patterns;
using UnityEngine;

public class MapGenerator : MonoSingleton<MapGenerator>, IGenerator
{
    public string Name => "Map";

    public static bool IsGenerating { get; set; }

    bool IGenerator.IsGenerating => IsGenerating;

    public event Action OnGenerationComplete;

    [Header("Terrain")]
    [Range(1f, 100f)] public float elevationNoiseScale = 40f;

    [Range(0f,    500f)] public float elevationScale = 1f;
    [Range(0.01f, 1f)]   public float heightStep     = 0.5f;

    [Min(0f)] public float elevationAmplitude = 20f;
    [Min(0f)] public float elevationFrequency = 1f;

    [Range(1, 6)] public int terrainOctaves = 4;

    [Header("Terrain Variation")]
    [Range(0f, 1f)] public float hillWeight = 0.3f;

    [Range(0f, 1f)]   public float ridgeWeight        = 0.2f;
    [Range(1f, 100f)] public float hillNoiseScale     = 15f;
    [Range(1f, 100f)] public float ridgeNoiseScale    = 25f;
    [Range(0f, 50f)]  public float domainWarpStrength = 8f;
    [Range(1f, 100f)] public float domainWarpScale    = 30f;

    [Header("Coasts")]
    [Range(0, 8)] public int maxCoastPatches = 8;

    [Range(5,  60)]  public int coastMinLength = 15;
    [Range(10, 120)] public int coastMaxLength = 74;
    [Range(2,  20)]  public int coastMinDepth  = 4;
    [Range(5,  40)]  public int coastMaxDepth  = 29;

    [Header("Lakes")]
    [Range(0, 5)] public int maxLakes = 5;

    [Range(2, 10)] public int lakeMinRadius = 3;
    [Range(3, 20)] public int lakeMaxRadius = 8;

    [Header("River")]
    [Range(0f, 2f)] public float waterDepthOffset = 0.3f;

    [Range(0, 5)] public int maxRivers = 3;

    [Range(1f, 20f)] public float riverNoiseScale = 7.4f;

    [Range(0f,    1f)]    public float riverAmplitude   = 0.078f;
    [Range(10,    500)]   public int   riverResolution  = 50;
    [Range(0.01f, 0.15f)] public float maxRiverCoverage = 0.05f;
    [Range(1,     6)]     public int   riverMinWidth    = 1;
    [Range(1,     6)]     public int   riverMaxWidth    = 3;
    [Range(1,     4)]     public int   riverOctaves     = 3;

    private WorldGrid _grid;

    private void Start()
    {
        _grid = WorldGrid.Instance;
    }

    public IEnumerator Generate(WorldGrid _generationGrid)
    {
        _grid        = _generationGrid;
        IsGenerating = true;

        _grid.InitGrid();

        GenerateTerrain();
        GenerateCoasts();
        GenerateLakes();

        var rng        = GameManager.Instance.RandomEngine;
        var riverCount = rng.Range(0, maxRivers + 1);
        for (var r = 0; r < riverCount; r++)
            GenerateRiver();

        IsGenerating = false;
        OnGenerationComplete?.Invoke();
        yield break;
    }

    private void GenerateTerrain()
    {
        var size = _grid.size;
        var rng  = GameManager.Instance.RandomEngine;

        var elevationOffsetX = rng.Range(0f, 1000f);
        var elevationOffsetY = rng.Range(0f, 1000f);
        var hillOffsetX      = rng.Range(0f, 1000f);
        var hillOffsetY      = rng.Range(0f, 1000f);
        var ridgeOffsetX     = rng.Range(0f, 1000f);
        var ridgeOffsetY     = rng.Range(0f, 1000f);

        var baseWeight = 1f - hillWeight - ridgeWeight;

        for (var x = 0; x < size; x++)
        {
            for (var y = 0; y < size; y++)
            {
                var warped = MathHelper.DomainWarp(x + elevationOffsetX, y + elevationOffsetY,
                                                   domainWarpScale, domainWarpStrength);

                var baseNoise = MathHelper.FBm(warped.x / elevationNoiseScale,
                                               warped.y / elevationNoiseScale, terrainOctaves,
                                               elevationAmplitude, elevationFrequency);

                var hillNoise = MathHelper.FBm((x + hillOffsetX) / hillNoiseScale,
                                               (y + hillOffsetY) / hillNoiseScale, 3);

                var ridgeNoise = MathHelper.RidgedFBm((x + ridgeOffsetX) / ridgeNoiseScale,
                                                      (y + ridgeOffsetY) / ridgeNoiseScale, 4);

                var combined = baseNoise * baseWeight + hillNoise * hillWeight + ridgeNoise * ridgeWeight;
                var height   = MathHelper.Quantize(combined * elevationScale, heightStep);

                _grid.Cells[x, y] = new WorldGrid.Cell
                {
                    Type   = WorldGrid.CellType.PLAIN,
                    Height = height
                };
            }
        }
    }

    private void GenerateCoasts()
    {
        if (maxCoastPatches <= 0) return;

        var size       = _grid.size;
        var rng        = GameManager.Instance.RandomEngine;
        var patchCount = rng.Range(0, maxCoastPatches + 1);

        for (var i = 0; i < patchCount; i++)
        {
            var edge = rng.Range(0, 4);

            var length = rng.Range(coastMinLength, Mathf.Min(coastMaxLength + 1, size));
            var depth  = rng.Range(coastMinDepth,  coastMaxDepth + 1);

            var start = rng.Range(0, size - length);

            var noiseOffsetX = rng.Range(0f, 1000f);
            var noiseOffsetY = rng.Range(0f, 1000f);

            for (var along = 0; along < length; along++)
            {
                var t          = (float)along / length;
                var depthNoise = MathHelper.FBm(t * 5f + noiseOffsetX, noiseOffsetY, terrainOctaves);

                var edgeFade = MathHelper.TriangleWave(t);
                edgeFade = Mathf.Pow(edgeFade, 0.5f);

                var localDepth = Mathf.RoundToInt(depth * depthNoise * edgeFade);
                if (localDepth <= 0) continue;

                for (var d = 0; d < localDepth; d++)
                {
                    int x, y;
                    switch (edge)
                    {
                        case 0: // Left edge
                            x = d;
                            y = start + along;
                            break;
                        case 1: // Right edge
                            x = size  - 1 - d;
                            y = start + along;
                            break;
                        case 2: // Top edge
                            x = start + along;
                            y = d;
                            break;
                        default: // Bottom edge
                            x = start + along;
                            y = size  - 1 - d;
                            break;
                    }

                    var pos = new Vector2Int(x, y);
                    if (!_grid.IsInBounds(pos)) continue;

                    _grid.Cells[x, y].Type   =  WorldGrid.CellType.WATER;
                    _grid.Cells[x, y].Height -= waterDepthOffset;
                }
            }
        }
    }

    private void GenerateLakes()
    {
        if (maxLakes <= 0) return;

        var size   = _grid.size;
        var margin = lakeMaxRadius + 2;
        var rng    = GameManager.Instance.RandomEngine;

        var lakeCount = rng.Range(0, maxLakes + 1);

        for (var i = 0; i < lakeCount; i++)
        {
            var cx = rng.Range(margin,        size          - margin);
            var cy = rng.Range(margin,        size          - margin);
            var rx = rng.Range(lakeMinRadius, lakeMaxRadius + 1);
            var ry = rng.Range(lakeMinRadius, lakeMaxRadius + 1);

            var noiseOffset = rng.Range(0f, 1000f);

            foreach (var p in MathHelper.GetPointsInEllipse(new Vector2Int(cx, cy), rx, ry))
            {
                var dx = p.x - cx;
                var dy = p.y - cy;

                var normalizedDist = MathHelper.GetEllipseNormalizedDistance(dx, dy, rx, ry);

                var angle     = Mathf.Atan2(dy, dx);
                var edgeNoise = MathHelper.FBm(angle * 3f, noiseOffset, 2);

                if (normalizedDist > 0.6f + edgeNoise * 0.4f) continue;
                if (!_grid.IsInBounds(p)) continue;

                _grid.Cells[p.x, p.y].Type   =  WorldGrid.CellType.WATER;
                _grid.Cells[p.x, p.y].Height -= waterDepthOffset;
            }
        }
    }

    private void PlaceRiverRadius(Vector2Int _center, int _radius, ref int _count, int _max)
    {
        foreach (var p in MathHelper.GetPointsInCircle(_center, _radius))
        {
            if (_count >= _max) return;

            if (!_grid.IsInBounds(p) || _grid.Cells[p.x, p.y].Is(WorldGrid.CellType.RIVER) ||
                _grid.Cells[p.x, p.y].Is(WorldGrid.CellType.WATER)) continue;

            _grid.Cells[p.x, p.y].Type   =  WorldGrid.CellType.RIVER;
            _grid.Cells[p.x, p.y].Height -= waterDepthOffset;
            _count++;
        }
    }

    private Vector2Int GetEdgePosition(int _edge)
    {
        var size = _grid.size;
        var rng  = GameManager.Instance.RandomEngine;
        return _edge switch
        {
            0 => new Vector2Int(0,        rng.Range(0, size)),          // Left edge
            1 => new Vector2Int(size - 1, rng.Range(0, size)),          // Right edge
            2 => new Vector2Int(rng.Range(0,           size), 0),       // Top edge
            _ => new Vector2Int(rng.Range(0,           size), size - 1) // Bottom edge
        };
    }

    private void GenerateRiver()
    {
        var size        = _grid.size;
        var minDistance = size / 2f;
        var rng         = GameManager.Instance.RandomEngine;

        Vector2Int startPos, endPos;

        var safetyRetries = 0;
        do
        {
            var startEdge = rng.Range(0, 4);
            startPos = GetEdgePosition(startEdge);
            var endEdge = (startEdge + rng.Range(1, 4)) % 4;
            endPos = GetEdgePosition(endEdge);
            safetyRetries++;
        } while (Vector2Int.Distance(startPos, endPos) < minDistance && safetyRetries < 100);

        var riverNoiseOffset = rng.Range(0f, 1000f);
        var widthNoiseOffset = rng.Range(0f, 1000f);

        var startF = new Vector2(startPos.x, startPos.y);
        var endF   = new Vector2(endPos.x,   endPos.y);
        var along  = (endF - startF).normalized;
        var perp   = MathHelper.GetPerpendicular(along);

        var scaledAmplitude  = riverAmplitude * size;
        var scaledResolution = Mathf.Max(riverResolution, Mathf.RoundToInt(Vector2.Distance(startF, endF) * 2f));

        Vector2Int? prevCell       = null;
        var         riverCellCount = 0;
        var         maxRiverCells  = Mathf.RoundToInt(size * size * maxRiverCoverage);

        // Iterate along the river path and place river cells with noise-based offsets and widths
        for (var i = 0; i <= scaledResolution; i++)
        {
            if (riverCellCount >= maxRiverCells) break;

            var t     = (float)i / scaledResolution;
            var baseP = Vector2.Lerp(startF, endF, t);

            var noiseVal = MathHelper.FBm(t * riverNoiseScale, riverNoiseOffset, riverOctaves);
            var offset   = (noiseVal - 0.5f) * 2f * scaledAmplitude;

            var displaced = baseP + perp * offset;
            var cell      = new Vector2Int(Mathf.RoundToInt(displaced.x), Mathf.RoundToInt(displaced.y));

            var widthNoise = MathHelper.FBm(t * riverNoiseScale * 2f, widthNoiseOffset, 2);
            var radius     = Mathf.RoundToInt(Mathf.Lerp(riverMinWidth, riverMaxWidth, widthNoise));

            if (prevCell.HasValue && prevCell.Value != cell)
            {
                foreach (var p in MathHelper.BresenhamLine(prevCell.Value, cell))
                {
                    if (riverCellCount >= maxRiverCells) break;
                    PlaceRiverRadius(p, radius, ref riverCellCount, maxRiverCells);
                }
            }
            else if (_grid.IsInBounds(cell))
                PlaceRiverRadius(cell, radius, ref riverCellCount, maxRiverCells);

            prevCell = cell;
        }
    }
}