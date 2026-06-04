using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Game Config", order = 0)]
public class GameConfig : ScriptableObject
{
    [Header("Building Registry")]
    public BuildingData[] buildingDataList;

    public BuildingData[] GetHousesData() =>
        System.Array.FindAll(buildingDataList, _data => _data.buildingType == BuildingData.BuildingType.HOUSE);

    public BuildingData[] GetTreesData() =>
        System.Array.FindAll(buildingDataList, _data => _data.buildingType == BuildingData.BuildingType.TREE);

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

        [Header("Buildings")]
        public Color houseColor;

        public BuildingData treeData;
        public Color        treeColor;

        [Header("Walls / Borders")]
        public Color wallColor;

        [Header("Debug Overlay")]
        public Color debugPlainColor;

        public Color debugWaterColor;
        public Color debugRiverColor;
        public Color debugRoadColor;
        public Color debugBridgeColor;
        public Color debugCityColor;
        public Color debugHouseColor;

        public static BiomeColorConfig Default => new()
        {
            plainColor  = new Color(0.3f,  0.8f,  0.3f),
            cityColor   = new Color(0.85f, 0.75f, 0.5f),
            waterColor  = new Color(0.2f,  0.4f,  0.8f),
            riverColor  = new Color(0.1f,  0.3f,  0.7f),
            roadColor   = new Color(0.55f, 0.5f,  0.4f),
            bridgeColor = new Color(0.6f,  0.45f, 0.25f),
            houseColor  = new Color(0.75f, 0.55f, 0.35f),
            treeColor   = new Color(0.25f, 0.55f, 0.2f),
            wallColor   = new Color(0.45f, 0.3f,  0.1f),

            debugPlainColor  = new Color(0.3f,  0.8f,  0.3f,   0.5f),
            debugWaterColor  = new Color(0.2f,  0.4f,  0.8f,   0.5f),
            debugRiverColor  = new Color(0.1f,  0.3f,  0.7f,   0.5f),
            debugRoadColor   = new Color(0.5f,  0.5f,  0.5f,   0.5f),
            debugBridgeColor = new Color(0.6f,  0.45f, 0.25f,  0.5f),
            debugCityColor   = new Color(1f,    0.92f, 0.016f, 1f),
            debugHouseColor  = new Color(0.75f, 0.55f, 0.35f,  0.7f)
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
            houseColor  = new Color(0.85f, 0.7f,  0.45f),
            treeColor   = new Color(0.55f, 0.45f, 0.15f),
            wallColor   = new Color(0.6f,  0.45f, 0.2f),

            debugPlainColor  = new Color(0.82f, 0.75f, 0.5f,  0.5f),
            debugWaterColor  = new Color(0.15f, 0.35f, 0.65f, 0.5f),
            debugRiverColor  = new Color(0.1f,  0.28f, 0.55f, 0.5f),
            debugRoadColor   = new Color(0.65f, 0.55f, 0.35f, 0.5f),
            debugBridgeColor = new Color(0.7f,  0.55f, 0.3f,  0.5f),
            debugCityColor   = new Color(1f,    0.85f, 0.3f,  1f),
            debugHouseColor  = new Color(0.85f, 0.7f,  0.45f, 0.7f)
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
            houseColor  = new Color(0.6f,  0.5f,  0.35f),
            treeColor   = new Color(0.15f, 0.4f,  0.25f),
            wallColor   = new Color(0.4f,  0.35f, 0.25f),

            debugPlainColor  = new Color(0.3f,  0.5f,  0.3f,  0.5f),
            debugWaterColor  = new Color(0.18f, 0.32f, 0.55f, 0.5f),
            debugRiverColor  = new Color(0.12f, 0.25f, 0.5f,  0.5f),
            debugRoadColor   = new Color(0.45f, 0.42f, 0.38f, 0.5f),
            debugBridgeColor = new Color(0.5f,  0.4f,  0.25f, 0.5f),
            debugCityColor   = new Color(0.9f,  0.85f, 0.6f,  1f),
            debugHouseColor  = new Color(0.6f,  0.5f,  0.35f, 0.7f)
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
            houseColor  = new Color(0.7f,  0.58f, 0.35f),
            treeColor   = new Color(0.45f, 0.55f, 0.1f),
            wallColor   = new Color(0.5f,  0.35f, 0.15f),

            debugPlainColor  = new Color(0.6f,  0.65f, 0.25f, 0.5f),
            debugWaterColor  = new Color(0.2f,  0.38f, 0.65f, 0.5f),
            debugRiverColor  = new Color(0.12f, 0.3f,  0.58f, 0.5f),
            debugRoadColor   = new Color(0.5f,  0.42f, 0.3f,  0.5f),
            debugBridgeColor = new Color(0.55f, 0.4f,  0.22f, 0.5f),
            debugCityColor   = new Color(0.95f, 0.8f,  0.3f,  1f),
            debugHouseColor  = new Color(0.7f,  0.58f, 0.35f, 0.7f)
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
            houseColor  = new Color(0.55f, 0.45f, 0.3f),
            treeColor   = new Color(0.2f,  0.38f, 0.18f),
            wallColor   = new Color(0.35f, 0.3f,  0.18f),

            debugPlainColor  = new Color(0.28f, 0.45f, 0.22f, 0.5f),
            debugWaterColor  = new Color(0.15f, 0.3f,  0.35f, 0.5f),
            debugRiverColor  = new Color(0.1f,  0.25f, 0.3f,  0.5f),
            debugRoadColor   = new Color(0.4f,  0.38f, 0.3f,  0.5f),
            debugBridgeColor = new Color(0.45f, 0.38f, 0.2f,  0.5f),
            debugCityColor   = new Color(0.7f,  0.65f, 0.4f,  1f),
            debugHouseColor  = new Color(0.55f, 0.45f, 0.3f,  0.7f)
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
            houseColor  = new Color(0.7f,  0.65f, 0.55f),
            treeColor   = new Color(0.55f, 0.65f, 0.6f),
            wallColor   = new Color(0.5f,  0.48f, 0.42f),

            debugPlainColor  = new Color(0.85f, 0.88f, 0.92f, 0.5f),
            debugWaterColor  = new Color(0.2f,  0.35f, 0.55f, 0.5f),
            debugRiverColor  = new Color(0.15f, 0.3f,  0.5f,  0.5f),
            debugRoadColor   = new Color(0.6f,  0.58f, 0.55f, 0.5f),
            debugBridgeColor = new Color(0.55f, 0.48f, 0.35f, 0.5f),
            debugCityColor   = new Color(0.8f,  0.75f, 0.6f,  1f),
            debugHouseColor  = new Color(0.7f,  0.65f, 0.55f, 0.7f)
        }
    };

    [Header("Near Cities")]
    public string[] cityNames;

    /// <summary>Returns a random palette from the array, or default if empty</summary>
    public BiomeColorConfig GetRandomPalette()
    {
        if (biomePalettes == null || biomePalettes.Length == 0)
            return BiomeColorConfig.Default;

        return biomePalettes[GameManager.Instance.RandomEngine.Next(0, biomePalettes.Length)];
    }

    /// <summary>Picks unique random names from the pool without repetition</summary>
    public List<string> GetRandomCityNames(int _count)
    {
        var rng    = GameManager.Instance.RandomEngine;
        var pool   = new List<string>(cityNames);
        var count  = Mathf.Min(_count, pool.Count);
        var result = new List<string>(count);

        for (var i = 0; i < count; i++)
        {
            var index = rng.Next(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return result;
    }

    /// <summary> Picks a random name from the pool that is not in the given exclusion list, or null if none available</summary>
    public string GetUniqueCityName(List<string> _exclude)
    {
        var rng  = GameManager.Instance.RandomEngine;
        var pool = new List<string>(cityNames);

        foreach (var _name in _exclude) pool.Remove(_name);

        return pool.Count == 0 ? null : pool[rng.Next(0, pool.Count)];
    }
}