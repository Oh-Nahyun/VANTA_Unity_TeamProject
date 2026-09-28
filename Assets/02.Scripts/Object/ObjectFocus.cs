using UnityEngine;

public class ObjectFocus : MonoBehaviour
{


    private static readonly int SelectCenterID =
        Shader.PropertyToID("_SelectCenter");

    private static readonly int SelectedRadiusID =
        Shader.PropertyToID("_SelectedRadius");
    [Header("세팅")]
    [SerializeField] private float _selectedRadius = 0.04f;
    [Header("참조")]
    [SerializeField] private Material _focusMaterial;
    [SerializeField] private Camera _targetCamera;

    private bool _isFocused;


    private void Awake()
    {
        if (_targetCamera == null)
            _targetCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (!_isFocused)
            return;

        UpdateFocusPosition();
    }




    public void StartFocus()
    {
        _isFocused = true;

        UpdateFocusPosition();

        _focusMaterial.SetFloat(
            SelectedRadiusID,
            _selectedRadius
        );
    }

    public void EndFocus()
    {
        _isFocused = false;

        _focusMaterial.SetFloat(
            SelectedRadiusID,
            0f
        );
    }

    private void UpdateFocusPosition()
    {
        Vector3 viewportPosition =
            _targetCamera.WorldToViewportPoint(transform.position);

        _focusMaterial.SetVector(
            SelectCenterID,
            new Vector4(
                viewportPosition.x,
                viewportPosition.y,
                0f,
                0f
            )
        );
    }

}
