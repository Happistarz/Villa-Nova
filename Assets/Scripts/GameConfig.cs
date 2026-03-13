using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Config/Game Config", order = 0)]
public class GameConfig : ScriptableObject
{
    [Header("Biomes")]
    public Color[] biomeColors = new[]
    {
        new Color(0.3f, 0.8f, 0.3f), // Plain
        new Color(0.8f, 0.8f, 0.3f), // Desert
        new Color(0.3f, 0.8f, 0.8f), // Swamp
        new Color(0.5f, 0.5f, 0.5f), // Mountain
        new Color(0.3f, 0.5f, 0.3f), // Taiga
        new Color(0.6f, 0.6f, 0.6f), // Tundra
        new Color(0.2f, 0.7f, 0.2f), // Jungle
        new Color(0.8f, 0.7f, 0.3f), // Savanna
        new Color(0.9f, 0.9f, 0.9f)  // Snow
    };
    
    [Header("Near Cities")]
    public string[] cityNames;

    public List<string> GetRandomCityNames(int _count)
    {
        var pool = new List<string>(cityNames);
        var count = Mathf.Min(_count, pool.Count);
        var result = new List<string>(count);

        for (var i = 0; i < count; i++)
        {
            var index = Random.Range(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return result;
    }
    
    public Color GetRandomBiomeColor()
    {
        return biomeColors[Random.Range(0, biomeColors.Length)];
    }
}

