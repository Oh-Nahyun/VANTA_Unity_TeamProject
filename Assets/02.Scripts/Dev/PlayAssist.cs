#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 촬영·테스트용 회피 서포터 (치트). 게임 코드를 바꾸지 않고, 보스의 "공격 신호"를 읽어 플레이어의 기존 대시(DashToPoint)로 피한다.
///   - 보스1: 할퀴기·침 히트박스 켜짐, 탄환 접근, 레이저 발사, 점프 착지
///   - 보스2: 공격 박스 켜짐, 검기 접근, 광역기 폭발, 제자리 회전참 시작
///   - 회피 QTE: 플레이어가 입력을 기다리면 성공 처리 (원래 성공 연출 그대로)
///   - 안전망 무적: 놓친 공격에도 맞지 않게 한다
/// 대시는 시작하는 순간 무적이 켜지므로, 판정이 켜진 프레임(애니메이션 갱신 뒤 LateUpdate)에 대시하면 다음 물리 판정 전에 피한다.
/// 설정은 PlayAssistSettings(에디터 리모컨 창이 바꾼다)를 따른다. 정식 빌드에는 들어가지 않는다.
/// </summary>
[DefaultExecutionOrder(10000)]
public class PlayAssist : MonoBehaviour
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

    // 리플렉션 (개발용 툴이라 보스 내부 값을 직접 읽는다. 필드 이름이 바뀌면 여기만 고친다)
    private static readonly FieldInfo Boss2AttackBox = typeof(Boss2AttackBase).GetField("_attackBoxCollider", Flags);
    private static readonly FieldInfo Boss2AttackType = typeof(Boss2AttackPattern).GetField("_currentAttackPatternType", Flags);
    private static readonly FieldInfo Boss2SpinState = typeof(Boss2AttackPattern).GetField("_spinStateName", Flags);
    private static readonly FieldInfo Boss2SpinRadius = typeof(Boss2AttackPattern).GetField("_spinRadius", Flags);
    private static readonly FieldInfo Boss2SpinOffset = typeof(Boss2AttackPattern).GetField("_spinCenterOffset", Flags);
    private static readonly FieldInfo StrikeCollider = typeof(Boss2AreaStrike).GetField("_collider2D", Flags);
    private static readonly FieldInfo StrikeRadius = typeof(Boss2AreaStrike).GetField("_radius", Flags);
    private static readonly FieldInfo ClawCollider = typeof(Boss1ClawHitBox).GetField("_collider", Flags);
    private static readonly FieldInfo SpitCollider = typeof(Boss1SpitHitBox).GetField("_collider", Flags);
    private static readonly FieldInfo LaserLine = typeof(Boss1Laser).GetField("_laserLineRenderer", Flags);
    private static readonly FieldInfo LaserFiring = typeof(Boss1Laser).GetField("_isFiring", Flags);
    private static readonly FieldInfo JumpVisual = typeof(Boss1JumpAttack).GetField("_visualPivot", Flags);
    private static readonly FieldInfo JumpZone = typeof(Boss1JumpAttack).GetField("_jumpWarningZone", Flags);
    private static readonly FieldInfo AvoidWaiting = typeof(PlayerAvoid).GetField("_isAvoidInteraction", Flags);
    private static readonly FieldInfo AvoidDone = typeof(PlayerAvoid).GetField("_isAvoidSucceeded", Flags);
    private static readonly MethodInfo AvoidSuccessMethod = typeof(PlayerAvoid).GetMethod("AvoidSuccess", Flags);
    private static readonly FieldInfo AvoidDuration = typeof(PlayerAvoid).GetField("_avoidDuration", Flags);
    // QA 자동 전투
    private static readonly FieldInfo AttackSlashing = typeof(PlayerAttack).GetField("_isSlashing", Flags);
    private static readonly FieldInfo AttackSprite = typeof(PlayerAttack).GetField("_spriteRenderer", Flags);
    private static readonly FieldInfo AttackDistance = typeof(PlayerAttack).GetField("_attackDistance", Flags);
    private static readonly MethodInfo AttackSlash = typeof(PlayerAttack).GetMethod("Slash", Flags);
    private static readonly FieldInfo FinisherCharging = typeof(PlayerFinisher).GetField("_isCharging", Flags);
    private static readonly MethodInfo FinisherAddCharge = typeof(PlayerFinisher).GetMethod("AddCharge", Flags);
    private static readonly FieldInfo PlayerDashing = typeof(PlayerMove).GetField("_isDashing", Flags);

    // 직전 프레임 상태 (켜지는 "순간"만 신호로 본다)
    private readonly Dictionary<Component, bool> _wasActive = new Dictionary<Component, bool>();
    private readonly Dictionary<Component, Vector2> _lastPosition = new Dictionary<Component, Vector2>();
    private readonly HashSet<Component> _handled = new HashSet<Component>();

    private Player _player;
    private PlayerMove _playerMove;
    private PlayerAvoid _playerAvoid;
    private Rigidbody2D _playerBody;
    private Vector2[] _mapPolygon;
    private float _nextScanTime;
    private bool _invincibleApplied;
    // 회피 QTE 입력 대기가 시작된 시각 (실제 시간). 대기 중이 아니면 음수.
    private float _qteWaitStart = -1f;
    private PlayerAttack _playerAttack;
    private PlayerFinisher _playerFinisher;
    private float _nextApproachTime;
    private float _nextChargeTime;
    private GUIStyle _labelStyle;

    // 주기적으로 다시 찾는 대상들
    private Boss2AttackBase[] _boss2Attacks = Array.Empty<Boss2AttackBase>();
    private Boss2AttackPattern[] _boss2Patterns = Array.Empty<Boss2AttackPattern>();
    private Boss1ClawHitBox[] _claws = Array.Empty<Boss1ClawHitBox>();
    private Boss1SpitHitBox[] _spits = Array.Empty<Boss1SpitHitBox>();
    private Boss1Laser[] _lasers = Array.Empty<Boss1Laser>();
    private Boss1JumpAttack[] _jumps = Array.Empty<Boss1JumpAttack>();

    // 게임이 시작되면 스스로 붙는다. 씬에 저장되지 않고 씬이 바뀌어도 유지된다. (기본은 꺼진 상태로 대기)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Spawn()
    {
        if (FindAnyObjectByType<PlayAssist>() != null)
            return;

        PlayAssistSettings.Load();
        PlayAssistSettings.ResetCount();
        // DontDestroyOnLoad 씬에 두면 플레이가 끝날 때 함께 사라지고, 씬 파일에는 저장되지 않는다.
        GameObject go = new GameObject("[PlayAssist]");
        DontDestroyOnLoad(go);
        go.AddComponent<PlayAssist>();
    }

    private void LateUpdate()
    {
        if (Input.GetKeyDown(PlayAssistSettings.ToggleKey))
        {
            PlayAssistSettings.Enabled = !PlayAssistSettings.Enabled;
        }
        if (Input.GetKeyDown(PlayAssistSettings.OverlayKey))
        {
            PlayAssistSettings.ShowOverlay = !PlayAssistSettings.ShowOverlay;
        }

        if (FindPlayer() == false)
            return;

        UpdateInvincible();
        if (PlayAssistSettings.Enabled == false)
            return;

        if (Time.unscaledTime >= _nextScanTime)
        {
            Rescan();
            _nextScanTime = Time.unscaledTime + 0.5f;
        }

        if (PlayAssistSettings.AutoQte)
        {
            TryAutoQte();
        }
        if (PlayAssistSettings.AutoDodge)
        {
            CheckBoss2();
            CheckBoss1();
            CheckProjectiles();
        }
        if (PlayAssistSettings.AutoBattle)
        {
            AutoBattle();
        }
    }

    // ---------------- QA 자동 전투 ----------------
    // 피니셔 차지 중이면 연타 입력을 대신 넣고, 아니면 가장 가까운 보스에게 대시로 다가가 기존 베기(Slash)를 실행한다.
    private void AutoBattle()
    {
        if (_playerFinisher != null && FinisherCharging != null && (bool)FinisherCharging.GetValue(_playerFinisher))
        {
            if (Time.unscaledTime >= _nextChargeTime)
            {
                FinisherAddCharge?.Invoke(_playerFinisher, null);
                _nextChargeTime = Time.unscaledTime + 0.08f;
            }
            return;
        }

        if (_playerAttack == null || _playerAttack.enabled == false || _playerMove == null || _playerMove.enabled == false)
            return;
        if (PlayerDashing != null && (bool)PlayerDashing.GetValue(_playerMove))
            return;

        Collider2D boss = FindBossCollider();
        if (boss == null)
            return;

        // 보스2가 공격하는 동안에는 파고들지 않는다. (콤보 다음 타에 스스로 맞으러 들어가는 것을 막는다)
        Boss2AttackPattern boss2Attack = boss.GetComponent<Boss2AttackPattern>();
        if (boss2Attack != null && (boss2Attack.IsAttacking || boss2Attack.IsPatternRunning))
            return;

        Vector2 player = _playerBody.position;
        Vector2 bossPoint = boss.bounds.center;
        float side = player.x <= bossPoint.x ? -1f : 1f;
        float reach = (AttackDistance != null ? (float)AttackDistance.GetValue(_playerAttack) : 0.3f) + 0.3f + boss.bounds.extents.x;
        bool inRange = Mathf.Abs(bossPoint.x - player.x) <= reach && Mathf.Abs(bossPoint.y - player.y) <= boss.bounds.extents.y + 0.3f;

        if (inRange == false)
        {
            if (Time.unscaledTime >= _nextApproachTime)
            {
                Vector2 standPoint = bossPoint + new Vector2(side * (boss.bounds.extents.x + 0.4f), 0f);
                _playerMove.DashToPoint(standPoint, null);
                _nextApproachTime = Time.unscaledTime + 0.7f;
            }
            return;
        }

        // 보스 쪽을 바라보게 한 뒤 기존 베기를 실행한다. (판정은 원래대로 애니메이션 이벤트에서)
        SpriteRenderer sprite = AttackSprite?.GetValue(_playerAttack) as SpriteRenderer;
        if (sprite != null)
        {
            sprite.flipX = bossPoint.x < player.x;
        }
        if (AttackSlashing != null && (bool)AttackSlashing.GetValue(_playerAttack) == false)
        {
            AttackSlash?.Invoke(_playerAttack, null);
        }
    }

    // 지금 맞을 수 있는(콜라이더가 켜진) 보스 중 가장 가까운 것
    private Collider2D FindBossCollider()
    {
        Collider2D best = null;
        float bestDistance = float.MaxValue;
        foreach (Boss boss in FindObjectsByType<Boss>(FindObjectsSortMode.None))
        {
            Consider(boss);
        }
        foreach (Boss1Life boss in FindObjectsByType<Boss1Life>(FindObjectsSortMode.None))
        {
            Consider(boss);
        }
        return best;

        void Consider(Component boss)
        {
            Collider2D col = boss.GetComponent<Collider2D>();
            if (col == null || col.enabled == false || boss.gameObject.activeInHierarchy == false)
                return;
            float d = Vector2.Distance(_playerBody.position, col.bounds.center);
            if (d < bestDistance)
            {
                bestDistance = d;
                best = col;
            }
        }
    }

    // ---------------- 보스2 ----------------
    private void CheckBoss2()
    {
        Vector2 player = _playerBody.position;

        // 근접 공격: 공격 박스가 켜지는 순간 플레이어가 가까우면 보스 반대쪽으로
        foreach (Boss2AttackBase attack in _boss2Attacks)
        {
            if (attack == null)
                continue;
            Collider2D box = Boss2AttackBox?.GetValue(attack) as Collider2D;
            if (box == null)
                continue;
            if (BecameActive(box, box.enabled && box.gameObject.activeInHierarchy) && IsNear(box, player, 0.6f))
            {
                Dodge(player - (Vector2)attack.transform.position, "보스2 근접 공격");
            }
        }

        // 제자리 회전참: 공격 애니메이션이 시작되는 순간 회전 반경 밖으로
        foreach (Boss2AttackPattern pattern in _boss2Patterns)
        {
            if (pattern == null)
                continue;
            object type = Boss2AttackType?.GetValue(pattern);
            Animator animator = pattern.GetComponent<Animator>();
            string spinState = Boss2SpinState?.GetValue(pattern) as string;
            bool spinning = type != null && (Boss2AttackPatternType)type == Boss2AttackPatternType.SpinningSlashInPlace
                && animator != null && spinState != null && animator.GetCurrentAnimatorStateInfo(0).IsName(spinState);
            if (BecameActive(pattern, spinning))
            {
                Vector2 center = (Vector2)pattern.transform.position + (Boss2SpinOffset != null ? (Vector2)Boss2SpinOffset.GetValue(pattern) : Vector2.zero);
                float radius = Boss2SpinRadius != null ? (float)Boss2SpinRadius.GetValue(pattern) : 1.5f;
                if (Vector2.Distance(player, center) < radius + 0.6f)
                {
                    Dodge(player - center, "보스2 제자리 회전참", radius + 1f - Vector2.Distance(player, center));
                }
            }
        }

        // 광역기 폭발: 판정이 켜지는 순간 반경 안이면 밖으로
        foreach (Boss2AreaStrike strike in FindObjectsByType<Boss2AreaStrike>(FindObjectsSortMode.None))
        {
            Collider2D col = StrikeCollider?.GetValue(strike) as Collider2D;
            if (col == null)
                continue;
            if (BecameActive(strike, col.enabled))
            {
                float radius = StrikeRadius != null ? (float)StrikeRadius.GetValue(strike) : 1f;
                Vector2 center = strike.transform.position;
                if (Vector2.Distance(player, center) < radius + 0.4f)
                {
                    Dodge(player - center, "보스2 광역기 폭발", radius + 0.8f - Vector2.Distance(player, center));
                }
            }
        }
    }

    // ---------------- 보스1 ----------------
    private void CheckBoss1()
    {
        Vector2 player = _playerBody.position;

        foreach (Boss1ClawHitBox claw in _claws)
        {
            Collider2D col = claw != null ? ClawCollider?.GetValue(claw) as Collider2D : null;
            if (col != null && BecameActive(claw, col.enabled) && IsNear(col, player, 0.6f))
            {
                Dodge(player - (Vector2)col.bounds.center, "보스1 할퀴기");
            }
        }
        foreach (Boss1SpitHitBox spit in _spits)
        {
            Collider2D col = spit != null ? SpitCollider?.GetValue(spit) as Collider2D : null;
            if (col != null && BecameActive(spit, col.enabled) && IsNear(col, player, 0.6f))
            {
                Dodge(player - (Vector2)col.bounds.center, "보스1 침");
            }
        }

        // 레이저: 발사 중 레이저 선 가까이 있으면 선에서 수직으로
        foreach (Boss1Laser laser in _lasers)
        {
            if (laser == null)
                continue;
            LineRenderer line = LaserLine?.GetValue(laser) as LineRenderer;
            bool firing = LaserFiring != null && (bool)LaserFiring.GetValue(laser) && line != null && line.enabled;
            if (firing == false || line.positionCount < 2)
                continue;
            Vector2 a = line.GetPosition(0), b = line.GetPosition(1);
            Vector2 closest = ClosestPointOnSegment(a, b, player);
            if (Vector2.Distance(player, closest) < 1.0f)
            {
                Vector2 away = player - closest;
                if (away.sqrMagnitude < 0.0001f)
                {
                    Vector2 dir = (b - a).normalized;
                    away = new Vector2(-dir.y, dir.x);
                }
                Dodge(away, "보스1 레이저");
            }
        }

        // 점프 내려찍기: 숨었던 보스 그림이 다시 나타나는(착지 시작) 순간 착지 지점에서 멀어진다
        foreach (Boss1JumpAttack jump in _jumps)
        {
            if (jump == null)
                continue;
            Transform visual = JumpVisual?.GetValue(jump) as Transform;
            GameObject zone = JumpZone?.GetValue(jump) as GameObject;
            bool landing = visual != null && visual.gameObject.activeSelf && zone != null;
            if (BecameActive(jump, landing))
            {
                Vector2 center = zone.transform.position;
                Collider2D zoneCol = zone.GetComponent<Collider2D>();
                float reach = zoneCol != null ? Mathf.Max(zoneCol.bounds.extents.x, zoneCol.bounds.extents.y) : 2f;
                if (Vector2.Distance(player, center) < reach + 0.5f)
                {
                    Dodge(player - center, "보스1 점프 착지", reach + 1f - Vector2.Distance(player, center));
                }
            }
        }
    }

    // ---------------- 탄환·검기 ----------------
    private void CheckProjectiles()
    {
        Vector2 player = _playerBody.position;
        CheckMoving(FindObjectsByType<Boss1Bullet>(FindObjectsSortMode.None), player, "보스1 탄환");
        CheckMoving(FindObjectsByType<Boss1SplitBullet>(FindObjectsSortMode.None), player, "보스1 분열탄");
        CheckMoving(FindObjectsByType<ProjectileBase>(FindObjectsSortMode.None), player, "보스2 검기");
    }

    // 프레임 사이 이동량으로 속도를 추정해, 곧(_projectileLead 초 안) 플레이어 가까이 지나갈 탄이면 진행 방향의 옆으로 피한다.
    private void CheckMoving<T>(T[] items, Vector2 player, string label) where T : Component
    {
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        foreach (T item in items)
        {
            Vector2 pos = item.transform.position;
            bool hasLast = _lastPosition.TryGetValue(item, out Vector2 last);
            _lastPosition[item] = pos;
            if (hasLast == false || _handled.Contains(item))
                continue;

            Vector2 velocity = (pos - last) / dt;
            float speed = velocity.magnitude;
            if (speed < 0.5f)
                continue;

            Vector2 dir = velocity / speed;
            Vector2 toPlayer = player - pos;
            float along = Vector2.Dot(toPlayer, dir);
            if (along < 0f)
                continue;
            float timeToReach = along / speed;
            float side = Mathf.Abs(dir.x * toPlayer.y - dir.y * toPlayer.x);
            float hitWidth = GetHalfSize(item) + 0.45f;
            if (timeToReach <= PlayAssistSettings.ProjectileLead && side < hitWidth)
            {
                Vector2 perpendicular = new Vector2(-dir.y, dir.x);
                if (Vector2.Dot(perpendicular, toPlayer) < 0f)
                {
                    perpendicular = -perpendicular;
                }
                if (Dodge(perpendicular, label))
                {
                    _handled.Add(item);
                }
            }
        }
    }

    // ---------------- 회피 QTE ----------------
    // 바로 성공하면 어색하므로, 입력 제한 시간(_avoidDuration)의 QteTiming 비율이 지났을 때(시간이 임박했을 때) 성공 처리한다.
    private void TryAutoQte()
    {
        if (_playerAvoid == null || AvoidWaiting == null || AvoidSuccessMethod == null)
            return;
        bool waiting = (bool)AvoidWaiting.GetValue(_playerAvoid);
        bool done = AvoidDone != null && (bool)AvoidDone.GetValue(_playerAvoid);
        if (waiting == false || done)
        {
            _qteWaitStart = -1f;
            return;
        }

        if (_qteWaitStart < 0f)
        {
            _qteWaitStart = Time.unscaledTime;
        }
        float limit = AvoidDuration != null ? (float)AvoidDuration.GetValue(_playerAvoid) : 2f;
        if (Time.unscaledTime - _qteWaitStart >= limit * PlayAssistSettings.QteTiming)
        {
            AvoidSuccessMethod.Invoke(_playerAvoid, null);
            PlayAssistSettings.Report($"회피 QTE 자동 성공 ({Time.unscaledTime - _qteWaitStart:F1}초 / 제한 {limit:F1}초)");
            _qteWaitStart = -1f;
        }
    }

    // ---------------- 안전망 무적 ----------------
    private void UpdateInvincible()
    {
        bool want = PlayAssistSettings.Enabled && PlayAssistSettings.Invincible;
        if (want)
        {
            _player.OffPlayerHittable();
            _invincibleApplied = true;
        }
        else if (_invincibleApplied)
        {
            _player.OnPlayerHittable();
            _invincibleApplied = false;
        }
    }

    private void OnDisable()
    {
        if (_invincibleApplied && _player != null)
        {
            _player.OnPlayerHittable();
            _invincibleApplied = false;
        }
    }

    // ---------------- 대시 ----------------
    // preferred 방향(보통 공격에서 멀어지는 쪽)을 먼저, 막히면 ±45°·±90°·뒤쪽으로 맵 안에 들어오는 자리를 찾아 기존 대시로 간다.
    private bool Dodge(Vector2 preferred, string label, float minDistance = 0f)
    {
        if (_playerMove == null || _playerMove.enabled == false)
            return false;
        if (PlayerDashing != null && (bool)PlayerDashing.GetValue(_playerMove))
            return false;

        if (preferred.sqrMagnitude < 0.0001f)
        {
            preferred = Vector2.up;
        }
        preferred.Normalize();
        float distance = Mathf.Max(PlayAssistSettings.DodgeDistance, minDistance);
        Vector2 from = _playerBody.position;

        float[] angles = { 0f, 45f, -45f, 90f, -90f, 135f, -135f, 180f };
        foreach (float angle in angles)
        {
            Vector2 dir = Quaternion.Euler(0f, 0f, angle) * preferred;
            Vector2 to = from + dir * distance;
            if (IsInsideMap(to))
            {
                _playerMove.DashToPoint(to, null);
                PlayAssistSettings.Report(label);
                return true;
            }
        }
        _playerMove.DashToPoint(from + preferred * distance, null);
        PlayAssistSettings.Report(label + " (맵 끝)");
        return true;
    }

    // ---------------- 도움 함수 ----------------
    // 이번 프레임에 꺼짐 → 켜짐으로 바뀌었는지
    private bool BecameActive(Component key, bool active)
    {
        _wasActive.TryGetValue(key, out bool was);
        _wasActive[key] = active;
        return active && was == false;
    }

    private bool IsNear(Collider2D col, Vector2 point, float margin)
    {
        Bounds b = col.bounds;
        b.Expand(new Vector3(margin * 2f, margin * 2f, 0f));
        return b.Contains(new Vector3(point.x, point.y, b.center.z));
    }

    private static float GetHalfSize(Component item)
    {
        Collider2D col = item.GetComponentInChildren<Collider2D>();
        if (col == null)
            return 0.2f;
        return Mathf.Max(col.bounds.extents.x, col.bounds.extents.y);
    }

    private static Vector2 ClosestPointOnSegment(Vector2 a, Vector2 b, Vector2 p)
    {
        Vector2 ab = b - a;
        float t = ab.sqrMagnitude > 0.0001f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
        return a + ab * t;
    }

    private bool IsInsideMap(Vector2 point)
    {
        if (_mapPolygon == null || _mapPolygon.Length < 3)
            return true;

        bool inside = false;
        for (int i = 0, j = _mapPolygon.Length - 1; i < _mapPolygon.Length; j = i++)
        {
            Vector2 a = _mapPolygon[i], b = _mapPolygon[j];
            if ((a.y > point.y) != (b.y > point.y) && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
            {
                inside = !inside;
            }
        }
        return inside;
    }

    private bool FindPlayer()
    {
        if (_player == null)
        {
            _player = FindAnyObjectByType<Player>();
            if (_player == null)
                return false;
            _playerMove = _player.GetComponent<PlayerMove>();
            _playerAvoid = _player.GetComponent<PlayerAvoid>();
            _playerAttack = _player.GetComponent<PlayerAttack>();
            _playerFinisher = _player.GetComponent<PlayerFinisher>();
            _playerBody = _player.GetComponent<Rigidbody2D>();
        }
        return _playerBody != null;
    }

    private void Rescan()
    {
        _boss2Attacks = FindObjectsByType<Boss2AttackBase>(FindObjectsSortMode.None);
        _boss2Patterns = FindObjectsByType<Boss2AttackPattern>(FindObjectsSortMode.None);
        _claws = FindObjectsByType<Boss1ClawHitBox>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        _spits = FindObjectsByType<Boss1SpitHitBox>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        _lasers = FindObjectsByType<Boss1Laser>(FindObjectsSortMode.None);
        _jumps = FindObjectsByType<Boss1JumpAttack>(FindObjectsSortMode.None);

        if (_mapPolygon == null)
        {
            EdgeColliderGenerator wall = FindAnyObjectByType<EdgeColliderGenerator>();
            EdgeCollider2D edge = wall != null ? wall.GetComponent<EdgeCollider2D>() : null;
            if (edge != null && edge.pointCount >= 3)
            {
                _mapPolygon = new Vector2[edge.pointCount];
                for (int i = 0; i < edge.pointCount; i++)
                {
                    _mapPolygon[i] = edge.transform.TransformPoint(edge.points[i] + edge.offset);
                }
            }
        }

        // 사라진 대상 기록 정리
        _handled.RemoveWhere(c => c == null);
        foreach (Component dead in new List<Component>(_lastPosition.Keys)) if (dead == null) _lastPosition.Remove(dead);
        foreach (Component dead in new List<Component>(_wasActive.Keys)) if (dead == null) _wasActive.Remove(dead);
    }

    private void OnGUI()
    {
        if (PlayAssistSettings.ShowOverlay == false)
            return;
        if (_labelStyle == null)
        {
            _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            _labelStyle.normal.textColor = Color.yellow;
        }
        string state = PlayAssistSettings.Enabled
            ? $"서포터 ON  [회피 {(PlayAssistSettings.AutoDodge ? "O" : "X")} / QTE {(PlayAssistSettings.AutoQte ? "O" : "X")} / 무적 {(PlayAssistSettings.Invincible ? "O" : "X")}]"
            : "서포터 OFF";
        GUI.Label(new Rect(10, 10, 600, 22), $"{state}   ({PlayAssistSettings.ToggleKey}: 켜기/끄기, {PlayAssistSettings.OverlayKey}: 표시 숨김)", _labelStyle);
        if (PlayAssistSettings.Enabled && string.IsNullOrEmpty(PlayAssistSettings.LastReport) == false)
        {
            GUI.Label(new Rect(10, 30, 600, 22), "최근: " + PlayAssistSettings.LastReport, _labelStyle);
        }
    }
}

