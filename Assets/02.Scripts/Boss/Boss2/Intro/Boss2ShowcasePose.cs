using DG.Tweening;
using UnityEngine;

/// <summary>
/// 보스2 소개(왕좌·이름) 동안의 독립 포즈.
///   Play : 보스 Animator 를 잠시 끄고 Idle Break 프레임을 반복하며, 몸 주위에서 검은 오라가 피어오른다.
///   Stop : 프레임을 멈추고 Animator 를 다시 켜 평소 동작으로 돌려보내고, 오라는 방출만 멈춰 남은 연기가 자연스럽게 사라진다.
/// timeScale 과 상관없이 실제 시간으로 재생한다.
/// </summary>
public class Boss2ShowcasePose : MonoBehaviour
{
    [Header("포즈")]
    [SerializeField] private Sprite[] _idleBreakFrames;
    [Tooltip("초당 프레임 (Boss2Idle 과 같은 12)")]
    [SerializeField] private float _fps = 12f;

    [Header("검은 오라")]
    [Tooltip("보스 발밑으로 옮겨 재생한다")]
    [SerializeField] private ParticleSystem _aura;

    private Animator _animator;
    private SpriteRenderer _renderer;
    private Tween _tween;

    public bool IsPlaying { get; private set; }

    /// <summary>보스를 Idle Break 포즈로 바꾸고 검은 오라를 피워 올린다.</summary>
    public void Play(Transform boss)
    {
        Stop();
        if (boss == null)
            return;

        _renderer = boss.GetComponent<SpriteRenderer>();
        _animator = boss.GetComponent<Animator>();
        IsPlaying = true;

        if (_renderer != null && _idleBreakFrames != null && _idleBreakFrames.Length > 0)
        {
            if (_animator != null)
            {
                _animator.enabled = false;
            }

            Sprite[] frames = _idleBreakFrames;
            int count = frames.Length;
            SpriteRenderer target = _renderer;
            target.sprite = frames[0];
            _tween = DOTween
                .To(() => 0, (int frame) => target.sprite = frames[Mathf.Min(frame, count - 1)], count, count / Mathf.Max(1f, _fps))
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .SetLoops(-1, LoopType.Restart)
                .SetLink(gameObject);
        }

        if (_aura != null)
        {
            Vector3 feet = GetFeetPosition(boss);
            _aura.transform.position = new Vector3(feet.x, feet.y, _aura.transform.position.z);
            _aura.Clear(true);
            _aura.Play(true);
        }
    }

    /// <summary>포즈를 끝내고 평소 동작으로 돌려보낸다. 오라는 방출만 멈춘다.</summary>
    public void Stop()
    {
        if (_tween != null)
        {
            _tween.Kill();
            _tween = null;
        }
        if (_animator != null)
        {
            _animator.enabled = true;
            _animator = null;
        }
        _renderer = null;

        if (_aura != null)
        {
            _aura.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        IsPlaying = false;
    }

    // 보스의 지금 보이는 그림 아래 끝 가운데 = 발밑
    private static Vector3 GetFeetPosition(Transform boss)
    {
        SpriteRenderer body = boss.GetComponent<SpriteRenderer>();
        if (body == null || body.sprite == null)
            return boss.position;

        // 투명 여백을 빼고 실제로 보이는 부분 기준
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        foreach (Vector2 vertex in body.sprite.vertices)
        {
            min = Vector2.Min(min, vertex);
            max = Vector2.Max(max, vertex);
        }
        float centerX = (min.x + max.x) * 0.5f * (body.flipX ? -1f : 1f);
        return boss.TransformPoint(new Vector3(centerX, min.y, 0f));
    }

    private void OnDisable()
    {
        Stop();
    }
}
