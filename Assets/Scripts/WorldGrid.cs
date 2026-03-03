using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

public class WorldGrid : MonoBehaviour
{
    public enum CellType
    {
        PLAIN,
        PATH,
        WATER,
        RIVER,
    }

    public class Cell
    {
        public CellType   Type;
        public Vector2Int Position;
        public float      Height;
    }

    public MeshFilter meshFilter;
    public int        size = 10;
    public int        seed;
    public bool       useRandomSeed = true;

    [Header("Terrain")]
    [Range(1f, 50f)] public float noiseScale = 20f;
    [Range(1f, 100f)] public float elevationNoiseScale = 40f; 
    [Range(0f, 5f)]  public float elevationScale = 0.5f; // Contrôle la hauteur des collines
    [Range(0.01f, 1f)] public float heightStep = 0.25f; // Pas de hauteur pour les terrasses

    [Range(1, 6)] public int terrainOctaves = 4;

    [Header("Coasts")]
    [Range(0, 8)] public int maxCoastPatches = 3;

    [Range(5,  60)]  public int coastMinLength = 15;
    [Range(10, 120)] public int coastMaxLength = 40;
    [Range(2,  20)]  public int coastMinDepth  = 4;
    [Range(5,  40)]  public int coastMaxDepth  = 12;

    [Header("Lakes")]
    [Range(0, 5)] public int maxLakes = 2;

    [Range(2, 10)] public int lakeMinRadius = 3;
    [Range(3, 20)] public int lakeMaxRadius = 8;

    [Header("River")]
    [Range(1f, 20f)] public float riverNoiseScale = 8f;

    [Range(0f,    1f)]    public float riverAmplitude   = 0.15f;
    [Range(10,    500)]   public int   riverResolution  = 200;
    [Range(0.01f, 0.15f)] public float maxRiverCoverage = 0.05f;
    [Range(1,     6)]     public int   riverMinWidth    = 1;
    [Range(1,     6)]     public int   riverMaxWidth    = 3;
    [Range(1,     4)]     public int   riverOctaves     = 3;

    public Cell[,] Cells;

    private Mesh _mesh;

    public void Start()
    {
        GenerateMap();
    }

