using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TitleDissolveEffect : MonoBehaviour
{
    [Header("디졸브 할 타이틀 이미지")]
    [SerializeField] private Image _titleImage;

    [Header("디졸브 할 버튼 이미지들")]
    [SerializeField] private List<Image> _buttonImages;

    [Header("타이틀 디졸브 효과 지속 시간")]
    public float TitleDissolveDuration = 1.2f;

    [Header("버튼 디졸브 효과 지속 시간")]
    public float ButtonDissolveDuration = 1f;

    /// <summary>
    /// 타이틀 이미지 머티리얼
    /// </summary>
    private Material _material;

    /// <summary>
    /// 버튼 이미지 머티리얼 목록
    /// </summary>
    private List<Material> _buttonMaterials = new List<Material>();

    /// <summary>
    /// 타이틀 디졸브 코루틴
    /// </summary>
    private Coroutine _dissolveTitleCoroutine;

    /// <summary>
    /// 버튼 디졸브 코루틴
    /// </summary>
    private Coroutine _dissolveButtonCoroutine;


    private void Awake()
    {
        _material = Instantiate(_titleImage.material);
        _titleImage.material = _material;
        _material.SetFloat("_DissolveAmount", 0f);

        foreach (Image buttonImage in _buttonImages)
        {
            Material material = Instantiate(buttonImage.material);
            buttonImage.material = material;
            material.SetFloat("_DissolveAmount", 0f);
            _buttonMaterials.Add(material);
        }
    }

    public void PlayTitleDissolve()
    {
        if (_dissolveTitleCoroutine != null)
        {
            StopCoroutine(_dissolveTitleCoroutine);
        }

        _dissolveTitleCoroutine = StartCoroutine(DissolveTitleCoroutine());
    }

    public void PlayButtonDissolve()
    {
        if (_dissolveButtonCoroutine != null)
        {
            StopCoroutine(_dissolveButtonCoroutine);
        }

        _dissolveButtonCoroutine = StartCoroutine(ButtonDissolveCoroutine());
    }

    private IEnumerator DissolveTitleCoroutine()
    {
        float timer = 0f;
        while (timer < TitleDissolveDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, timer / TitleDissolveDuration);
            _material.SetFloat("_DissolveAmount", progress);

            yield return null;
        }

        _material.SetFloat("_DissolveAmount", 1f);
        _dissolveTitleCoroutine = null;
    }

    private IEnumerator ButtonDissolveCoroutine()
    {
        float timer = 0f;
        while (timer < ButtonDissolveDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, timer / ButtonDissolveDuration);
            foreach (Material material in _buttonMaterials)
            {
                material.SetFloat("_DissolveAmount", progress);
            }

            yield return null;
        }

        foreach (Material material in _buttonMaterials)
        {
            material.SetFloat("_DissolveAmount", 1f);
        }

        _dissolveButtonCoroutine = null;
    }
}
