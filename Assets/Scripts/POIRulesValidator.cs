using System;
using System.Collections.Generic;
using UnityEngine;

public static class POIRulesValidator
{
    public static bool IsValid(POIData _poiData, Vector2Int _position, WorldGrid _grid,
                               List<Vector2Int> _placedPOIs)
    {
        var cell = _grid.GetCell(_position);
        if (cell == null || cell.Value.POI) return false;

        if (cell.Value.IsOccupied) return false;

        if (cell.Value.Type is WorldGrid.CellType.WATER or WorldGrid.CellType.RIVER)
            return false;

        foreach (var rule in _poiData.Rules)
        {
            switch (rule.rule)
            {
                case POIData.POIRule.NEAR_CITY:
                    if (!HasTypeInRadius(_position, _grid, WorldGrid.CellType.CITY, rule.value))
                        return false;
                    break;

                case POIData.POIRule.NEAR_WATER:
                    if (!HasWaterInRadius(_position, _grid, rule.value))
                        return false;
                    break;

                case POIData.POIRule.POI_DISTANCE:
                    if (!IsMinDistanceFromPOIs(_position, _placedPOIs, rule.value))
                        return false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return true;
    }

    public static float Score(POIData _poiData, Vector2Int _position, WorldGrid _grid,
                              List<Vector2Int> _placedPOIs, Vector2Int _cityCenter)
    {
        var score = 0f;

        foreach (var rule in _poiData.Rules)
        {
            var ruleScore = rule.rule switch
            {
                POIData.POIRule.NEAR_CITY =>
                    ScoreProximityToType(_position, _grid, WorldGrid.CellType.CITY, rule.value),
                POIData.POIRule.NEAR_WATER   => ScoreProximityToWater(_position, _grid, rule.value),
                POIData.POIRule.POI_DISTANCE => ScoreDistanceFromPOIs(_position, _placedPOIs, rule.value),
                _                            => throw new ArgumentOutOfRangeException()
            };
            score += ruleScore * rule.scoreWeight;
        }

        var distToCenter = Vector2Int.Distance(_position, _cityCenter);
        score -= distToCenter * 0.3f;

        return score;
    }

    private static bool HasTypeInRadius(Vector2Int _pos, WorldGrid _grid, WorldGrid.CellType _type,
                                        float _radius)
    {
        _grid.FillTileBuffer(_pos, _radius);
        
        for (var i = 0; i < WorldGrid.TileBufferCount; i++)
            if (WorldGrid.TileBuffer[i].Type == _type)
                return true;
        
        return false;
    }

    private static bool HasWaterInRadius(Vector2Int _pos, WorldGrid _grid, float _radius)
    {
        _grid.FillTileBuffer(_pos, _radius);
        
        for (var i = 0; i < WorldGrid.TileBufferCount; i++)
            if (WorldGrid.TileBuffer[i].Type is WorldGrid.CellType.WATER or WorldGrid.CellType.RIVER)
                return true;
        
        return false;
    }

    private static bool IsMinDistanceFromPOIs(Vector2Int _pos, List<Vector2Int> _placedPOIs, float _minDist)
    {
        var minDistSq = _minDist * _minDist;
        
        foreach (var poi in _placedPOIs)
            if ((poi - _pos).sqrMagnitude < minDistSq)
                return false;
        
        return true;
    }

    private static float ScoreProximityToType(Vector2Int _pos, WorldGrid _grid, WorldGrid.CellType _type,
                                              float _radius)
    {
        var count = 0;
        _grid.FillTileBuffer(_pos, _radius);
        for (var i = 0; i < WorldGrid.TileBufferCount; i++)
            if (WorldGrid.TileBuffer[i].Type == _type)
                count++;

        return count * 2f;
    }

    private static float ScoreProximityToWater(Vector2Int _pos, WorldGrid _grid, float _radius)
    {
        var closest = float.MaxValue;
        _grid.FillTileBuffer(_pos, _radius);
        for (var i = 0; i < WorldGrid.TileBufferCount; i++)
        {
            ref var c = ref WorldGrid.TileBuffer[i];
            if (c.Type is not (WorldGrid.CellType.WATER or WorldGrid.CellType.RIVER)) continue;
            var dist = Vector2Int.Distance(_pos, c.Position);
            if (dist < closest) closest = dist;
        }

        return closest < float.MaxValue ? (_radius - closest) / _radius * 3f : 0f;
    }

    private static float ScoreDistanceFromPOIs(Vector2Int _pos, List<Vector2Int> _placedPOIs, float _idealDist)
    {
        if (_placedPOIs.Count == 0) return 1f;

        var score = 0f;
        foreach (var poi in _placedPOIs)
        {
            var dist = Vector2Int.Distance(_pos, poi);
            score += 1f - Mathf.Abs(dist - _idealDist) / _idealDist;
        }

        return score / _placedPOIs.Count;
    }
}