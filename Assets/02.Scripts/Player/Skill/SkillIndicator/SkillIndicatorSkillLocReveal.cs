using UnityEngine;

public class SkillIndicatorSkillLocReveal : MonoBehaviour
{
   
    [Header("펫 번호 (0 ~ 4)")]
    [SerializeField] private int _petIndex;

    private static readonly int[] PetStartIDs =
    {
        Shader.PropertyToID("_PetStart1"),
        Shader.PropertyToID("_PetStart2"),
        Shader.PropertyToID("_PetStart3"),
        Shader.PropertyToID("_PetStart4"),
        Shader.PropertyToID("_PetStart5")
    };

    private static readonly int[] PetProgressIDs =
    {
        Shader.PropertyToID("_PetProgress1"),
        Shader.PropertyToID("_PetProgress2"),
        Shader.PropertyToID("_PetProgress3"),
        Shader.PropertyToID("_PetProgress4"),
        Shader.PropertyToID("_PetProgress5")
    };

    private static readonly int[] PetActiveIDs =
    {
        Shader.PropertyToID("_PetActive1"),
        Shader.PropertyToID("_PetActive2"),
        Shader.PropertyToID("_PetActive3"),
        Shader.PropertyToID("_PetActive4"),
        Shader.PropertyToID("_PetActive5")
    };


    private float _previousAngle;
    private float _progress;

    [SerializeField]private bool _isRevealing;


  

    public void SetPetNumber(int number)
    {
        _petIndex = number;
    }

    private void Update()
    {
        if (!_isRevealing)
            return;

        UpdateReveal();
    }


    public void StartReveal()
    {
        

        if (_petIndex < 0 || _petIndex >= 5)
        {
            Debug.LogWarning($"{name} : Pet Index가 잘못되었습니다.");
            return;
        }

        float startAngle = GetCurrentAngle();

        _previousAngle = startAngle;
        _progress = 0f;

        Shader.SetGlobalFloat(
            PetStartIDs[_petIndex],
            startAngle
        );

        Shader.SetGlobalFloat(
            PetProgressIDs[_petIndex],
            0f
        );

        Shader.SetGlobalFloat(
            PetActiveIDs[_petIndex],
            1f
        );

        _isRevealing = true;
    }


    private void UpdateReveal()
    {
        float currentAngle = GetCurrentAngle();

        // 이전 프레임과 현재 프레임 사이의
        // 가장 짧은 각도 차이 (-0.5 ~ +0.5)
        float delta =
            Mathf.DeltaAngle(
                _previousAngle * 360f,
                currentAngle * 360f
            ) / 360f;

        /*
         * 현재 펫이 시계/반시계 중 어느 방향으로
         * 회전하든 이동한 거리만 누적.
         *
         * 한 방향으로만 돈다면 Abs가 가장 간단함.
         */
        _progress += Mathf.Abs(delta);

        _progress = Mathf.Clamp01(_progress);

        Shader.SetGlobalFloat(
            PetProgressIDs[_petIndex],
            _progress
        );
        Debug.Log(
            $"Pet {_petIndex} | Progress {_progress}"
        );
        _previousAngle = currentAngle;
    }


    private float GetCurrentAngle()
    {
        HitableObject boss = FindAnyObjectByType<HitableObject>();
        Vector2 direction =
            (Vector2)transform.position -
            (Vector2)boss.transform.position;

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            );

        // -PI ~ PI
        // ↓
        // 0 ~ 1
        float angle01 =
            angle / (Mathf.PI * 2f);

        if (angle01 < 0f)
            angle01 += 1f;

        return angle01;
    }


    private void OnDisable()
    {
        _isRevealing = false;
    }
}
