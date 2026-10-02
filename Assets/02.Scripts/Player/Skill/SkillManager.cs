using System;
using System.Collections.Generic;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    private SkillIndicatorController _skillIndicatorController;

    
    private SkillFocusScreenController _skillFocusScreenController;
    private SkillDashPointCreator _skillDashPointCreator;
    private SkillDashPointCalculater _skillDashPointCalculater;
    private SkillSpaceController _skillSpaceController;
    private SkillAttack _skillAttack;
    
    [SerializeField]
    private WeaknessThreadController _weaknessThreadController;

    [Header("참조")]
    [SerializeField] private PlayerAttack _playerAttack;
    [SerializeField] private PlayerMove _playerMove;
    [SerializeField] private Animator _animator;
    [Header("스킬 사용시 이펙트")]
    [SerializeField] private GameObject _skillUseEffect;

    [Header("스킬 사용중 타임 슬로우정도")]
    [SerializeField]
    private float _timeSlowAmount = 0.2f;

    [Header("현재 스킬 스택 개수")]
    [SerializeField]
    private int _skillStack = 0;

    public int GetSkillStack => _skillStack;

    [Header("최대 스킬 스택 개수")]
    [SerializeField]
    private int _maxSkillStack = 0;

    [Header("스택 쌓기 위해 필요한 피격 개수")]
    [SerializeField]
    private int _countForSkillStack = 10;

    [SerializeField]
    private int _currentHitCount = 0;

    [Header("스킬 유지 시간")]
    [SerializeField]
    private float _skillTime = 10f;
    
    private Tween _skillSequence;
    private bool _isSkillUse;

    private void Awake()
    {
        _skillFocusScreenController =
            GetComponent<SkillFocusScreenController>();

        _skillDashPointCreator =
            GetComponent<SkillDashPointCreator>();

        _skillDashPointCalculater =
            GetComponent<SkillDashPointCalculater>();

        _skillAttack =
            GetComponent<SkillAttack>();

        _skillIndicatorController =
            GetComponent<SkillIndicatorController>();
       _skillSpaceController=GetComponent<SkillSpaceController>();
    }


    private void Update()
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
        if (_maxSkillStack > _skillStack)
        {
            _skillStack++;

            CreateSkillIndicator();
        }
    }


    private void SkillInput()
    {
        if (Input.GetMouseButtonDown(1) && _skillStack > 0&&!_isSkillUse)
        {
            SkillUseStart();
        }
    }
    private void SkillUseStart()
    {
        _skillSequence?.Kill();
        _isSkillUse = true;
        _skillUseEffect.SetActive(true);

        _skillSequence =
            DOVirtual.DelayedCall(
                _skillTime,
                AutoSkillUseStart,
                true
            );
        
        Shader.SetGlobalFloat(
            "_PetCount",
            _skillStack
        );
        
        _playerMove.DashToReverseSide();

        _skillSpaceController.StartSkillSpaceDissolve(_skillTime);

        _weaknessThreadController.StartWeaknessThread();


        _animator.SetBool(
            "IsSkillUse",
            true
        );

        _playerAttack.enabled = false;
        _playerMove.enabled = false;

        Time.timeScale = _timeSlowAmount;

        ScreenControl();
    }
    

    

    private void ScreenControl()
    {
        _skillFocusScreenController
            .StartFocusScreenSetting(
                DashPointCreat
            );
    }


    private void DashPointCreat()
    {
        List<Vector3> dashPoints =
            _skillDashPointCreator.CreateDashPoint(
                _skillStack
                
            );

        MoveToPointSkillIndicators(
            dashPoints
        );
        
        DOVirtual.DelayedCall(1.5f, () =>
        {
            StartDashPointCalculate();
        });
    }


    private void StartDashPointCalculate()
    {
        _skillDashPointCalculater.enabled = true;

        _skillDashPointCalculater.Activate(
            _skillStack,
            StartDash
        );
    }


    private void StartDash()
    {
        List<SkillIndicator> _choicedSequnce =
            _skillDashPointCalculater
                .GetChoiceSequence();

        _skillAttack.SetWeaknessDirection(
            _weaknessThreadController
                .GetThreadDirection()
        );

        _skillAttack.DashStart(
            _choicedSequnce,
            SkillUseEnd
        );

        _skillDashPointCalculater.enabled = false;

        _skillUseEffect.SetActive(false);
    }


    public void SkillUseEnd()
    {
        _skillSequence?.Kill();
        _skillSequence = null;
        _isSkillUse = false;

        _weaknessThreadController
            .EndWeaknessThread();
        
        _skillStack = 0;

        _skillIndicatorController
            .ClearPet();

        _skillFocusScreenController
            .EndFocusScreenSetting();

        _animator.SetBool(
            "IsSkillUse",
            false
        );

        _playerAttack.enabled = true;
        _playerMove.enabled = true;

        Time.timeScale = 1f;
    }


    private void AutoSkillUseStart()
    {
        _skillDashPointCalculater
            .AutoSkillIndicatorSelect();

    }


    private void CreateSkillIndicator()
    {
        _skillIndicatorController
            .AddPet();
    }


    private void MoveToPointSkillIndicators(
        List<Vector3> dashPoints
    )
    {
        _skillIndicatorController
            .MoveToPoint(dashPoints);
    }


    private void OnDestroy()
    {
        _skillSequence?.Kill();
        
    }
}
