using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Config/Game Config", order = 0)]
public class GameConfig : ScriptableObject
{
    [Header("Near Cities")]
    [Tooltip("Liste de noms possibles pour les villes voisines.")]
    public string[] cityNames =
    {
        "Ashford",
        "Brindlemark",
        "Thornwall",
        "Dunhollow",
        "Foxmere",
        "Glenhaven",
        "Ironvale",
        "Keldrath",
        "Millreach",
        "Oakhurst",
        "Ravenspire",
        "Stonebridge",
        "Westmoor",
        "Cinderfell",
        "Elmsworth",
    };

    /// <summary>
    /// Pioche <paramref name="_count"/> noms uniques au hasard dans la liste.
    /// Si on demande plus de noms qu'il n'y en a, retourne tous les noms mélangés.
    /// </summary>
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
}

