using UnityEngine;

/// <summary>
/// 보스2 공격 예고 인디케이터 하나. Boss2IndicatorSpawner 가 만든다.
///   - 테두리(Border)는 처음부터 보이고, 안쪽 채움(Fill)이 예고 시간 동안 차오른다. (광역기 인디케이터와 같은 방식)
///   - 직선(Line): 시작점에서 끝점 쪽으로 채움이 길어진다.
///   - 원(Circle): 채움이 가운데부터 커진다.
///   - 부채꼴(Fan): 채움이 꼭짓점(보스)에서 바깥으로 커진다.
/// 다 차면 스스로 사라진다. 공격이 끊기면 Cancel 로 바로 지운다.
/// </summary>
public class Boss2Indicator : MonoBehaviour
{
    private enum Shape { Line, Circle, Fan }

    private Shape _shape;
    private SpriteRenderer _fill;
    // 부채꼴: 꼭짓점을 기준으로 크기를 키워 안쪽에서 바깥으로 차오른다
    private Transform _fanFill;
    private float _duration;
    private float _elapsed;

    // 직선: 채움의 최종 길이·두께
    private float _length;
    private float _fillWidth;

    // 원: 채움의 최종 스케일
    private float _fillEndScale;

    public bool IsFinished => _elapsed >= _duration;

    public void InitLine(SpriteRenderer fill, float length, float fillWidth, float duration)
    {
        _shape = Shape.Line;
        _fill = fill;
        _length = length;
        _fillWidth = fillWidth;
        _duration = Mathf.Max(0.01f, duration);
        Apply(0f);
    }

    public void InitCircle(SpriteRenderer fill, float fillEndScale, float duration)
    {
        _shape = Shape.Circle;
        _fill = fill;
        _fillEndScale = fillEndScale;
        _duration = Mathf.Max(0.01f, duration);
        Apply(0f);
    }

    public void InitFan(Transform fill, float duration)
    {
        _shape = Shape.Fan;
        _fanFill = fill;
        _duration = Mathf.Max(0.01f, duration);
        Apply(0f);
    }

    /// <summary>공격이 취소됐을 때 바로 지운다.</summary>
    public void Cancel()
    {
        Destroy(gameObject);
    }

    // 부채꼴은 실행 중에 만든 메시를 쓰므로 함께 지운다.
    private void OnDestroy()
    {
        foreach (MeshFilter filter in GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh != null)
            {
                Destroy(filter.sharedMesh);
            }
        }
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(_elapsed / _duration);
        Apply(progress);

        if (progress >= 1f)
        {
            Destroy(gameObject);
        }
    }

    private void Apply(float progress)
    {
        // 끝으로 갈수록 빨리 차오르는 느낌 (긴장감)
        float k = progress * progress * (3f - 2f * progress);
        if (_shape == Shape.Fan)
        {
            if (_fanFill != null)
            {
                float scale = Mathf.Max(0.001f, k);
                _fanFill.localScale = new Vector3(scale, scale, 1f);
            }
            return;
        }

        if (_fill == null)
            return;

        switch (_shape)
        {
            case Shape.Line:
            {
                _fill.size = new Vector2(Mathf.Max(0.001f, _length * k), _fillWidth);
                break;
            }
            case Shape.Circle:
            {
                float scale = _fillEndScale * k;
                _fill.transform.localScale = new Vector3(scale, scale, 1f);
                break;
            }
        }
    }
}
