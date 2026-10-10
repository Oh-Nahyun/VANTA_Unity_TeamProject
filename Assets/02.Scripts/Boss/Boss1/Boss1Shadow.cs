using UnityEngine;

public class Boss1Shadow : MonoBehaviour
{
    [Header("보스 스프라이트 + 머리 헤드 3개")]
    [SerializeField] private SpriteRenderer[] _sources;

    [Header("반사 모양")]
    [SerializeField] private float _mirrorY = 0f;          // 이 오브젝트 기준 바닥선 높이. 플레이 중에 맞추기
    [SerializeField] private Color _reflectionColor = new Color(0.36f, 0.36f, 0.36f, 0.57f);
    [SerializeField] private Material _reflectionMaterial; // 비우면 원본 머티리얼 사용

    private SpriteRenderer[] _mirrors;
    private static readonly int Phase2Amount = Shader.PropertyToID("_Phase2Amount");

    private void Awake()
    {
        _mirrors = new SpriteRenderer[_sources.Length];

        for (int i = 0; i < _sources.Length; i++)
        {
            SpriteRenderer source = _sources[i];

            GameObject mirrorObject = new GameObject(source.name + "_Reflection");
            mirrorObject.transform.SetParent(transform, false);
            mirrorObject.layer = gameObject.layer;

            SpriteRenderer mirror = mirrorObject.AddComponent<SpriteRenderer>();
            mirror.sharedMaterial = _reflectionMaterial != null ? _reflectionMaterial : source.sharedMaterial;

            mirror.sortingLayerID = source.sortingLayerID;
            mirror.sortingOrder = -source.sortingOrder;

            _mirrors[i] = mirror;
        }
    }
    
    private void LateUpdate()
    {
        for (int i = 0; i < _sources.Length; i++)
        {
            CopyToMirror(_sources[i], _mirrors[i]);
        }
    }

    private void CopyToMirror(SpriteRenderer source, SpriteRenderer mirror)
    {
        bool visible = source.enabled && source.gameObject.activeInHierarchy && source.sprite != null;
        mirror.enabled = visible;
        if (!visible)
        {
            return;
        }
        
        mirror.sprite = source.sprite;
        mirror.flipX = source.flipX;
        mirror.flipY = source.flipY;
        
        mirror.forceRenderingOff = source.forceRenderingOff;
        
        Color color = _reflectionColor;
        color.a *= source.color.a;
        mirror.color = color;

        Transform sourceTransform = source.transform;
        Transform mirrorTransform = mirror.transform;
        
        Vector3 localPoint = transform.InverseTransformPoint(sourceTransform.position);
        mirrorTransform.localPosition = new Vector3(localPoint.x, 2f * _mirrorY - localPoint.y, 0f);

        Vector3 rootScale = transform.lossyScale;
        Vector3 sourceScale = sourceTransform.lossyScale;
        mirrorTransform.localScale = new Vector3(
            sourceScale.x / rootScale.x,
            -sourceScale.y / rootScale.y,
            1f);
        
        float angle = sourceTransform.eulerAngles.z - transform.eulerAngles.z;
        mirrorTransform.localRotation = Quaternion.Euler(0f, 0f, -angle);
    }
    
    public void SetPhase2Amount(float amount)
    {
        if (_mirrors == null)
        {
            return;
        }

        foreach (SpriteRenderer mirror in _mirrors)
        {
            if (mirror == null)
            {
                continue;
            }

            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            mirror.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(Phase2Amount, amount);
            mirror.SetPropertyBlock(propertyBlock);
        }
    }
}
