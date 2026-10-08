public interface IBoss1MeleeAttack
{
    bool IsFinished
    {
        get;
    }

    void EnterAttack();
    void UpdateAttack();
    void ExitAttack();
}