    public void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            GenerateMap();
    }

    private bool IsInBounds(Vector2Int _pos)
    {
        return _pos.x >= 0 && _pos.x < size && _pos.y >= 0 && _pos.y < size;
    }


    private Vector2Int GetEdgePosition(int _edge)
    {
        return _edge switch
        {
            0 => new Vector2Int(0,        Random.Range(0, size)),          // left
            1 => new Vector2Int(size - 1, Random.Range(0, size)),          // right
            2 => new Vector2Int(Random.Range(0,           size), 0),       // bottom
            _ => new Vector2Int(Random.Range(0,           size), size - 1) // top
        };
    }

    public void GenerateMap()
    {
        if (useRandomSeed)
            seed = System.DateTime.Now.Millisecond;
            
        Random.InitState(seed);
        _mesh           = new Mesh { name = "WorldGridMesh", indexFormat = IndexFormat.UInt32 };
        meshFilter.mesh = _mesh;

        Cells = new Cell[size, size];

        var elevationOffsetX = Random.Range(0f, 1000f);
        var elevationOffsetY = Random.Range(0f, 1000f);

        for (var x = 0; x < size; x++)
        {
            for (var y = 0; y < size; y++)
            {
                var noiseVal = FBm((x + elevationOffsetX) / elevationNoiseScale, (y + elevationOffsetY) / elevationNoiseScale, terrainOctaves);
                var height = Mathf.Floor(noiseVal * elevationScale / heightStep) * heightStep;
                             
                Cells[x, y] = new Cell { Type = CellType.PLAIN, Position = new Vector2Int(x, y), Height = height };
            }
        }

        GenerateCoasts();

        GenerateLakes();

        var riverCount = Random.Range(0, 3);
        for (var r = 0; r < riverCount; r++)
            GenerateRiver();

        BuildMesh();
    }

    private void GenerateCoasts()
    {
        if (maxCoastPatches <= 0) return;

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
                var depthNoise = FBm(t * 5f + noiseOffsetX, noiseOffsetY, terrainOctaves);

                var edgeFade = 1f - Mathf.Abs(2f * t - 1f);
                edgeFade = Mathf.Pow(edgeFade, 0.5f);

                var localDepth = Mathf.RoundToInt(depth * depthNoise * edgeFade);
                if (localDepth <= 0) continue;

                for (var d = 0; d < localDepth; d++)
                {
                    int x, y;
                    switch (edge)
                    {
                        case 0: // left
                            x = d;
                            y = start + along;
                            break;
                        case 1: // right
                            x = size  - 1 - d;
                            y = start + along;
                            break;
                        case 2: // bottom
                            x = start + along;
                            y = d;
                            break;
                        default: // top
                            x = start + along;
                            y = size  - 1 - d;
                            break;
                    }

                    var pos = new Vector2Int(x, y);
                    if (IsInBounds(pos))
                    {
                        Cells[x, y].Type   = CellType.WATER;
                        Cells[x, y].Height = -0.5f;
                    }
                }
            }
        }
    }

    private void GenerateLakes()
    {
        if (maxLakes <= 0) return;

        var lakeCount = Random.Range(0, maxLakes + 1);
        var margin    = lakeMaxRadius + 2;

        for (var i = 0; i < lakeCount; i++)
        {
            var cx = Random.Range(margin,        size          - margin);
            var cy = Random.Range(margin,        size          - margin);
            var rx = Random.Range(lakeMinRadius, lakeMaxRadius + 1);
            var ry = Random.Range(lakeMinRadius, lakeMaxRadius + 1);

            var noiseOffset = Random.Range(0f, 1000f);

            for (var dx = -rx; dx <= rx; dx++)
            {
                for (var dy = -ry; dy <= ry; dy++)
                {
                    var normalizedDist = (float)(dx * dx) / (rx * rx) + (float)(dy * dy) / (ry * ry);
                    if (normalizedDist > 1f) continue;

                    var angle     = Mathf.Atan2(dy, dx);
                    var edgeNoise = FBm(angle * 3f, noiseOffset, 2);
                    if (normalizedDist > 0.6f + edgeNoise * 0.4f) continue;

                    var px = cx + dx;
                    var py = cy + dy;
                    if (!IsInBounds(new Vector2Int(px, py))) continue;

                    Cells[px, py].Type   = CellType.WATER;
                    Cells[px, py].Height = -0.5f;
                }
            }
        }
    }

    private float FBm(float _x, float _y, int _octaves)
    {
        var value     = 0f;
        var amplitude = 1f;
        var frequency = 1f;
        var max       = 0f;

        for (var i = 0; i < _octaves; i++)
        {
            value     += Mathf.PerlinNoise(_x * frequency, _y * frequency) * amplitude;
            max       += amplitude;
            amplitude *= 0.5f;
            frequency *= 2f;
        }

        return value / max;
    }

    private void PlaceRiverRadius(Vector2Int _center, int _radius, ref int _count, int _max)
    {
        for (var dx = -_radius; dx <= _radius; dx++)
        {
            for (var dy = -_radius; dy <= _radius; dy++)
            {
                if (dx * dx + dy * dy > _radius * _radius) continue;

                var p = new Vector2Int(_center.x + dx, _center.y + dy);
                if (!IsInBounds(p) || Cells[p.x, p.y].Type == CellType.RIVER ||
                    Cells[p.x, p.y].Type                   == CellType.WATER) continue;
                if (_count >= _max) return;

                Cells[p.x, p.y].Type   = CellType.RIVER;
                Cells[p.x, p.y].Height = -0.5f;
                _count++;
            }
        }
    }

    private void GenerateRiver()
    {
        var minDistance = size / 2f;

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
        var perp   = new Vector2(-along.y, along.x);

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

            var noiseVal = FBm(t * riverNoiseScale, riverNoiseOffset, riverOctaves);
            var offset   = (noiseVal - 0.5f) * 2f * scaledAmplitude;

            var displaced = baseP + perp * offset;
            var cell      = new Vector2Int(Mathf.RoundToInt(displaced.x), Mathf.RoundToInt(displaced.y));

            var widthNoise = FBm(t * riverNoiseScale * 2f, widthNoiseOffset, 2);
            var radius     = Mathf.RoundToInt(Mathf.Lerp(riverMinWidth, riverMaxWidth, widthNoise));

            if (prevCell.HasValue && prevCell.Value != cell)
            {
                foreach (var p in BresenhamLine(prevCell.Value, cell))
                {
                    if (riverCellCount >= maxRiverCells) break;
                    PlaceRiverRadius(p, radius, ref riverCellCount, maxRiverCells);
                }
            }
            else if (IsInBounds(cell))
            {
                PlaceRiverRadius(cell, radius, ref riverCellCount, maxRiverCells);
            }

            prevCell = cell;
        }
    }

    private void BuildMesh()
    {
        var vertices  = new List<Vector3>();
        var triangles = new List<int>();
        var colors    = new List<Color>();
        var normals   = new List<Vector3>();

        for (var x = 0; x < size; x++)
        {
            for (var y = 0; y < size; y++)
            {
                var cell = Cells[x, y];

                var color = cell.Type switch
                {
                    CellType.PLAIN => new Color(0.3f, 0.8f, 0.3f),
                    CellType.WATER => new Color(0.2f, 0.4f, 0.8f),
                    CellType.RIVER => new Color(0.1f, 0.3f, 0.7f),
                    _              => Color.magenta
                };
                
                // Use the cell's calculated height
                var cellHeight = cell.Height;

                var vIndex = vertices.Count;

                vertices.Add(new Vector3(x,     cellHeight, y));
                vertices.Add(new Vector3(x + 1, cellHeight, y));
                vertices.Add(new Vector3(x,     cellHeight, y + 1));
                vertices.Add(new Vector3(x + 1, cellHeight, y + 1));

                colors.Add(color);
                colors.Add(color);
                colors.Add(color);
                colors.Add(color);

                normals.Add(Vector3.up);
                normals.Add(Vector3.up);
                normals.Add(Vector3.up);
                normals.Add(Vector3.up);

                triangles.Add(vIndex);
                triangles.Add(vIndex + 2);
                triangles.Add(vIndex + 1);
                triangles.Add(vIndex + 2);
                triangles.Add(vIndex + 3);
                triangles.Add(vIndex + 1);

                // Build borders for all cells where there is a height difference
                BuildCellBorders(x, y, vertices, triangles, colors, normals);
            }
        }

        _mesh.Clear();
        _mesh.vertices  = vertices.ToArray();
        _mesh.triangles = triangles.ToArray();
        _mesh.colors    = colors.ToArray();
        _mesh.normals   = normals.ToArray();
        _mesh.RecalculateBounds();
    }

    private void BuildCellBorders(int _x, int _y, List<Vector3> _vertices, List<int> _triangles,
                                  List<Color> _colors, List<Vector3> _normals)
    {
        var directions = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        var wallColor  = new Color(0.45f, 0.3f, 0.1f); // Dirt brown

        var cell          = Cells[_x, _y];
        var currentHeight = cell.Height;

        foreach (var dir in directions)
        {
            var nx = _x + dir.x;
            var ny = _y + dir.y;

            float neighborHeight;

            if (!IsInBounds(new Vector2Int(nx, ny)))
                neighborHeight = -2.0f; // Edge of the world, drop down deep
            else
                neighborHeight = Cells[nx, ny].Height;

            // Only build a wall if we are higher than the neighbor
            if (currentHeight <= neighborHeight + 0.001f) continue;

            var vIndex = _vertices.Count;
            var normal = new Vector3(dir.x, 0, dir.y);

            // Wall geometry: from currentHeight down to neighborHeight
            var topY    = currentHeight;
            var bottomY = neighborHeight;

            if (dir == Vector2Int.right) // x+1 face
            {
                _vertices.Add(new Vector3(_x + 1, bottomY, _y));
                _vertices.Add(new Vector3(_x + 1, topY,    _y));
                _vertices.Add(new Vector3(_x + 1, bottomY, _y + 1));
                _vertices.Add(new Vector3(_x + 1, topY,    _y + 1));
            }
            else if (dir == Vector2Int.left) // x face
            {
                _vertices.Add(new Vector3(_x, bottomY, _y + 1));
                _vertices.Add(new Vector3(_x, topY,    _y + 1));
                _vertices.Add(new Vector3(_x, bottomY, _y));
                _vertices.Add(new Vector3(_x, topY,    _y));
            }
            else if (dir == Vector2Int.up) // y+1 face
            {
                _vertices.Add(new Vector3(_x + 1, bottomY, _y + 1));
                _vertices.Add(new Vector3(_x + 1, topY,    _y + 1));
                _vertices.Add(new Vector3(_x,     bottomY, _y + 1));
                _vertices.Add(new Vector3(_x,     topY,    _y + 1));
            }
            else // down / y face
            {
                _vertices.Add(new Vector3(_x,     bottomY, _y));
                _vertices.Add(new Vector3(_x,     topY,    _y));
                _vertices.Add(new Vector3(_x + 1, bottomY, _y));
                _vertices.Add(new Vector3(_x + 1, topY,    _y));
            }

            _normals.Add(normal);
            _normals.Add(normal);
            _normals.Add(normal);
            _normals.Add(normal);

            _colors.Add(wallColor);
            _colors.Add(wallColor);
            _colors.Add(wallColor);
            _colors.Add(wallColor);

            _triangles.Add(vIndex);
            _triangles.Add(vIndex + 1);
            _triangles.Add(vIndex + 2);
            _triangles.Add(vIndex + 2);
            _triangles.Add(vIndex + 1);
            _triangles.Add(vIndex + 3);
        }
    }

    private static IEnumerable<Vector2Int> BresenhamLine(Vector2Int _from, Vector2Int _to)
    {
        var x0 = _from.x;
        var y0 = _from.y;
        var x1 = _to.x;
        var y1 = _to.y;

        var dx  = Mathf.Abs(x1 - x0);
        var dy  = Mathf.Abs(y1 - y0);
        var sx  = x0 < x1 ? 1 : -1;
        var sy  = y0 < y1 ? 1 : -1;
        var err = dx - dy;

        while (true)
        {
            yield return new Vector2Int(x0, y0);

            if (x0 == x1 && y0 == y1) break;

            var e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0  += sx;
            }

            if (e2 >= dx) continue;

            err += dx;
            y0  += sy;
        }
    }
}


