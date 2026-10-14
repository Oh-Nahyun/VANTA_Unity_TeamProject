using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스2 공격 인식 범위. 걷는 중 플레이어가 이 안에 들어오면 Boss2MovePattern 이 그 자리에서 이동을 끝내고 바로 공격하게 한다.
/// 공격 판정(AttackZone)·그림(SpinningZone)과 분리된 트리거라 데미지를 주지 않는다.
/// Boss2 자식의 트리거 BoxCollider2D 와 같은 오브젝트에 붙인다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Boss2DetectZone : MonoBehaviour
{
    // 지금 범위 안에 있는 플레이어 (리지드바디 기준 오브젝트)
    private readonly HashSet<GameObject> _inside = new HashSet<GameObject>();

    /// <summary>target(플레이어)이 지금 인식 범위 안에 있는지</summary>
    public bool IsTargetInside(Transform target)
    {
        if (target == null)
            return false;

        _inside.RemoveWhere(go => go == null);
        foreach (GameObject go in _inside)
        {
            if (go.transform == target || target.IsChildOf(go.transform) || go.transform.IsChildOf(target))
                return true;
        }
        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        GameObject target = GetPlayer(other);
        if (target != null)
        {
            _inside.Add(target);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        GameObject target = GetPlayer(other);
        if (target != null)
        {
            _inside.Remove(target);
        }
    }

    // 콜라이더가 꺼지거나(사망 시) 오브젝트가 꺼지면 범위 안 기록을 비운다.
    private void OnDisable()
    {
        _inside.Clear();
    }

    private static GameObject GetPlayer(Collider2D other)
    {
        GameObject target = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
        return target.CompareTag("Player") ? target : null;
    }
}
