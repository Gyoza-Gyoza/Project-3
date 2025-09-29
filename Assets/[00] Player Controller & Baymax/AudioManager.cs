using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // ===== Singleton =====
    public static AudioManager Instance { get; private set; }

    // Static façade — call from anywhere
    public static void Play(string key, float delay = 0f)
    {
        if (Instance == null) return;
        Instance.PlayByKey(key, delay);
    }
    public static void Play(AudioClip clip, float volume = 1f, float delay = 0f)
    {
        if (Instance == null || clip == null) return;
        Instance.PlayClip(clip, Mathf.Clamp01(volume), delay);
    }
    public static void PlayBGM(AudioClip clip, float volume = 1f)
    {
        if (Instance == null || clip == null) return;
        Instance.PlayBGM_Internal(clip, Mathf.Clamp01(volume));
    }
    public static void StopBGM()
    {
        if (Instance == null) return;
        Instance.StopBGM_Internal();
    }

    // ===== Inspector: SFX Library =====
    [System.Serializable]
    public class Sound
    {
        public string Key;                        // e.g., "Attack1"
        public AudioClip Clip;
        [Range(0f, 1f)] public float Volume = 1f; // per-clip volume
    }

    [Header("SFX Library")]
    [Tooltip("Define your playable clips with per-clip volumes.")]
    public List<Sound> Clips = new List<Sound>();

    [Header("SFX Pool")]
    [Tooltip("How many AudioSources to start with. Pool expands if needed.")]
    [Min(1)] public int InitialSources = 6;
    [Tooltip("Parent newly created pooled sources under this (optional).")]
    public Transform SourcesParent;

    // ===== Inspector: BGM =====
    [Header("BGM")]
    [SerializeField] private AudioSource bgmSource; // optional; auto-created if null

    // ===== Internals =====
    private readonly Dictionary<string, Sound> _library = new Dictionary<string, Sound>();
    private readonly List<AudioSource> _pool = new List<AudioSource>();
    private int _poolCountCreated = 0;

    // ===== Boot =====
    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Build key -> sound
        _library.Clear();
        foreach (var s in Clips)
        {
            if (s != null && !string.IsNullOrEmpty(s.Key) && s.Clip != null)
                _library[s.Key] = s; // last wins if duplicate keys
        }

        // Warm up pool
        int count = Mathf.Max(1, InitialSources);
        for (int i = 0; i < count; i++)
            _pool.Add(CreatePooledSource());

        // BGM source
        if (bgmSource == null)
        {
            var go = new GameObject("BGM_Source");
            go.transform.SetParent(transform, false);
            bgmSource = go.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;       // always loop BGM
            bgmSource.spatialBlend = 0f; // 2D
        }
    }

    private AudioSource CreatePooledSource()
    {
        var go = new GameObject("SFX_Source_" + _poolCountCreated++);
        go.transform.SetParent(SourcesParent ? SourcesParent : transform, false);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 0f; // 2D by default
        return src;
    }

    private AudioSource GetFreeSource()
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            if (!_pool[i].isPlaying) return _pool[i];
        }
        // expand if all busy
        var src = CreatePooledSource();
        _pool.Add(src);
        return src;
    }

    // ===== Instance SFX Methods =====
    private void PlayByKey(string key, float delay)
    {
        if (string.IsNullOrEmpty(key) || !_library.TryGetValue(key, out var sound) || sound.Clip == null)
            return;

        PlayClip(sound.Clip, sound.Volume, delay);
    }

    private void PlayClip(AudioClip clip, float volume, float delay)
    {
        if (delay <= 0f)
        {
            var src = GetFreeSource();
            src.PlayOneShot(clip, volume);
        }
        else
        {
            StartCoroutine(PlayRoutine(clip, volume, delay));
        }
    }

    private IEnumerator PlayRoutine(AudioClip clip, float volume, float delay)
    {
        yield return new WaitForSeconds(delay);
        var src = GetFreeSource();
        src.PlayOneShot(clip, volume);
    }

    // ===== Instance BGM Methods =====
    private void PlayBGM_Internal(AudioClip clip, float volume)
    {
        if (bgmSource.clip == clip && bgmSource.isPlaying)
        {
            // already playing this track; just update volume
            bgmSource.volume = volume;
            return;
        }

        bgmSource.clip = clip;
        bgmSource.volume = volume;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    private void StopBGM_Internal()
    {
        if (bgmSource.isPlaying) bgmSource.Stop();
        bgmSource.clip = null;
    }
}
