using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class DebugRenderer : AbstractRenderer
{
    public float height = 2f;

    [Header("Colors")]
    public Color plainColor = new(0.3f, 0.8f, 0.3f, 0.5f);

    public Color waterColor = new(0.2f, 0.4f, 0.8f, 0.5f);
    public Color riverColor = new(0.1f, 0.3f, 0.7f, 0.5f);
    public Color cityColor  = Color.yellow;

    private Mesh _mesh;

    public override void BuildMesh()
    {
        if (!_mesh)
        {
            _mesh = new Mesh { name = "DebugMesh", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            
            meshFilter.mesh = _mesh;
        }

        if (!ToggleRenderer()) return;

        _mesh.Clear();

        var vertices  = new List<Vector3>();
        var triangles = new List<int>();
        var colors    = new List<Color>();

        var cellSize = Constants.Instance.CellSize;
        var half = cellSize * 0.5f;

        for (var x = 0; x < WorldGrid.Instance.size; x++)
            for (var y = 0; y < WorldGrid.Instance.size; y++)
            {
                var cell = WorldGrid.Instance.Cells[x, y];
                var color = cell.Type switch
                {
                    WorldGrid.CellType.PLAIN => plainColor,
                    WorldGrid.CellType.WATER => waterColor,
                    WorldGrid.CellType.RIVER => riverColor,
                    WorldGrid.CellType.CITY  => cityColor,
                    _                        => new Color(1f, 0f, 1f, 0.5f)
                };

                var cx = x + half;
                var cz = y + half;

                var vIndex = vertices.Count;

                vertices.Add(new Vector3(cx - half, height, cz - half));
                vertices.Add(new Vector3(cx + half, height, cz - half));
                vertices.Add(new Vector3(cx - half, height, cz + half));
                vertices.Add(new Vector3(cx + half, height, cz + half));

                colors.Add(color);
                colors.Add(color);
                colors.Add(color);
                colors.Add(color);

                triangles.Add(vIndex);
                triangles.Add(vIndex + 2);
                triangles.Add(vIndex + 1);
                triangles.Add(vIndex + 2);
                triangles.Add(vIndex + 3);
                triangles.Add(vIndex + 1);
            }

        _mesh.SetVertices(vertices);
        _mesh.SetTriangles(triangles, 0);
        _mesh.SetColors(colors);
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
    }
}