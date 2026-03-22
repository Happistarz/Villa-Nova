using UnityEngine;

[CreateAssetMenu(fileName = "POI Data", menuName = "POI Data", order = 0)]
public class POIData : ScriptableObject
{
    public enum POIType
    {
        CHURCH,
        MARKET,
        TOWN_HALL,
        WELL,
    }

    public enum POIRule
    {
        NEAR_CITY,
        NEAR_WATER,
        POI_DISTANCE,
        NEAR_ROAD,
    }

    [System.Serializable]
    public struct POIRuleData
    {
        public POIRule rule;
        public float   value;
        public float   scoreWeight;
    }

    public POIType    Type;
    public Color      DebugColor = Color.magenta;
    public Vector2Int SpawnRange = new(1,1);

    public POIRuleData[] Rules;

    public BuildingData BuildingData;

    public override string ToString()
    {
        return $"{Type} (Rules: {Rules.Length}), Building: {BuildingData.name}, SpawnRange: {SpawnRange}, DebugColor: {DebugColor}";
    }
}