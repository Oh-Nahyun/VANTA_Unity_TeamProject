using DG.Tweening;
using UnityEngine;

/// <summary>
/// 순보 연출. from → to 사이에 보스 잔상을 차례로 남겼다가 흐리게 사라지게 한다. 실제 위치는 바꾸지 않는다.
/// 순간이동할 자리가 맵 경계 때문에 원래 의도(대상 위)와 달라졌을 때, 원래 자리에서 옮겨 온 것처럼 보이게 쓴다.
/// </summary>
public class Boss2AfterImage : MonoBehaviour
{
    [Tooltip("잔상에 쓸 스프라이트. 비워 두면 그 순간 보스가 그리고 있는 스프라이트를 쓴다.\n사라진 상태에서 순보하는 회전베기처럼 지금 그림이 비어 있을 때 지정한다")]
    [SerializeField] private Sprite _ghostSprite;

    [Tooltip("잔상 그림이 실제로 보이는 중심(피벗 기준, 오른쪽을 볼 때). 맵 밖 잔상을 거르는 데 쓴다.\n(0,0) 이면 스프라이트 bounds 중심을 쓴다")]
    [SerializeField] private Vector2 _ghostVisualOffset = Vector2.zero;

    [SerializeField] private Color _ghostColor = new Color(0.7f, 0.07f, 0.12f, 0.6f);

    [Tooltip("from 과 to 사이에 남길 잔상 수 (양 끝 포함)")]
    [SerializeField] private int _ghostCount = 5;

    [Tooltip("잔상이 앞에서부터 하나씩 생기는 간격(초)")]
    [SerializeField] private float _spawnInterval = 0.02f;

    [Tooltip("잔상 하나가 사라지는 데 걸리는 시간(초)")]
    [SerializeField] private float _fadeTime = 0.25f;

    private SpriteRenderer _renderer;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// from 에서 to 로 순보한 것처럼 잔상을 남긴다. isInside 가 있으면 그 안의 잔상만 남겨 맵 밖에 그려지지 않게 한다.
    /// </summary>
    public void Play(Vector2 from, Vector2 to, bool faceLeft, System.Func<Vector2, bool> isInside = null)
    {
        Sprite sprite = _ghostSprite != null ? _ghostSprite : (_renderer != null ? _renderer.sprite : null);
        if (sprite == null || _ghostCount <= 0)
            return;

        // 스프라이트는 피벗에서 떨어진 곳(예: 회전베기 프레임은 위쪽)에 그려질 수 있다.
        // 맵 안인지는 피벗이 아니라 실제로 보이는 그림의 중심으로 판단해야 맵 밖에 잔상이 보이지 않는다.
        Vector2 localOffset = _ghostVisualOffset != Vector2.zero ? _ghostVisualOffset : (Vector2)sprite.bounds.center;
        Vector2 visualOffset = Vector2.Scale(localOffset, transform.lossyScale);
        if (faceLeft)
        {
            visualOffset.x = -visualOffset.x;
        }

        int count = Mathf.Max(_ghostCount, 2);
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / (count - 1);
            Vector2 position = Vector2.Lerp(from, to, t);
            if (isInside != null && isInside(position + visualOffset) == false)
                continue;

            SpawnGhost(sprite, position, faceLeft, i * _spawnInterval);
        }
    }

    private void SpawnGhost(Sprite sprite, Vector2 position, bool faceLeft, float delay)
    {
        GameObject ghost = new GameObject("Boss2AfterImage");
        ghost.transform.position = new Vector3(position.x, position.y, transform.position.z);
        ghost.transform.localScale = transform.lossyScale;

        // 기본 스프라이트 머티리얼을 쓴다. 보스 머티리얼(핏빛·발광 셰이더)은 마스크가 없는 잔상에 맞지 않는다.
        SpriteRenderer ghostRenderer = ghost.AddComponent<SpriteRenderer>();
        ghostRenderer.sprite = sprite;
        ghostRenderer.flipX = faceLeft;
        if (_renderer != null)
        {
            ghostRenderer.sortingLayerID = _renderer.sortingLayerID;
            ghostRenderer.sortingOrder = _renderer.sortingOrder - 1;
        }

        Color color = _ghostColor;
        ghostRenderer.color = new Color(color.r, color.g, color.b, 0f);

        // 앞쪽 잔상부터 차례로 나타나 순보 방향이 보이게 하고, 곧바로 흐려지며 사라진다.
        Sequence sequence = DOTween.Sequence();
        sequence.AppendInterval(delay);
        sequence.AppendCallback(() => ghostRenderer.color = color);
        sequence.Append(DOVirtual.Float(color.a, 0f, _fadeTime,
            alpha => ghostRenderer.color = new Color(color.r, color.g, color.b, alpha)));
        sequence.OnComplete(() => Destroy(ghost));
        sequence.SetLink(ghost);
    }
}
