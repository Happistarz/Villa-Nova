using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TerrainRenderer : MonoBehaviour
{
    public MeshFilter meshFilter;
    public WorldGrid grid;
    
    private Mesh _mesh;
    
    public void BuildMesh()
    {   
        _mesh      = new Mesh { name = "WorldGridMesh", indexFormat = IndexFormat.UInt32 };
        meshFilter.mesh = _mesh;
        
        var vertices  = new List<Vector3>();
        var triangles = new List<int>();
        var colors    = new List<Color>();
        var normals   = new List<Vector3>();

        for (var x = 0; x < grid.size; x++)
        {
            for (var y = 0; y < grid.size; y++)
            {
                var cell = grid.Cells[x, y];

                var color = cell.Type switch
                {
                    WorldGrid.CellType.PLAIN => new Color(0.3f, 0.8f, 0.3f),
                    WorldGrid.CellType.WATER => new Color(0.2f, 0.4f, 0.8f),
                    WorldGrid.CellType.RIVER => new Color(0.1f, 0.3f, 0.7f),
                    _                        => Color.magenta
                };
                
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
        var wallColor  = new Color(0.45f, 0.3f, 0.1f);

        var cell          = grid.Cells[_x, _y];
        var currentHeight = cell.Height;

        foreach (var dir in directions)
        {
            var nx = _x + dir.x;
            var ny = _y + dir.y;

            float neighborHeight;

            if (!grid.IsInBounds(new Vector2Int(nx, ny)))
                neighborHeight = -2.0f;
            else
                neighborHeight = grid.Cells[nx, ny].Height;

            if (currentHeight <= neighborHeight + 0.001f) continue;

            var vIndex = _vertices.Count;
            var normal = new Vector3(dir.x, 0, dir.y);

            var bottomY = neighborHeight;

            if (dir == Vector2Int.right) // X+1
            {
                _vertices.Add(new Vector3(_x + 1, bottomY, _y));
                _vertices.Add(new Vector3(_x + 1, currentHeight,    _y));
                _vertices.Add(new Vector3(_x + 1, bottomY, _y + 1));
                _vertices.Add(new Vector3(_x + 1, currentHeight,    _y + 1));
            }
            else if (dir == Vector2Int.left) // X-1
            {
                _vertices.Add(new Vector3(_x, bottomY, _y + 1));
                _vertices.Add(new Vector3(_x, currentHeight,    _y + 1));
                _vertices.Add(new Vector3(_x, bottomY, _y));
                _vertices.Add(new Vector3(_x, currentHeight,    _y));
            }
            else if (dir == Vector2Int.up) // Y+1
            {
                _vertices.Add(new Vector3(_x + 1, bottomY, _y + 1));
                _vertices.Add(new Vector3(_x + 1, currentHeight,    _y + 1));
                _vertices.Add(new Vector3(_x,     bottomY, _y + 1));
                _vertices.Add(new Vector3(_x,     currentHeight,    _y + 1));
            }
            else // Y-1
            {
                _vertices.Add(new Vector3(_x,     bottomY, _y));
                _vertices.Add(new Vector3(_x,     currentHeight,    _y));
                _vertices.Add(new Vector3(_x + 1, bottomY, _y));
                _vertices.Add(new Vector3(_x + 1, currentHeight,    _y));
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
}