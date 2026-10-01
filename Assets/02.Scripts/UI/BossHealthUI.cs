using UnityEngine;
using UnityEngine.UI;

public class BossHealthUI : MonoBehaviour
{
    /// <summary>
    /// 보스 게임 오브젝트
    /// </summary>
    private GameObject _boss;

    /// <summary>
    /// 보스1 체력 클래스
    /// </summary>
    private Boss1Life _boss1;

    /// <summary>
    /// 보스2 체력 클래스
    /// </summary>
    private Boss2 _boss2;

    /// <summary>
    /// 보스 체력 비율 슬라이더
    /// </summary>
    private Slider _bossHealthRatio;

    private void Start()
    {
        _boss = GameObject.FindGameObjectWithTag("Boss");
        _boss1 = _boss.GetComponent<Boss1Life>();
        _boss2 = _boss.GetComponent<Boss2>();
        _bossHealthRatio = GetComponentInChildren<Slider>();
    }

    private void Update()
    {
        if (_boss1 != null)
        {
            _bossHealthRatio.value = _boss1.HealthRatio;
        }
        else if (_boss2 != null)
        {
            _bossHealthRatio.value = _boss2.HealthRatio;
        }
    }
}
