using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class SkillDashPointCalculater : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera _camera;
    [SerializeField] private Transform _player;
    [Header("Dash Point")]
    [SerializeField] private LayerMask _dashPointLayer;


    [Tooltip("궤적을 바닥에서 살짝 띄우는 값")]
    [SerializeField] private float _lineHeight = 0.1f;

    private bool _isActivated = false;
    private int _dashPointCount;
    
    private SkillDashLineIndicator _skillDashLineIndicator;
    private SkillIndicator _selectedDashPoint;
    [SerializeField]private List<SkillIndicator> _choiceSequence = new List<SkillIndicator>();
    private Action _completeAction; 
    
    private Sequence _autoSelectSequence;

    private void Awake()
    {
        if (_camera == null)
            _camera = Camera.main;
        _skillDashLineIndicator=gameObject.GetComponentInChildren<SkillDashLineIndicator>();
    }
    

    private void OnEnable()
    {
        _skillDashLineIndicator.enabled = true;
    }
    private void OnDisable()
    {
        _skillDashLineIndicator.enabled = false;
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
        
        if (_camera == null ||  _player == null)
            return;

        UpdateDashPointSelection();
        UpdateTrajectory();
		
		if(_selectedDashPoint!=null&&Input.GetMouseButtonDown(0))
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
            return;
        }
        
        _skillDashLineIndicator.ShowPreview(
            _selectedDashPoint
        );
    }

    private void SkillIndicatorSelect()
    {
        _selectedDashPoint.ChoicedToAttack();
        _choiceSequence.Add(_selectedDashPoint);
        _skillDashLineIndicator.AddNewPoint(_selectedDashPoint);
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

    public void AutoSkillIndicatorSelect()
    {
        _autoSelectSequence?.Kill();

        _autoSelectSequence = DOTween.Sequence();

        var allSkillIndicators=FindObjectsByType<SkillIndicator>(FindObjectsSortMode.None);
        
        foreach (SkillIndicator dashPoint in allSkillIndicators)
        {
            if (_choiceSequence.Contains(dashPoint))
                continue;

            SkillIndicator point = dashPoint;

            _autoSelectSequence.AppendCallback(() =>
            {
                _selectedDashPoint = point;
                SkillIndicatorSelect();
            });

            _autoSelectSequence.AppendInterval(0.15f);
        }
    }
    
}
