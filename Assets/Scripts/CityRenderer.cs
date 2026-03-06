using System.Collections.Generic;
using UnityEngine;

public class CityRenderer : MonoBehaviour
{
    private const int _INSTANCED_BATCH_SIZE = 1023;

    [System.Serializable]
    public struct LodLevel
    {
        public Mesh   mesh;
        public float  maxDistance;
    }

    public Material material;
    public Vector3  eulerRotation = new(-90, 0, 0);
    public Vector3  scale         = Vector3.one;

    [Header("LOD")]
    public LodLevel[] lodLevels;
    public float lodRebakeThreshold = 5f;

    private readonly List<Vector3>    _positions  = new();
    private readonly List<Matrix4x4>  _matrices   = new();

    private List<Matrix4x4[]>[] _lodBatches;

    private Camera   _cam;
    private Vector3  _lastCamPos = Vector3.positiveInfinity;
    private bool     _baked;

    private void Awake()
    {
        _cam = Camera.main;
    }

    public void AddHouse(Vector3 _position)
    {
        var matrix = Matrix4x4.TRS(_position, Quaternion.Euler(eulerRotation), scale);
        _positions.Add(_position);
        _matrices.Add(matrix);
    }

    public void BakeBatches()
    {
        if (lodLevels == null || lodLevels.Length == 0)
        {
            Debug.LogWarning("[CityRenderer] No LOD levels assigned.");
            return;
        }

        _lodBatches = new List<Matrix4x4[]>[lodLevels.Length];
        for (var i = 0; i < lodLevels.Length; i++)
            _lodBatches[i] = new List<Matrix4x4[]>();

        _lastCamPos = Vector3.positiveInfinity; // force immediate bake
        _baked = true;
        RebakeLodBatches();
    }

    public void ClearHouses()
    {
        _positions.Clear();
        _matrices.Clear();
        _lodBatches = null;
        _baked = false;
    }

    private void Update()
    {
        if (!_baked || _lodBatches == null) return;

        if (_cam != null)
        {
            var camPos = _cam.transform.position;
            if (Vector3.SqrMagnitude(camPos - _lastCamPos) > lodRebakeThreshold * lodRebakeThreshold)
            {
                _lastCamPos = camPos;
                RebakeLodBatches();
            }
        }

        for (var lod = 0; lod < lodLevels.Length; lod++)
        {
            if (lodLevels[lod].mesh == null) continue;
            foreach (var batch in _lodBatches[lod])
                Graphics.DrawMeshInstanced(lodLevels[lod].mesh, 0, material, batch);
        }
    }

    private void RebakeLodBatches()
    {
        foreach (var bucket in _lodBatches)
            bucket.Clear();

        var accumulators = new List<Matrix4x4>[lodLevels.Length];
        for (var i = 0; i < lodLevels.Length; i++)
            accumulators[i] = new List<Matrix4x4>();

        var camPos = _cam != null ? _cam.transform.position : Vector3.zero;

        for (var i = 0; i < _positions.Count; i++)
        {
            var dist = Vector3.Distance(_positions[i], camPos);
            var lod  = ResolveLod(dist);
            accumulators[lod].Add(_matrices[i]);
        }

        for (var lod = 0; lod < lodLevels.Length; lod++)
            SplitIntoBatches(accumulators[lod], _lodBatches[lod]);
    }

    private int ResolveLod(float _distance)
    {
        for (var i = 0; i < lodLevels.Length - 1; i++)
            if (_distance <= lodLevels[i].maxDistance)
                return i;

        return lodLevels.Length - 1; // farthest LOD
    }

    private static void SplitIntoBatches(List<Matrix4x4> _src, List<Matrix4x4[]> _dest)
    {
        for (var i = 0; i < _src.Count; i += _INSTANCED_BATCH_SIZE)
        {
            var count = Mathf.Min(_INSTANCED_BATCH_SIZE, _src.Count - i);
            var batch = new Matrix4x4[count];
            _src.CopyTo(i, batch, 0, count);
            _dest.Add(batch);
        }
    }
}