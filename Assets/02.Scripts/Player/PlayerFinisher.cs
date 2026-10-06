using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerFinisher : MonoBehaviour
{
    
    [SerializeField]private SkillManager _skillManager;
    [SerializeField]private Animator _animator;

    private void Update()
    {

        if (Input.GetButtonDown("Fire1"))
        {
            StartFinisher();
        }
    }

    public void StartFinisher()
    {
        GetComponent<PlayerMove>().enabled = false;
        GetComponent<PlayerAttack>().enabled = false;
        _skillManager.enabled = false;
        
        _animator.SetTrigger("StartFinisher");
        
        HitableObject hitableObject = FindAnyObjectByType<HitableObject>();
        ObjectFocus objectFocus = hitableObject.GetComponent<ObjectFocus>();

        if (objectFocus == null)
        {
            
            objectFocus = hitableObject.gameObject.AddComponent<ObjectFocus>();

        }

        objectFocus.StartFocus();
        
        hitableObject.GetComponent<IBossMovable>().MoveBlock();
        
        CameraFocusingController.Instance.Focus(hitableObject.transform,1.2f);
    }
}
