using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 그림자 왕좌 곁에 소환되는 그림자 병사 한 명.
///   Appear : 그림자 안개가 발밑에 모여 아래부터 형체가 차오른 뒤, 자기 무기를 든 대기 프레임을 반복한다.
///   Vanish : 색이 빠지고 조각나 위로 흩어진 뒤 숨는다.
/// 왕좌와 같이 timeScale 과 상관없이 실제 시간으로 재생한다. 소환·배치는 ShadowSoldierTroop 이 맡는다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ShadowSoldier : MonoBehaviour
{
    [Header("프레임")]
    [SerializeField] private Sprite[] _appearFrames;
    [SerializeField] private Sprite[] _idleFrames;
    [SerializeField] private Sprite[] _vanishFrames;

    [Header("재생")]
    [Tooltip("형체화(나타남) 초당 프레임. 낮출수록 천천히 나타난다 (12프레임 ÷ 이 값 = 걸리는 초)")]
    [FormerlySerializedAs("_fps")]
    [SerializeField] private float _appearFps = 12f;
    [Tooltip("흩어짐(사라짐) 초당 프레임. 낮출수록 천천히 사라진다 (12프레임 ÷ 이 값 = 걸리는 초)")]
    [SerializeField] private float _vanishFps = 12f;
    [Tooltip("대기(무기 idle) 초당 프레임")]
    [SerializeField] private float _idleFps = 10f;

    private SpriteRenderer _renderer;
    private Tween _tween;

    public bool IsVisible { get; private set; }

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _renderer.enabled = false;
    }

    /// <summary>형체화하며 나타나고, 끝나면 무기 idle 프레임을 반복한다.</summary>
    public void Appear(bool faceLeft, string sortingLayer, int sortingOrder)
    {
        _renderer.sortingLayerName = sortingLayer;
        _renderer.sortingOrder = sortingOrder;
        _renderer.flipX = faceLeft;

        IsVisible = true;
        _renderer.enabled = true;
        PlayFrames(_appearFrames, _appearFps, 0, PlayIdle);
    }

    /// <summary>흩어지며 사라진다. 끝나면 onFinished 를 부른다.</summary>
    public void Vanish(Action onFinished = null)
    {
        if (IsVisible == false)
        {
            onFinished?.Invoke();
            return;
        }

        PlayFrames(_vanishFrames, _vanishFps, 0, () =>
        {
            Hide();
            onFinished?.Invoke();
        });
    }

    /// <summary>연출 없이 바로 숨긴다.</summary>
    public void Hide()
    {
        KillTween();
        IsVisible = false;
        if (_renderer != null)
        {
            _renderer.enabled = false;
        }
    }

    /// <summary>대기 프레임에서 실제로 보이는 부분의 범위 (피벗 기준 로컬 유닛, 투명 여백 제외). 프레임이 없으면 false.</summary>
    public bool TryGetVisibleExtents(out Vector2 min, out Vector2 max)
    {
        min = new Vector2(float.MaxValue, float.MaxValue);
        max = new Vector2(float.MinValue, float.MinValue);
        bool found = false;
        if (_idleFrames == null)
            return false;

        foreach (Sprite frame in _idleFrames)
        {
            if (frame == null)
                continue;
            foreach (Vector2 vertex in frame.vertices)
            {
                min = Vector2.Min(min, vertex);
                max = Vector2.Max(max, vertex);
                found = true;
            }
        }
        return found;
    }

    private void PlayIdle()
    {
        // 병사마다 대기 동작이 같은 박자로 움직이지 않도록 시작 프레임을 섞는다.
        int start = _idleFrames != null && _idleFrames.Length > 0 ? UnityEngine.Random.Range(0, _idleFrames.Length) : 0;
        PlayFrames(_idleFrames, _idleFps, -1, null, start);
    }

    private void PlayFrames(Sprite[] frames, float fps, int loops, Action onComplete, int startFrame = 0)
    {
        KillTween();
        if (frames == null || frames.Length == 0)
        {
            onComplete?.Invoke();
            return;
        }

        int count = frames.Length;
        _renderer.sprite = frames[startFrame % count];
        _tween = DOTween
            .To(() => 0, (int frame) => _renderer.sprite = frames[(Mathf.Min(frame, count - 1) + startFrame) % count], count, count / Mathf.Max(1f, fps))
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
