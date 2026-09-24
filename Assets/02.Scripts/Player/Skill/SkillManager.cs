using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    [Header("참조")]
    [SerializeField]private PlayerAttack _playerAttack;
    [SerializeField]private PlayerMove _playerMove;
    [Header("현재 스킬 스택 개수")][SerializeField]private int _skillStack=0;
    [Header("스택 쌓기 위해 필요한 피격 개수")][SerializeField]private int _countForSkillStack=10;
    [SerializeField]private int _currentHitCount=0;

    [Header("스킬 유지 시간")][SerializeField]private float _skillTime=10f;
    private float curTime=0;

    private Tween _skillSequence;
    
    
    private SkillScreenController _skillScreenController;
    private SkillDashPointCreator _skillDashPointCreator;
    private SkillDashPointCalculater _skillDashPointCalculater;
    [SerializeField]private SkillAttack _skillAttack;
    
    private TimeController _timeController;
    
    void Start()
    {
        _timeController = GetComponent<TimeController>();
        _skillScreenController=GetComponent<SkillScreenController>();
        _skillDashPointCreator=GetComponent<SkillDashPointCreator>();
        _skillDashPointCalculater=GetComponent<SkillDashPointCalculater>();
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
        _skillSequence=DOVirtual.DelayedCall(_skillTime,SkillUseEnd, true);

        
        _playerAttack.enabled = false;
        _playerMove.enabled = false;
        _timeController.StartTimeSlow(0.2f);

        ScreenControl();
    }

    private void ScreenControl()
    {
        _skillScreenController.SkillScreenSettingStart(DashPointCreat);
    }

    private void DashPointCreat()
    {
        _skillDashPointCreator.CreateDashPoint(_skillStack,StartDashPointCalculate);
    }

    private void StartDashPointCalculate()
    {
        _skillDashPointCalculater.enabled=true;
        _skillDashPointCalculater.Activate(_skillStack,StartDash);
    }

    private void StartDash()
    {
        List<SkillIndicator>_choicedSequnce=_skillDashPointCalculater.GetChoiceSequence();
        _skillAttack.DashStart(_choicedSequnce,SkillUseEnd);
        _skillDashPointCalculater.enabled=false;
    }
    
    
    
    public void SkillUseEnd()
    {
        if (_skillSequence!=null)
        {
            _skillSequence.Kill();
        }

        _skillStack = 0;
        
        _skillScreenController.SkillScreenSettingEnd();
        
        _playerAttack.enabled = true;
        _playerMove.enabled = true;
        
        _timeController.StopTimeSlow();
    }
}
