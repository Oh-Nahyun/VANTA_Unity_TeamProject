using DG.Tweening;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    [Header("현재 스킬 스택 개수")][SerializeField]private int _skillStack=0;
    [Header("스택 쌓기 위해 필요한 피격 개수")][SerializeField]private int _countForSkillStack=10;
    [SerializeField]private int _currentHitCount=0;

    [Header("스킬 유지 시간")][SerializeField]private float _skillTime=10f;
    private float curTime=0;

    
    private SkillScreenController _skillScreenController;
    private TimeController _timeController;
    void Start()
    {
        _timeController = GetComponent<TimeController>();
        _skillScreenController=GetComponent<SkillScreenController>();
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
        _timeController.StartTimeSlow(0.2f);
        _skillScreenController.SkillScreenSettingStart();
        DOVirtual.DelayedCall(_skillTime,SkillUseEnd, true);

    }

    private void SkillUseEnd()
    {
        _skillScreenController.SkillScreenSettingEnd();
        _timeController.StopTimeSlow();
    }
}
