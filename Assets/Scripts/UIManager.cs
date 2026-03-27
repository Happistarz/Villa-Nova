using Core.Variables;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    public Image terrainRenderImage;

    public Image  debugRenderImage;
    public Image  zoomImage;
    public Button generateButton;
    public Text   generateButtonText;

    [Header("Renderer Toggles")]
    public AbstractRenderer terrainRenderer;

    public AbstractRenderer debugRenderer;

    [Header("Camera Mode")]
    public Button cameraMainButton;

    public Button           cameraCloseButton;
    public Button           cameraFreeButton;
    public CameraController cameraController;

    [Header("Variables")]
    public BoolVariable terrainEnabled;

    public BoolVariable  debugEnabled;
    public FloatVariable zoomLevel;

    [Header("Settings")]
    public float minZoomLevel;

    public float maxZoomLevel        = 100f;
    public Color renderNormalColor   = Color.white;
    public Color renderDisabledColor = new(0.5f, 0.5f, 0.5f, 0.5f);
    public Color cameraActiveColor   = Color.white;
    public Color cameraInactiveColor = new(0.5f, 0.5f, 0.5f, 0.5f);

    [Header("Generate Button")]
    public string generateDefaultText = "GENERATE";

    public string generatingBaseText = "GENERATING";
    public float  ellipsisSpeed      = 0.4f;

    public InputActionReference generateMapAction;

    private float  _ellipsisTimer;
    private int    _ellipsisDots;
    private string _originalButtonText;

    private void Start()
    {
        RefreshRendererIcon(terrainRenderImage, terrainEnabled.Value);
        RefreshRendererIcon(debugRenderImage,   debugEnabled.Value);
        OnZoomLevelChanged();

        if (generateButtonText)
            _originalButtonText = generateButtonText.text;

        generateMapAction.action.Enable();
        generateMapAction.action.performed += OnGeneratePerformed;

        MapGenerator.Instance.OnGenerationComplete     += OnGenerationComplete;
        GenerationPipeline.Instance.OnPipelineComplete += OnPipelineComplete;

        if (cameraMainButton)
            cameraMainButton.onClick.AddListener(() => SetCameraMode(CameraController.CameraStateType.MAIN));
        if (cameraCloseButton)
            cameraCloseButton.onClick.AddListener(() => SetCameraMode(CameraController.CameraStateType.CLOSE));
        if (cameraFreeButton)
            cameraFreeButton.onClick.AddListener(() => SetCameraMode(CameraController.CameraStateType.FREE));

        if (terrainEnabled) terrainEnabled.OnChanged += OnTerrainEnabledChanged;
        if (debugEnabled) debugEnabled.OnChanged     += OnDebugEnabledChanged;

        if (!cameraController) return;

        cameraController.OnModeChanged += RefreshCameraButtons;
        RefreshCameraButtons(cameraController.ActiveMode);
    }

    private void OnGeneratePerformed(InputAction.CallbackContext _)
    {
        generateButton.onClick.Invoke();
    }

    private void OnDestroy()
    {
        generateMapAction.action.performed -= OnGeneratePerformed;

        if (MapGenerator.HasInstance)
            MapGenerator.Instance.OnGenerationComplete -= OnGenerationComplete;

        if (GenerationPipeline.HasInstance)
            GenerationPipeline.Instance.OnPipelineComplete -= OnPipelineComplete;

        if (terrainEnabled) terrainEnabled.OnChanged -= OnTerrainEnabledChanged;
        if (debugEnabled) debugEnabled.OnChanged     -= OnDebugEnabledChanged;

        if (cameraController) cameraController.OnModeChanged -= RefreshCameraButtons;
    }

    private void OnTerrainEnabledChanged(bool _enabled) => RefreshRendererIcon(terrainRenderImage, _enabled);
    private void OnDebugEnabledChanged(bool   _enabled) => RefreshRendererIcon(debugRenderImage,   _enabled);

    #region Renderer

    public void OnTerrainToggleClicked()
    {
        if (terrainRenderer) terrainRenderer.ToggleVisibility();
    }

    public void OnDebugToggleClicked()
    {
        if (debugRenderer) debugRenderer.ToggleVisibility();
    }

    public void OnTerrainRenderToggle()
    {
        RefreshRendererIcon(terrainRenderImage, terrainEnabled.Value);
    }

    public void OnDebugRenderToggle()
    {
        RefreshRendererIcon(debugRenderImage, debugEnabled.Value);
    }

    private void RefreshRendererIcon(Image _image, bool _enabled)
    {
        if (!_image) return;
        _image.color = _enabled ? renderNormalColor : renderDisabledColor;
    }

    #endregion

    #region Camera Mode

    public void SetCameraMode(CameraController.CameraStateType _mode)
    {
        if (!cameraController) return;
        cameraController.SetMode(_mode);
    }

    private void RefreshCameraButtons(CameraController.CameraStateType _active)
    {
        SetButtonColor(cameraMainButton,  _active == CameraController.CameraStateType.MAIN);
        SetButtonColor(cameraCloseButton, _active == CameraController.CameraStateType.CLOSE);
        SetButtonColor(cameraFreeButton,  _active == CameraController.CameraStateType.FREE);
    }

    private void SetButtonColor(Button _button, bool _active)
    {
        if (!_button) return;

        var colors = _button.colors;
        colors.normalColor = _active ? cameraActiveColor : cameraInactiveColor;
        _button.colors     = colors;
    }

    #endregion

    #region Zoom

    private float _lastZoomValue = -1f;

    private void OnZoomLevelChanged()
    {
        if (Mathf.Approximately(_lastZoomValue, zoomLevel.value)) return;

        _lastZoomValue = zoomLevel.value;

        var remappedZoom = Mathf.Lerp(minZoomLevel, maxZoomLevel, zoomLevel.value);

        var rectTransform = zoomImage.rectTransform;
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, remappedZoom);
    }

    #endregion

    #region Generation

    private void Update()
    {
        OnZoomLevelChanged();
        UpdateGeneratingEllipsis();
    }

    public void OnGenerateMapButtonPressed()
    {
        if (GenerationPipeline.Instance.IsAnyGenerating) return;

        if (generateButton) generateButton.interactable = false;

        _ellipsisTimer = 0f;
        _ellipsisDots  = 0;

        GenerationPipeline.Instance.StartGeneration();
    }

    private void OnGenerationComplete()
    {
        if (generateButtonText)
            generateButtonText.text = generatingBaseText + new string('.', _ellipsisDots);
    }

    private void OnPipelineComplete()
    {
        if (generateButton) generateButton.interactable = true;

        if (generateButtonText)
            generateButtonText.text = _originalButtonText ?? generateDefaultText;
    }

    private void UpdateGeneratingEllipsis()
    {
        if (!GenerationPipeline.Instance.IsAnyGenerating || !generateButtonText) return;

        _ellipsisTimer += Time.deltaTime;

        if (_ellipsisTimer < ellipsisSpeed) return;

        _ellipsisTimer = 0f;
        _ellipsisDots  = (_ellipsisDots + 1) % 4;

        generateButtonText.text = generatingBaseText + new string('.', _ellipsisDots);
    }

    #endregion
}