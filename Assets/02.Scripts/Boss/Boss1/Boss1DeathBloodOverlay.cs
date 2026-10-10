using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Boss1DeathBloodOverlay : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private Sprite[] _frames;
    [SerializeField] private float _frameRate = 20f;

    private void Awake()
    {
        _image.enabled = false;
    }

    public void Play()
    {
        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        _image.enabled = true;

        float frameTime = 1f / _frameRate;
        foreach (Sprite frame in _frames)
        {
            _image.sprite = frame;
            yield return new WaitForSeconds(frameTime);
        }

        Destroy(gameObject);
    }
}
