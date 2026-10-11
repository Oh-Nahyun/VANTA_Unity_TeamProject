using System.Collections;
using UnityEngine;

public class CrystalPrison : MonoBehaviour
{
    [Header("펫 크리스탈 프리즌 지속 시간")]
    [SerializeField] private float _skillDuration = 5f;

    [Header("펫 크리스탈 프리즌 공격력")]
    [SerializeField] private float _damage = 20f;

    [Header("펫 크리스탈 프리즌 파괴 준비 이펙트 재생 시간")]
    [SerializeField] private float _breakReadyDuration = 0.4f;

    [Header("펫 크리스탈 프리즌 생성 효과음")]
    [SerializeField] private AudioClip _crystalPrisonCreateSound;

    [Header("펫 크리스탈 프리즌 속박 시작 효과음")]
    [SerializeField] private AudioClip _crystalPrisonBindSound;

    [Header("펫 크리스탈 프리즌 유지 중 반복 효과음")]
    [SerializeField] private AudioClip _crystalPrisonHoldSound;

    [Header("펫 크리스탈 프리즌 파괴 준비 효과음")]
    [SerializeField] private AudioClip _crystalPrisonBreakReadySound;

    [Header("펫 크리스탈 프리즌 파괴 효과음")]
    [SerializeField] private AudioClip _crystalPrisonBreakSound;

    [Header("펫 크리스탈 프리즌 유지 효과음이 서서히 켜지고 꺼지는 시간")]
    [SerializeField] private float _crystalPrisonHoldSoundFadeDuration = 0.3f;

    /// <summary>
    /// 펫 공격 타겟
    /// </summary>
    public GameObject AttackTarget;

    /// <summary>
    /// 펫 크리스탈 프리즌 앞쪽 애니메이터
    /// </summary>
    private Animator _animatorFront;

    /// <summary>
    /// 펫 크리스탈 프리즌 뒤쪽 애니메이터
    /// </summary>
    private Animator _animatorBack;

    /// <summary>
    /// 펫 크리스탈 프리즌 장판 애니메이터
    /// </summary>
    private Animator _animatorGround;

    /// <summary>
    /// 펫 크리스탈 프리즌 이펙트 클래스
    /// </summary>
    private CrystalPrisonVFX _crystalPrisonVFX;

    /// <summary>
    /// 펫 크리스탈 프리즌 사용 시 대기 시간 계산용 변수
    /// </summary>
    private float _timer;

    /// <summary>
    /// 펫 크리스탈 프리즌 효과 발동 전 필요한 대기 시간
    /// </summary>
    private float _timeBeforeUseSkill;

    /// <summary>
    /// 펫 크리스탈 프리즌 파괴 전 필요한 대기 시간
    /// </summary>
    private float _timeBeforeDestroy;

    /// <summary>
    /// 펫 크리스탈 프리즌 파괴 준비 이펙트 시작 여부 확인용 변수
    /// </summary>
    private bool _isBreakReadyStarted;

    /// <summary>
    /// 펫 크리스탈 프리즌 종료 코루틴 시작 여부 확인용 변수
    /// </summary>
    private bool _isSkillProcessStart;

    /// <summary>
    /// 펫 크리스탈 프리즌 유지 중 반복 효과음 오디오 소스
    /// </summary>
    private AudioSource _crystalPrisonHoldSoundSource;

    /// <summary>
    /// 펫 크리스탈 프리즌 종료 여부 확인용 변수
    /// </summary>
    private bool _isSkillEnd;

    private void Awake()
    {
        _animatorFront = GetComponentsInChildren<Animator>()[0];
        _animatorBack = GetComponentsInChildren<Animator>()[1];
        _animatorGround = GetComponentsInChildren<Animator>()[2];
        _crystalPrisonVFX = GetComponentInChildren<CrystalPrisonVFX>();
    }

    private void Start()
    {
        _timer = 0f;
        _timeBeforeUseSkill = 0.7f;
        _timeBeforeDestroy = 0.75f;

        SoundManager.Instance.PlayEffectSound(_crystalPrisonCreateSound);
    }

    private void Update()
    {
        if (AttackTarget == null) return;

        _timer += Time.deltaTime;

        if (!_isBreakReadyStarted && _timer >= _timeBeforeUseSkill + _skillDuration - _breakReadyDuration)
        {
            PlayBreakReadyVFX();
        }

        if (_timer >= _timeBeforeUseSkill && !_isSkillEnd)
        {
            IBossMovable moveTarget = AttackTarget.GetComponent<IBossMovable>();
            StartSkillProcess(moveTarget);
        }
    }

    private void StartSkillProcess(IBossMovable moveTarget)
    {
        moveTarget.MoveBlock();

        if (_isSkillProcessStart) return;

        _isSkillProcessStart = true;
        SoundManager.Instance.PlayEffectSound(_crystalPrisonBindSound);
        _crystalPrisonHoldSoundSource = SoundManager.Instance.PlayLoopSound(_crystalPrisonHoldSound, 1f, _crystalPrisonHoldSoundFadeDuration);
        StartCoroutine(EndSkillProcessCoroutine(moveTarget));
    }

    private IEnumerator EndSkillProcessCoroutine(IBossMovable moveTarget)
    {
        yield return new WaitForSeconds(_skillDuration);

        _isSkillEnd = true;

        if (AttackTarget != null)
        {
            moveTarget.MoveUnblock();
            Hit();
        }

        PlayEndAnimation();
        Destroy(gameObject, _timeBeforeDestroy);
    }

    private void Hit()
    {
        HitableObject hitTarget = AttackTarget.GetComponent<HitableObject>();
        if (hitTarget == null) return;

        hitTarget.ExecuteHitEvent(_damage, DamageIndicatorType.Normal);
    }

    private void StopCrystalPrisonHoldSound(float fadeOutDuration)
    {
        if (_crystalPrisonHoldSoundSource == null) return;

        SoundManager.Instance.StopLoopSound(_crystalPrisonHoldSoundSource, fadeOutDuration);
        _crystalPrisonHoldSoundSource = null;
    }

    private void OnDestroy()
    {
        if (SoundManager.Instance == null) return;

        StopCrystalPrisonHoldSound(0f);
    }

    private void PlayBreakReadyVFX()
    {
        _isBreakReadyStarted = true;
        SoundManager.Instance.PlayEffectSound(_crystalPrisonBreakReadySound);
        StopCrystalPrisonHoldSound(_crystalPrisonHoldSoundFadeDuration);

        if (_crystalPrisonVFX != null)
        {
            _crystalPrisonVFX.PlayCrystalPrisonBreakReadyVFX();
        }
    }

    private void PlayEndAnimation()
    {
        SoundManager.Instance.PlayEffectSound(_crystalPrisonBreakSound);
        StopCrystalPrisonHoldSound(_crystalPrisonHoldSoundFadeDuration);

        _animatorFront.SetTrigger("IsEnd");
        _animatorBack.SetTrigger("IsEnd");
        _animatorGround.SetTrigger("IsEnd");

        if (_crystalPrisonVFX != null)
        {
            _crystalPrisonVFX.StopCrystalPrisonCreateVFX();
            _crystalPrisonVFX.StopCrystalPrisonHoldVFX();
            _crystalPrisonVFX.StopCrystalPrisonBreakReadyVFX();
            _crystalPrisonVFX.PlayCrystalPrisonBreakVFX();
        }
    }
}
