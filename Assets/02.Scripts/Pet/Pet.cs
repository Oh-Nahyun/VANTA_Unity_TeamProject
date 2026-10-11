using UnityEngine;

public class Pet : MonoBehaviour
{
    /// <summary>
    /// 펫 애니메이터
    /// </summary>
    public Animator Animator;

    /// <summary>
    /// 펫 스프라이트 렌더러
    /// </summary>
    public SpriteRenderer SpriteRenderer;

    /// <summary>
    /// 펫 원형 콜라이더 2D
    /// </summary>
    public CircleCollider2D CircleCollider2D;

    /// <summary>
    /// 플레이어 스킬 매니저 클래스
    /// </summary>
    public SkillManager PlayerSkillManager;

    /// <summary>
    /// 펫 이동 클래스
    /// </summary>
    public PetMove PetMove;

    [Header("펫 스폰 효과음")]
    [SerializeField] private AudioClip _spawnSound;

    [Header("펫 사망 효과음")]
    [SerializeField] private AudioClip _deathSound;

    private void Awake()
    {
        Animator = GetComponent<Animator>();
        SpriteRenderer = GetComponent<SpriteRenderer>();
        CircleCollider2D = GetComponent<CircleCollider2D>();
        PlayerSkillManager = GameObject.FindGameObjectWithTag("Player").GetComponentInChildren<SkillManager>();
        PetMove = GetComponent<PetMove>();
    }

    private void Start()
    {
        SoundManager.Instance.PlayEffectSound(_spawnSound);
    }

    public void Death()
    {
        SoundManager.Instance.PlayEffectSound(_deathSound);
        Animator.SetTrigger("IsDeath");
    }
}
