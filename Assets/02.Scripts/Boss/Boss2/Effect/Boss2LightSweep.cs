using UnityEngine;

/// <summary>
/// 빛줄기(달빛 등)를 한쪽 끝을 축으로 천천히 돌린다. 이 오브젝트의 Z 회전만 바꾼다.
/// 빛줄기 파티클은 이 오브젝트의 자식으로 두고, Simulation Space = Local · Render Alignment = Local 이어야 함께 돈다.
/// 켜질 때(OnEnable)마다 처음 각도부터 다시 돈다. 코드에서는 Play() 로 다시 시작할 수 있다.
/// </summary>
public class Boss2LightSweep : MonoBehaviour
{
    [Tooltip("시작 각도(Z, 도). 0 이면 빛이 아래로 떨어지고, -45 면 오른쪽 위에서 왼쪽 아래로 떨어진다")]
    [SerializeField] private float _startAngle = -45f;

    [Tooltip("돌릴 각도(도). 양수 = 반시계 방향")]
    [SerializeField] private float _sweepAngle = 30f;

    [Tooltip("다 돌기까지 걸리는 시간(초). 빛줄기 파티클 수명과 맞춘다")]
    [SerializeField] private float _duration = 6f;

    [Tooltip("시작과 끝을 부드럽게 (SmoothStep)")]
    [SerializeField] private bool _easeInOut = true;

    private float _elapsed;
    private bool _isPlaying;

    private void OnEnable()
    {
        Play();
    }

    public void Play()
    {
        _elapsed = 0f;
        _isPlaying = true;
        ApplyAngle(0f);
    }

    private void Update()
    {
        if (_isPlaying == false)
            return;

        _elapsed += Time.deltaTime;
        float t = _duration > 0f ? Mathf.Clamp01(_elapsed / _duration) : 1f;
        ApplyAngle(t);

        if (t >= 1f)
        {
            _isPlaying = false;
        }
    }

    private void ApplyAngle(float t)
    {
        float eased = _easeInOut ? Mathf.SmoothStep(0f, 1f, t) : t;
        transform.localRotation = Quaternion.Euler(0f, 0f, _startAngle + _sweepAngle * eased);
    }

    // Scene 뷰에서 시작·끝 방향을 선으로 보여 준다. (빛은 이 오브젝트의 아래쪽(-Y)으로 뻗는다)
    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position;
        float baseAngle = transform.parent != null ? transform.parent.eulerAngles.z : 0f;
        Gizmos.color = new Color(0.7f, 0.85f, 1f);
        Gizmos.DrawLine(origin, origin + Quaternion.Euler(0f, 0f, baseAngle + _startAngle) * Vector3.down * 6f);
        Gizmos.color = new Color(1f, 0.9f, 0.5f);
        Gizmos.DrawLine(origin, origin + Quaternion.Euler(0f, 0f, baseAngle + _startAngle + _sweepAngle) * Vector3.down * 6f);
    }
}
