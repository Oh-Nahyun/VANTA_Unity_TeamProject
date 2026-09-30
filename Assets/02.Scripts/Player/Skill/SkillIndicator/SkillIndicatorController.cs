using System.Collections.Generic;
using UnityEngine;

public class SkillIndicatorController : MonoBehaviour
{
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
            target = gameObject.transform;
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
        _pets.Add(spawned);
        
        SkillIndicatorMove indicatorMove =
            spawned.GetComponent<SkillIndicatorMove>();
        indicatorMove.InitSetting(target,_pets.Count-1);
        

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
