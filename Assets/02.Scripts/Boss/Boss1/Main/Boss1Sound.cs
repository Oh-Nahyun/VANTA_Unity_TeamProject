using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Boss1SoundType
{
    FootStep,
    Claw,
    Spit,
    ShotAimed,
    ShotSplit,
    LaserCharge,
    LaserChargeP2,
    LaserFire,
    Land,
    JumpTakeoff,
    Roar,
    Buff,
    DeathCollapse,
    DeathExplosion,
    DeathSplat,
    Hp0
}

// 보스1 전용 효과음 재생기.
// 팀 SoundManager와 별도로 자체 AudioSource를 사용해서 서로 간섭하지 않음.
// 설정 UI의 마스터/효과음 볼륨은 SoundManager가 저장하는 PlayerPrefs 값을 읽어서 반영함.
public class Boss1Sound : MonoBehaviour
{
    [Serializable]
    private class SoundEntry
    {
        public Boss1SoundType Type;
        public AudioClip Clip;

        [Range(0f, 1f)]
        public float Volume = 1f;

        [Tooltip("재생할 때마다 음높이를 랜덤. 필요하면 0으로")]
        [Range(0f, 0.3f)]
        public float PitchRandom;
    }

    [Header("효과음 목록")]
    [SerializeField] private List<SoundEntry> _sounds = new List<SoundEntry>();

    [Header("동시에 재생가능한 소리")]
    [SerializeField] private int _voiceCount = 8;

    [Header("Stop 할 때 페이드 시간")]
    [SerializeField] private float _stopFadeDuration = 0.08f;

    private const string MasterVolumeKey = "MasterVolume";
    private const string EffectVolumeKey = "EffectVolume";

    private readonly Dictionary<Boss1SoundType, SoundEntry> _soundTable = new Dictionary<Boss1SoundType, SoundEntry>();

    private AudioSource[] _voices;
    private Boss1SoundType[] _voiceTypes;
    private float[] _voiceStartTimes;
    private Coroutine[] _fadeCoroutines;

    private void Awake()
    {
        foreach (SoundEntry entry in _sounds)
        {
            _soundTable[entry.Type] = entry;
        }

        GameObject voiceRoot = new GameObject("Boss1SoundVoices");
        voiceRoot.transform.SetParent(transform, false);

        _voices = new AudioSource[_voiceCount];
        _voiceTypes = new Boss1SoundType[_voiceCount];
        _voiceStartTimes = new float[_voiceCount];
        _fadeCoroutines = new Coroutine[_voiceCount];

        for (int i = 0; i < _voiceCount; i++)
        {
            AudioSource source = voiceRoot.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f; // 2D
            _voices[i] = source;
        }
    }

    public void Play(Boss1SoundType type)
    {
        if (!_soundTable.TryGetValue(type, out SoundEntry entry) || entry.Clip == null)
        {
            return;
        }

        int index = GetFreeVoiceIndex();
        AudioSource source = _voices[index];

        StopFade(index);

        source.clip = entry.Clip;
        source.volume = entry.Volume * GetGlobalVolume();
        source.pitch = 1f + UnityEngine.Random.Range(-entry.PitchRandom, entry.PitchRandom);
        source.Play();

        _voiceTypes[index] = type;
        _voiceStartTimes[index] = Time.unscaledTime;
    }

    // 재생 중인 해당 소리를 짧게 페이드하면서 멈춤
    public void Stop(Boss1SoundType type)
    {
        if (_voices == null)
        {
            return;
        }

        for (int i = 0; i < _voices.Length; i++)
        {
            if (_voices[i] == null || !_voices[i].isPlaying || _voiceTypes[i] != type)
            {
                continue;
            }

            StopFade(i);

            // 비활성화(사망 후 Destroy 등) 중에는 코루틴을 못 돌리니 바로 멈춤
            if (!isActiveAndEnabled)
            {
                _voices[i].Stop();
                continue;
            }

            _fadeCoroutines[i] = StartCoroutine(FadeOutRoutine(i));
        }
    }

    private int GetFreeVoiceIndex()
    {
        int oldestIndex = 0;

        for (int i = 0; i < _voices.Length; i++)
        {
            if (!_voices[i].isPlaying)
            {
                return i;
            }

            if (_voiceStartTimes[i] < _voiceStartTimes[oldestIndex])
            {
                oldestIndex = i;
            }
        }

        // 전부 재생 중이면 가장 오래된 소리를 끊고 재사용
        return oldestIndex;
    }

    private float GetGlobalVolume()
    {
        return PlayerPrefs.GetFloat(MasterVolumeKey, 1f) * PlayerPrefs.GetFloat(EffectVolumeKey, 1f);
    }

    private IEnumerator FadeOutRoutine(int index)
    {
        AudioSource source = _voices[index];
        float startVolume = source.volume;
        float time = 0f;

        while (time < _stopFadeDuration)
        {
            time += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, time / _stopFadeDuration);
            yield return null;
        }

        source.Stop();
        _fadeCoroutines[index] = null;
    }

    private void StopFade(int index)
    {
        if (_fadeCoroutines[index] != null)
        {
            StopCoroutine(_fadeCoroutines[index]);
            _fadeCoroutines[index] = null;
        }
    }

    // 컴포넌트를 처음 추가하거나 Reset 메뉴를 누르면 목록을 자동으로 채움
    private void Reset()
    {
        _sounds.Clear();

        foreach (Boss1SoundType type in Enum.GetValues(typeof(Boss1SoundType)))
        {
            SoundEntry entry = new SoundEntry { Type = type };

            // 자주 반복되는 소리만 살짝 흔들어서 덜 단조롭게
            if (type == Boss1SoundType.FootStep)
            {
                entry.PitchRandom = 0.08f;
            }
            else if (type == Boss1SoundType.ShotAimed || type == Boss1SoundType.ShotSplit || type == Boss1SoundType.Claw)
            {
                entry.PitchRandom = 0.05f;
            }

            _sounds.Add(entry);
        }
    }
}
