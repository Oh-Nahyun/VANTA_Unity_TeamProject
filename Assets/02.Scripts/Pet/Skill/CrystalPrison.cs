using System.Collections;
using UnityEngine;

public class CrystalPrison : MonoBehaviour
{
    [Header("펫 크리스탈 프리즌 지속 시간")]
    [SerializeField] private float _skillDuration = 5f;

    [Header("펫 크리스탈 프리즌 공격력")]
    [SerializeField] private float _damage = 20f;

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
    }

    private void Update()
    {
        if (AttackTarget == null) return;

        _timer += Time.deltaTime;
        if (_timer >= _timeBeforeUseSkill)
        {
            IBossMovable moveTarget = AttackTarget.GetComponent<IBossMovable>();
            StartSkillProcess(moveTarget);
        }
    }

    private void StartSkillProcess(IBossMovable moveTarget)
    {
        moveTarget.MoveBlock();
        StartCoroutine(EndSkillProcessCoroutine(moveTarget));
    }

    private IEnumerator EndSkillProcessCoroutine(IBossMovable moveTarget)
    {
        yield return new WaitForSeconds(_skillDuration);

        moveTarget.MoveUnblock();
        Hit();

        PlayEndAnimation();
        Destroy(gameObject, _timeBeforeDestroy);
    }

    private void Hit()
    {
        HitableObject hitTarget = AttackTarget.GetComponent<HitableObject>();
        hitTarget.ExecuteHitEvent(_damage, DamageIndicatorType.Normal);
    }

    private void PlayEndAnimation()
    {
        _animatorFront.SetTrigger("IsEnd");
        _animatorBack.SetTrigger("IsEnd");
        _animatorGround.SetTrigger("IsEnd");

        if (_crystalPrisonVFX != null)
        {
            _crystalPrisonVFX.StopCrystalPrisonCreateVFX();
            _crystalPrisonVFX.StopCrystalPrisonHoldVFX();
        }
    }
}
