using System;
using UnityEngine;

[Serializable]
public struct CameraModeConfig
{
    public float   zoomSpeed;
    public float   minRadius;
    public float   maxRadius;
    public Vector2 pitchBounds;
    public float   rotateSpeed;
    public float   autoRotateSpeed;
    public bool    orthographic;
    public float   orthoSize;

    public static CameraModeConfig DefaultMain => new()
    {
        zoomSpeed       = 1300f,
        minRadius       = 45f,
        maxRadius       = 200f,
        pitchBounds     = new Vector2(30f, 15f),
        rotateSpeed     = 0.5f,
        autoRotateSpeed = 2f,
        orthographic    = false,
        orthoSize       = 50f
    };

    public static CameraModeConfig DefaultClose => new()
    {
        zoomSpeed       = 650f,
        minRadius       = 25f,
        maxRadius       = 80f,
        pitchBounds     = new Vector2(65f, 22f),
        rotateSpeed     = 0.3f,
        autoRotateSpeed = 1f,
        orthographic    = true,
        orthoSize       = 30f
    };
}

