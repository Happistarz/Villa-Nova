using System.Collections.Generic;
using UnityEngine;

public static class MathHelper
{
    public static float FBm(float _x, float _y, int _octaves)
    {
        var value     = 0f;
        var amplitude = 1f;
        var frequency = 1f;
        var max       = 0f;

        for (var i = 0; i < _octaves; i++)
        {
            value     += Mathf.PerlinNoise(_x * frequency, _y * frequency) * amplitude;
            max       += amplitude;
            amplitude *= 0.5f;
            frequency *= 2f;
        }

        return value / max;
    }

    public static float Remap(float _value, float _from1, float _to1, float _from2, float _to2)
    {
        return (_value - _from1) / (_to1 - _from1) * (_to2 - _from2) + _from2;
    }

    public static float TriangleWave(float t)
    {
        return 1f - Mathf.Abs(2f * t - 1f);
    }

    public static float Quantize(float _value, float _step)
    {
        return Mathf.Floor(_value / _step) * _step;
    }

    public static Vector2 GetPerpendicular(Vector2 _v)
    {
        return new Vector2(-_v.y, _v.x);
    }
    
    public static IEnumerable<Vector2Int> GetPointsInCircle(Vector2Int _center, int _radius)
    {
        for (var dx = -_radius; dx <= _radius; dx++)
        {
            for (var dy = -_radius; dy <= _radius; dy++)
            {
                if (dx * dx + dy * dy <= _radius * _radius)
                {
                    yield return new Vector2Int(_center.x + dx, _center.y + dy);
                }
            }
        }
    }

    public static IEnumerable<Vector2Int> GetPointsInEllipse(Vector2Int _center, int _rx, int _ry)
    {
        for (var dx = -_rx; dx <= _rx; dx++)
        {
            for (var dy = -_ry; dy <= _ry; dy++)
            {
                if ((dx * dx) / (float)(_rx * _rx) + (dy * dy) / (float)(_ry * _ry) <= 1f)
                {
                    yield return new Vector2Int(_center.x + dx, _center.y + dy);
                }
            }
        }
    }
    
    public static IEnumerable<Vector2Int> BresenhamLine(Vector2Int _from, Vector2Int _to)
    {
        var x0 = _from.x;
        var y0 = _from.y;
        var x1 = _to.x;
        var y1 = _to.y;

        var dx  = Mathf.Abs(x1 - x0);
        var dy  = Mathf.Abs(y1 - y0);
        var sx  = x0 < x1 ? 1 : -1;
        var sy  = y0 < y1 ? 1 : -1;
        var err = dx - dy;

        while (true)
        {
            yield return new Vector2Int(x0, y0);

            if (x0 == x1 && y0 == y1) break;

            var e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0  += sx;
            }

            if (e2 >= dx) continue;

            err += dx;
            y0  += sy;
        }
    }
    
    public static float Normalize(float _dx, float _dy, float _rx, float _ry)
    {
        var distance = Mathf.Sqrt(_dx * _dx + _dy * _dy);
        var radius   = Mathf.Sqrt(_rx * _rx + _ry * _ry);
        return distance / radius;
    }

    public static float GetEllipseNormalizedDistance(float _dx, float _dy, float _rx, float _ry)
    {
        return Mathf.Sqrt((_dx * _dx) / (_rx * _rx) + (_dy * _dy) / (_ry * _ry));
    }
}