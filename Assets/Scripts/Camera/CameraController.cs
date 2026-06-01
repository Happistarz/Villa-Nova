using System;
using Core;
using Core.Variables;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public enum CameraStateType
    {
        MAIN,
        CLOSE,
        FREE
    }

    [Header("Input Actions")]
    public InputActionReference scrollAction;

    public InputActionReference rotateDelta;
    public InputActionReference rotateButton;
    public InputActionReference moveAction;
    public InputActionReference verticalMoveAction;

    [Header("Main Mode")]
    public CameraModeConfig mainConfig = CameraModeConfig.DefaultMain;

    [Header("Close Mode")]
    public CameraModeConfig closeConfig = CameraModeConfig.DefaultClose;

    [Header("Free Mode")]
    public float freeMoveSpeed        = 30f;
    public float freeMoveSpeedMin     = 5f;
    public float freeMoveSpeedMax     = 200f;
    public float freeMoveSpeedScroll  = 5f;

    public float   freeLookSensitivity = 0.3f;
    public Vector3 freeBounds          = Vector3.one * 400f;

    [Header("Start Mode")]
    public float duration = 2f;

    public float startPitch = 45f;

    [Header("Shared")]
    public Transform mapCenterMarker;

    public Transform cityCenterMarker;

    public float         smoothTime         = 0.08f;
    public float         transitionDuration = 0.5f;
    public FloatVariable zoomLevel;

    public new Camera camera;

    public Vector3          MapCenter     => mapCenterMarker ? mapCenterMarker.position : Vector3.zero;
    public Vector3          CityCenter    => cityCenterMarker ? cityCenterMarker.position : Vector3.zero;
    public CameraStateType? RequestedMode { get; set; }
    public CameraStateType  ActiveMode    { get; private set; } = CameraStateType.MAIN;

    public float CurrentFreeMoveSpeed { get; private set; }
    public float FreeMoveSpeedNormalized =>
        Mathf.InverseLerp(freeMoveSpeedMin, freeMoveSpeedMax, CurrentFreeMoveSpeed);

    public void SetFreeMoveSpeed(float _speed)
    {
        CurrentFreeMoveSpeed = Mathf.Clamp(_speed, freeMoveSpeedMin, freeMoveSpeedMax);
    }

    public event Action<CameraStateType> OnModeChanged;

    private FiniteStateMachine<CameraController> _fsm;

    private CameraMainState  _mainState;
    private CameraCloseState _closeState;
    private CameraFreeState  _freeState;
    private CameraStartState _startState;
    private bool             _inputEnabled;

    private void Start()
    {
        _startState = new CameraStartState(this);
        _mainState  = new CameraMainState(this);
        _closeState = new CameraCloseState(this);
        _freeState  = new CameraFreeState(this);

        _startState.Transitions.Add(
            new Transition<CameraController>(_mainState, _ctx =>
            {
                if (!_ctx._startState.IsDone) return 0f;
                _ctx._mainState.SkipNextTransition();
                return 1f;
            }));

        _mainState.Transitions.Add(new Transition<CameraController>(
                                       _closeState, _ctx => _ctx.RequestedMode == CameraStateType.CLOSE ? 1f : 0f));
        _mainState.Transitions.Add(new Transition<CameraController>(
                                       _freeState, _ctx => _ctx.RequestedMode == CameraStateType.FREE ? 1f : 0f));

        _closeState.Transitions.Add(new Transition<CameraController>(
                                        _mainState, _ctx => _ctx.RequestedMode == CameraStateType.MAIN ? 1f : 0f));
        _closeState.Transitions.Add(new Transition<CameraController>(
                                        _freeState, _ctx => _ctx.RequestedMode == CameraStateType.FREE ? 1f : 0f));

        _freeState.Transitions.Add(new Transition<CameraController>(
                                       _mainState, _ctx => _ctx.RequestedMode == CameraStateType.MAIN ? 1f : 0f));
        _freeState.Transitions.Add(new Transition<CameraController>(
                                       _closeState, _ctx => _ctx.RequestedMode == CameraStateType.CLOSE ? 1f : 0f));

        _fsm = new FiniteStateMachine<CameraController>(_startState);
        _fsm.Start();
    }

    private void Update()
    {
        HandleModeInput();

        var previousMode = ActiveMode;
        _fsm.Update();
        UpdateActiveMode();
        RequestedMode = null;

        if (ActiveMode != previousMode)
            OnModeChanged?.Invoke(ActiveMode);
    }

    public void SetMode(CameraStateType _mode)
    {
        if (_mode == ActiveMode) return;
        RequestedMode = _mode;
    }

    private void UpdateActiveMode()
    {
        var current = _fsm.CurrentState;
        if (current      == _mainState) ActiveMode  = CameraStateType.MAIN;
        else if (current == _closeState) ActiveMode = CameraStateType.CLOSE;
        else if (current == _freeState) ActiveMode  = CameraStateType.FREE;
    }

    private void HandleModeInput()
    {
        if (!_inputEnabled) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            RequestedMode = CameraStateType.MAIN;
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
            RequestedMode = CameraStateType.CLOSE;
        else if (Keyboard.current.digit3Key.wasPressedThisFrame)
            RequestedMode = CameraStateType.FREE;
    }

    public void StartAnimation()
    {
        if (_fsm.CurrentState == _startState)
            _startState?.StartAnimation();
    }

    #region Input Helpers

    public float ReadScroll()
    {
        return scrollAction?.action?.ReadValue<float>() ?? 0f;
    }

    public Vector2 ReadRotateDelta()
    {
        return rotateDelta?.action?.ReadValue<Vector2>() ?? Vector2.zero;
    }

    public bool IsRotateHeld()
    {
        return rotateButton?.action != null && rotateButton.action.IsPressed();
    }

    public Vector2 ReadMove()
    {
        return moveAction?.action?.ReadValue<Vector2>() ?? Vector2.zero;
    }

    public bool FreeLookJustPressed()
    {
        return rotateButton?.action != null && rotateButton.action.WasPressedThisFrame();
    }

    public bool FreeLookJustReleased()
    {
        return rotateButton?.action != null && rotateButton.action.WasReleasedThisFrame();
    }
    
    public float ReadVerticalMove()
    {
        return verticalMoveAction?.action?.ReadValue<float>() ?? 0f;
    }

    #endregion

    #region Lifecycle

    private void EnableActions()
    {
        scrollAction?.action?.Enable();
        rotateDelta?.action?.Enable();
        rotateButton?.action?.Enable();
        moveAction?.action?.Enable();
    }

    public void EnableAllActions()
    {
        _inputEnabled = true;
        EnableActions();
    }

    public void OnEnable() => EnableActions();

    private void OnDisable()
    {
        scrollAction?.action?.Disable();
        rotateDelta?.action?.Disable();
        rotateButton?.action?.Disable();
        moveAction?.action?.Disable();
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmos()
    {
        var pivot = MapCenter;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pivot, 0.4f);

        if (!Application.isPlaying) return;

        Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
        Gizmos.DrawLine(pivot, transform.position);

        if (!(freeBounds.sqrMagnitude > 0f)) return;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.15f);
        Gizmos.DrawWireCube(MapCenter, freeBounds);
    }

    #endregion
}