using UnityEngine;
using UnityEngine.InputSystem;

public class SkillDashPointCalculater : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera _camera;
    [SerializeField] private Transform _player;

    [Header("Dash Point")]
    [SerializeField] private LayerMask _dashPointLayer;

    [Header("Trajectory")]
    [SerializeField] private LineRenderer _lineRenderer;

    [Tooltip("궤적을 바닥에서 살짝 띄우는 값")]
    [SerializeField] private float _lineHeight = 0.1f;

    private SkillIndicator _selectedDashPoint;

    private void Awake()
    {
        if (_camera == null)
            _camera = Camera.main;
    }

    private void Start()
    {
        if (_lineRenderer == null)
            return;

        _lineRenderer.positionCount = 2;
        _lineRenderer.enabled = false;
    }

    private void Update()
    {
        if (_camera == null || _lineRenderer == null || _player == null)
            return;

        UpdateDashPointSelection();
        UpdateTrajectory();
    }

    private void UpdateDashPointSelection()                                                                                                                                                                 
  {                                                                                                                                                                                                       
      SkillIndicator nextDashPoint = null;                                                                                                                                                                
                                                                                                                                                                                                          
      if (Mouse.current != null)                                                                                                                                                                          
      {                                                                                                                                                                                                   
          Vector2 mousePosition = Mouse.current.position.ReadValue();                                                                                                                                     
          nextDashPoint = SkillIndicator.GetAtScreenPosition(                                                                                                                                             
              _camera,                                                                                                                                                                                    
              mousePosition,                                                                                                                                                                              
              _dashPointLayer);                                                                                                                                                                           
      }                                                                                                                                                                                                   
                                                                                                                                                                                                          
      if (_selectedDashPoint == nextDashPoint)                                                                                                                                                            
          return;                                                                                                                                                                                         
                                                                                                                                                                                                          
      if (_selectedDashPoint != null)                                                                                                                                                                     
          _selectedDashPoint.Deselect();                                                                                                                                                                  
                                                                                                                                                                                                          
      _selectedDashPoint = nextDashPoint;                                                                                                                                                                 
                                                                                                                                                                                                          
      if (_selectedDashPoint != null)                                                                                                                                                                     
          _selectedDashPoint.Select();                                                                                                                                                                    
  }                               
    
    private void UpdateTrajectory()
    {
        if (_selectedDashPoint == null)
        {
            _lineRenderer.enabled = false;
            return;
        }

        _lineRenderer.enabled = true;

        Vector3 lineOffset = -_camera.transform.forward * _lineHeight;
        Vector3 start = _player.position + lineOffset;
        Vector3 end = _selectedDashPoint.transform.position + lineOffset;

        _lineRenderer.SetPosition(0, start);
        _lineRenderer.SetPosition(1, end);
    }
}
