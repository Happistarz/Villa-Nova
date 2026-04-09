using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildingData", menuName = "Building Data", order = 0)]
public class BuildingData : ScriptableObject
{
    [Header("General")]
    public int buildingSize = 3;
    
    public List<Vector2Int> buildingArea;

    [Header("Terrain")]
    public float flatTolerance = 0.1f;

    [Serializable]
    public class BuildingLOD
    {
        public Mesh  mesh;
        public float distanceThreshold;
    }

    public List<BuildingLOD> lods;

    [Header("Debug")]
    public Color debugColor = Color.red;

    /// <summary>Returns the LOD mesh for the given camera distance</summary>
    public Mesh GetLODMesh(float _distance)
    {
        foreach (var lod in lods)
            if (_distance < lod.distanceThreshold)
                return lod.mesh;

        return lods[^1].mesh;
    }
}