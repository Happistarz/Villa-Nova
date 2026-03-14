using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Game Config", order = 0)]
public class GameConfig : ScriptableObject
{
    [System.Serializable]
    public struct BiomeColorConfig
    {
        [Header("Terrain")]
        public Color plainColor;

        public Color cityColor;

        [Header("Water")]
        public Color waterColor;

        public Color riverColor;

        [Header("Roads")]
        public Color roadColor;

        public Color bridgeColor;

        [Header("Walls / Borders")]
        public Color wallColor;

        [Header("Debug Overlay")]
        public Color debugPlainColor;

        public Color debugWaterColor;
        public Color debugRiverColor;
        public Color debugRoadColor;
        public Color debugBridgeColor;
        public Color debugCityColor;

        public static BiomeColorConfig Default => new()
        {
            plainColor  = new Color(0.3f,  0.8f,  0.3f),
            cityColor   = new Color(0.85f, 0.75f, 0.5f),
            waterColor  = new Color(0.2f,  0.4f,  0.8f),
            riverColor  = new Color(0.1f,  0.3f,  0.7f),
            roadColor   = new Color(0.55f, 0.5f,  0.4f),
            bridgeColor = new Color(0.6f,  0.45f, 0.25f),
            wallColor   = new Color(0.45f, 0.3f,  0.1f),

            debugPlainColor  = new Color(0.3f, 0.8f,  0.3f,   0.5f),
            debugWaterColor  = new Color(0.2f, 0.4f,  0.8f,   0.5f),
            debugRiverColor  = new Color(0.1f, 0.3f,  0.7f,   0.5f),
            debugRoadColor   = new Color(0.5f, 0.5f,  0.5f,   0.5f),
            debugBridgeColor = new Color(0.6f, 0.45f, 0.25f,  0.5f),
            debugCityColor   = new Color(1f,   0.92f, 0.016f, 1f),
        };
    }