/// <summary>회피 서포터 설정. 리모컨 창(에디터)과 단축키가 바꾸고, PlayAssist 가 읽는다.</summary>
public static class PlayAssistSettings
{
    public static bool Enabled;
    public static bool AutoDodge = true;
    public static bool AutoQte = true;
    public static bool Invincible;
    // QA 용: 보스에게 다가가 기존 베기를 실행하고, 피니셔 차지 연타를 대신 넣는다.
    public static bool AutoBattle;
    public static bool ShowOverlay = true;
    public static float DodgeDistance = 2.5f;
    public static float ProjectileLead = 0.15f;
    // 회피 QTE 를 입력 제한 시간의 몇 % 가 지났을 때 성공시킬지 (0.8 = 2초 중 1.6초)
    public static float QteTiming = 0.8f;
    public static KeyCode ToggleKey = KeyCode.F8;
    public static KeyCode OverlayKey = KeyCode.F9;

    public static string LastReport { get; private set; }
    public static int DodgeCount { get; private set; }
    public static event Action Changed;

    public static void Report(string message)
    {
        DodgeCount++;
        LastReport = $"{message} ({DodgeCount})";
        Changed?.Invoke();
    }

    public static void ResetCount()
    {
        DodgeCount = 0;
        LastReport = null;
        Changed?.Invoke();
    }

