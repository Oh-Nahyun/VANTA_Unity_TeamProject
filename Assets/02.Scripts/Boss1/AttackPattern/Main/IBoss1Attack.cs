public interface IBoss1Attack
{
    bool CanUse();

    float GetWeight();

    void EnterAttack();

    void UpdateAttack();

    bool IsFinished
    {
        get;
    }

    void ExitAttack();
}
