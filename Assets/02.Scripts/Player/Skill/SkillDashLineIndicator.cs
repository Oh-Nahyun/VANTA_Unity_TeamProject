using UnityEngine;
using UnityEngine.Serialization;

public class SkillDashLineIndicator : MonoBehaviour
{
    [SerializeField] private Transform _player;


    [Header("라인 랜더러")]
    [SerializeField] private LineRenderer _selectedLineRenderer;
    [SerializeField] private LineRenderer _previewLineRenderer;

    private int _pointCount;



    private void OnEnable()
    {
        ResetLine();
    }
    private void OnDisable()
    {
        ResetLine();
    }


    public void AddNewPoint(SkillIndicator selectedIndicator)
    {
        if (selectedIndicator == null)
            return;

        // 최초 시작점 = Player
        if (_pointCount == 0)
        {
            _selectedLineRenderer.positionCount = 1;
            _selectedLineRenderer.SetPosition(
                0,
                _player.position
            );

            _pointCount = 1;
        }

        _selectedLineRenderer.positionCount++;

        _selectedLineRenderer.SetPosition(
            _pointCount,
            selectedIndicator.transform.position
        );

        _pointCount++;

        _selectedLineRenderer.enabled = true;

        // 확정됐으니 기존 Preview 제거
        HidePreview();
    }
    public void ShowPreview(SkillIndicator indicator)
    {
        if (indicator == null)
        {
            HidePreview();
            return;
        }

        Vector3 startPosition;

        // 아직 선택한 게 없다면 Player부터
        if (_pointCount == 0)
        {
            startPosition = _player.position;
        }
        else
        {
            // 마지막 확정 Point부터
            startPosition =
                _selectedLineRenderer.GetPosition(_pointCount - 1);
        }

        _previewLineRenderer.positionCount = 2;

        _previewLineRenderer.SetPosition(
            0,
            startPosition
        );

        _previewLineRenderer.SetPosition(
            1,
            indicator.transform.position
        );

        _previewLineRenderer.enabled = true;
    }

    public void HidePreview()
    {
        _previewLineRenderer.positionCount = 0;
        _previewLineRenderer.enabled = false;
    }


    public void ResetLine()
    {
        _pointCount = 0;

        _selectedLineRenderer.positionCount = 0;
        _selectedLineRenderer.enabled = false;
    }
}
