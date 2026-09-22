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
    public static  DamageIndicatorFactory Instance=>_instance;

    [Header("데미지 표시 프리팹")][SerializeField]private GameObject _indicatorPrefab;
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

    public void IndicateDamage(Vector3 loc,float damage,DamageIndicatorType type)
    {
        DamageIndicator spawnedDamageIndicator=Instantiate(_indicatorPrefab).GetComponent<DamageIndicator>();
        spawnedDamageIndicator.transform.position = loc;
        
    }
    
}
