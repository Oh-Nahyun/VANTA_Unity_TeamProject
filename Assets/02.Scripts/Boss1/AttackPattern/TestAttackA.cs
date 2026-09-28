using UnityEngine;

public class TestAttackA : MonoBehaviour, IBoss1Attack
{
    [SerializeField] private float _attackDuration = 1f;
    [SerializeField] private float _weight = 50f;

    private float _timer;

    public bool IsFinished { get; private set; }

    public bool CanUse()
    {
        return true;
    }

    public float GetWeight()
    {
        return _weight;
    }

    public void EnterAttack()
    {
        _timer = 0f;
        IsFinished = false;

        Debug.Log("Test Attack A 시작");
    }

    public void UpdateAttack()
    {
        _timer += Time.deltaTime;

        if (_timer >= _attackDuration)
        {
            IsFinished = true;
        }
    }

    public void ExitAttack()
    {
        Debug.Log("Test Attack A 종료");
    }
}
