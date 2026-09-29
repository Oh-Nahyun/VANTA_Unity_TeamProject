using System.Collections.Generic;
using UnityEngine;

public class SkillIndicatorController : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField] private GameObject _skillIndicatorPrefab;

    private readonly List<GameObject> _pets = new();

    void Update()
    {

    }
    public void AddPet()
    {
        Transform target;

        if (_pets.Count == 0)
        {
            target = _player;
        }
        else
        {
            target = _pets[^1].transform;
        }

        Vector3 spawnPosition =
            target.position - target.right * 0.1f;

        GameObject spawned = Instantiate(
            _skillIndicatorPrefab,
            spawnPosition,
            Quaternion.identity
        );

        spawned.GetComponent<Animator>().SetTrigger("Spawn");

        SkillIndicatorMove indicatorMove =
            spawned.GetComponent<SkillIndicatorMove>();
        indicatorMove.SetTarget(target);
        SkillIndicatorSkillLocReveal skillIndicatorSkillLocReveal =spawned.GetComponent<SkillIndicatorSkillLocReveal>();
        skillIndicatorSkillLocReveal.SetPetNumber(_pets.Count);
        _pets.Add(spawned);
    }


    public void CommandToPetAttack()
    {
        if (_pets.Count <= 0)
        {
            return;
        }

        foreach (GameObject pet in _pets)
        {
            SkillIndicatorAttack skillIndicatorAttack = pet.GetComponent<SkillIndicatorAttack>();
            if (!skillIndicatorAttack.IsAttacking)
            {
                skillIndicatorAttack.Attack();
                return;
            }
        }
    }


    public void MoveToPoint(List<Vector3> points)
    {
        for (int i = 0; i < points.Count; i++)
        {
            _pets[i].GetComponent<SkillIndicatorMove>().MoveToPoint(points[i]);
        }
    }

    public void ClearPet()
    {
        _pets.Clear();
    }
}
