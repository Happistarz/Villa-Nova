using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct DistanceFieldJob : IJob
{
    public const float SQRT2 = 1.41421356237f;
    
    public int GridSize;

    /// <summary>
    /// Input/output: initialize source cells to 0 and everything else to float.MaxValue.
    /// </summary>
    public NativeArray<float> Distances;

    public void Execute()
    {
        // top-left to bottom-right pass
        for (var y = 0; y < GridSize; y++)
        {
            for (var x = 0; x < GridSize; x++)
            {
                var idx = y * GridSize + x;
                var cur = Distances[idx];

                // Check left, top, and diagonals
                if (x > 0)
                    cur = math.min(cur, Distances[idx - 1] + 1f);

                if (y > 0)
                {
                    cur = math.min(cur, Distances[(y - 1) * GridSize + x] + 1f);

                    if (x > 0)
                        cur = math.min(cur, Distances[(y - 1) * GridSize + (x - 1)] + SQRT2);

                    if (x < GridSize - 1)
                        cur = math.min(cur, Distances[(y - 1) * GridSize + x + 1] + SQRT2);
                }

                Distances[idx] = cur;
            }
        }

        // bottom-right to top-left pass
        for (var y = GridSize - 1; y >= 0; y--)
        {
            for (var x = GridSize - 1; x >= 0; x--)
            {
                var idx = y * GridSize + x;
                var cur = Distances[idx];

                // Check right, bottom, and diagonals
                if (x < GridSize - 1)
                    cur = math.min(cur, Distances[idx + 1] + 1f);

                if (y < GridSize - 1)
                {
                    cur = math.min(cur, Distances[(y + 1) * GridSize + x] + 1f);

                    if (x > 0)
                        cur = math.min(cur, Distances[(y + 1) * GridSize + (x - 1)] + SQRT2);

                    if (x < GridSize - 1)
                        cur = math.min(cur, Distances[(y + 1) * GridSize + x + 1] + SQRT2);
                }

                Distances[idx] = cur;
            }
        }
    }
}

