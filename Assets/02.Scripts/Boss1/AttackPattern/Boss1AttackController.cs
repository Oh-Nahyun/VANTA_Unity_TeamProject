using UnityEngine;

public class Boss1AttackController : MonoBehaviour
{
    [SerializeField] private float _chaseDuration = 1f;

    private AttackState _currentState;
    private IBoss1Attack _currentAttack;

    private float _stateTimer;

    private enum AttackState
    {
        Chase,
        Attack
    }

    private void Start()
    {
        ChangeState(AttackState.Chase);
    }

    private void Update()
    {
        switch (_currentState)
        {
            case AttackState.Chase:
                UpdateChase();
                break;

            case AttackState.Attack:
                UpdateAttack();
                break;
        }
    }

    /// <summary>
    /// 공격 사이의 추격 상태
    /// </summary>
    private void UpdateChase()
    {
        _stateTimer += Time.deltaTime;

        if (_stateTimer >= _chaseDuration)
        {
            // 이후 공격 선택 로직 추가
        }
    }

    /// <summary>
    /// 현재 선택된 공격 실행 상태
    /// </summary>
    private void UpdateAttack()
    {
        if (_currentAttack == null)
        {
            return;
        }

        _currentAttack.UpdateAttack();

        if (_currentAttack.IsFinished)
        {
            _currentAttack.ExitAttack();
            _currentAttack = null;

            ChangeState(AttackState.Chase);
        }
    }

    private void ChangeState(AttackState newState)
    {
        _currentState = newState;
        _stateTimer = 0f;

        Debug.Log($"Boss1 Attack State : {_currentState}");
    }
}
