using UnityEngine;

/// <summary>
/// 보스2 검에서 불꽃이 튀는 연출. Boss2 루트에 붙인다.
///   - 공격을 휘두를 때마다 (공격 판정이 켜지는 StartDamage, 검기·회전참의 베는 프레임)
///   - 회피(QTE)에 실패해 공격이 이어질 때
/// 불꽃은 검이 지나가는 자리(공격 박스 AttackZone) 기준 위치에 만들고, 프리팹이 끝나면 스스로 사라진다.
/// </summary>
public class Boss2BladeSpark : MonoBehaviour
{
    [Tooltip("불꽃 파티클 (HitSparkVFX). 한 번 터지고 스스로 사라지는 프리팹")]
    [SerializeField] private GameObject _sparkPrefab;
    [Tooltip("검이 지나가는 자리. 보스가 바라보는 쪽으로 옮겨지는 공격 박스(AttackZone)")]
    [SerializeField] private Transform _bladePoint;
    [Tooltip("검 자리 기준 위치 보정 (오른쪽을 볼 때 기준, 왼쪽을 보면 x 를 뒤집는다)")]
    [SerializeField] private Vector2 _offset = new Vector2(0f, 0.3f);
    [SerializeField] private float _scale = 1f;
    [Tooltip("같은 순간 여러 번 불려도(이벤트 중복 등) 한 번만 튀도록 하는 최소 간격(초, 실시간)")]
    [SerializeField] private float _minInterval = 0.05f;

    [Header("사운드")]
    [Tooltip("불꽃이 튈 때 이 중 하나를 무작위로 낸다 (Sword Blocked 1~3). 같은 소리는 연달아 나오지 않는다")]
    [SerializeField] private AudioClip[] _sparkSounds;
    [Range(0f, 1f)]
    [SerializeField] private float _sparkSoundVolume = 1f;

    private float _lastPlayTime = float.NegativeInfinity;
    private int _lastSoundIndex = -1;

    /// <summary>검 쪽에서 불꽃을 한 번 튀긴다.</summary>
    public void Play()
    {
        if (_sparkPrefab == null || isActiveAndEnabled == false)
            return;
        if (Time.realtimeSinceStartup - _lastPlayTime < _minInterval)
            return;
        _lastPlayTime = Time.realtimeSinceStartup;

        Transform point = _bladePoint != null ? _bladePoint : transform;
        // 공격 박스는 바라보는 쪽으로 옮겨지므로, 보스 기준 x 부호로 방향을 판단한다.
        bool isLeft = point != transform && point.position.x < transform.position.x;
        Vector3 offset = new Vector3(isLeft ? -_offset.x : _offset.x, _offset.y, 0f);

        GameObject spark = Instantiate(_sparkPrefab, point.position + offset, Quaternion.identity);
        spark.transform.localScale = new Vector3(isLeft ? -_scale : _scale, _scale, 1f);

        PlaySparkSound();
    }

    // 직전에 낸 소리를 빼고 무작위로 하나 고른다.
    private void PlaySparkSound()
    {
        if (_sparkSounds == null || _sparkSounds.Length == 0 || SoundManager.Instance == null)
            return;

        int index = Random.Range(0, _sparkSounds.Length);
        if (_sparkSounds.Length > 1 && index == _lastSoundIndex)
        {
            index = (index + Random.Range(1, _sparkSounds.Length)) % _sparkSounds.Length;
        }
        _lastSoundIndex = index;
        SoundManager.Instance.PlayEffectSound(_sparkSounds[index], _sparkSoundVolume);
    }
}
