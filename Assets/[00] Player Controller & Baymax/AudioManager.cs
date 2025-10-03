using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple pooled AudioManager without pitch control (clips play at default pitch = 1).
/// Features:
/// - Pooling of AudioSources
/// - Inspector-editable SFX entries (key, clip list, per-sfx volume, spatial settings, max instances)
/// - Per-key MaxInstances throttling
/// - QueuePlay aggregator (collect requests per frame and flush in LateUpdate)
/// - Safe stealing when pool exhausted
/// </summary>

[System.Serializable]
public class SFXEntry
{
    public string Key; // unique identifier used to play this sound
    public List<AudioClip> Clips = new List<AudioClip>(); // allow random variation
    [Range(0f, 1f)] public float Volume = 1f; // per-sfx volume multiplier
    public int MaxInstances = 8; // max concurrent instances of this key
    public bool Spatial = false; // will set spatialBlend if true
    [Range(0f, 1f)] public float SpatialBlend = 1f; // 0 = 2D, 1 = 3D
    public float MinDistance = 1f;
    public float MaxDistance = 30f;
    public AudioRolloffMode Rolloff = AudioRolloffMode.Logarithmic;
    [HideInInspector] public float _lastPlayed = -999f; // optional cooldown use
    public float MinInterval = 0f; // optional min interval between plays
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Pool")]
    public int PoolSize = 32;

    [Header("SFX")]
    public List<SFXEntry> SfxList = new List<SFXEntry>();

    [Header("Global")]
    [Range(0f, 1f)] public float MasterVolume = 1f;

    // internals
    AudioSource[] _sources;
    Queue<int> _freeSources;
    Dictionary<string, SFXEntry> _sfxMap;
    Dictionary<string, int> _activeCounts;
    string[] _sourcePlayingKey;

