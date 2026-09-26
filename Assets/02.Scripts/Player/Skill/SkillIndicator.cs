using UnityEditor.SearchService;
using UnityEngine;

public class SkillIndicator : MonoBehaviour
{
    private ObjectSelect _objectSelector;
    private ObjectSquash _objectSquash;
    private ObjectFocus _objectFocus;
    private SkillIndicatorOrbit  _skillIndicatorOrbit;
    private SkillIndicatorMove _skillIndicatorMove;
    
    private bool _isChoiced;
    private void Awake()
    {
        _objectSelector = GetComponent<ObjectSelect>();
        _objectFocus=GetComponent<ObjectFocus>();
        _skillIndicatorOrbit=GetComponent<SkillIndicatorOrbit>();
        _skillIndicatorMove=GetComponent<SkillIndicatorMove>();
    }
    public static SkillIndicator GetAtScreenPosition(Camera targetCamera, Vector2 screenPosition, LayerMask layerMask)
    {
        
        Ray ray = targetCamera.ScreenPointToRay(screenPosition);
        RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(ray, Mathf.Infinity, layerMask);

        SkillIndicator closestIndicator = null;
        float closestDistanceSqr = float.PositiveInfinity;

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null)
                continue;

            SkillIndicator indicator = hit.collider.GetComponentInParent<SkillIndicator>();
            if (indicator == null || indicator.GetIsChoiced())
                continue;

            // A cursor has screen-space coordinates, so compare it with the
            // indicator's projected transform position in the same space.
            Vector2 indicatorScreenPosition = targetCamera.WorldToScreenPoint(indicator.transform.position);
            float distanceSqr = (indicatorScreenPosition - screenPosition).sqrMagnitude;

            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestIndicator = indicator;
            }
        }

        return closestIndicator;
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

    public bool GetIsChoiced()
    {
        return _isChoiced;
    }
    public void ChoicedToAttack()
    {
        _isChoiced = true;
        Deselect();
        if (_objectSquash != null)
            _objectSquash.PlaySquash();
        
        _skillIndicatorOrbit.StopOrbit();
    }

    public void SkillAttackComplete()
    {
        Destroy(this.gameObject);
    }
    
}
