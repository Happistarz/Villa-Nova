using UnityEngine;
using UnityEngine.UI;

public class NearbyCityDisplay : MonoBehaviour
{
    public Text cityName;
    public Text cityDistance;
    
    public void SetCityInfo(string name, float distance)
    {
        cityName.text = name;
        cityDistance.text = $"{distance:F1} km";
    }
}