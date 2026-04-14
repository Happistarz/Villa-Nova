using UnityEngine;

[CreateAssetMenu(fileName = "HousePlacementConfig", menuName = "Generation/HousePlacementConfig")]
public class HousePlacementConfig : ScriptableObject
{
    [Header("Placement Threshold")]
    [Range(0f, 1f)] public float placementThreshold = 0.35f;

    [Header("Factor Weights")]
    [Min(0f)] public float urbanityWeight = 1f;

    [Header("Proximity")]
    [Min(0f)] public float waterWeight       = 0.3f;
    [Min(0f)] public float roadWeight        = 1f;
    [Min(0f)] public float cityCenterWeight  = 0.6f;
    [Min(0f)] public float houseClusterWeight = 0.8f;

    [Min(0f)] public float roadDepthWeight = 0.4f;

    [Header("Distance Falloff")]
    [Range(5f, 100f)]  public float waterMaxDistance   = 30f;
    [Range(5f, 60f)]   public float roadMaxDistance    = 15f;
    [Range(10f, 200f)] public float centerMaxDistance  = 80f;
    [Range(2f, 30f)]   public float clusterMaxDistance = 30f;

    [Header("Road Depth")]
    [Range(1f, 10f)]   public float roadDepthIdeal     = 3f;
    [Range(2f, 20f)]   public float roadDepthMax       = 8f;

    [Header("Edge Fade")]
    [Range(0f, 1f)] public float edgeFadeStart = 0.3f;
    [Range(0f, 1f)] public float edgeFadeStrength = 0.7f;

    [Header("Organic Noise")]
    public float noiseScale = 0.05f;
    [Range(0f, 0.5f)] public float noiseAmplitude = 0.15f;
}