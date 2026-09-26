using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class HitDummy : MonoBehaviour
{
    [Header("히트 더미 스탯")]
    [SerializeField] private float _health = 10f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void TakeDamage(int damage)
    {
        if (damage < 0)
        {
            Debug.Log("데미지는 음수 일 수 없다.");
            return;
        }

        _health -= (float)damage;
        if (_health <= 0)
        {
            // 체력이 바닥나면 펑!
            Destroy(gameObject);
        }

    }
}
