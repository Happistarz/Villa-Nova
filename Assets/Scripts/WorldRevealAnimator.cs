using UnityEngine;

public class WorldRevealAnimator : MonoBehaviour
{
    [Header("References")]
    public TerrainRenderer terrainRenderer;

    [Header("Animation Settings")]
    [Range(5f, 60f)]   public float revealSpeed    = 20f;
    [Range(1f, 10f)]   public float revealWidth    = 4f;
    [Range(-20f, -1f)] public float dropHeight     = -8f;
    [Range(0f, 1f)]    public float bounceStrength = 0.3f;
    [Range(0f, 1f)]    public float colorDarkness  = 0.15f;
    [Range(0f, 1f)]    public float startDelay     = 0.05f;

    [Header("Easing")]
    public AnimationCurve easingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public event System.Action OnRevealComplete;

    private MaterialPropertyBlock _propBlock;
    private float _currentRadius;
    private float _maxRadius;
    private float _elapsedTime;
    private float _totalDuration;
    private bool  _isRevealing;
    private Vector4 _center;

    private static readonly int _REVEAL_CENTER_ID   = Shader.PropertyToID("_RevealCenter");
    private static readonly int _REVEAL_RADIUS_ID   = Shader.PropertyToID("_RevealRadius");
    private static readonly int _REVEAL_WIDTH_ID    = Shader.PropertyToID("_RevealWidth");
    private static readonly int _DROP_HEIGHT_ID     = Shader.PropertyToID("_DropHeight");
    private static readonly int _BOUNCE_STRENGTH_ID = Shader.PropertyToID("_BounceStrength");
    private static readonly int _COLOR_DARKNESS_ID  = Shader.PropertyToID("_ColorDarkness");

    private void Start()
    {
        _propBlock = new MaterialPropertyBlock();
        WorldGrid.Instance.OnMapGenerated += OnMapGenerated;

        SetRevealRadius(-10f);
    }

    private void OnDestroy()
    {
        if (WorldGrid.Instance)
            WorldGrid.Instance.OnMapGenerated -= OnMapGenerated;
    }

    private void OnMapGenerated()
    {
        StartReveal();
    }

    public void StartReveal()
    {
        if (!terrainRenderer)
        {
            OnRevealComplete?.Invoke();
            return;
        }

        var size = WorldGrid.Instance.size;

        _center = new Vector4(size / 2f, 0f, size / 2f, 0f);
        _maxRadius = size * 0.71f + revealWidth;

        _propBlock.SetVector(_REVEAL_CENTER_ID, _center);
        _propBlock.SetFloat(_REVEAL_WIDTH_ID, revealWidth);
        _propBlock.SetFloat(_DROP_HEIGHT_ID, dropHeight);
        _propBlock.SetFloat(_BOUNCE_STRENGTH_ID, bounceStrength);
        _propBlock.SetFloat(_COLOR_DARKNESS_ID, colorDarkness);

        if (!terrainRenderer.enabledRender)
        {
            SetRevealRadius(_maxRadius + 100f);

            OnRevealComplete?.Invoke();
            return;
        }

        _totalDuration = _maxRadius / revealSpeed;
        _elapsedTime   = -startDelay;
        _currentRadius = -revealWidth;
        _isRevealing   = true;

        SetRevealRadius(-revealWidth);
    }

    private void Update()
    {
        if (!_isRevealing) return;

        _elapsedTime += Time.deltaTime;

        if (_elapsedTime < 0f)
        {
            SetRevealRadius(-revealWidth);
            return;
        }

        var normalizedTime = Mathf.Clamp01(_elapsedTime / _totalDuration);

        var easedTime = easingCurve.Evaluate(normalizedTime);

        _currentRadius = Mathf.Lerp(-revealWidth, _maxRadius, easedTime);
        SetRevealRadius(_currentRadius);

        if (normalizedTime >= 1f)
        {
            _isRevealing = false;

            SetRevealRadius(_maxRadius + 100f);

            OnRevealComplete?.Invoke();
        }
    }

    private void SetRevealRadius(float _radius)
    {
        if (!terrainRenderer || !terrainRenderer.meshRenderer) return;

        _propBlock.SetFloat(_REVEAL_RADIUS_ID, _radius);
        terrainRenderer.meshRenderer.SetPropertyBlock(_propBlock);
    }
}




