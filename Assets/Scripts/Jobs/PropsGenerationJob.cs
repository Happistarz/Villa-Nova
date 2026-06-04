using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[BurstCompile]
public struct PropsGenerationJob : IJobParallelFor
{
    [ReadOnly] public int   GridSize;
    [ReadOnly] public int   WaterAttractionRadius;
    [ReadOnly] public float WaterAttractionBonus;

    [ReadOnly] public NativeArray<GridJobUtilities.JobCellData> Cells;

    public NativeArray<float> Result;

    public void Execute(int _i)
    {
        var x    = _i % GridSize;
        var y    = _i / GridSize;
        var cell = Cells[_i];

        if (cell.IsOccupied                        || cell.HasPoi ||
            cell.Type == WorldGrid.CellType.WATER  ||
            cell.Type == WorldGrid.CellType.RIVER  ||
            cell.Type == WorldGrid.CellType.ROAD   ||
            cell.Type == WorldGrid.CellType.BRIDGE ||
            cell.Type == WorldGrid.CellType.CITY   ||
            cell.Type == WorldGrid.CellType.HOUSE)
        {
            Result[_i] = 0f;
            return;
        }

        var wx = MathHelper.FBm(x * 0.007f + 100f, y * 0.007f, 2,                 0.5f, 1f);
        var wy = MathHelper.FBm(x                    * 0.007f, y * 0.007f + 200f, 2,    0.5f, 1f);

        var warpX = x + (wx - 0.5f) * 32f;
        var warpY = y + (wy - 0.5f) * 32f;

        var patchMask = MathHelper.FBm(warpX, warpY, 3, 0.5f, 0.009f);

        var density = MathHelper.FBm(warpX + 71.3f, warpY + 53.7f, 2, 0.5f, 0.04f);

        var patchScore = Mathf.Clamp01((patchMask - 0.35f) * 3.5f) * (0.5f + density * 0.5f);

        var soloNoise = MathHelper.FBm(x                  * 0.09f + 513.1f, y * 0.09f + 271.7f, 1, 0.5f, 1f);
        var soloScore = Mathf.Clamp01((soloNoise - 0.75f) * 5f) * 0.55f;

        var score = Mathf.Max(patchScore, soloScore);

        if (WaterAttractionRadius > 0 && HasWaterNeighbour(x, y))
            score = Mathf.Min(1f, score + WaterAttractionBonus);

        Result[_i] = score;
    }

    private bool HasWaterNeighbour(int _x, int _y)
    {
        for (var dy = -WaterAttractionRadius; dy <= WaterAttractionRadius; dy++)
            for (var dx = -WaterAttractionRadius; dx <= WaterAttractionRadius; dx++)
            {
                var nx = _x + dx;
                var ny = _y + dy;

                if (nx < 0 || nx >= GridSize || ny < 0 || ny >= GridSize) continue;

                var type = Cells[ny * GridSize + nx].Type;
                if (type is WorldGrid.CellType.WATER or WorldGrid.CellType.RIVER)
                    return true;
            }

        return false;
    }
}