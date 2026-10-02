using UnityEngine;

public class PlayerPetCaller : MonoBehaviour
{
    [Header("펫 참조")][SerializeField] private PetAttack _petAttack;

    private static PlayerPetCaller _instance;
    public static PlayerPetCaller Instance=>_instance;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void CallPetToFire()
    {
        _petAttack.FireEnergyBullet();
    }

    public void CallPetToAttackSkill()
    {
        _petAttack.UseCrystalPrison();
    }
    public void CallPetToUtillSkill()
    {
        _petAttack.UseCrystalSanctuary();
    }
}
