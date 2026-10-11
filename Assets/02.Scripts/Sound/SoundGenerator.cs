using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SoundGenerator : MonoBehaviour
{
    [Header("실행될 오디오")] [SerializeField] private AudioClip _sound;

    void OnEnable()
    {
        SoundManager.Instance.PlayEffectSound(_sound);
    }

    public void ActivateSound()
    {
        SoundManager.Instance.PlayEffectSound(_sound);
    }

    public static void AddHoverSound(Button button, AudioClip hoverSound)
    {
        AddEventTriggerSound(button.gameObject, EventTriggerType.PointerEnter, hoverSound);
    }

    public static void AddReleaseSound(Slider slider, AudioClip releaseSound)
    {
        AddEventTriggerSound(slider.gameObject, EventTriggerType.PointerUp, releaseSound);
    }

    private static void AddEventTriggerSound(GameObject target, EventTriggerType eventType, AudioClip sound)
    {
        EventTrigger trigger = target.GetComponent<EventTrigger>();

        if (trigger == null)
        {
            trigger = target.AddComponent<EventTrigger>();
        }

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = eventType;
        entry.callback.AddListener(eventData => SoundManager.Instance.PlayEffectSound(sound));

        trigger.triggers.Add(entry);
    }
}
