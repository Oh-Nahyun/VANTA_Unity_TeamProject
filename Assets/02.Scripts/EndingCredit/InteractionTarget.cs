using UnityEngine;

public class InteractionTarget : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        PetJump petJump = other.GetComponentInParent<PetJump>();

        if (petJump == null)
        {
            petJump = other.GetComponentInChildren<PetJump>();
        }

        if (petJump != null)
        {
            petJump.Jump();
        }
    }
}