    [Header("Biome Palettes")]
    public BiomeColorConfig[] biomePalettes =
    {
        BiomeColorConfig.Default,

        // Desert
        new()
        {
            plainColor  = new Color(0.82f, 0.75f, 0.5f),
            cityColor   = new Color(0.85f, 0.75f, 0.5f),
            waterColor  = new Color(0.15f, 0.35f, 0.65f),
            riverColor  = new Color(0.1f,  0.28f, 0.55f),
            roadColor   = new Color(0.65f, 0.55f, 0.35f),
            bridgeColor = new Color(0.7f,  0.55f, 0.3f),
            wallColor   = new Color(0.6f,  0.45f, 0.2f),

            debugPlainColor  = new Color(0.82f, 0.75f, 0.5f,  0.5f),
            debugWaterColor  = new Color(0.15f, 0.35f, 0.65f, 0.5f),
            debugRiverColor  = new Color(0.1f,  0.28f, 0.55f, 0.5f),
            debugRoadColor   = new Color(0.65f, 0.55f, 0.35f, 0.5f),
            debugBridgeColor = new Color(0.7f,  0.55f, 0.3f,  0.5f),
            debugCityColor   = new Color(1f,    0.85f, 0.3f,  1f),
        },

        // Taiga
        new()
        {
            plainColor  = new Color(0.3f,  0.5f,  0.3f),
            cityColor   = new Color(0.65f, 0.6f,  0.5f),
            waterColor  = new Color(0.18f, 0.32f, 0.55f),
            riverColor  = new Color(0.12f, 0.25f, 0.5f),
            roadColor   = new Color(0.45f, 0.42f, 0.38f),
            bridgeColor = new Color(0.5f,  0.4f,  0.25f),
            wallColor   = new Color(0.4f,  0.35f, 0.25f),

            debugPlainColor  = new Color(0.3f,  0.5f,  0.3f,  0.5f),
            debugWaterColor  = new Color(0.18f, 0.32f, 0.55f, 0.5f),
            debugRiverColor  = new Color(0.12f, 0.25f, 0.5f,  0.5f),
            debugRoadColor   = new Color(0.45f, 0.42f, 0.38f, 0.5f),
            debugBridgeColor = new Color(0.5f,  0.4f,  0.25f, 0.5f),
            debugCityColor   = new Color(0.9f,  0.85f, 0.6f,  1f),
        },

        // Harvest
        new()
        {
            plainColor  = new Color(0.6f,  0.65f, 0.25f),
            cityColor   = new Color(0.8f,  0.7f,  0.45f),
            waterColor  = new Color(0.2f,  0.38f, 0.65f),
            riverColor  = new Color(0.12f, 0.3f,  0.58f),
            roadColor   = new Color(0.5f,  0.42f, 0.3f),
            bridgeColor = new Color(0.55f, 0.4f,  0.22f),
            wallColor   = new Color(0.5f,  0.35f, 0.15f),

            debugPlainColor  = new Color(0.6f,  0.65f, 0.25f, 0.5f),
            debugWaterColor  = new Color(0.2f,  0.38f, 0.65f, 0.5f),
            debugRiverColor  = new Color(0.12f, 0.3f,  0.58f, 0.5f),
            debugRoadColor   = new Color(0.5f,  0.42f, 0.3f,  0.5f),
            debugBridgeColor = new Color(0.55f, 0.4f,  0.22f, 0.5f),
            debugCityColor   = new Color(0.95f, 0.8f,  0.3f,  1f),
        },

        // Marshland
        new()
        {
            plainColor  = new Color(0.28f, 0.45f, 0.22f),
            cityColor   = new Color(0.55f, 0.5f,  0.35f),
            waterColor  = new Color(0.15f, 0.3f,  0.35f),
            riverColor  = new Color(0.1f,  0.25f, 0.3f),
            roadColor   = new Color(0.4f,  0.38f, 0.3f),
            bridgeColor = new Color(0.45f, 0.38f, 0.2f),
            wallColor   = new Color(0.35f, 0.3f,  0.18f),

            debugPlainColor  = new Color(0.28f, 0.45f, 0.22f, 0.5f),
            debugWaterColor  = new Color(0.15f, 0.3f,  0.35f, 0.5f),
            debugRiverColor  = new Color(0.1f,  0.25f, 0.3f,  0.5f),
            debugRoadColor   = new Color(0.4f,  0.38f, 0.3f,  0.5f),
            debugBridgeColor = new Color(0.45f, 0.38f, 0.2f,  0.5f),
            debugCityColor   = new Color(0.7f,  0.65f, 0.4f,  1f),
        },

        // Winter
        new()
        {
            plainColor  = new Color(0.85f, 0.88f, 0.92f),
            cityColor   = new Color(0.75f, 0.72f, 0.65f),
            waterColor  = new Color(0.2f,  0.35f, 0.55f),
            riverColor  = new Color(0.15f, 0.3f,  0.5f),
            roadColor   = new Color(0.6f,  0.58f, 0.55f),
            bridgeColor = new Color(0.55f, 0.48f, 0.35f),
            wallColor   = new Color(0.5f,  0.48f, 0.42f),

            debugPlainColor  = new Color(0.85f, 0.88f, 0.92f, 0.5f),
            debugWaterColor  = new Color(0.2f,  0.35f, 0.55f, 0.5f),
            debugRiverColor  = new Color(0.15f, 0.3f,  0.5f,  0.5f),
            debugRoadColor   = new Color(0.6f,  0.58f, 0.55f, 0.5f),
            debugBridgeColor = new Color(0.55f, 0.48f, 0.35f, 0.5f),
            debugCityColor   = new Color(0.8f,  0.75f, 0.6f,  1f),
        },
    };

    [Header("Near Cities")]
    public string[] cityNames;

    // ─────────────────────────── API ───────────────────────────

    /// <summary>Picks a random biome palette for the current generation.</summary>
    public BiomeColorConfig GetRandomPalette()
    {
        if (biomePalettes == null || biomePalettes.Length == 0)
            return BiomeColorConfig.Default;

        return biomePalettes[Random.Range(0, biomePalettes.Length)];
    }

    /// <summary>Legacy — returns just the plain color from a random palette.</summary>
    public Color GetRandomBiomeColor()
    {
        return GetRandomPalette().plainColor;
    }

    public List<string> GetRandomCityNames(int _count)
    {
        var pool   = new List<string>(cityNames);
        var count  = Mathf.Min(_count, pool.Count);
        var result = new List<string>(count);

        for (var i = 0; i < count; i++)
        {
            var index = Random.Range(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return result;
    }
}