using System;
using System.Collections.Generic;
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

    private bool _isActivated = false;
    private int _dashPointCount;
    private SkillIndicator _selectedDashPoint;
    [SerializeField]private List<SkillIndicator> _choiceSequence = new List<SkillIndicator>();
    private Action _completeAction; 
    private void Awake()
    {
        if (_camera == null)
            _camera = Camera.main;
    }
    

    private void OnEnable()
    {
        if (_lineRenderer == null)
            return;

        _lineRenderer.positionCount = 2;
        _lineRenderer.enabled = false;
    }
    private void OnDisable()
    {
        if (_lineRenderer == null)
            return;

        _lineRenderer.positionCount = 2;
        _lineRenderer.enabled = false;
    }
    
    public void Activate(int pointCount,Action onComplete)
    {
        _choiceSequence.Clear();
        
        _completeAction=onComplete;
        _dashPointCount=pointCount;
        _isActivated = true;
    }
    
    private void Update()
    {
        if (!_isActivated)
            return;
        
        if (_camera == null || _lineRenderer == null || _player == null)
            return;

        UpdateDashPointSelection();
        UpdateTrajectory();
		
		if(_selectedDashPoint!=null&&Input.GetMouseButton(0))
		{
            SkillIndicatorSelect();
		}
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

      if (_selectedDashPoint == null)
          return;

      if (_selectedDashPoint.GetIsChoiced())
      {
          _selectedDashPoint = null;
          return;
      }
      
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

    private void SkillIndicatorSelect()
    {
        _selectedDashPoint.ChoicedToAttack();
        _choiceSequence.Add(_selectedDashPoint);
        _selectedDashPoint = null;

      
        if (_choiceSequence.Count == _dashPointCount)
        {
            _isActivated = false;
            _completeAction.Invoke();
        }
    }

    public List<SkillIndicator> GetChoiceSequence()
    {
        return _choiceSequence;
    }
    
}
