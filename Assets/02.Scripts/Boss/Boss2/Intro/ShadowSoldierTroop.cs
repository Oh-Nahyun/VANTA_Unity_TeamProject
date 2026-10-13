using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 그림자 왕좌가 나타날 때 그림자 병사 4~5명을 무작위로 소환하고, 왕좌가 흩어질 때 함께 흩어지게 한다.
/// 위치는 보스와 화면 가운데의 중간쯤을 중심으로 한 고리(일정 거리) 위에서 무작위로 고르고, 화면 밖으로 나가지 않게 맞춘다.
/// ShadowThrone 과 같은 오브젝트에 붙이고 ShadowThrone 의 Troop 칸에 연결한다. 시간은 실제 시간(timeScale 무시)으로 센다.
/// </summary>
public class ShadowSoldierTroop : MonoBehaviour
{
    [Header("병사")]
    [Tooltip("소환할 병사 프리팹. 한 번에 서로 다른 병사를 고른다")]
    [SerializeField] private ShadowSoldier[] _soldierPrefabs;
    [SerializeField] private int _minCount = 4;
    [SerializeField] private int _maxCount = 5;
    [Tooltip("병사 크기 배율 (스프라이트는 보스2와 같은 PPU 18 로 잘려 있다)")]
    [SerializeField] private float _scale = 1f;

    [Header("배치 (원형)")]
    [Tooltip("고리 중심 = 보스 → 화면 가운데 사이 비율 (0 = 보스, 0.5 = 중간, 1 = 화면 가운데)")]
    [Range(0f, 1f)]
    [SerializeField] private float _centerBlend = 0.5f;
    [Tooltip("중심에서 이 거리 범위 안(고리)에 무작위로 세운다")]
    [SerializeField] private float _minRadius = 2.2f;
    [SerializeField] private float _maxRadius = 3f;
    [Tooltip("보스와 이 거리 안쪽에는 세우지 않는다 (보스를 가리지 않게)")]
    [SerializeField] private float _innerDistance = 1.3f;
    [Tooltip("병사끼리 최소 간격")]
    [SerializeField] private float _minSpacing = 0.9f;
    [Tooltip("화면 가장자리에서 이만큼 안쪽까지만 세운다 (병사 그림 전체가 화면에 들어오게)")]
    [SerializeField] private float _screenMargin = 0.2f;

    [Header("타이밍")]
    [Tooltip("왕좌가 나타나기 시작한 뒤 병사들이 나타나기 시작할 때까지 (초)")]
    [SerializeField] private float _appearStartDelay = 0.4f;
    [Tooltip("병사마다 나타나는 시간을 이 범위 안에서 엇갈리게 한다 (초)")]
    [SerializeField] private float _appearStagger = 0.7f;
    [Tooltip("흩어질 때 병사마다 엇갈리는 시간 (초)")]
    [SerializeField] private float _vanishStagger = 0.35f;

    [Header("정렬")]
    [SerializeField] private string _sortingLayer = "VfxBack";
    [Tooltip("왕좌(0)보다 앞에 그린다. 아래쪽에 선 병사일수록 앞에 온다")]
    [SerializeField] private int _sortingOrder = 1;

    private readonly List<ShadowSoldier> _soldiers = new List<ShadowSoldier>();
    private readonly List<Coroutine> _routines = new List<Coroutine>();

    /// <summary>보스와 화면 가운데의 중간쯤을 중심으로 한 고리 위에 병사들을 엇갈려 소환한다.</summary>
    public void Summon(Transform boss)
    {
        Clear();
        if (boss == null || _soldierPrefabs == null || _soldierPrefabs.Length == 0)
            return;

        List<ShadowSoldier> picks = PickSoldiers(Random.Range(_minCount, _maxCount + 1));
        List<float> delays = new List<float>();
        for (int i = 0; i < picks.Count; i++)
        {
            delays.Add(_appearStartDelay + Random.Range(0f, _appearStagger));
        }
        delays.Sort();
        _routines.Add(StartCoroutine(SummonRoutine(boss, picks, delays)));
    }

