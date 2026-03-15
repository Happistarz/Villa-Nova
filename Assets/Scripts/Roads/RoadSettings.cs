using UnityEngine;

[System.Serializable]
public struct RoadSettings
{
    [Header("Noise")]
    [Range(0f, 20f)] public float noiseStrength;

    [Range(0.01f, 0.3f)] public float noiseScale;

    [HideInInspector] public float noiseOffsetX;
    [HideInInspector] public float noiseOffsetY;

    [Header("Cost Weights")]
    public float waterPenalty;

    public float elevationMultiplier;
    public float occupiedPenalty;

    [Header("Road Merging")]
    public float roadBonus;

    public               float roadProximityBonus;
    [Range(0, 5)] public int   roadProximityRadius;

    [Header("Bridges")]
    public float bridgePenalty;

    [Range(1, 20)] public int maxBridgeLength;

    [Header("Stamping")]
    [Range(1, 5)] public int roadWidth;

    [Header("Traversal")]
    public bool allowOutOfBounds;

    public static RoadSettings Default => new()
    {
        noiseStrength       = 3f,
        noiseScale          = 0.05f,
        noiseOffsetX        = 0f,
        noiseOffsetY        = 0f,
        waterPenalty        = 15f,
        elevationMultiplier = 2f,
        roadBonus           = 0.8f,
        roadProximityBonus  = 0.3f,
        roadProximityRadius = 3,
        occupiedPenalty     = 50f,
        bridgePenalty       = 8f,
        maxBridgeLength     = 10,
        roadWidth           = 1,
        allowOutOfBounds    = true,
    };
}

