using Core.Patterns;
using UnityEngine;
using Random = UnityEngine.Random;

public class MapGenerator : MonoSingleton<MapGenerator>
{
    public event System.Action OnMapGenerated;
    public event System.Action OnGenerationFullyComplete;

    public static bool IsGenerating { get; set; }

    [Header("General")]
    public int  seed;
    public bool useRandomSeed = true;

    [Header("Terrain")]
    [Range(1f, 50f)] public float noiseScale = 20f;

    [Range(1f,    100f)] public float elevationNoiseScale = 40f;
    [Range(0f,    5f)]   public float elevationScale      = 1f;
    [Range(0.01f, 1f)]   public float heightStep          = 0.5f;

    [Range(1, 6)] public int terrainOctaves = 4;

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

    public void GenerateMap()
    {
        if (IsGenerating) return;
        IsGenerating = true;

        if (useRandomSeed)
            seed = System.DateTime.Now.Millisecond;

        Random.InitState(seed);

        _grid.InitGrid();

        GenerateTerrain();
        GenerateCoasts();
        GenerateLakes();

        var riverCount = Random.Range(0, 3);
        for (var r = 0; r < riverCount; r++)
            GenerateRiver();

        OnMapGenerated?.Invoke();
    }

    public void NotifyGenerationComplete()
    {
        IsGenerating = false;
        OnGenerationFullyComplete?.Invoke();
    }

    private void GenerateTerrain()
    {
        var size = _grid.size;

        var elevationOffsetX = Random.Range(0f, 1000f);
        var elevationOffsetY = Random.Range(0f, 1000f);

        for (var x = 0; x < size; x++)
        {
            for (var y = 0; y < size; y++)
            {
                var noiseVal = MathHelper.FBm((x + elevationOffsetX) / elevationNoiseScale,
                                              (y + elevationOffsetY) / elevationNoiseScale, terrainOctaves);
                var height = MathHelper.Quantize(noiseVal * elevationScale, heightStep);

                _grid.Cells[x, y] = new WorldGrid.Cell
                {
                    Type     = WorldGrid.CellType.PLAIN,
                    Position = new Vector2Int(x, y),
                    Height   = height
                };
            }
        }
    }

    private void GenerateCoasts()
    {
        if (maxCoastPatches <= 0) return;

        var size       = _grid.size;
        var patchCount = Random.Range(0, maxCoastPatches + 1);

        for (var i = 0; i < patchCount; i++)
        {
            var edge = Random.Range(0, 4);

            var length = Random.Range(coastMinLength, Mathf.Min(coastMaxLength + 1, size));
            var depth  = Random.Range(coastMinDepth,  coastMaxDepth + 1);

            var start = Random.Range(0, size - length);

            var noiseOffsetX = Random.Range(0f, 1000f);
            var noiseOffsetY = Random.Range(0f, 1000f);

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
                        case 0:
                            x = d;
                            y = start + along;
                            break;
                        case 1:
                            x = size - 1 - d;
                            y = start + along;
                            break;
                        case 2:
                            x = start + along;
                            y = d;
                            break;
                        default:
                            x = start + along;
                            y = size - 1 - d;
                            break;
                    }

                    var pos = new Vector2Int(x, y);
                    if (!_grid.IsInBounds(pos)) continue;

                    _grid.Cells[x, y].Type   = WorldGrid.CellType.WATER;
                    _grid.Cells[x, y].Height = -0.5f;
                }
            }
        }
    }

    private void GenerateLakes()
    {
        if (maxLakes <= 0) return;

        var size      = _grid.size;
        var lakeCount = Random.Range(0, maxLakes + 1);
        var margin    = lakeMaxRadius + 2;

        for (var i = 0; i < lakeCount; i++)
        {
            var cx = Random.Range(margin,        size          - margin);
            var cy = Random.Range(margin,        size          - margin);
            var rx = Random.Range(lakeMinRadius, lakeMaxRadius + 1);
            var ry = Random.Range(lakeMinRadius, lakeMaxRadius + 1);

            var noiseOffset = Random.Range(0f, 1000f);

            foreach (var p in MathHelper.GetPointsInEllipse(new Vector2Int(cx, cy), rx, ry))
            {
                var dx = p.x - cx;
                var dy = p.y - cy;

                var normalizedDist = MathHelper.GetEllipseNormalizedDistance(dx, dy, rx, ry);

                var angle     = Mathf.Atan2(dy, dx);
                var edgeNoise = MathHelper.FBm(angle * 3f, noiseOffset, 2);

                if (normalizedDist > 0.6f + edgeNoise * 0.4f) continue;
                if (!_grid.IsInBounds(p)) continue;

                _grid.Cells[p.x, p.y].Type   = WorldGrid.CellType.WATER;
                _grid.Cells[p.x, p.y].Height = -0.5f;
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

            _grid.Cells[p.x, p.y].Type   = WorldGrid.CellType.RIVER;
            _grid.Cells[p.x, p.y].Height = -0.5f;
            _count++;
        }
    }

    private Vector2Int GetEdgePosition(int _edge)
    {
        var size = _grid.size;
        return _edge switch
        {
            0 => new Vector2Int(0,        Random.Range(0, size)),
            1 => new Vector2Int(size - 1, Random.Range(0, size)),
            2 => new Vector2Int(Random.Range(0,           size), 0),
            _ => new Vector2Int(Random.Range(0,           size), size - 1)
        };
    }

    private void GenerateRiver()
    {
        var size        = _grid.size;
        var minDistance  = size / 2f;

        Vector2Int startPos, endPos;
        var        safetyRetries = 0;
        do
        {
            var startEdge = Random.Range(0, 4);
            startPos = GetEdgePosition(startEdge);
            var endEdge = (startEdge + Random.Range(1, 4)) % 4;
            endPos = GetEdgePosition(endEdge);
            safetyRetries++;
        } while (Vector2Int.Distance(startPos, endPos) < minDistance && safetyRetries < 100);

        var riverNoiseOffset = Random.Range(0f, 1000f);
        var widthNoiseOffset = Random.Range(0f, 1000f);

        var startF = new Vector2(startPos.x, startPos.y);
        var endF   = new Vector2(endPos.x,   endPos.y);
        var along  = (endF - startF).normalized;
        var perp   = MathHelper.GetPerpendicular(along);

        var scaledAmplitude  = riverAmplitude * size;
        var scaledResolution = Mathf.Max(riverResolution, Mathf.RoundToInt(Vector2.Distance(startF, endF) * 2f));

        Vector2Int? prevCell       = null;
        var         riverCellCount = 0;
        var         maxRiverCells  = Mathf.RoundToInt(size * size * maxRiverCoverage);

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

