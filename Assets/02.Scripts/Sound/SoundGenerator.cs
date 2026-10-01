using UnityEngine;

public class SoundGenerator : MonoBehaviour
{
    [Header("실행될 오디오")][SerializeField]private AudioClip _sound;
    void OnEnable()
    {
        SoundManager.Instance.PlayEffectSound(_sound);
    }
}
