using UnityEngine;

public class ObjectFocus : MonoBehaviour
{
    private static readonly int SelectCenterID =
        Shader.PropertyToID("_SelectCenter");

    private static readonly int SelectedRadiusID =
        Shader.PropertyToID("_SelectedRadius");

    [Header("세팅")]
    [SerializeField] private float _selectedRadius = 0.2f;

    [Header("Boss1 세팅")]
    [SerializeField]
    private Vector2 _boss1FocusOffset =
        new Vector2(0f, 1f);

    private Camera _targetCamera;

    private bool _isFocused;
    private bool _isBoss1;

    private void Awake()
    {

        _targetCamera = Camera.main;

        _isBoss1 =
            TryGetComponent<Boss1Life>(out _);

    
    }

    private void LateUpdate()
    {
        if (!_isFocused)
            return;

        UpdateFocusPosition();
    }

    public void StartFocus()
    {
        Debug.Log(
            $"[ObjectFocus] StartFocus : {gameObject.name}"
        );

        _isFocused = true;

        Shader.SetGlobalFloat(
            SelectedRadiusID,
            _selectedRadius
        );

        UpdateFocusPosition();

        Debug.Log(
            $"[ObjectFocus] Radius : " +
            $"{Shader.GetGlobalFloat(SelectedRadiusID)}"
        );
        
        Debug.Log(                                                                                                                                                     
            $"Boss SelectCenter: {Shader.GetGlobalVector("_SelectCenter")}, " +                                                                                        
            $"Boss SelectedRadius: {Shader.GetGlobalFloat("_SelectedRadius")}"                                                                                         
        );         
    }

    public void EndFocus()
    {
        Debug.Log(
            $"[ObjectFocus] EndFocus : {gameObject.name}"
        );

        _isFocused = false;

        Shader.SetGlobalFloat(
            SelectedRadiusID,
            0f
        );
    }

    private void UpdateFocusPosition()
    {
          
        if (_targetCamera == null)
        {
            Debug.LogError(
                "[ObjectFocus] Camera가 null입니다."
            );

            return;
        }

        Vector3 focusPosition =
            transform.position;

        if (_isBoss1)
        {
            focusPosition += new Vector3(
                _boss1FocusOffset.x,
                _boss1FocusOffset.y,
                0f
            );
        }

        Vector3 viewportPosition =
            _targetCamera.WorldToViewportPoint(
                focusPosition
            );
                                                                                                                                                                 
                                                                                                                                                                       
          
        Shader.SetGlobalVector(                                                                                                                                        
            SelectCenterID,                                                                                                                                            
            new Vector4(viewportPosition.x, viewportPosition.y, 0f, 0f)                                                                                                
        );                        
        Shader.SetGlobalFloat(SelectedRadiusID, _selectedRadius);                                                                                                                
  
        
    }

    private void OnDisable()
    {
        if (_isFocused)
            EndFocus();
    }

    private void OnDestroy()
    {
        if (_isFocused)
        {
            Shader.SetGlobalFloat(
                SelectedRadiusID,
                0f
            );
        }
    }
}
