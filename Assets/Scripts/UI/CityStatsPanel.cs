using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CityStatsPanel : MonoBehaviour
{
    [Header("Stats")]
    public Text cityName;

    public Text populationText;
    public Text buildingsText;
    public Text poisText;
    public Text roadsText;
    public Text cityAreaText;

    [Header("Content")]
    public Image backgroundImage;

    public RectTransform buttonCaret;
    public GameObject    statsContent;

    private bool  _isExpanded = true;
    private float backgroundAlpha;

    private void Start()
    {
        GenerationPipeline.Instance.OnPipelineComplete += RefreshStats;

        backgroundAlpha = backgroundImage.color.a;
    }

    private void OnDestroy()
    {
        if (GenerationPipeline.HasInstance)
            GenerationPipeline.Instance.OnPipelineComplete -= RefreshStats;
    }

    private void RefreshStats()
    {
        var grid     = WorldGrid.Instance;
        var houseGen = CityGenerator.Instance?.houseGenerator;
        var cityGen  = CityGenerator.Instance;

        SetText(cityName, cityGen?.CityName ?? "Unnamed City");

        SetText(populationText, houseGen?.TotalPopulation         ?? 0);
        SetText(buildingsText,  houseGen?.PlacedCount             ?? 0);
        SetText(poisText,       cityGen?.PlacedPOIPositions.Count ?? 0);
        SetText(
            roadsText,
            grid?.CountCellsOfType(WorldGrid.CellType.ROAD, WorldGrid.CellType.BRIDGE) ?? 0
        );
        SetText(
            cityAreaText,
            grid?.CountCellsOfType(WorldGrid.CellType.CITY, WorldGrid.CellType.HOUSE) ?? 0
        );
    }

    private static void SetText<T>(Text _t, T _value)
    {
        if (_t && _value is int @int)
            _t.text = @int.ToString("N0");
        else
            _t.text = _value.ToString();
    }

    public void Toggle() => SetExpanded(!_isExpanded);

    private void SetExpanded(bool _expanded)
    {
        _isExpanded = _expanded;

        backgroundImage.DOFade(_isExpanded ? backgroundAlpha : 0f, 0.5f);
        statsContent.SetActive(_isExpanded);
        buttonCaret.DOLocalRotate(_isExpanded ? Vector3.zero : (Vector3.forward * 90), 0.25f);
    }
}