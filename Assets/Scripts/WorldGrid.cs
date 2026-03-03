using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class WorldGrid : MonoBehaviour
{
    public enum CellType
    {
        PLAIN,
        WATER,
        RIVER,
    }

    public class Cell
    {
        public CellType   Type;
        public Vector2Int Position;
    }

    public                      int   width           = 10;
    public                      int   height          = 10;
    [Range(0.05f, 0.5f)]  public float WaterThreshold    = 0.275f;
    [Range(1f,    50f)]   public float NoiseScale       = 20f;
    [Range(1f,    20f)]   public float RiverNoiseScale  = 4f;
    [Range(0f,    10f)]   public float RiverAmplitude   = 2.5f;
    [Range(10,    200)]   public int   RiverResolution  = 50;
    [Range(0.01f, 0.15f)] public float MaxRiverCoverage = 0.05f;

    public Cell[,] Cells;

    private GameObject _parent;

    public void Start()
    {
        _parent = new GameObject("WorldGrid");

        GenerateMap();
    }

    public void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            GenerateMap();
    }

    private bool IsInBounds(Vector2Int _pos)
    {
        return _pos.x >= 0 && _pos.x < width && _pos.y >= 0 && _pos.y < height;
    }


    private Vector2Int GetEdgePosition(int _edge)
    {
        return _edge switch
        {
            0 => new Vector2Int(0,         Random.Range(0, height)),           // left
            1 => new Vector2Int(width - 1, Random.Range(0, height)),           // right
            2 => new Vector2Int(Random.Range(0,            width), 0),         // bottom
            _ => new Vector2Int(Random.Range(0,            width), height - 1) // top
        };
    }

    private void PlaceRiverCell(Vector2Int _pos)
    {
        Cells[_pos.x, _pos.y].Type = CellType.RIVER;

        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        go.transform.SetParent(_parent.transform);
        go.transform.position                      = new Vector3(_pos.x, 1f, _pos.y);
        go.transform.localScale                    = Vector3.one * 0.1f;
        go.GetComponent<Renderer>().material.color = Color.cyan;
    }

    public void GenerateMap()
    {
        var seed = System.DateTime.Now.Millisecond;
        Random.InitState(seed);

        // clear parent children
        for (var i = _parent.transform.childCount - 1; i >= 0; i--)
            Destroy(_parent.transform.GetChild(i).gameObject);

        Cells = new Cell[width, height];

        // perlin noise parameters
        var offsetX = Random.Range(-1000f, 1000f);
        var offsetY = Random.Range(-1000f, 1000f);

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                var sample = new Vector2((x + offsetX) / NoiseScale, (y + offsetY) / NoiseScale);

                var noiseValue = Mathf.PerlinNoise(sample.x, sample.y);
                var type       = noiseValue > WaterThreshold ? CellType.PLAIN : CellType.WATER;

                Cells[x, y] = new Cell
                {
                    Type     = type,
                    Position = new Vector2Int(x, y)
                };

                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(_parent.transform);
                cube.transform.position                      = new Vector3(x, 0, y);
                cube.GetComponent<Renderer>().material.color = type == CellType.PLAIN ? Color.green : Color.blue;
            }
        }

        // --- river generation ---

        var minDistance = Mathf.Min(width, height) / 2f;

        // pick start and end on different edges, with minimum distance
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

        // river noise offset
        var riverNoiseOffset = Random.Range(0f, 1000f);

        // direction from start to end and perpendicular
        var startF = new Vector2(startPos.x, startPos.y);
        var endF   = new Vector2(endPos.x,   endPos.y);
        var along  = (endF - startF).normalized;
        var perp   = new Vector2(-along.y, along.x);

        Vector2Int? prevCell       = null;
        var         riverCellCount = 0;
        var         maxRiverCells  = Mathf.RoundToInt(width * height * MaxRiverCoverage);

        for (var i = 0; i <= RiverResolution; i++)
        {
            if (riverCellCount >= maxRiverCells) break;
            var t     = (float)i / RiverResolution;
            var baseP = Vector2.Lerp(startF, endF, t);

            var noiseVal = Mathf.PerlinNoise(t * RiverNoiseScale, riverNoiseOffset);
            var offset   = math.remap(0f, 1f, -RiverAmplitude, RiverAmplitude, noiseVal);

            var displaced = baseP + perp * offset;
            var cell      = new Vector2Int(Mathf.RoundToInt(displaced.x), Mathf.RoundToInt(displaced.y));

            // rasterize line from previous point to current
            if (prevCell.HasValue && prevCell.Value != cell)
            {
                foreach (var p in BresenhamLine(prevCell.Value, cell))
                {
                    if (riverCellCount >= maxRiverCells) break;
                    if (!IsInBounds(p) || Cells[p.x, p.y].Type == CellType.RIVER ||
                        Cells[p.x, p.y].Type                   == CellType.WATER) continue;
                    
                    PlaceRiverCell(p);
                    riverCellCount++;
                }
            }
            else if (IsInBounds(cell) && Cells[cell.x, cell.y].Type != CellType.RIVER &&
                     Cells[cell.x, cell.y].Type                     != CellType.WATER)
            {
                PlaceRiverCell(cell);
                riverCellCount++;
            }

            prevCell = cell;
        }
    }

    private static System.Collections.Generic.IEnumerable<Vector2Int> BresenhamLine(Vector2Int _from, Vector2Int _to)
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