    // aggregator (QueuePlay)
    Dictionary<string, List<Vector3?>> _frameRequests = new Dictionary<string, List<Vector3?>>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildMap();
        CreatePool();
    }

    void BuildMap()
    {
        _sfxMap = new Dictionary<string, SFXEntry>();
        _activeCounts = new Dictionary<string, int>();

        foreach (var s in SfxList)
        {
            if (string.IsNullOrWhiteSpace(s.Key))
            {
                Debug.LogWarning("AudioManager: SfxList contains entry with empty Key. Skipping.");
                continue;
            }
            if (_sfxMap.ContainsKey(s.Key))
            {
                Debug.LogWarning($"AudioManager: Duplicate SFX Key '{s.Key}'. Only first will be used.");
                continue;
            }
            _sfxMap[s.Key] = s;
            _activeCounts[s.Key] = 0;
        }
    }

    void CreatePool()
    {
        PoolSize = Mathf.Max(1, PoolSize);
        _sources = new AudioSource[PoolSize];
        _freeSources = new Queue<int>(PoolSize);
        _sourcePlayingKey = new string[PoolSize];

        for (int i = 0; i < PoolSize; i++)
        {
            var go = new GameObject($"AudioSource_{i}");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f; // default 2D
            _sources[i] = src;
            _freeSources.Enqueue(i);
            _sourcePlayingKey[i] = null;
        }
    }

    #region Aggregator (QueuePlay)
    /// <summary>
    /// Queue a play request for the current frame. Call QueuePlay for AOE loops;
    /// AudioManager will aggregate and flush in LateUpdate.
    /// </summary>
    public void QueuePlay(string key, Vector3? position = null)
    {
        if (string.IsNullOrEmpty(key)) return;
        if (!_frameRequests.TryGetValue(key, out var list))
        {
            list = new List<Vector3?>();
            _frameRequests[key] = list;
        }
        list.Add(position);
    }

    void LateUpdate()
    {
        if (_frameRequests.Count == 0) return;

        foreach (var kv in _frameRequests)
        {
            var key = kv.Key;
            var requests = kv.Value;
            int count = requests.Count;

            // choose aggregated position: if only one, use it; if many, average positions if available
            Vector3? pos = null;
            if (count == 1) pos = requests[0];
            else
            {
                Vector3 sum = Vector3.zero; int n = 0;
                foreach (var p in requests) if (p.HasValue) { sum += p.Value; n++; }
                if (n > 0) pos = sum / n;
            }

            // compute a gentle volume scale for multiple hits: logarithmic-ish
            float scale = Mathf.Clamp01(Mathf.Log10(count + 1f) * 0.6f + 0.2f);

            // Try to play aggregated result (the PlaySFX call will enforce MaxInstances and culling)
            PlaySFX(key, pos, scale);
        }

        _frameRequests.Clear();
    }
    #endregion

    /// <summary>
    /// Play an SFX by key.
    /// Returns true if played, false if blocked by MaxInstances or key missing.
    /// position = null -> treat as 2D unless entry.Spatial == true
    /// </summary>
    public bool PlaySFX(string key, Vector3? position = null, float volumeMultiplier = 1f, bool ignoreMaxInstances = false)
    {
        if (!Instance) return false;

        if (!_sfxMap.TryGetValue(key, out var entry))
        {
            Debug.LogWarning($"AudioManager: PlaySFX called with unknown key '{key}'");
            return false;
        }

        // optional per-entry min interval cooldown
        if (entry.MinInterval > 0f && Time.time - entry._lastPlayed < entry.MinInterval)
            return false;

        if (!ignoreMaxInstances && _activeCounts.TryGetValue(key, out var cur) && cur >= entry.MaxInstances)
        {
            return false;
        }

        if (entry.Clips == null || entry.Clips.Count == 0)
        {
            Debug.LogWarning($"AudioManager: SFX '{key}' has no clips assigned.");
            return false;
        }

        // pick a random non-null clip
        AudioClip clip = null;
        int tries = 0;
        while (clip == null && tries < entry.Clips.Count)
        {
            clip = entry.Clips[Random.Range(0, entry.Clips.Count)];
            tries++;
        }
        if (clip == null) { Debug.LogWarning($"AudioManager: SFX '{key}' has only null clips."); return false; }

        // distance cull (if spatial and position provided)
        if (position.HasValue && entry.Spatial)
        {
            var listener = Camera.main ? Camera.main.transform : null;
            if (listener != null)
            {
                float maxAudible = entry.MaxDistance > 0 ? entry.MaxDistance : 60f;
                if (Vector3.Distance(position.Value, listener.position) > maxAudible)
                    return false;
            }
        }

        int idx = GetFreeSourceIndex();
        if (idx == -1)
        {
            // steal a source: choose first one (could be improved with priority)
            idx = 0;
            var oldKey = _sourcePlayingKey[idx];
            if (!string.IsNullOrEmpty(oldKey) && _activeCounts.ContainsKey(oldKey))
                _activeCounts[oldKey] = Mathf.Max(0, _activeCounts[oldKey] - 1);
            _sources[idx].Stop();
            _sourcePlayingKey[idx] = null;
        }

        var src = _sources[idx];

        // position / spatial settings
        if (position.HasValue)
        {
            src.transform.position = position.Value;
            src.spatialBlend = entry.Spatial ? entry.SpatialBlend : 0f;
        }
        else
        {
            // keep at manager position for 2D or if entry.Spatial true we'll still set spatialBlend
            src.transform.position = transform.position;
            src.spatialBlend = entry.Spatial ? entry.SpatialBlend : 0f;
        }

        // set 3D rolloff settings if spatial
        if (entry.Spatial)
        {
            src.rolloffMode = entry.Rolloff;
            src.minDistance = Mathf.Max(0.01f, entry.MinDistance);
            src.maxDistance = Mathf.Max(src.minDistance + 0.1f, entry.MaxDistance);
            src.dopplerLevel = 0f;
        }

        // apply clip & volume (no pitch)
        src.clip = clip;
        src.volume = Mathf.Clamp01(entry.Volume * MasterVolume * volumeMultiplier);
        src.Play();

        // bookkeeping
        _sourcePlayingKey[idx] = key;
        _activeCounts[key] = _activeCounts.ContainsKey(key) ? _activeCounts[key] + 1 : 1;
        entry._lastPlayed = Time.time;

        StartCoroutine(ReturnToPoolAfter(idx, clip.length, key));

        return true;
    }

    int GetFreeSourceIndex()
    {
        if (_freeSources == null) return -1;
        if (_freeSources.Count == 0) return -1;
        return _freeSources.Dequeue();
    }

    IEnumerator ReturnToPoolAfter(int index, float seconds, string key)
    {
        yield return new WaitForSeconds(seconds);

        // If the source is done (or stopped), return it to the pool
        if (_sources[index] != null)
        {
            if (!_sources[index].isPlaying)
            {
                if (!string.IsNullOrEmpty(key) && _activeCounts.ContainsKey(key))
                    _activeCounts[key] = Mathf.Max(0, _activeCounts[key] - 1);

                _sourcePlayingKey[index] = null;
                _freeSources.Enqueue(index);
            }
            else
            {
                // poll until finished
                while (_sources[index] != null && _sources[index].isPlaying)
                    yield return null;

                if (!string.IsNullOrEmpty(key) && _activeCounts.ContainsKey(key))
                    _activeCounts[key] = Mathf.Max(0, _activeCounts[key] - 1);

                _sourcePlayingKey[index] = null;
                if (_freeSources != null) _freeSources.Enqueue(index);
            }
        }
    }

    // Convenience wrappers to match common call sites (no pitch params)
    public bool PlaySFX(string key) => PlaySFX(key, null, 1f, false);
    public bool PlaySFX(string key, Vector3 position) => PlaySFX(key, position, 1f, false);
    public bool PlaySFX(string key, float volumeMultiplier) => PlaySFX(key, null, volumeMultiplier, false);
    public bool PlaySFX(string key, Vector3 position, float volumeMultiplier) => PlaySFX(key, position, volumeMultiplier, false);

    // Optional helper: get how many instances are active for a key
    public int GetActiveInstanceCount(string key)
    {
        if (_activeCounts != null && _activeCounts.TryGetValue(key, out var c)) return c;
        return 0;
    }

    // If you edit SfxList in inspector at runtime, call this to rebuild maps
    public void RebuildFromInspector()
    {
        BuildMap();
    }
}
