public interface IBoss1RangedAttack
{
    bool IsFinished
    {
        get;
    }

    void EnterAttack();
    void UpdateAttack();
    void ExitAttack();
}
