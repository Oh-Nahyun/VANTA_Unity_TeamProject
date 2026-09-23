using System;
using UnityEngine;

public class DamageDummy : MonoBehaviour
{
    private BoxCollider2D _collider;
    public int Damage = 1;

    private void Awake()
    {
        _collider = GetComponent<BoxCollider2D>();
    }
     private void Start()
    {
            
    }

     private void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Boss2"))
        {
            Boss2 boss2 = other.gameObject.GetComponent<Boss2>();
            if (boss2 != null)
            {
                boss2.TakeDamage(Damage);
            }
        }
    }
}
