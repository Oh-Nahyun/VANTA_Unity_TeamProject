using UnityEngine;

public class BezierCurve : MonoBehaviour
{
    [Header("펫 에너지 탄 이동 곡선 정도")]
    [SerializeField] private float _curveAmount = 2f;
    
    [Header("펫 에너지 탄 이동 곡선의 제어점 위치 변경용 변수 (0과 가까울수록 시작점과 가까워짐)")]
    [Range(0f, 1f)]
    [SerializeField] private float _controlPointPositionRatio = 0.2f;
    
    /// <summary>
    /// 베지어 곡선 시작점
    /// </summary>
    private Vector3 _startPoint;
    
    /// <summary>
    /// 베지어 곡선 제어점
    /// </summary>
    private Vector3 _controlPoint;
    
    /// <summary>
    /// 베지어 곡선 끝점
    /// </summary>
    private Vector3 _endPoint;
    
    public void SetCurveAmount(float curveAmount)
    {
        _curveAmount = curveAmount;
    }

    public void SetAllPoints(Vector3 startPoint, Vector3 endPoint)
    {
        _startPoint = startPoint;
        _endPoint = endPoint;
        
        Vector3 direction = (_endPoint - _startPoint).normalized;
        Vector3 vertical = new Vector3(-direction.y, direction.x, 0f);
        Vector3 controlBasePoint = Vector3.Lerp(_startPoint, _endPoint, _controlPointPositionRatio);
        
        _controlPoint = controlBasePoint + vertical * _curveAmount;
    }
    
    public Vector3 GetPointOnCurve(float progress)
    {
        Vector3 point1 = Vector3.Lerp(_startPoint, _controlPoint, progress);
        Vector3 point2 = Vector3.Lerp(_controlPoint, _endPoint, progress);
        
        return Vector3.Lerp(point1, point2, progress);
    }
    
    public Vector3 GetDirectionOnCurve(float progress)
    {
        Vector3 direction = 2f * (1f - progress) * (_controlPoint - _startPoint) 
                            + 2f * progress * (_endPoint - _controlPoint);

        return direction.normalized;
    }
}
