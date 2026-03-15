using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildingData", menuName = "Building Data", order = 0)]
public class BuildingData : ScriptableObject
{
    public enum Rotation
    {
        NONE,
        ROTATE90,
        ROTATE180,
        ROTATE270
    }

    [Header("Zone multi-cases")]
    public int buildingSize = 3;
    
    public List<Vector2Int> buildingArea;

    [Header("Rotation")]
    public bool randomizeRotation;
    
    [HideInInspector]
    public Rotation rotation;

    [Header("Contraintes de terrain")]
    public float flatTolerance = 0.1f;

    [Header("Rendu")]
    public Material material;

    [Serializable]
    public class BuildingLOD
    {
        public Mesh  mesh;
        public float distanceThreshold;
    }

    public List<BuildingLOD> lods;

    [Header("Debug")]
    
    public Color debugColor = Color.red;

    public Mesh GetLODMesh(float _distance)
    {
        foreach (var lod in lods)
            if (_distance < lod.distanceThreshold)
                return lod.mesh;

        return lods[^1].mesh;
    }
}