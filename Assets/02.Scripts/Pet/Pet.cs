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
    /// 펫 리지드바디
    /// </summary>
    public Rigidbody2D Rigidbody2D;

    private void Awake()
    {
        Animator = GetComponent<Animator>();
        SpriteRenderer = GetComponent<SpriteRenderer>();
        Rigidbody2D =  GetComponent<Rigidbody2D>();
    }
}
