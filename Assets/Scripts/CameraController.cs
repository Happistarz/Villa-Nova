using Core.Extensions;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionReference scrollAction;
    public InputActionReference rotateDelta;
    public InputActionReference rotateButton;
    public InputActionReference heightUpAction;
    public InputActionReference heightDownAction;

    [Header("Zoom")]
    public float zoomSpeed  = 1300f;
    public float minRadius  = 45f;
    public float maxRadius  = 200f;
    public Vector2 xRotationBounds = new(12.5f, -15f);

    [Header("Height")]
    public float heightSpeed = 100f;
    public float minHeight   = 10f;

    [Header("Rotation")]
    public Transform rotationPivot;
    public float rotateSpeed     = 0.5f;
    public float autoRotateSpeed = 2f;

    [Header("Smoothing")]
    public float smoothTime = 0.08f;

    private float _orbitRadius;
    private float _targetOrbitRadius;
    private float _orbitRadiusVelocity;

    private float _maxHeight   = 100f;
    private float _orbitHeight;
    private float _targetOrbitHeight;
    private float _orbitHeightVelocity;

    private float _yaw;
    private float _targetYaw;
    private float _yawVelocity;
    private float _pitch;

    private Vector3 PivotPosition => rotationPivot ? rotationPivot.position : Vector3.zero;

    private void Start()
    {
        _pitch     = transform.eulerAngles.x;
        _yaw       = transform.eulerAngles.y;
        _targetYaw = _yaw;

        var toCamera   = transform.position - PivotPosition;
        var flatOffset = toCamera.Flat();

        _orbitRadius       = flatOffset.magnitude;
        _targetOrbitRadius = _orbitRadius;

        _maxHeight         = transform.position.y;
        _orbitHeight       = toCamera.y;
        _targetOrbitHeight = _orbitHeight;

        EnableActions();
    }

    private void Update()
    {
        HandleZoom();
        HandleHeight();
        HandleRotation();
        ApplyTransform();
    }

    private void HandleZoom()
    {
        if (scrollAction?.action == null) return;
        var scroll = scrollAction.action.ReadValue<float>();
        if (scroll == 0f) return;

        _targetOrbitRadius += scroll / 120f * zoomSpeed;
        _targetOrbitRadius =  Mathf.Clamp(_targetOrbitRadius, minRadius, maxRadius);
    }

    private void HandleHeight()
    {
        var delta = 0f;

        if (heightUpAction?.action != null && heightUpAction.action.IsPressed())
            delta += 1f;
        if (heightDownAction?.action != null && heightDownAction.action.IsPressed())
            delta -= 1f;

        if (delta == 0f) return;

        _targetOrbitHeight += delta * heightSpeed * Time.deltaTime;
        _targetOrbitHeight =  Mathf.Clamp(_targetOrbitHeight, minHeight, _maxHeight);
    }

    private void HandleRotation()
    {
        var isHeld = rotateButton?.action != null && rotateButton.action.IsPressed();

        if (isHeld && rotateDelta?.action != null)
        {
            var delta = rotateDelta.action.ReadValue<Vector2>();
            _targetYaw += delta.x * rotateSpeed;
        }
        else
            _targetYaw += autoRotateSpeed * Time.deltaTime;
    }

    private void ApplyTransform()
    {
        _yaw         = Mathf.SmoothDampAngle(_yaw, _targetYaw, ref _yawVelocity, smoothTime);
        _orbitRadius = Mathf.SmoothDamp(_orbitRadius, _targetOrbitRadius, ref _orbitRadiusVelocity, smoothTime);
        _orbitHeight = Mathf.SmoothDamp(_orbitHeight, _targetOrbitHeight, ref _orbitHeightVelocity, smoothTime);

        var zoomT = Mathf.Clamp01((_orbitRadius - minRadius) / (maxRadius - minRadius));
        _pitch = Mathf.Lerp(xRotationBounds.x, xRotationBounds.y, zoomT);

        var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        var pivot    = PivotPosition;

        var flatRotation = Quaternion.Euler(0f, _yaw, 0f);
        transform.position = pivot + flatRotation * new Vector3(0f, 0f, -_orbitRadius) + Vector3.up * _orbitHeight;
        transform.rotation = rotation;
    }

    private void OnDrawGizmos()
    {
        var pivot = PivotPosition;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pivot, 0.4f);

        if (!Application.isPlaying) return;

        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        DrawGizmoCircle(pivot + Vector3.up * _orbitHeight, _orbitRadius, 48);

        Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
        DrawGizmoCircle(pivot + Vector3.up * _orbitHeight, minRadius, 32);
        Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
        DrawGizmoCircle(pivot + Vector3.up * _orbitHeight, maxRadius, 48);

        Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
        Gizmos.DrawLine(pivot, transform.position);
    }

    private static void DrawGizmoCircle(Vector3 _center, float _radius, int _segments)
    {
        var step = 360f / _segments;
        var prev = _center + new Vector3(_radius, 0f, 0f);

        for (var i = 1; i <= _segments; i++)
        {
            var angle = i * step * Mathf.Deg2Rad;
            var next  = _center + new Vector3(Mathf.Cos(angle) * _radius, 0f, Mathf.Sin(angle) * _radius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }

    private void EnableActions()
    {
        scrollAction?.action?.Enable();
        rotateDelta?.action?.Enable();
        rotateButton?.action?.Enable();
        heightUpAction?.action?.Enable();
        heightDownAction?.action?.Enable();
    }

    private void OnEnable() => EnableActions();

    private void OnDisable()
    {
        scrollAction?.action?.Disable();
        rotateDelta?.action?.Disable();
        rotateButton?.action?.Disable();
        heightUpAction?.action?.Disable();
        heightDownAction?.action?.Disable();
    }
}