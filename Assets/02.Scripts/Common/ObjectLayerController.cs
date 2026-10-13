using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class ObjectLayerController : MonoBehaviour
{
    [Header("Sorting Settings")]
    [SerializeField]
    private string _sortingLayerName = "Default";

    [SerializeField]
    private int _orderInLayer = 0;

    private Renderer _renderer;

    private void Awake()
    {
        ApplySortingLayer();
    }

    private void OnValidate()
    {
        ApplySortingLayer();
    }

    public void ApplySortingLayer()
    {
        if (_renderer == null)
            _renderer = GetComponent<Renderer>();

        if (_renderer == null)
            return;

        _renderer.sortingLayerName = _sortingLayerName;
        _renderer.sortingOrder = _orderInLayer;
    }

    public void SetSortingLayer(string layerName)
    {
        _sortingLayerName = layerName;
        ApplySortingLayer();
    }

    public void SetSortingOrder(int order)
    {
        _orderInLayer = order;
        ApplySortingLayer();
    }

    public void SetSortingLayerAndOrder(
        string layerName,
        int order
    )
    {
        _sortingLayerName = layerName;
        _orderInLayer = order;

        ApplySortingLayer();
    }
}
