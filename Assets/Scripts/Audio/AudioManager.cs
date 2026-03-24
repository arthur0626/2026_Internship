using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 효과음 싱글턴 매니저.
/// 씬에 빈 GameObject를 만들어 이 스크립트를 부착하고,
/// Inspector 에서 sounds 배열에 키·클립·볼륨을 등록하면 된다.
///
/// 사용법:  AudioManager.Play("mine");
/// </summary>
public class AudioManager : MonoBehaviour
{
    // ── 싱글턴 ───────────────────────────────────────────────────────────
    public static AudioManager Instance { get; private set; }

    // ── 사운드 등록 ───────────────────────────────────────────────────────
    [System.Serializable]
    public struct SoundEntry
    {
        [Tooltip("코드에서 사용하는 식별 키 (예: mine, handcuff_pickup, upgrade_complete …)")]
        public string key;

        [Tooltip("재생할 AudioClip")]
        public AudioClip clip;

        [Range(0f, 1f)]
        [Tooltip("개별 볼륨 (masterVolume 와 곱해짐)")]
        public float volume;

        [Tooltip("피치 랜덤 범위 (0 이면 고정, 0.1 이면 ±0.1 랜덤)")]
        [Range(0f, 0.5f)]
        public float pitchVariance;
    }

    [Header("효과음 목록")]
    [SerializeField] private SoundEntry[] sounds;

    [Header("전체 볼륨")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;

    [Header("동시 재생 채널 수 (동시에 겹쳐 날 수 있는 최대 소리)")]
    [SerializeField] private int poolSize = 8;

    // ── 내부 ─────────────────────────────────────────────────────────────
    private readonly Dictionary<string, SoundEntry> _map = new Dictionary<string, SoundEntry>();
    private AudioSource[] _pool;
    private int _poolIndex;

    // ─────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // AudioSource 풀 생성
        _pool = new AudioSource[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            _pool[i] = gameObject.AddComponent<AudioSource>();
            _pool[i].playOnAwake = false;
        }

        // 키 → SoundEntry 맵 빌드
        if (sounds != null)
            foreach (var s in sounds)
                if (!string.IsNullOrEmpty(s.key) && s.clip != null)
                    _map[s.key] = s;
    }

    // ── 공개 API ─────────────────────────────────────────────────────────

    /// <summary>등록된 키의 효과음을 한 번 재생한다. 키가 없거나 클립이 없으면 무시.</summary>
    public static void Play(string key)
    {
        if (Instance == null) return;
        Instance.PlayInternal(key);
    }

    // ── 내부 재생 ─────────────────────────────────────────────────────────
    private void PlayInternal(string key)
    {
        if (!_map.TryGetValue(key, out var entry)) return;

        // 라운드-로빈으로 풀에서 소스 선택
        var src   = _pool[_poolIndex % poolSize];
        _poolIndex = (_poolIndex + 1) % poolSize;

        src.pitch  = 1f + Random.Range(-entry.pitchVariance, entry.pitchVariance);
        src.PlayOneShot(entry.clip, entry.volume * masterVolume);
    }
}
