using Unity.Cinemachine;
using UnityEngine;

public class CameraShakeGenerator : MonoBehaviour
{
    [Header("VFX 유지 시간")]
    [SerializeField] private float _lifeTime = 0.5f;

    [Header("카메라 쉐이크")]
    [SerializeField] private CinemachineImpulseSource _impulseSource;

    private void OnEnable()
    {
        _impulseSource.GenerateImpulse();

        Destroy(gameObject, _lifeTime);
    }
}
