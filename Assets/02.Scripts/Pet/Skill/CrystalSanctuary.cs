using System;
using System.Collections;
using UnityEngine;

public class CrystalSanctuary : MonoBehaviour
{
    [Header("펫 크리스탈 생츄어리 지속 시간")]
    [SerializeField] private float _skillDuration = 7f;

    [Header("펫 크리스탈 생츄어리 파괴 시 반지름 감소율")]
    [SerializeField] private float _radiusDecreaseRatio = 0.01f;

    /// <summary>
    /// 플레이어 클래스
    /// </summary>
    private Player _player;

    /// <summary>
    /// 펫 크리스탈 생츄어리 애니메이터
    /// </summary>
    private Animator _animator;

    /// <summary>
    /// 펫 크리스탈 생츄어리 콜라이더 2D
    /// </summary>
    private CapsuleCollider2D _capsuleCollider2D;

    /// <summary>
    /// 펫 크리스탈 생츄어리 파괴 전 필요한 대기 시간
    /// </summary>
    private float _timeBeforeDestroy;

    /// <summary>
    /// 펫 크리스탈 생츄어리 파괴 시작에 대한 확인용 변수
    /// </summary>
    private bool _isDestroyStart;

    /// <summary>
    /// 펫 크리스탈 생츄어리 파동 이펙트 클래스
    /// </summary>
    private CrystalSanctuaryWaveVFX _crystalSanctuaryWaveVFX;

    /// <summary>
    /// 펫 크리스탈 생츄어리 유지/소멸 보조 이펙트 클래스
    /// </summary>
    private CrystalSanctuaryAuraVFX _crystalSanctuaryAuraVFX;

    private void Awake()
    {
        _player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
        _animator = GetComponent<Animator>();
        _capsuleCollider2D = GetComponent<CapsuleCollider2D>();
        _crystalSanctuaryWaveVFX = GetComponent<CrystalSanctuaryWaveVFX>();
        _crystalSanctuaryAuraVFX = GetComponent<CrystalSanctuaryAuraVFX>();
    }

    private void Start()
    {
        _timeBeforeDestroy = 0.75f;
        _isDestroyStart = false;

        StartCoroutine(StartSkillProcessCoroutine());
    }

    private void Update()
    {
        if (_isDestroyStart)
        {
            _capsuleCollider2D.size -= new Vector2(_radiusDecreaseRatio, _radiusDecreaseRatio);
        }
    }

    private IEnumerator StartSkillProcessCoroutine()
    {
        float warningTime = Mathf.Min(_crystalSanctuaryAuraVFX.CollapseWarningTime, _skillDuration);

        yield return new WaitForSeconds(_skillDuration - warningTime);

        _crystalSanctuaryAuraVFX.PlayCollapseWarning();

        yield return new WaitForSeconds(warningTime);

        _isDestroyStart = true;
        _crystalSanctuaryAuraVFX.PlayCollapse();

        PlayEndAnimation();
        Destroy(gameObject, _timeBeforeDestroy);
    }

    private void PlayEndAnimation()
    {
        _animator.SetTrigger("IsDestroy");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        _player.OffPlayerHittable();
        _animator.SetBool("IsPlayerInside", true);
        _crystalSanctuaryWaveVFX.PlayEnterWaveVFX();
        _crystalSanctuaryAuraVFX.SetPlayerInside(true);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        _player.OffPlayerHittable();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        _player.OnPlayerHittable();
        _animator.SetBool("IsPlayerInside", false);
        _crystalSanctuaryWaveVFX.PlayExitWaveVFX();
        _crystalSanctuaryAuraVFX.SetPlayerInside(false);
    }
}
