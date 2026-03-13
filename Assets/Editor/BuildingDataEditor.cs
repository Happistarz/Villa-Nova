using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(BuildingData))]
public class BuildingDataEditor : Editor
{
    private const float CELL_SIZE = 24f;

    private int _previewRotation;

    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();
        var defaultInspector = new IMGUIContainer(() => DrawDefaultInspector());
        root.Add(defaultInspector);

        var gridContainer = new IMGUIContainer(DrawBuildingAreaGrid);
        root.Add(gridContainer);

        return root;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var data = (BuildingData)target;
        if (data.buildingArea == null || data.buildingArea.Count == 0)
            EditorGUILayout.HelpBox("Building Area is empty. Use the grid below to define it.", MessageType.Warning);
    }

    private void OnSceneGUI()
    {
        var data = (BuildingData)target;
        if (data.buildingArea == null || data.buildingArea.Count == 0)
            return;

        Handles.color = data.debugColor;
        foreach (var offset in data.buildingArea)
        {
            var rotated = BuildingAreaHelper.RotateOffset(offset, _previewRotation);
            var worldPos = new Vector3(rotated.x, 0, rotated.y);
            Handles.DrawWireCube(worldPos, Vector3.one);
        }
    }

    private static void DrawRectBorder(Rect _rect, Color _color)
    {
        EditorGUI.DrawRect(new Rect(_rect.x, _rect.y, _rect.width, 1), _color);
        EditorGUI.DrawRect(new Rect(_rect.x, _rect.yMax - 1, _rect.width, 1), _color);
        EditorGUI.DrawRect(new Rect(_rect.x, _rect.y, 1, _rect.height), _color);
        EditorGUI.DrawRect(new Rect(_rect.xMax - 1, _rect.y, 1, _rect.height), _color);
    }

    private static void DrawLegendSwatch(Color _color, string _label)
    {
        var rect = GUILayoutUtility.GetRect(12, 12, GUILayout.Width(12));
        EditorGUI.DrawRect(rect, _color);
        EditorGUILayout.LabelField(_label, GUILayout.Width(80));
    }
    
    private void DrawBuildingAreaGrid()
    {
        var data = (BuildingData)target;
        if (data.buildingArea == null)
            data.buildingArea = new List<Vector2Int>();

        var gridSize = data.buildingSize;
        var totalSize = gridSize * CELL_SIZE;

        var rect = GUILayoutUtility.GetRect(totalSize, totalSize);
        DrawRectBorder(rect, Color.gray);

        for (var x = 0; x < gridSize; x++)
        {
            for (var y = 0; y < gridSize; y++)
            {
                var cellRect = new Rect(rect.x + x * CELL_SIZE, rect.y + y * CELL_SIZE, CELL_SIZE, CELL_SIZE);
                var offset = new Vector2Int(x - gridSize / 2, y - gridSize / 2);
                var isOccupied = data.buildingArea.Contains(offset);

                EditorGUI.DrawRect(cellRect, isOccupied ? data.debugColor : new Color(0, 0, 0, 0.1f));
                if (!GUI.Button(cellRect, GUIContent.none)) continue;
                
                if (isOccupied)
                    data.buildingArea.Remove(offset);
                else
                    data.buildingArea.Add(offset);

                EditorUtility.SetDirty(data);
            }
        }

        GUILayout.Space(10);
        DrawLegendSwatch(data.debugColor, "Occupied Cell");
    }
}