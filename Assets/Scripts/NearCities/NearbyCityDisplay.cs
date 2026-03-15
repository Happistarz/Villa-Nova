using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class NearbyCityDisplay : MonoBehaviour
{
    public CanvasGroup content;
    public Text        cityName;
    public Text        cityDistance;
    public float       startHeightOffset = -20f;

    private Vector3 _finalPosition;
    private float   _fadeDelay = 0.2f;

    public void DisplayInfos(string _name, float _distance, Vector3 _position, int _cityIndex)
    {
        cityName.text     = _name;
        cityDistance.text = $"{_distance:F1} km";

        var startPosition = new Vector3(_position.x, _position.y + startHeightOffset, _position.z);
        transform.position = startPosition;
        _finalPosition     = _position;

        transform.DOMove(_finalPosition, 1f).SetEase(Ease.OutCubic);

        _fadeDelay    = _cityIndex * 0.2f;
        content.alpha = 0f;
        content.DOFade(1f, 1f).SetEase(Ease.OutCubic).SetDelay(_fadeDelay);
    }

    public void Hide(PrefabPool<NearbyCityDisplay> _pool)
    {
        content.DOFade(0f, 0.5f).SetEase(Ease.OutCubic).SetDelay(_fadeDelay)
               .OnComplete(() => _pool.Release(this));
    }
}