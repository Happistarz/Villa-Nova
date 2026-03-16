using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Color = UnityEngine.Color;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TerrainRenderer : AbstractRenderer
{
    private Mesh                        _mesh;
    private GameConfig.BiomeColorConfig _colorConfig;

    private void Awake()
    {
        OnRenderToggled += () => { CityGenerator.Instance.cityRenderer.enabled = renderEnabled.Value; };

        CityGenerator.Instance.cityRenderer.enabled = renderEnabled.Value;

        MapGenerator.Instance.OnGenerationComplete += () =>
        {
            _colorConfig = GameManager.Instance.Config
                ? GameManager.Instance.Config.GetRandomPalette()
                : GameConfig.BiomeColorConfig.Default;

            GameManager.Instance.ActiveColorConfig = _colorConfig;
        };
    }

    public override void BuildMesh()
    {

        if (!_mesh)
        {
            _mesh = new Mesh { name = "WorldGridMesh", indexFormat = IndexFormat.UInt32 };

            meshFilter.mesh = _mesh;
        }

        if (!ToggleRenderer()) return;


        var vertices  = new List<Vector3>();
        var triangles = new List<int>();
        var colors    = new List<Color>();
        var normals   = new List<Vector3>();

        for (var x = 0; x < WorldGrid.Instance.size; x++)
        {
            for (var y = 0; y < WorldGrid.Instance.size; y++)
            {
                var cell = WorldGrid.Instance.Cells[x, y];

                var color = cell.Type switch
                {
                    WorldGrid.CellType.PLAIN  => _colorConfig.plainColor,
                    WorldGrid.CellType.WATER  => _colorConfig.waterColor,
                    WorldGrid.CellType.RIVER  => _colorConfig.riverColor,
                    WorldGrid.CellType.CITY   => _colorConfig.cityColor,
                    WorldGrid.CellType.ROAD   => _colorConfig.roadColor,
                    WorldGrid.CellType.BRIDGE => _colorConfig.bridgeColor,
                    _                         => Color.magenta
                };

                var cellHeight = cell.Height;

                var vIndex = vertices.Count;

                vertices.Add(new Vector3(x,                       cellHeight, y));
                vertices.Add(new Vector3(x + Constants.CELL_SIZE, cellHeight, y));
                vertices.Add(new Vector3(x,                       cellHeight, y + Constants.CELL_SIZE));
                vertices.Add(new Vector3(x + Constants.CELL_SIZE, cellHeight, y + Constants.CELL_SIZE));

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

                BuildCellBorders(x, y, _colorConfig, vertices, triangles, colors, normals);
            }
        }

        _mesh.Clear();
        _mesh.vertices  = vertices.ToArray();
        _mesh.triangles = triangles.ToArray();
        _mesh.colors    = colors.ToArray();
        _mesh.normals   = normals.ToArray();
        _mesh.RecalculateBounds();

        var bounds = _mesh.bounds;
        bounds.Expand(new Vector3(0f, 20f, 0f));
        _mesh.bounds = bounds;
    }

    private void BuildCellBorders(int           _x,        int           _y, GameConfig.BiomeColorConfig _p,
                                  List<Vector3> _vertices, List<int>     _triangles,
                                  List<Color>   _colors,   List<Vector3> _normals)
    {
        var directions = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        var cell          = WorldGrid.Instance.Cells[_x, _y];
        var currentHeight = cell.Height;

        foreach (var dir in directions)
        {
            var nx = _x + dir.x;
            var ny = _y + dir.y;

            float neighborHeight;

            if (!WorldGrid.Instance.IsInBounds(new Vector2Int(nx, ny)))
                neighborHeight = -2.0f;
            else
                neighborHeight = WorldGrid.Instance.Cells[nx, ny].Height;

            if (currentHeight <= neighborHeight + 0.001f) continue;

            var vIndex = _vertices.Count;
            var normal = new Vector3(dir.x, 0, dir.y);

            var color = cell.Type switch
            {
                WorldGrid.CellType.ROAD   => _p.roadColor,
                WorldGrid.CellType.BRIDGE => _p.bridgeColor,
                _                         => _p.wallColor
            };

            var bottomY = neighborHeight;

            if (dir == Vector2Int.right) // X+1
            {
                _vertices.Add(new Vector3(_x + Constants.CELL_SIZE, bottomY,       _y));
                _vertices.Add(new Vector3(_x + Constants.CELL_SIZE, currentHeight, _y));
                _vertices.Add(new Vector3(_x + Constants.CELL_SIZE, bottomY,       _y + Constants.CELL_SIZE));
                _vertices.Add(new Vector3(_x + Constants.CELL_SIZE, currentHeight, _y + Constants.CELL_SIZE));
            }
            else if (dir == Vector2Int.left) // X-1
            {
                _vertices.Add(new Vector3(_x, bottomY,       _y + Constants.CELL_SIZE));
                _vertices.Add(new Vector3(_x, currentHeight, _y + Constants.CELL_SIZE));
                _vertices.Add(new Vector3(_x, bottomY,       _y));
                _vertices.Add(new Vector3(_x, currentHeight, _y));
            }
            else if (dir == Vector2Int.up) // Y+1
            {
                _vertices.Add(new Vector3(_x + Constants.CELL_SIZE, bottomY,       _y + Constants.CELL_SIZE));
                _vertices.Add(new Vector3(_x + Constants.CELL_SIZE, currentHeight, _y + Constants.CELL_SIZE));
                _vertices.Add(new Vector3(_x,                       bottomY,       _y + Constants.CELL_SIZE));
                _vertices.Add(new Vector3(_x,                       currentHeight, _y + Constants.CELL_SIZE));
            }
            else // Y-1
            {
                _vertices.Add(new Vector3(_x,                       bottomY,       _y));
                _vertices.Add(new Vector3(_x,                       currentHeight, _y));
                _vertices.Add(new Vector3(_x + Constants.CELL_SIZE, bottomY,       _y));
                _vertices.Add(new Vector3(_x + Constants.CELL_SIZE, currentHeight, _y));
            }

            _normals.Add(normal);
            _normals.Add(normal);
            _normals.Add(normal);
            _normals.Add(normal);

            _colors.Add(color);
            _colors.Add(color);
            _colors.Add(color);
            _colors.Add(color);

            _triangles.Add(vIndex);
            _triangles.Add(vIndex + 1);
            _triangles.Add(vIndex + 2);
            _triangles.Add(vIndex + 2);
            _triangles.Add(vIndex + 1);
            _triangles.Add(vIndex + 3);
        }
    }
}