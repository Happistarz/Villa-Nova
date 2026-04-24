using System.Collections.Generic;
using UnityEngine;

public class CityRenderer : MonoBehaviour
{
    private const int _BATCH_SIZE = 1023;

    public Material defaultMaterial;


    [Header("LOD")]
    public float lodRebakeThreshold = 5f;

    /// <summary>One instance of a building placed in the world</summary>
    private struct BuildingInstance
    {
        public Vector3   Position;
        public Matrix4x4 Matrix;
    }

    /// <summary>All instances that share the same BuildingData</summary>
    private class BuildingGroup
    {
        public BuildingData                Data;
        public readonly List<BuildingInstance> Instances = new();

        // One batch list per LOD level
        public List<Matrix4x4[]>[] LodBatches;
    }

    private readonly Dictionary<BuildingData, BuildingGroup> _groups = new();

    private Camera  _cam;
    private Vector3 _lastCamPos = Vector3.positiveInfinity;
    private bool    _baked;

    private void Awake()
    {
        _cam = Camera.main;
    }
    
    /// <summary>Registers a building instance for rendering</summary>
    public void AddBuilding(Vector3 _position, int _rotation, BuildingData _data)
    {
        if (!_data || _data.lods == null || _data.lods.Count == 0) return;

        if (!_groups.TryGetValue(_data, out var group))
        {
            group = new BuildingGroup { Data = _data };
            _groups[_data] = group;
        }

        var baseRot  = Quaternion.Euler(_data.meshRotation);
        var gridRot  = Quaternion.Euler(0, _rotation * 90f, 0);
        var matrix   = Matrix4x4.TRS(_position + _data.meshOffset, gridRot * baseRot, _data.meshScale);

        group.Instances.Add(new BuildingInstance { Position = _position, Matrix = matrix });
    }

    /// <summary>Bakes all groups into GPU-ready batches</summary>
    public void BakeBatches()
    {
        foreach (var group in _groups.Values)
        {
            var lodCount = group.Data.lods.Count;
            group.LodBatches = new List<Matrix4x4[]>[lodCount];

            for (var i = 0; i < lodCount; i++)
                group.LodBatches[i] = new List<Matrix4x4[]>();
        }

        _lastCamPos = Vector3.positiveInfinity;
        _baked = true;
        RebakeLodBatches();
    }

    public void Clear()
    {
        _groups.Clear();
        _baked = false;
    }

    private void Update()
    {
        if (!_baked || _groups.Count == 0) return;

        if (_cam)
        {
            var camPos = _cam.transform.position;
            if (Vector3.SqrMagnitude(camPos - _lastCamPos) > lodRebakeThreshold * lodRebakeThreshold)
            {
                _lastCamPos = camPos;
                RebakeLodBatches();
            }
        }

        var mat = defaultMaterial;

        foreach (var group in _groups.Values)
        {
            if (group.LodBatches == null) continue;

            for (var lod = 0; lod < group.Data.lods.Count; lod++)
            {
                var mesh = group.Data.lods[lod].mesh;
                if (!mesh) continue;

                foreach (var batch in group.LodBatches[lod])
                    Graphics.DrawMeshInstanced(mesh, 0, mat, batch);
            }
        }
    }

    private void RebakeLodBatches()
    {
        var camPos = _cam ? _cam.transform.position : Vector3.zero;

        foreach (var group in _groups.Values)
        {
            var lodCount = group.Data.lods.Count;

            // Clear existing batches
            foreach (var bucket in group.LodBatches)
                bucket.Clear();

            // Accumulate per-LOD
            var accumulators = new List<Matrix4x4>[lodCount];
            for (var i = 0; i < lodCount; i++)
                accumulators[i] = new List<Matrix4x4>();

            foreach (var inst in group.Instances)
            {
                var dist = Vector3.Distance(inst.Position, camPos);
                var lod  = ResolveLod(group.Data, dist);
                accumulators[lod].Add(inst.Matrix);
            }

            for (var lod = 0; lod < lodCount; lod++)
                SplitIntoBatches(accumulators[lod], group.LodBatches[lod]);
        }
    }

    private static int ResolveLod(BuildingData _data, float _distance)
    {
        for (var i = 0; i < _data.lods.Count - 1; i++)
            if (_distance <= _data.lods[i].distanceThreshold)
                return i;

        return _data.lods.Count - 1;
    }

    private static void SplitIntoBatches(List<Matrix4x4> _src, List<Matrix4x4[]> _dest)
    {
        for (var i = 0; i < _src.Count; i += _BATCH_SIZE)
        {
            var count = Mathf.Min(_BATCH_SIZE, _src.Count - i);
            var batch = new Matrix4x4[count];
            _src.CopyTo(i, batch, 0, count);
            _dest.Add(batch);
        }
    }
}