using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct UrbanityJob : IJobParallelFor
{
    [ReadOnly] public int   GridSize;
    [ReadOnly] public int2  CitySettlePos;
    [ReadOnly] public float MaxRadius;
    [ReadOnly] public float NoiseScale;
    [ReadOnly] public float NoiseAmplitude;
    [ReadOnly] public int   Seed;

    public NativeArray<float> Results;

    public void Execute(int _i)
    {
        var x = _i % GridSize;
        var y = _i / GridSize;

        var dist = math.distance(new float2(x, y), CitySettlePos);

        var radial = math.max(0f, 1f - dist / MaxRadius);

        // Add noise to break up the circular pattern and create a more organic distribution
        var nx        = x * NoiseScale + Seed;
        var ny        = y * NoiseScale + Seed;
        var noiseMask = math.saturate(noise.snoise(new float2(nx, ny)) * NoiseAmplitude + 0.5f);

        Results[_i] = radial * noiseMask;
    }
}