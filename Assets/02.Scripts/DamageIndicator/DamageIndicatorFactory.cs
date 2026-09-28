using System;

using UnityEngine;


public enum DamageIndicatorType
{
    Normal,
    Middle,
    Critical,
}
public class DamageIndicatorFactory : MonoBehaviour
{
    private static DamageIndicatorFactory _instance;
    public static DamageIndicatorFactory Instance => _instance;

    [Header("일반 데미지 표시 프리팹")][SerializeField] private GameObject _normalIndicatorPrefab;
    [Header("중간 데미지 표시 프리팹")][SerializeField] private GameObject _middleIndicatorPrefab;
    [Header("치명타데미지 표시 프리팹")][SerializeField] private GameObject _criticalIndicatorPrefab;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void IndicateDamage(Vector3 loc, float damage, DamageIndicatorType type)
    {
        GameObject prefab = null;
        float ranXOffset = UnityEngine.Random.Range(-0.3f, 0.3f);
        float ranYOffset = UnityEngine.Random.Range(-0.2f, 0.2f);

        loc.y += ranYOffset;
        loc.x += ranXOffset;



        switch (type)
        {
            case DamageIndicatorType.Normal:
                {
                    prefab = _normalIndicatorPrefab;
                    break;
                }
            case DamageIndicatorType.Middle:
                {
                    prefab = _middleIndicatorPrefab;
                    break;
                }
            case DamageIndicatorType.Critical:
                {
                    prefab = _criticalIndicatorPrefab;
                    break;
                }
        }

        if (prefab == null)
            return;

        DamageIndicator indicator =
            Instantiate(prefab, transform).GetComponent<DamageIndicator>();
        indicator.transform.position = loc;
        indicator.Init(damage);

    }

}