    /// <summary>병사들을 엇갈려 흩어지게 하고, 다 흩어지면 지운다.</summary>
    public void Dismiss()
    {
        StopRoutines();
        foreach (ShadowSoldier soldier in _soldiers)
        {
            if (soldier == null)
                continue;
            _routines.Add(StartCoroutine(VanishAfter(soldier, Random.Range(0f, _vanishStagger))));
        }
        _soldiers.Clear();
    }

    /// <summary>연출 없이 바로 지운다. (테스트 재시작·연출 중단)</summary>
    public void Clear()
    {
        StopRoutines();
        foreach (ShadowSoldier soldier in _soldiers)
        {
            if (soldier != null)
            {
                Destroy(soldier.gameObject);
            }
        }
        _soldiers.Clear();
    }

    // 병사가 나타나는 순간마다 위치를 고른다. (화면 범위는 카메라가 당겨진 뒤의 구도 기준)
    private IEnumerator SummonRoutine(Transform boss, List<ShadowSoldier> picks, List<float> delays)
    {
        bool faceLeft = IsPlayerOnLeft(boss);
        List<Vector2> placed = new List<Vector2>();
        float elapsed = 0f;
        for (int i = 0; i < picks.Count; i++)
        {
            yield return new WaitForSecondsRealtime(delays[i] - elapsed);
            elapsed = delays[i];
            if (boss == null)
                yield break;

            if (TryPickPosition(boss, picks[i], placed, out Vector2 position) == false)
                continue;
            placed.Add(position);

            ShadowSoldier soldier = Instantiate(picks[i], new Vector3(position.x, position.y, boss.position.z), Quaternion.identity);
            soldier.transform.localScale = new Vector3(_scale, _scale, 1f);
            _soldiers.Add(soldier);

            // 아래쪽(앞쪽)에 선 병사가 위로 그려지도록 발 높이로 순서를 정한다.
            int order = _sortingOrder + Mathf.Max(0, Mathf.RoundToInt((boss.position.y + 10f - position.y) * 10f));
            soldier.Appear(faceLeft, _sortingLayer, order);
        }
    }

    private IEnumerator VanishAfter(ShadowSoldier soldier, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (soldier == null)
            yield break;

        // 아직 나타나기 전이면 Vanish 가 바로 끝나므로 그대로 지워진다.
        soldier.Vanish(() =>
        {
            if (soldier != null)
            {
                Destroy(soldier.gameObject);
            }
        });
    }

    // 서로 다른 병사를 count 명 고른다. 종류가 모자라면 다시 섞어서 채운다.
    private List<ShadowSoldier> PickSoldiers(int count)
    {
        List<ShadowSoldier> pool = new List<ShadowSoldier>();
        List<ShadowSoldier> picks = new List<ShadowSoldier>();
        while (picks.Count < count)
        {
            if (pool.Count == 0)
            {
                foreach (ShadowSoldier prefab in _soldierPrefabs)
                {
                    if (prefab != null)
                    {
                        pool.Add(prefab);
                    }
                }
                if (pool.Count == 0)
                    break;
            }
            int index = Random.Range(0, pool.Count);
            picks.Add(pool[index]);
            pool.RemoveAt(index);
        }
        return picks;
    }

