using System;
using System.Collections;

public interface IGenerator
{
    string       Name         { get; }
    bool         IsGenerating { get; }
    event Action OnGenerationComplete;

    IEnumerator Generate(WorldGrid _grid);
}