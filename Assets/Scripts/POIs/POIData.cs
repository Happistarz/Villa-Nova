using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "POI Data", menuName = "POI Data", order = 0)]
public class POIData : ScriptableObject
{
    public enum POIType
    {
        CHURCH,
        MARKET,
        TOWN_HALL,
        WELL
    }

    public enum POIRule
    {
        NEAR_CITY,
        NEAR_WATER,
        POI_DISTANCE,
        NEAR_ROAD,
        HIGH_ELEVATION,
    }

    [System.Serializable]
    public struct POIRuleData
    {
        public POIRule rule;
        public float   value;
        public float   scoreWeight;
    }

    [FormerlySerializedAs("Type")]       public POIType    type;
    [FormerlySerializedAs("DebugColor")] public Color      debugColor = Color.magenta;
    [FormerlySerializedAs("SpawnRange")] public Vector2Int spawnRange = new(1,1);

    [FormerlySerializedAs("Rules")] public POIRuleData[] rules;

    [FormerlySerializedAs("BuildingData")] public BuildingData buildingData;

    public override string ToString()
    {
        return $"{type} (Rules: {rules.Length}), Building: {buildingData.name}, SpawnRange: {spawnRange}, DebugColor: {debugColor}";
    }
}