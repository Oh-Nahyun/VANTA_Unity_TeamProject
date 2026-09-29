using System.Collections;
using UnityEngine;

public class CrystalSanctuary : MonoBehaviour
{
    [Header("펫 크리스탈 생츄어리 지속 시간")]
    [SerializeField] private float _skillDuration = 7f;

    /// <summary>
    /// 펫 크리스탈 생츄어리 애니메이터
    /// </summary>
    private Animator _animator;

    /// <summary>
    /// 펫 크리스탈 생츄어리 파괴 전 필요한 대기 시간
    /// </summary>
    private float _timeBeforeDestroy;

    /// <summary>
    /// 펫 크리스탈 생츄어리 안에 플레이어가 들어왔는지에 대한 확인용 변수
    /// </summary>
    private bool _isPlayerInside;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        _timeBeforeDestroy = 0.75f;
        _isPlayerInside = false;

        StartCoroutine(StartSkillProcessCoroutine());
    }

    private IEnumerator StartSkillProcessCoroutine()
    {
        yield return new WaitForSeconds(_skillDuration);

        PlayEndAnimation();
        Destroy(gameObject, _timeBeforeDestroy);
    }

    private void PlayEndAnimation()
    {
        _animator.SetTrigger("IsDestroy");
    }
}
