using System;
using System.Collections;
using System.Collections.Generic;
using Core.Patterns;
using UnityEngine;

public class AudioManager : MonoSingleton<AudioManager>
{
    [Serializable]
    public struct SfxEntry
    {
        public string    key;
        public AudioClip clip;
        [Range(0f, 1f)]
        public float     volume;
    }

    [Header("Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("BGM")]
    public AudioClip bgmClip;
    [Range(0f, 1f)]
    public float bgmVolume = 1f;

    [Header("SFX Library")]
    public List<SfxEntry> sfxLibrary = new();

    private Dictionary<string, SfxEntry> _sfxLookup;

    protected override void Awake()
    {
        base.Awake();
        RebuildLookup();
    }

    private void RebuildLookup()
    {
        _sfxLookup = new Dictionary<string, SfxEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in sfxLibrary)
        {
            if (string.IsNullOrWhiteSpace(entry.key)) continue;
            _sfxLookup[entry.key] = entry;
        }
    }

    public void PlayButtonClick() => PlaySFX("button_click");

    public void PlaySFX(string _key)
    {
        if (_sfxLookup.Count <= 0) RebuildLookup();

        if (!_sfxLookup.TryGetValue(_key, out var entry))
        {
            Debug.LogWarning($"SFX key '{_key}' not found in library.", this);
            return;
        }

        if (!entry.clip)
        {
            Debug.LogWarning($"SFX key '{_key}' has no clip assigned.", this);
            return;
        }

        sfxSource.PlayOneShot(entry.clip, entry.volume);
    }

    public void PlayBGM()
    {
        if (!bgmClip) return;

        bgmSource.clip   = bgmClip;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    public void StopBGM()  => bgmSource.Stop();
    public void StopSFX()  => sfxSource.Stop();

    public IEnumerator PlayTerrainGenerationSound()
    {
        yield return new WaitForSeconds(1f);
    }

}