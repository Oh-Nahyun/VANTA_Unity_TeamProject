using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class SkillIndicatorController : MonoBehaviour
{
    [SerializeField] private GameObject _skillIndicatorPrefab;

    [Header("Pet Attack")]
    [SerializeField] private float _attackInterval = 0.06f;

    private readonly List<GameObject> _pets = new();

    private Sequence _attackCommandSequence;

    public void AddPet()
    {
        if (_pets.Count >= 5)
            return;

        Transform target = transform;

        Vector3 spawnPosition =
            target.position
            + Vector3.left
            + Vector3.up * 0.5f;

        GameObject spawned = Instantiate(
            _skillIndicatorPrefab,
            spawnPosition,
            Quaternion.identity
        );

        _pets.Add(spawned);

        SkillIndicatorMove indicatorMove =
            spawned.GetComponent<SkillIndicatorMove>();

        indicatorMove.InitSetting(
            this.gameObject,
            _pets.Count - 1
        );
    }

    public void CommandToPetAttack()
    {
        if (_pets.Count <= 0)
            return;

        _attackCommandSequence?.Kill();

        _attackCommandSequence =
            DOTween.Sequence();

        for (int i = 0; i < _pets.Count; i++)
        {
            GameObject pet = _pets[i];

            if (pet == null)
                continue;

            SkillIndicatorAttack skillIndicatorAttack =
                pet.GetComponent<SkillIndicatorAttack>();

            if (skillIndicatorAttack == null)
                continue;

            float delay =
                i * _attackInterval;

            _attackCommandSequence.InsertCallback(
                delay,
                () =>
                {
                    if (skillIndicatorAttack != null)
                    {
                        skillIndicatorAttack.Attack();
                    }
                }
            );
        }
    }

    public void MoveToPoint(List<Vector3> points)
    {
        int count = Mathf.Min(
            points.Count,
            _pets.Count
        );

        for (int i = 0; i < count; i++)
        {
            if (_pets[i] == null)
                continue;

            _pets[i].GetComponent<SkillIndicatorAttack>().PauseAttack();

            _pets[i]
                .GetComponent<SkillIndicatorMove>()
                .MoveToPoint(
                    points[i],
                    i,
                    count
                );
        }
    }

    public void ClearPet()
    {
        _attackCommandSequence?.Kill();

        _pets.Clear();
    }

    private void OnDestroy()
    {
        _attackCommandSequence?.Kill();
    }
}
