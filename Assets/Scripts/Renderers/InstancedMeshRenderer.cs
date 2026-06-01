using System.Collections.Generic;
using UnityEngine;

public class InstancedMeshRenderer : MonoBehaviour
{
    private const int _BATCH_SIZE = 1023;

    public Material defaultMaterial;
    public float    lodRebakeThreshold = 5f;

    private struct Instance
    {
        public Vector3   Position;
        public Matrix4x4 Matrix;
    }

    private class Group
    {
        public BuildingData          Data;
        public readonly List<Instance> Instances = new();
        public List<Matrix4x4[]>[]   LodBatches;
    }

    private static readonly int _BASE_COLOR_ID = Shader.PropertyToID("_BaseColor");
    private static readonly int _COLOR_ID     = Shader.PropertyToID("_Color");

    private readonly Dictionary<BuildingData, Group> _groups = new();

    private Camera                _cam;
    private Vector3               _lastCamPos = Vector3.positiveInfinity;
    private bool                  _baked;
    private MaterialPropertyBlock _propertyBlock;
    private bool                  _useColor;

    private void Awake()
    {
        _cam           = Camera.main;
        _propertyBlock = new MaterialPropertyBlock();
    }

    /// <summary>Set a tint color applied to all instances via MaterialPropertyBlock</summary>
    public void SetColor(Color _color)
    {
        _propertyBlock.SetColor(_BASE_COLOR_ID, _color);
        _propertyBlock.SetColor(_COLOR_ID,     _color);
        _useColor = true;
    }

    /// <summary>Clears the tint color so instances render with the default material color</summary>
    public void ClearColor()
    {
        _propertyBlock = new MaterialPropertyBlock();
        _useColor      = false;
    }

    /// <summary>Registers an instance to be rendered</summary>
    public void AddInstance(Vector3 _position, int _rotationSteps, BuildingData _data)
    {
        if (!_data || _data.lods == null || _data.lods.Count == 0) return;

        if (!_groups.TryGetValue(_data, out var group))
        {
            group          = new Group { Data = _data };
            _groups[_data]  = group;
        }

        var baseRot = Quaternion.Euler(_data.meshRotation);
        var gridRot = Quaternion.Euler(0, _rotationSteps * 90f, 0);
        var matrix  = Matrix4x4.TRS(_position + _data.meshOffset, gridRot * baseRot, _data.meshScale);

        group.Instances.Add(new Instance { Position = _position, Matrix = matrix });
    }

    /// <summary>Precomputes batches of instance matrices for each LOD based on the current camera position</summary>
    public void BakeBatches()
    {
        foreach (var group in _groups.Values)
        {
            var lodCount    = group.Data.lods.Count;
            group.LodBatches = new List<Matrix4x4[]>[lodCount];

            for (var i = 0; i < lodCount; i++)
                group.LodBatches[i] = new List<Matrix4x4[]>();
        }

        _lastCamPos = Vector3.positiveInfinity;
        _baked      = true;
        RebakeLodBatches();
    }

    /// <summary>Clears all instances and resets the renderer</summary>
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

        var mpb = _useColor ? _propertyBlock : null;

        foreach (var group in _groups.Values)
        {
            if (group.LodBatches == null) continue;

            for (var lod = 0; lod < group.Data.lods.Count; lod++)
            {
                var mesh = group.Data.lods[lod].mesh;
                if (!mesh) continue;

                foreach (var batch in group.LodBatches[lod])
                    Graphics.DrawMeshInstanced(mesh, 0, defaultMaterial, batch, batch.Length, mpb);
            }
        }
    }

    private void RebakeLodBatches()
    {
        var camPos = _cam ? _cam.transform.position : Vector3.zero;

        foreach (var group in _groups.Values)
        {
            var lodCount = group.Data.lods.Count;

            foreach (var bucket in group.LodBatches)
                bucket.Clear();

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



