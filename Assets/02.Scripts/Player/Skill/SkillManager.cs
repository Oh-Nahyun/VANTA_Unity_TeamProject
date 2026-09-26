using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    
    private SkillIndicatorController _skillIndicatorController;
    private SkillFocusScreenController _skillFocusScreenController;
    private SkillDashPointCreator _skillDashPointCreator;
    private SkillDashPointCalculater _skillDashPointCalculater;
    private SkillAttack _skillAttack;
    [SerializeField]private WeaknessThreadController _weaknessThreadController;
    [Header("참조")]
    [SerializeField]private PlayerAttack _playerAttack;
    [SerializeField]private PlayerMove _playerMove;
   
    [Header("스킬 사용중 타임 슬로우정도")] [SerializeField]
    private float _timeSlowAmount=0.2f;
    [Header("현재 스킬 스택 개수")][SerializeField]private int _skillStack=0;
    [Header("스택 쌓기 위해 필요한 피격 개수")][SerializeField]private int _countForSkillStack=10;
    [SerializeField]private int _currentHitCount=0;

    [Header("스킬 유지 시간")][SerializeField]private float _skillTime=10f;
    private float curTime=0;

    private Tween _skillSequence;
    
    


    private void Awake()
    {
        _skillFocusScreenController=GetComponent<SkillFocusScreenController>();
        _skillDashPointCreator=GetComponent<SkillDashPointCreator>();
        _skillDashPointCalculater=GetComponent<SkillDashPointCalculater>();
        _skillAttack = GetComponent<SkillAttack>();
        _skillIndicatorController=GetComponent<SkillIndicatorController>();
        
    }
    

    // Update is called once per frame
    void Update()
    {
        SkillInput();
        
    }

    public void CalHitCount()
    {
        _currentHitCount++;
        if (_currentHitCount >= _countForSkillStack)
        {
            _currentHitCount = 0;
            MakeSkillStack();
        }
    }

    private void MakeSkillStack()
    {
        _skillStack++;
        CreateSkillIndicator();
    }


    private void SkillInput()
    {
        if (Input.GetMouseButtonDown(1)&&_skillStack>0)
        {
            SkillUseStart();
        }
    }

    private void SkillUseStart()
    {
        _skillSequence=DOVirtual.DelayedCall(_skillTime,AutoSkillUseStart, true);
        
        _playerMove.DashToReverseSide();
        
        _weaknessThreadController.StartWeaknessThread();
        
        _playerAttack.enabled = false;
        _playerMove.enabled = false;
        Time.timeScale=_timeSlowAmount;

        ScreenControl();
    }

    private void ScreenControl()
    {
        _skillFocusScreenController.StartFocusScreenSetting(DashPointCreat);
    }

    private void DashPointCreat()
    {
        List<Vector3>dashPoints=_skillDashPointCreator.CreateDashPoint(_skillStack,StartDashPointCalculate);
        MoveToPointSkillIndicators(dashPoints);
    }

    private void StartDashPointCalculate()
    {
        
        _skillDashPointCalculater.enabled=true;
        _skillDashPointCalculater.Activate(_skillStack,StartDash);
    }

    private void StartDash()
    {
        List<SkillIndicator>_choicedSequnce=_skillDashPointCalculater.GetChoiceSequence();
        _skillAttack.SetWeaknessDirection(_weaknessThreadController.GetThreadDirection());
        _skillAttack.DashStart(_choicedSequnce,SkillUseEnd);
        _skillDashPointCalculater.enabled=false;
    }
    
    
    
    public void SkillUseEnd()
    {
        if (_skillSequence!=null)
        {
            _skillSequence.Kill();
        }

        _weaknessThreadController.EndWeaknessThread();

        _skillStack = 0;
        _skillIndicatorController.ClearPet();
        
        _skillFocusScreenController.EndFocusScreenSetting();
        
        _playerAttack.enabled = true;
        _playerMove.enabled = true;

        Time.timeScale=1f;
    }

    private void AutoSkillUseStart()
    {
        _skillDashPointCalculater.AutoSkillIndicatorSelect();
    }
    
    private void CreateSkillIndicator()
    {
        _skillIndicatorController.AddPet();
    }

    private void MoveToPointSkillIndicators(List<Vector3> dashPoints)
    {
        _skillIndicatorController.MoveToPoint(dashPoints);
    }
}
