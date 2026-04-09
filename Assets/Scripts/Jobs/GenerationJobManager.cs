using System;
using System.Collections;
using Core.Patterns;
using Unity.Jobs;

public class GenerationJobManager : MonoSingleton<GenerationJobManager>
{
    /// <summary>Dispatches an IJobParallelFor and yields until complete</summary>
    public static IEnumerator DispatchJob<T>(T         _job,        int                  _arrayLength, int _batchCount,
                                             Action<T> _onComplete, params IDisposable[] _disposables)
        where T : struct, IJobParallelFor
    {
        var handle = _job.Schedule(_arrayLength, _batchCount);

        while (!handle.IsCompleted)
            yield return null;

        handle.Complete();
        _onComplete?.Invoke(_job);

        foreach (var disposable in _disposables)
            disposable?.Dispose();
    }

    /// <summary>Dispatches a single IJob and yields until complete</summary>
    public static IEnumerator DispatchJob<T>(T _job, Action<T> _onComplete, params IDisposable[] _disposables)
        where T : struct, IJob
    {
        var handle = _job.Schedule();

        while (!handle.IsCompleted)
            yield return null;

        handle.Complete();
        _onComplete?.Invoke(_job);

        foreach (var disposable in _disposables)
            disposable?.Dispose();
    }
}