using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionReference panAction;

    public InputActionReference rotateDelta;
    public InputActionReference rotateButton;

    [Header("Pan")]
    public float panSpeed = 0.3f;

    [Header("Rotation")]
    public Transform rotationPivot;

    public float rotateSpeed     = 0.3f;
    public float autoRotateSpeed = 2f;

    [Header("Smoothing")]
    public float smoothTime = 0.08f;

    private Vector3 _basePos;
    private Vector3 _targetBasePos;
    private Vector3 _basePosVelocity;

    private float _yaw;
    private float _targetYaw;
    private float _yawVelocity;
    private float _pitch;

    private Vector3 YawForward => new(Mathf.Sin(_targetYaw * Mathf.Deg2Rad), 0f, Mathf.Cos(_targetYaw * Mathf.Deg2Rad));
    private Vector3 YawRight => new(Mathf.Cos(_targetYaw * Mathf.Deg2Rad), 0f, -Mathf.Sin(_targetYaw * Mathf.Deg2Rad));

    private void Start()
    {
        _basePos       = transform.position;
        _targetBasePos = transform.position;
        _pitch         = transform.eulerAngles.x;
        _yaw           = transform.eulerAngles.y;
        _targetYaw     = _yaw;

        panAction?.action?.Enable();
        rotateDelta?.action?.Enable();
        rotateButton?.action?.Enable();
    }

    private void Update()
    {
        HandlePan();
        HandleRotation();
        ApplyTransform();
    }

    private void HandlePan()
    {
        if (panAction?.action == null) return;
        var scroll = panAction.action.ReadValue<float>();
        if (scroll == 0f) return;

        _targetBasePos -= YawRight   * (scroll * panSpeed * Time.deltaTime);
        _targetBasePos += YawForward * (scroll * panSpeed * Time.deltaTime);
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
        _basePos = Vector3.SmoothDamp(_basePos, _targetBasePos, ref _basePosVelocity, smoothTime);
        _yaw     = Mathf.SmoothDampAngle(_yaw, _targetYaw, ref _yawVelocity, smoothTime);

        var rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        if (rotationPivot)
        {
            var pivot      = rotationPivot.position;
            var toBase     = _basePos - pivot;
            var flatRadius = new Vector3(toBase.x, 0f, toBase.z).magnitude;
            var height     = toBase.y;

            var orbitPos = pivot + rotation * new Vector3(0f, 0f, -flatRadius) + Vector3.up * height;
            transform.position = orbitPos;
        }
        else
            transform.position = _basePos;

        transform.rotation = rotation;
    }

    private void OnEnable()
    {
        panAction?.action?.Enable();
        rotateDelta?.action?.Enable();
        rotateButton?.action?.Enable();
    }

    private void OnDisable()
    {
        panAction?.action?.Disable();
        rotateDelta?.action?.Disable();
        rotateButton?.action?.Disable();
    }
}