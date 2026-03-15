using System.Collections.Generic;
using UnityEngine;
using Color = UnityEngine.Color;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class DebugRenderer : AbstractRenderer
{
    public float height = 2f;

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

        var p = GameManager.Instance.ActiveColorConfig;

        var vertices  = new List<Vector3>();
        var triangles = new List<int>();
        var colors    = new List<Color>();

        const float HALF = Constants.CELL_SIZE * 0.5f;

        for (var x = 0; x < WorldGrid.Instance.size; x++)
            for (var y = 0; y < WorldGrid.Instance.size; y++)
            {
                var cell = WorldGrid.Instance.Cells[x, y];

                Color color;
                if (cell.POI)
                    color = cell.POI.DebugColor;
                else
                    color = cell.Type switch
                    {
                        WorldGrid.CellType.PLAIN  => p.debugPlainColor,
                        WorldGrid.CellType.WATER  => p.debugWaterColor,
                        WorldGrid.CellType.RIVER  => p.debugRiverColor,
                        WorldGrid.CellType.CITY   => p.debugCityColor,
                        WorldGrid.CellType.ROAD   => p.debugRoadColor,
                        WorldGrid.CellType.BRIDGE => p.debugBridgeColor,
                        _                         => new Color(1f, 0f, 1f, 0.5f)
                    };

                var cx = x + HALF;
                var cz = y + HALF;

                var vIndex = vertices.Count;

                vertices.Add(new Vector3(cx - HALF, height, cz - HALF));
                vertices.Add(new Vector3(cx + HALF, height, cz - HALF));
                vertices.Add(new Vector3(cx - HALF, height, cz + HALF));
                vertices.Add(new Vector3(cx + HALF, height, cz + HALF));

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