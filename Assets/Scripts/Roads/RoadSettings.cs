using UnityEngine;

[System.Serializable]
public struct RoadSettings
{
    [Header("Noise")]
    [Range(0f, 20f)] public float noiseStrength;

    [Range(0.01f, 0.3f)] public float noiseScale;

    [Header("Cost Weights")]
    public float waterPenalty;

    public float elevationMultiplier;
    public float occupiedPenalty;

    [Range(1, 20)] public int maxBridgeLength;

    [Header("Stamping")]
    [Range(1, 5)] public int roadWidth;

    public static RoadSettings Default => new()
    {
        noiseStrength       = 3f,
        noiseScale          = 0.05f,
        waterPenalty        = 15f,
        elevationMultiplier = 2f,
        occupiedPenalty     = 50f,
        maxBridgeLength     = 10,
        roadWidth           = 1
    };
}