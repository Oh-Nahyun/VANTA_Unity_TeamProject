using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TitleDissolveEffect : MonoBehaviour
{
    [Header("디졸브 효과를 줄 이미지 UI")]
    [SerializeField] private Image _dissolveImage;

    [Header("디졸브 효과 지속 시간")]
    public float DissolveDuration = 1.2f;

    /// <summary>
    /// 디졸브 효과를 줄 이미지 UI의 머티리얼
    /// </summary>
    private Material _material;

    /// <summary>
    /// 디졸브 코루틴
    /// </summary>
    private Coroutine _dissolveCoroutine;

    private void Awake()
    {
        _material = Instantiate(_dissolveImage.material);
        _dissolveImage.material = _material;
        _material.SetFloat("_DissolveAmount", 0f);
    }

    public void PlayDissolve()
    {
        if (_dissolveCoroutine != null)
        {
            StopCoroutine(_dissolveCoroutine);
        }

        _dissolveCoroutine = StartCoroutine(DissolveCoroutine());
    }

    private IEnumerator DissolveCoroutine()
    {
        float timer = 0f;
        while (timer < DissolveDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, timer / DissolveDuration);
            _material.SetFloat("_DissolveAmount", progress);

            yield return null;
        }

        _material.SetFloat("_DissolveAmount", 1f);
        _dissolveCoroutine = null;
    }
}
