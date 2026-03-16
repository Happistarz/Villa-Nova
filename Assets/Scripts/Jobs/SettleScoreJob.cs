using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct SettleScoreJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<GridJobUtilities.JobCellData> GridCells;

    [ReadOnly] public int   Size;
    [ReadOnly] public float SearchRadius;

    public NativeArray<float> Results;

    public void Execute(int _index)
    {
        var x = _index % Size;
        var y = _index / Size;
        var currentCell = GridCells[_index];

        var score = 0f;

        var r = (int)math.ceil(SearchRadius);
        for (var i = -r; i <= r; i++)
        {
            for (var j = -r; j <= r; j++)
            {
                if (i * i + j * j > SearchRadius * SearchRadius) continue;

                var nX = x + i;
                var nY = y + j;

                if (nX < 0 || nX >= Size || nY < 0 || nY >= Size) continue;
                var nIndex   = nY * Size + nX;
                var neighbor = GridCells[nIndex];

                switch (neighbor.Type)
                {
                    case WorldGrid.CellType.WATER or WorldGrid.CellType.RIVER:
                        score += 1f;
                        break;
                    case WorldGrid.CellType.PLAIN:
                        score += 0.5f;
                        break;
                }
            }
        }

        var distToCenter = math.distance(new float2(x, y), new float2(Size / 2f, Size / 2f));
        score -= distToCenter * 0.3f;

        if (currentCell.Type is WorldGrid.CellType.WATER or WorldGrid.CellType.RIVER)
        {
            score -= 999f;
        }

        Results[_index] = score;
    }
}