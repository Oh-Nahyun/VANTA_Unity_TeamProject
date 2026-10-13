using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 보스2 뒤에 나타나는 그림자 왕좌.
///   Appear : 그림자 안개가 모여 아래부터 형체가 차오른 뒤(형체화), 그림자가 일렁이는 대기 프레임을 반복한다.
///   Vanish : 색이 빠져 그림자가 되고 조각나 위로 흩어진 뒤 숨는다.
/// 보스 머리 위쪽에 VfxBack 레이어로 그려지고, 크기는 보스의 보이는 높이에 맞춘다. timeScale 과 상관없이 실제 시간으로 재생한다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ShadowThrone : MonoBehaviour
{
    [Header("프레임")]
    [SerializeField] private Sprite[] _appearFrames;
    [SerializeField] private Sprite[] _idleFrames;
    [SerializeField] private Sprite[] _vanishFrames;

    [Header("재생")]
    [Tooltip("형체화(나타남) 초당 프레임. 낮출수록 천천히 나타난다 (14프레임 ÷ 이 값 = 걸리는 초)")]
    [FormerlySerializedAs("_fps")]
    [SerializeField] private float _appearFps = 12f;
    [Tooltip("흩어짐(사라짐) 초당 프레임. 낮출수록 천천히 사라진다 (14프레임 ÷ 이 값 = 걸리는 초)")]
    [SerializeField] private float _vanishFps = 12f;
    [Tooltip("대기(일렁임) 초당 프레임")]
    [SerializeField] private float _idleFps = 6f;

    [Header("위치·크기")]
    [Tooltip("왕좌 받침 바닥을 보스 머리 꼭대기에서 이만큼 옮긴 곳에 둔다 (스프라이트 피벗이 받침 바닥 가운데)")]
    [SerializeField] private Vector2 _offsetFromHead = new Vector2(0f, -0.2f);
    [Tooltip("왕좌 높이 = 보스의 보이는 높이 × 이 값")]
    [SerializeField] private float _sizeRelativeToBoss = 1.25f;

    [Header("정렬")]
    [SerializeField] private string _sortingLayer = "VfxBack";
    [SerializeField] private int _sortingOrder = 0;

    [Header("그림자 병사")]
    [Tooltip("비워 두면 병사 없이 왕좌만 나타난다")]
    [SerializeField] private ShadowSoldierTroop _troop;

    private SpriteRenderer _renderer;
    private Tween _tween;

    public bool IsVisible { get; private set; }

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _renderer.enabled = false;
    }

    /// <summary>보스 뒤에서 형체화하며 나타나고, 끝나면 대기 프레임을 반복한다.</summary>
    public void Appear(Transform boss)
    {
        _renderer.sortingLayerName = _sortingLayer;
        _renderer.sortingOrder = _sortingOrder;
        if (boss != null)
        {
            PlaceAboveHead(boss);
        }

        IsVisible = true;
        _renderer.enabled = true;
        PlayFrames(_appearFrames, _appearFps, 0, PlayIdle);
        if (_troop != null && boss != null)
        {
            _troop.Summon(boss);
        }
    }

    /// <summary>그림자로 흩어지며 사라진다. 끝나면 onFinished 를 부른다.</summary>
    public void Vanish(Action onFinished = null)
    {
        if (IsVisible == false)
        {
            onFinished?.Invoke();
            return;
        }

        if (_troop != null)
        {
            _troop.Dismiss();
        }
        PlayFrames(_vanishFrames, _vanishFps, 0, () =>
        {
            Hide();
            onFinished?.Invoke();
        });
    }

    /// <summary>연출 없이 바로 숨긴다. (테스트 재시작·연출 중단)</summary>
    public void Hide()
    {
        KillTween();
        if (_troop != null)
        {
            _troop.Clear();
        }
        IsVisible = false;
        if (_renderer != null)
        {
            _renderer.enabled = false;
        }
    }

    private void PlayIdle()
    {
        // -1 = 무한 반복
        PlayFrames(_idleFrames, _idleFps, -1, null);
    }

    private void PlayFrames(Sprite[] frames, float fps, int loops, Action onComplete)
    {
        KillTween();
        if (frames == null || frames.Length == 0)
        {
            onComplete?.Invoke();
            return;
        }

        int count = frames.Length;
        _renderer.sprite = frames[0];
        _tween = DOTween
            .To(() => 0, (int frame) => _renderer.sprite = frames[Mathf.Min(frame, count - 1)], count, count / Mathf.Max(1f, fps))
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .SetLoops(loops, LoopType.Restart)
            .SetLink(gameObject)
            .OnComplete(() =>
            {
                _tween = null;
                onComplete?.Invoke();
            });
    }

    // 보스의 지금 보이는 모습 기준: 크기는 보스 높이 × _sizeRelativeToBoss, 받침 바닥은 머리 꼭대기 + _offsetFromHead.
    private void PlaceAboveHead(Transform boss)
    {
        SpriteRenderer body = boss.GetComponent<SpriteRenderer>();
        Sprite throneSprite = _idleFrames != null && _idleFrames.Length > 0 ? _idleFrames[0] : _renderer.sprite;
        if (body == null || body.sprite == null || throneSprite == null)
            return;

        GetVisibleRange(body.sprite, out Vector2 bossMin, out Vector2 bossMax);
        float bossScaleY = Mathf.Abs(boss.lossyScale.y);
        float bossHeight = (bossMax.y - bossMin.y) * bossScaleY;
        float headTop = boss.position.y + bossMax.y * bossScaleY;

        GetVisibleRange(throneSprite, out Vector2 throneMin, out Vector2 throneMax);
        float throneHeight = Mathf.Max(0.001f, throneMax.y - throneMin.y);
        float parentScale = transform.parent != null ? Mathf.Abs(transform.parent.lossyScale.y) : 1f;
        float scale = bossHeight * _sizeRelativeToBoss / throneHeight / Mathf.Max(0.001f, parentScale);
        transform.localScale = new Vector3(scale, scale, 1f);

        transform.position = new Vector3(boss.position.x + _offsetFromHead.x, headTop + _offsetFromHead.y, transform.position.z);
    }

    // 스프라이트에서 실제로 보이는 부분의 범위 (피벗 기준 로컬 유닛, 투명 여백 제외)
    private static void GetVisibleRange(Sprite sprite, out Vector2 min, out Vector2 max)
    {
        min = new Vector2(float.MaxValue, float.MaxValue);
        max = new Vector2(float.MinValue, float.MinValue);
        foreach (Vector2 vertex in sprite.vertices)
        {
            min = Vector2.Min(min, vertex);
            max = Vector2.Max(max, vertex);
        }
    }

    private void KillTween()
    {
        if (_tween != null)
        {
            _tween.Kill();
            _tween = null;
        }
    }

    private void OnDisable()
    {
        KillTween();
    }
}
