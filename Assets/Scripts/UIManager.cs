using Core.Variables;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    public Image terrainRenderImage;
    public Image debugRenderImage;
    public Image zoomImage;
    public Button generateButton;
    public Text generateButtonText;
    
    [Header("Variables")]
    public BoolVariable terrainEnabled;
    public BoolVariable debugEnabled;
    public FloatVariable zoomLevel;
    
    [Header("Settings")]
    public float minZoomLevel;
    public float maxZoomLevel = 100f;
    public Color renderNormalColor = Color.white;
    public Color renderDisabledColor = new(0.5f, 0.5f, 0.5f, 0.5f);

    [Header("Generate Button")]
    public string generateDefaultText = "GENERATE";
    public string generatingBaseText  = "GENERATING";
    public float  ellipsisSpeed       = 0.4f;

    private float _ellipsisTimer;
    private int   _ellipsisDots;
    private string _originalButtonText;
    
    private void Start()
    {
        OnTerrainRenderToggle();
        OnDebugRenderToggle();
        OnZoomLevelChanged();

        if (generateButtonText)
            _originalButtonText = generateButtonText.text;
        
        WorldGrid.Instance.OnGenerationFullyComplete += OnGenerationComplete;
    }

    private void OnDestroy()
    {
        if (WorldGrid.HasInstance)
            WorldGrid.Instance.OnGenerationFullyComplete -= OnGenerationComplete;
    }
    
    public void OnTerrainRenderToggle()
    {
        SetIconAlpha(terrainRenderImage, terrainEnabled.Value);
    }
    
    public void OnDebugRenderToggle()
    {
        SetIconAlpha(debugRenderImage, debugEnabled.Value);
    }

    private void SetIconAlpha(Image _image, bool _enabled)
    {
        if (!_image) return;

        _image.color = _enabled ? renderNormalColor : renderDisabledColor;
    }

    private float _lastZoomValue = -1f;

    private void OnZoomLevelChanged()
    {
        if (Mathf.Approximately(_lastZoomValue, zoomLevel.value)) return;

        _lastZoomValue = zoomLevel.value;

        var remappedZoom = Mathf.Lerp(minZoomLevel, maxZoomLevel, zoomLevel.value);
        
        var rectTransform = zoomImage.rectTransform;
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, remappedZoom);
    }

    private void Update()
    {
        OnZoomLevelChanged();
        UpdateGeneratingEllipsis();
    }
    
    public void OnGenerateMapButtonPressed()
    {
        if (WorldGrid.IsGenerating) return;
        
        if (generateButton) generateButton.interactable = false;
        
        _ellipsisTimer = 0f;
        _ellipsisDots  = 0;
        
        WorldGrid.Instance.GenerateMap();
    }

    private void OnGenerationComplete()
    {
        if (generateButton) generateButton.interactable = true;
        
        if (generateButtonText)
            generateButtonText.text = _originalButtonText ?? generateDefaultText;
    }

    private void UpdateGeneratingEllipsis()
    {
        if (!WorldGrid.IsGenerating || !generateButtonText) return;

        _ellipsisTimer += Time.deltaTime;

        if (_ellipsisTimer < ellipsisSpeed) return;

        _ellipsisTimer = 0f;
        _ellipsisDots  = (_ellipsisDots + 1) % 4;

        generateButtonText.text = generatingBaseText + new string('.', _ellipsisDots);
    }
}