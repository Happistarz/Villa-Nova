using Core;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraFreeState : State<CameraController>
{
    private float _yaw;
    private float _pitch;

    private float      _transitionTimer;
    private Vector3    _transitionStartPos;
    private Quaternion _transitionStartRot;
    private Vector3    _targetEntryPos;
    private Quaternion _targetEntryRot;
    private bool       _transitioning;
    private bool       _isDragging;
    private bool       _initialized;
    private Vector3    _savedPosition;
    private Quaternion _savedRotation;
    private Bounds     _freeBounds;

    public CameraFreeState(CameraController _context) : base(_context)
    {
    }

    public override void Enter()
    {
        var cam = Context.camera;
        cam.orthographic = false;

        _transitionStartPos = cam.transform.position;
        _transitionStartRot = cam.transform.rotation;
        _transitionTimer    = 0f;
        _transitioning      = true;

        if (_initialized)
        {
            _targetEntryPos = _savedPosition;
            _targetEntryRot = _savedRotation;
        }
        else
        {
            _targetEntryPos = cam.transform.position;
            _targetEntryRot = cam.transform.rotation;
            _initialized    = true;
        }

        var euler = _targetEntryRot.eulerAngles;
        _yaw   = euler.y;
        _pitch = euler.x;
        if (_pitch > 180f) _pitch -= 360f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    public override void Exit()
    {
        _savedPosition = Context.camera.transform.position;
        _savedRotation = Context.camera.transform.rotation;

        _isDragging          = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    public override void Update()
    {
        if (_transitioning)
        {
            _transitionTimer += Time.deltaTime / Context.transitionDuration;
            if (_transitionTimer >= 1f)
            {
                _transitioning                    = false;
                Context.camera.transform.position = _targetEntryPos;
                Context.camera.transform.rotation = _targetEntryRot;
            }
            else
            {
                var t = Mathf.SmoothStep(0f, 1f, _transitionTimer);
                Context.camera.transform.position = Vector3.Lerp(_transitionStartPos, _targetEntryPos, t);
                Context.camera.transform.rotation = Quaternion.Slerp(_transitionStartRot, _targetEntryRot, t);
                return;
            }
        }
        
        _freeBounds.center = Context.MapCenter;
        _freeBounds.size = Context.freeBounds;

        UpdateDragState();
        HandleLook();
        HandleMovement();
    }

    private void UpdateDragState()
    {
        if (Context.FreeLookJustPressed())
        {
            var overUI = EventSystem.current && EventSystem.current.IsPointerOverGameObject();
            _isDragging = !overUI;
        }

        if (Context.FreeLookJustReleased())
            _isDragging = false;

        Cursor.lockState = _isDragging ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !_isDragging;
    }

    private void HandleLook()
    {
        if (!_isDragging) return;

        var delta = Context.ReadRotateDelta();
        _yaw   += delta.x * Context.freeLookSensitivity;
        _pitch -= delta.y * Context.freeLookSensitivity;
        _pitch =  Mathf.Clamp(_pitch, -89f, 89f);

        Context.camera.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    private void HandleMovement()
    {
        var input = Context.ReadMove();
        if (input.sqrMagnitude < 0.001f) return;

        var t         = Context.camera.transform;
        var direction = (t.forward * -input.y + t.right * -input.x).normalized;
        var nextPos   = t.position + direction * (Context.freeMoveSpeed * Time.deltaTime);

        if (Context.freeBounds.sqrMagnitude > 0f)
            nextPos = _freeBounds.ClosestPoint(nextPos);

        t.position = nextPos;
    }
}