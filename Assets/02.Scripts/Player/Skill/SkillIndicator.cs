using UnityEditor.SearchService;
using UnityEngine;

public class SkillIndicator : MonoBehaviour
{
    private ObjectSelect _objectSelector;
    private ObjectFocus _objectFocus;
    private void Awake()
    {
        _objectSelector = GetComponent<ObjectSelect>();
        _objectFocus=GetComponent<ObjectFocus>();
    }
    public static SkillIndicator GetAtScreenPosition(Camera targetCamera, Vector2 screenPosition, LayerMask layerMask)
    {
        Ray ray = targetCamera.ScreenPointToRay(screenPosition);
        RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, layerMask);

        return hit.collider == null ? null : hit.collider.GetComponentInParent<SkillIndicator>();
    }

    public void Select()
    {
        _objectSelector.StartSelect();
        _objectFocus.StartFocus();
    }

    public void Deselect()
    {
        
        _objectSelector.EndSelect();
        _objectFocus.EndFocus();
    }
}
