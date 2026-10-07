using UnityEngine;

public abstract class Boss : HitableObject
{
    // Idle로 제어 할수도 있고 FSM을 꺼서 제어 할 수도 있어서 PauseBoss로 명명. 
    public abstract void PauseBoss();
    public abstract void ResumeBoss();
    public virtual void EnableBoss()
    {
        if (gameObject != null)
        {
            gameObject.SetActive(true);
        }
    }
    public virtual void DisableBoss()
    {
        if (gameObject != null)
        {
            gameObject.SetActive(false);
        }
    }
}