    // 고리(중심에서 [minRadius, maxRadius]) 위의 무작위 점을 화면 안으로 맞춘 뒤, 보스·다른 병사와 떨어져 있으면 쓴다.
    private bool TryPickPosition(Transform boss, ShadowSoldier prefab, List<Vector2> placed, out Vector2 position)
    {
        Vector2 bossCenter = GetVisibleCenter(boss);
        Camera camera = Camera.main;
        bool hasView = camera != null && camera.orthographic;
        Rect view = hasView ? GetViewRect(camera) : new Rect(bossCenter, Vector2.zero);
        Vector2 center = Vector2.Lerp(bossCenter, view.center, _centerBlend);
        GetSpriteExtents(prefab, out Vector2 spriteMin, out Vector2 spriteMax);

        // 병사 그림 전체가 화면 안에 들어오는 피벗 위치 범위
        float minX = view.xMin + _screenMargin - spriteMin.x;
        float maxX = view.xMax - _screenMargin - spriteMax.x;
        float minY = view.yMin + _screenMargin - spriteMin.y;
        float maxY = view.yMax - _screenMargin - spriteMax.y;

        for (int attempt = 0; attempt < 30; attempt++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(_minRadius, _maxRadius);
            if (hasView)
            {
                position.x = minX <= maxX ? Mathf.Clamp(position.x, minX, maxX) : view.center.x;
                position.y = minY <= maxY ? Mathf.Clamp(position.y, minY, maxY) : view.center.y;
            }

            if (Vector2.Distance(position, bossCenter) < _innerDistance)
                continue;

            bool tooClose = false;
            foreach (Vector2 other in placed)
            {
                if (Vector2.Distance(other, position) < _minSpacing)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose == false)
                return true;
        }
        position = Vector2.zero;
        return false;
    }

    // 직교 카메라가 비추는 월드 범위.
    // 얼굴 클로즈업에서 뒤로 당겨지는 블렌드 중이면 지금 화면이 아니라 블렌드가 끝났을 때(들어오는 카메라)의 화면을 쓴다.
    private static Rect GetViewRect(Camera camera)
    {
        float halfHeight = camera.orthographicSize;
        Vector2 c = camera.transform.position;

        CinemachineBrain brain = camera.GetComponent<CinemachineBrain>();
        if (brain != null)
        {
            ICinemachineCamera target = brain.ActiveBlend != null ? brain.ActiveBlend.CamB : brain.ActiveVirtualCamera;
            if (target != null)
            {
                CameraState state = target.State;
                halfHeight = state.Lens.OrthographicSize;
                c = state.GetFinalPosition();
            }
        }

        float halfWidth = halfHeight * camera.aspect;
        return new Rect(c.x - halfWidth, c.y - halfHeight, halfWidth * 2f, halfHeight * 2f);
    }

    // 보스의 지금 보이는 그림 가운데
    private static Vector2 GetVisibleCenter(Transform boss)
    {
        SpriteRenderer body = boss.GetComponent<SpriteRenderer>();
        if (body == null || body.sprite == null)
            return boss.position;
        return body.bounds.center;
    }

    // 병사 그림에서 실제로 보이는 부분의 피벗 기준 범위 (크기 배율 적용, 투명 여백 제외)
    private void GetSpriteExtents(ShadowSoldier prefab, out Vector2 min, out Vector2 max)
    {
        if (prefab.TryGetVisibleExtents(out min, out max) == false)
        {
            min = Vector2.zero;
            max = Vector2.zero;
            return;
        }
        min *= _scale;
        max *= _scale;
    }

    // 병사들은 플레이어 쪽을 바라본다. (스프라이트 원본은 오른쪽을 본다)
    private static bool IsPlayerOnLeft(Transform boss)
    {
        Player player = FindAnyObjectByType<Player>();
        return player != null && player.transform.position.x < boss.position.x;
    }

    private void StopRoutines()
    {
        foreach (Coroutine routine in _routines)
        {
            if (routine != null)
            {
                StopCoroutine(routine);
            }
        }
        _routines.Clear();
    }

    private void OnDisable()
    {
        Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.55f, 0.27f, 0.82f);
        DrawCircle(transform.position, _minRadius);
        DrawCircle(transform.position, _maxRadius);
    }

    private static void DrawCircle(Vector3 center, float radius)
    {
        const int segments = 48;
        Vector3 prev = center + Vector3.right * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 next = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
