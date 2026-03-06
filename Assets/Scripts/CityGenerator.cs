using System.Collections;
using Core.Patterns;
using UnityEngine;

public class CityGenerator : MonoSingleton<CityGenerator>
{
    public WorldRevealAnimator revealAnimator;

    public float settlerSearchRadius = 5f;
    
    public CityRenderer cityRenderer;

    private WorldGrid _grid;

    private void Start()
    {
        _grid = WorldGrid.Instance;
        
        if (revealAnimator)
            revealAnimator.OnRevealComplete += GenerateCity;

        _grid.OnMapGenerated += OnMapGenerated;
    }

    private void OnDestroy()
    {
        if (revealAnimator)
            revealAnimator.OnRevealComplete -= GenerateCity;

        if (WorldGrid.HasInstance)
            WorldGrid.Instance.OnMapGenerated -= OnMapGenerated;
    }

    private void OnMapGenerated()
    {
        if (!revealAnimator || !revealAnimator.isActiveAndEnabled)
            StartCoroutine(GenerateCityCoroutine());
    }

    private void GenerateCity()
    {
        StartCoroutine(GenerateCityCoroutine());
    }

    private IEnumerator GenerateCityCoroutine()
    {
        var bestHomePoint = Vector2Int.zero;
        yield return StartCoroutine(FindSettlePosCoroutine(_result => bestHomePoint = _result));

        var cell = _grid.GetCell(bestHomePoint);
        if (cell == null)
        {
            _grid.NotifyGenerationComplete();
            yield break;
        }
        
        var tempCell = cell.Value;
        tempCell.Type = WorldGrid.CellType.CITY;
        _grid.UpdateCell(bestHomePoint, tempCell);

        if (_grid.debugRenderer && _grid.debugRenderer.renderEnabled.Value)
            _grid.debugRenderer.BuildMesh();

        yield return StartCoroutine(PlaceHousesCoroutine(cell.Value));

        cityRenderer.BakeBatches();

        _grid.NotifyGenerationComplete();
    }

    private IEnumerator FindSettlePosCoroutine(System.Action<Vector2Int> _onComplete)
    {
        var bestPoint = Vector2Int.zero;
        var bestScore = float.MinValue;

        for (var x = 0; x < _grid.size; x++)
        {
            for (var y = 0; y < _grid.size; y++)
            {
                var point = new Vector2Int(x, y);
                var score = EvaluateSettlePoint(point);

                if (!(score > bestScore)) continue;

                bestScore = score;
                bestPoint = point;
            }
            
            if (x % 10 == 0)
                yield return null;
        }

        _onComplete?.Invoke(bestPoint);
    }

    private float EvaluateSettlePoint(Vector2Int _point)
    {
        var score = 0f;
        var cell  = _grid.GetCell(_point);

        var nearbyCells = _grid.GetTilesInRadius(_point, settlerSearchRadius);
        foreach (var nearbyCell in nearbyCells)
        {
            switch (nearbyCell.Type)
            {
                case WorldGrid.CellType.WATER or WorldGrid.CellType.RIVER:
                    score += 1f;
                    break;
                case WorldGrid.CellType.PLAIN:
                    score += 0.5f;
                    break;
            }
        }

        var center           = new Vector2Int(_grid.size / 2, _grid.size / 2);
        var distanceToCenter = Vector2Int.Distance(_point, center);
        score -= distanceToCenter * 0.3f;

        if (cell?.Type is WorldGrid.CellType.WATER or WorldGrid.CellType.RIVER)
        {
            score -= 999f;
        }

        return score;
    }
    
    private IEnumerator PlaceHousesCoroutine(WorldGrid.Cell _cityCell)
    {
        var count = 0;
        
        const int RADIUS = 32;
        for (var x = -RADIUS; x <= RADIUS; x++)
        {
            for (var y = -RADIUS; y <= RADIUS; y++)
            {
                var point = new Vector2Int(_cityCell.Position.x + x, _cityCell.Position.y + y);
                var cell  = _grid.GetCell(point);

                if (cell?.Type != WorldGrid.CellType.PLAIN) continue;
                var worldPos = _grid.CellToWorld(point);
                // Instantiate(housePrefab, worldPos, housePrefab.transform.rotation.WithYRotation(Random.Range(0,360)), transform);
                cityRenderer.AddHouse(worldPos);
                
                count++;
                
                if (count % 20 == 0)
                    yield return null;
            }
        }
    }
}