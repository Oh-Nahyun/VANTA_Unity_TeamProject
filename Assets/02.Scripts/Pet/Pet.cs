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

    private void Awake()
    {
        Animator = GetComponent<Animator>();
        SpriteRenderer = GetComponent<SpriteRenderer>();
        CircleCollider2D = GetComponent<CircleCollider2D>();
        PlayerSkillManager = GameObject.FindGameObjectWithTag("Player").GetComponentInChildren<SkillManager>();
    }
}
