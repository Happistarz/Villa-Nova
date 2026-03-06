using UnityEngine;

public class CityGenerator : MonoBehaviour
{
    public WorldRevealAnimator revealAnimator;

    public float settlerSearchRadius = 5f;

    private WorldGrid _grid;

    void Start()
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
            GenerateCity();
    }

    void GenerateCity()
    {
        var bestHomePoint = FindSettlePos();

        var cell = _grid.GetCell(bestHomePoint);
        if (cell == null)
        {
            _grid.NotifyGenerationComplete();
            return;
        }
        
        var tempCell = cell.Value;
        tempCell.Type = WorldGrid.CellType.CITY;
        _grid.UpdateCell(bestHomePoint, tempCell);

        if (_grid.debugRenderer && _grid.debugRenderer.renderEnabled.Value)
            _grid.debugRenderer.BuildMesh();

        _grid.NotifyGenerationComplete();
    }

    Vector2Int FindSettlePos()
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
        }

        return bestPoint;
    }

    float EvaluateSettlePoint(Vector2Int _point)
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
}