using UnityEngine;
using UnityEngine.InputSystem;

public class CityGenerator : MonoBehaviour
{
    public WorldGrid grid;
    
    public float settlerSearchRadius = 5f;
    
    private GameObject _settler;
    
    void Start()
    {
        grid.GenerateMap();
        GenerateCity();
    }
    
    public void Update()
    {
        if (!Keyboard.current.spaceKey.wasPressedThisFrame) return;
        
        grid.GenerateMap();
        GenerateCity();
    }
    
    void GenerateCity()
    {
        // find's map best home starting point
        var bestHomePoint = FindSettlePos();
        
        if (_settler) Destroy(_settler);
        
        _settler = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _settler.transform.position = new Vector3(bestHomePoint.x, 2, bestHomePoint.y);
        _settler.transform.localScale = new Vector3(1, 4, 1);
        _settler.GetComponent<Renderer>().material.color = Color.red;
    }
    
    Vector2Int FindSettlePos()
    {
        var bestPoint = Vector2Int.zero;
        var bestScore = float.MinValue;

        for (var x = 0; x < grid.size; x++)
        {
            for (var y = 0; y < grid.size; y++)
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
        var cell  = grid.GetCell(_point);
        
        var nearbyCells = grid.GetTilesInRadius(_point, settlerSearchRadius);
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
        
        var center = new Vector2Int(grid.size / 2, grid.size / 2);
        var distanceToCenter = Vector2Int.Distance(_point, center);
        score -= distanceToCenter * 0.3f;
        
        if (cell.Type is WorldGrid.CellType.WATER or WorldGrid.CellType.RIVER)
        {
            score -= 999f;
        }
        
        return score;
    }
}