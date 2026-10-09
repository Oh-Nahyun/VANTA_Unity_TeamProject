using UnityEngine;

public class GroundMove : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 2f;

    private void Update()
    {
        transform.position += Vector3.left * _moveSpeed * Time.deltaTime;
    }
}
