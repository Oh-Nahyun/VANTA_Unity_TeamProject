using UnityEngine;

public class BezierCurve : MonoBehaviour
{
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

    public void SetPoints(Vector3 startPoint, Vector3 controlPoint, Vector3 endPoint)
    {
        _startPoint = startPoint;
        _controlPoint = controlPoint;
        _endPoint = endPoint;
    }
    
    public void SetEndPoint(Vector3 endPoint)
    {
        _endPoint = endPoint;
    }
    
    public Vector3 GetPointOnCurve(float progress)
    {
        Vector3 point1 = Vector3.Lerp(_startPoint, _controlPoint, progress);
        Vector3 point2 = Vector3.Lerp(_controlPoint, _endPoint, progress);
        
        return Vector3.Lerp(point1, point2, progress);
    }
}
