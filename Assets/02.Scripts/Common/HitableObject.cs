using UnityEngine;

public abstract class HitableObject : MonoBehaviour
{
    public void ExecuteHitEvent()
    {
        Debug.Log("Hit");
        HitEvent();
    }
    protected abstract void HitEvent(); //피격시 일어날 행동들 
}
