using UnityEngine;

[CreateAssetMenu(fileName = "RoadAgent", menuName = "RoadAgent", order = 0)]
public class RoadAgentConfig : ScriptableObject
{
    public RoadGraph.EdgeType roadType = RoadGraph.EdgeType.MAIN;

    [Header("Segment")]
    [Range(1, 5)] public int roadWidth = 1;
    [Range(5, 60)] public int minSteps = 10;
    [Range(10, 100)] public int maxSteps = 30;
    
    [Header("Branching")]
    [Range(0f, 0.5f)] public float branchProbability = 0.2f;
    [Range(3, 20)] public int minBranchSteps = 3;

    [Header("Direction")]
    [Range(0f, 0.3f)] public float dirNoiseScale = 0.1f;
    [Range(0f, 45f)] public float dirNoiseStrength = 15f;

    [Header("Constraint")]
    [Range(0f, 1f)] public float minUrbanity = 0.2f;

    [Range(1, 50)] public int maxAgents = 20;
}