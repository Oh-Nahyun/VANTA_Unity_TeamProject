using TMPro;
using UnityEngine;

public class Boss2 : MonoBehaviour
{
    [Header("보스2 스탯")]
    [SerializeField] private float _health = 100f;
    
    private Animator _animator;

    private void Awake()
    {
        _animator =  GetComponent<Animator>();
    }
     private void Start()
    {
    }

     private void Update()
    {
        
    }


    public void TakeDamage(int damage)
    {
        if (damage > 0)
        {
            _health -= damage;
            if (_health <= 0)
            {
                //Death();
                _animator.SetTrigger("Death");
            }
        }
        else
        {
            Debug.LogWarning("데미지는 음수 불가");
        }
        
    }
    
    
    private void Death()
    {
        // Effet 
        //Instantiate(_deathEffectPrefab, transform.position, Quaternion.identity);

        // Destroy
        Destroy(gameObject);
    }
}