    private const string Prefix = "PlayAssist.";

    // 리모컨 창이 저장한 설정을 읽는다. (에디터에서만. 개발 빌드는 기본값 + 단축키로 켠다)
    public static void Load()
    {
#if UNITY_EDITOR
        Enabled = UnityEditor.EditorPrefs.GetBool(Prefix + "Enabled", false);
        AutoDodge = UnityEditor.EditorPrefs.GetBool(Prefix + "AutoDodge", true);
        AutoQte = UnityEditor.EditorPrefs.GetBool(Prefix + "AutoQte", true);
        Invincible = UnityEditor.EditorPrefs.GetBool(Prefix + "Invincible", false);
        AutoBattle = UnityEditor.EditorPrefs.GetBool(Prefix + "AutoBattle", false);
        ShowOverlay = UnityEditor.EditorPrefs.GetBool(Prefix + "ShowOverlay", true);
        DodgeDistance = UnityEditor.EditorPrefs.GetFloat(Prefix + "DodgeDistance", 2.5f);
        ProjectileLead = UnityEditor.EditorPrefs.GetFloat(Prefix + "ProjectileLead", 0.15f);
        QteTiming = UnityEditor.EditorPrefs.GetFloat(Prefix + "QteTiming", 0.8f);
#endif
    }

    public static void Save()
    {
#if UNITY_EDITOR
        UnityEditor.EditorPrefs.SetBool(Prefix + "Enabled", Enabled);
        UnityEditor.EditorPrefs.SetBool(Prefix + "AutoDodge", AutoDodge);
        UnityEditor.EditorPrefs.SetBool(Prefix + "AutoQte", AutoQte);
        UnityEditor.EditorPrefs.SetBool(Prefix + "Invincible", Invincible);
        UnityEditor.EditorPrefs.SetBool(Prefix + "AutoBattle", AutoBattle);
        UnityEditor.EditorPrefs.SetBool(Prefix + "ShowOverlay", ShowOverlay);
        UnityEditor.EditorPrefs.SetFloat(Prefix + "DodgeDistance", DodgeDistance);
        UnityEditor.EditorPrefs.SetFloat(Prefix + "ProjectileLead", ProjectileLead);
        UnityEditor.EditorPrefs.SetFloat(Prefix + "QteTiming", QteTiming);
#endif
    }
}
#endif
