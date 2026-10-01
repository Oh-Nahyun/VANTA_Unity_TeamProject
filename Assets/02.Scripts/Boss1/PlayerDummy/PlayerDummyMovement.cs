using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerDummyMovement : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;

    private void Update()
    {
        Vector2 direction = Vector2.zero;

        if (Keyboard.current.wKey.isPressed)
            direction.y += 1;

        if (Keyboard.current.sKey.isPressed)
            direction.y -= 1;

        if (Keyboard.current.aKey.isPressed)
            direction.x -= 1;

        if (Keyboard.current.dKey.isPressed)
            direction.x += 1;

        direction = direction.normalized;

        transform.position += (Vector3)(direction * _moveSpeed * Time.deltaTime);
    }
}
