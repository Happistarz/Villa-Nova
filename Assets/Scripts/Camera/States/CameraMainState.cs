using Core;
using Core.Extensions;
using UnityEngine;

public class CameraMainState : State<CameraController>
{
    private float _orbitRadius, _targetOrbitRadius, _orbitRadiusVelocity;
    private float _orbitHeight, _targetOrbitHeight, _orbitHeightVelocity;
    private float _yaw,         _targetYaw,         _yawVelocity;
    private float _pitch;

    private float      _transitionTimer;
    private Vector3    _transitionStartPos;
    private Quaternion _transitionStartRot;
    private bool       _transitioning;
    private bool       _skipTransition;

    public CameraMainState(CameraController _context) : base(_context)
    {
    }

    public void SkipNextTransition() => _skipTransition = true;

    public override void Enter()
    {
        var cam = Context.camera;
        var cfg = Context.mainConfig;

        cam.orthographic = cfg.orthographic;

        var pivot    = Context.MapCenter;
        var toCamera = cam.transform.position - pivot;
        var flat     = toCamera.Flat();

        _orbitRadius       = Mathf.Clamp(flat.magnitude, cfg.minRadius, cfg.maxRadius);
        _targetOrbitRadius = _orbitRadius;

        if (_orbitHeight <= 0f)
            _orbitHeight = toCamera.y;
        
        _targetOrbitHeight = _orbitHeight;

        _yaw       = cam.transform.eulerAngles.y;
        _targetYaw = _yaw;

        _pitch = cam.transform.eulerAngles.x;

        var rotation  = Quaternion.Euler(_pitch, _yaw, 0f);
        var flatRot   = Quaternion.Euler(0f,     _yaw, 0f);
        var targetPos = Context.MapCenter + flatRot * new Vector3(0f, 0f, -_orbitRadius) + Vector3.up * _orbitHeight;

        if (_skipTransition)
        {
            _transitioning  = false;
            _skipTransition = false;
        }
        else
        {
            var t = Mathf.SmoothStep(0f, 1f, _transitionTimer);
            Context.camera.transform.position = Vector3.Lerp(_transitionStartPos, targetPos, t);
            Context.camera.transform.rotation = Quaternion.Slerp(_transitionStartRot, rotation, t);
            _transitionTimer                  = 0f;
            _transitioning                    = true;
        }

        _orbitRadiusVelocity = 0f;
        _orbitHeightVelocity = 0f;
        _yawVelocity         = 0f;
    }

    public override void Update()
    {
        if (_transitioning)
        {
            _transitionTimer += Time.deltaTime / Context.transitionDuration;
            if (_transitionTimer >= 1f)
                _transitioning = false;
        }

        HandleZoom();
        HandleRotation();
        ApplyTransform();
    }

    private void HandleZoom()
    {
        var scroll = Context.ReadScroll();
        if (Mathf.Approximately(scroll, 0f)) return;

        var cfg = Context.mainConfig;
        _targetOrbitRadius += scroll / 120f * cfg.zoomSpeed;
        _targetOrbitRadius =  Mathf.Clamp(_targetOrbitRadius, cfg.minRadius, cfg.maxRadius);
    }

    private void HandleRotation()
    {
        var cfg    = Context.mainConfig;
        var isHeld = Context.IsRotateHeld();

        if (isHeld)
        {
            var delta = Context.ReadRotateDelta();
            _targetYaw += delta.x * cfg.rotateSpeed;
        }
        else
        {
            _targetYaw += cfg.autoRotateSpeed * Time.deltaTime;
        }
    }

    private void ApplyTransform()
    {
        var cfg = Context.mainConfig;
        var st  = Context.smoothTime;

        _yaw         = Mathf.SmoothDampAngle(_yaw, _targetYaw, ref _yawVelocity, st);
        _orbitRadius = Mathf.SmoothDamp(_orbitRadius, _targetOrbitRadius, ref _orbitRadiusVelocity, st);
        _orbitHeight = Mathf.SmoothDamp(_orbitHeight, _targetOrbitHeight, ref _orbitHeightVelocity, st);

        if (Context.zoomLevel)
            Context.zoomLevel.value = Mathf.InverseLerp(cfg.minRadius, cfg.maxRadius, _orbitRadius);

        var zoomT = Mathf.Clamp01((_orbitRadius - cfg.minRadius) / (cfg.maxRadius - cfg.minRadius));
        _pitch = Mathf.Lerp(cfg.pitchBounds.x, cfg.pitchBounds.y, zoomT);

        var pivot    = Context.MapCenter;
        var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        var flatRot  = Quaternion.Euler(0f,     _yaw, 0f);

        var targetPos = pivot + flatRot * new Vector3(0f, 0f, -_orbitRadius) + Vector3.up * _orbitHeight;

        if (_transitioning)
        {
            var t = Mathf.SmoothStep(0f, 1f, _transitionTimer);
            Context.camera.transform.position = Vector3.Lerp(_transitionStartPos, targetPos, t);
            Context.camera.transform.rotation = Quaternion.Slerp(_transitionStartRot, rotation, t);
        }
        else
        {
            Context.camera.transform.position = targetPos;
            Context.camera.transform.rotation = rotation;
        }
    }
}