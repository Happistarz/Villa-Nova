using UnityEngine;

namespace Generators
{
    [CreateAssetMenu(fileName = "UrbanityConfig", menuName = "UrbanityConfig", order = 0)]
    public class UrbanityConfig : ScriptableObject
    {
        [Range(20,    80)]   public float maxRadius  = 40f;
        [Range(0.01f, 0.1f)] public float noiseScale = 0.03f;
        [Range(0f, 0.5f)] public float noiseAmplitude = 0.4f;
        [Range(0.3f, 0.7f)] public float denseThreshold = 0.5f;
        [Range(0.1f, 0.4f)] public float suburbanThreshold = 0.2f;
    }
}