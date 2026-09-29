using UnityEngine;

public class PlayerPetCaller : MonoBehaviour
{
    [Header("펫 참조")][SerializeField] private PetAttack _petAttack;

    public void CallPetToFire()
    {
        _petAttack.FireEnergyBullet();
    }
}
