using Core;
using DG.Tweening;
using UnityEngine;

public class CameraStartState : State<CameraController>
{
    public bool IsDone { get; private set; }

    public CameraStartState(CameraController _context) : base(_context)
    {
    }

    public override void Enter()
    {
        var cam   = Context.camera.transform;
        var pivot = Context.MapCenter;
        var cfg   = Context.mainConfig;

        var startHeight = (cfg.minRadius + cfg.maxRadius) * 0.5f;
        var startPos    = pivot + Vector3.up * startHeight;
        var startRot    = Quaternion.Euler(Context.startPitch, cam.eulerAngles.y, 0f);

        cam.position = startPos;
        cam.rotation = startRot;
    }

    public void StartAnimation()
    {
        var cam   = Context.camera.transform;
        var cfg   = Context.mainConfig;
        var pivot = Context.MapCenter;

        var targetRadius = (cfg.minRadius + cfg.maxRadius) * 0.5f;
        var zoomT        = Mathf.InverseLerp(cfg.minRadius, cfg.maxRadius, targetRadius);
        var targetPitch  = Mathf.Lerp(cfg.pitchBounds.x, cfg.pitchBounds.y, zoomT);
        var targetYaw    = cam.eulerAngles.y;

        var flatRot   = Quaternion.Euler(0f, targetYaw, 0f);
        var targetPos = pivot + flatRot * new Vector3(0f, 0f, -targetRadius) + Vector3.up * targetRadius;
        var targetRot = Quaternion.Euler(targetPitch, targetYaw, 0f);

        cam.DOMove(targetPos, Context.duration).SetEase(Ease.InOutSine);
        cam.DORotateQuaternion(targetRot, Context.duration)
           .SetEase(Ease.InOutSine)
           .OnComplete(() => IsDone = true);
    }
}