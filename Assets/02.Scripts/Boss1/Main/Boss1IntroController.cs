using System.Collections;
using UnityEngine;

public class Boss1IntroController : MonoBehaviour
{
    [Header("등장 경고 시간")]
    [SerializeField] private float _waringDuration = 2f;
    
    [SerializeField] private SpriteRenderer[] _bossSpriteRenderers;
    [SerializeField] private GameObject _warningZonePrefab;
    [SerializeField] private Animator _animator;

    private Boss1AttackController _boss1AttackController;
    
    private GameObject _warningZone;

    private void Awake()
    {
        SetBossVisible(false);
        _boss1AttackController = GetComponent < Boss1AttackController>();
    }
    
    private void Start()
    {
        StartIntro();
    }

    public void StartIntro()
    {
        StartCoroutine(IntroRoutine());
    }

    private IEnumerator IntroRoutine()
    {
        _warningZone = Instantiate(_warningZonePrefab, transform.position, Quaternion.identity);
        
        yield return new WaitForSeconds(_waringDuration);

        SetBossVisible(true);
        
        _animator.Play("IntroJumpLand", 0, 0f);
        
        
        
        //보스 패턴 시작이라서 제일 아래에 넣어야함
        _boss1AttackController.StartBattle();
    }

    private void SetBossVisible(bool visible)
    {
        foreach (SpriteRenderer spriteRenderer in _bossSpriteRenderers)
        {
            spriteRenderer.forceRenderingOff = !visible;
        }
    }

    public void RemoveWarningZone()
    {
        if (_warningZone == null)
        {
            return;
        }
        
        Destroy(_warningZone);
        _warningZone = null;
    }
}
