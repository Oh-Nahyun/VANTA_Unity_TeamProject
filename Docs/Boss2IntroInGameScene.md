# 보스2 등장 시네마틱 — InGameScene 적용 설명서

TestInGameScene(`Assets/01.Scenes/Boss2/TestInGameScene.unity`)에서 만든 **보스2 등장 연출**을
본 게임 씬 InGameScene(`Assets/01.Scenes/Main/InGameScene.unity`)에 옮기는 방법. 연출 관련 내용만 다룬다.

---

## 0. 먼저 알아둘 것

### 이미 프로젝트 전체에 적용된 것 (InGameScene 에서 따로 할 일 없음)
| 항목 | 내용 |
|---|---|
| Boss2 프리팹 `_runOnStart = false` | 보스2가 켜져도 FSM(공격)이 자동으로 시작하지 않는다. 시네마틱이 끝나고 1초 뒤 시작한다. |
| 연출 에셋 | Timeline·시그널·베기 텍스처·DarkBurstVFX 등은 에셋이라 씬과 무관하게 이미 있다. (1장 표 참고) |

> ⚠ **InGameScene 에 시네마틱을 넣기 전까지는** 보스1 처치 후 보스2가 켜져도 FSM 을 시작해 줄 곳이 없어서 **보스2가 서 있기만 한다.**
> 시네마틱 적용(2장)과 흐름 연결(3장)을 같이 진행해야 한다.

### TestInGameScene 에서 테스트용으로만 바꾼 것 (InGameScene 에 옮기지 말 것)
- `Boss1` 비활성 / `Boss2` 활성 — 시네마틱 단독 테스트를 위한 설정. 본 게임은 보스1 활성 · 보스2 비활성 그대로 둔다.

---

## 1. 옮길 대상

### 씬 오브젝트 — `Boss2IntroCinematic` (프리팹: `Assets/03.Prefabs/Boss2/Intro/Boss2IntroCinematic.prefab`)
```
Boss2IntroCinematic   PlayableDirector · Animator · SignalReceiver · CinemachineImpulseSource · Boss2IntroCinematic
├─ Gas                DarkBurstVFX (검은 기운 분출, 정렬 레이어 VfxBack)
├─ Cloud              CloudFogVFX (떠다니는 구름)
├─ Slashes            X 베기 두 획 (Slash_1 / Slash_2, 각 Core + Glow)
├─ IntroOverlay       Screen Space Overlay 캔버스(Sort Order 500): Flash(흰 반짝임) · Blackout(암전)
└─ Boss2IntroCamera   얼굴 줌인용 CinemachineCamera (Priority 100, 비활성으로 시작)
```

### 이 오브젝트가 쓰는 에셋 (이미 프로젝트에 있음, 옮길 필요 없음)
| 에셋 | 경로 |
|---|---|
| Timeline | `Assets/05.Animations/Boss2/Intro/Boss2Intro.playable` |
| 시그널 6개 | `Assets/05.Animations/Boss2/Intro/Signals/` |
| 런타임 스크립트 | `Assets/02.Scripts/Boss/Boss2/Intro/` (`Boss2IntroCinematic`, `Boss2IntroSlash`) |
| 베기 텍스처·글로우 머티리얼·가산 셰이더 | `Assets/09.VFX/Boss2/Intro/` |
| 가스 / 구름 | `Assets/03.Prefabs/Boss2/Effect/DarkBurstVFX.prefab`, `CloudFogVFX.prefab` |
| 임시 폭발 | `Assets/09.VFX/Boss2/Essential 2D Particle FX/URP Compatible/Explosion URP 1/Red Explosion URP 1.prefab` |

---

## 2. InGameScene 에 시네마틱 배치

### 방법 A — 프리팹 배치 (권장)
1. InGameScene 을 연다.
2. `Boss2IntroCinematic.prefab` 을 Hierarchy **루트**에 끌어다 놓는다. 위치 (0, 0, 0).
3. `Boss2IntroCinematic` 컴포넌트에서 씬 오브젝트 두 칸을 연결한다. (프리팹에는 씬 오브젝트를 담을 수 없어서 비어 있다)

   | 칸 | 넣을 것 |
   |---|---|
   | `Boss2` | 씬의 **Boss2** (비활성 상태여도 됨) |
   | `Boss1` | 씬의 **Boss1** (가스·베기·보스2 등장 위치의 기준) |

4. `PlayableDirector` 확인
   - Playable = `Boss2Intro`
   - Timeline 창(Window → Sequencing → Timeline)에서 트랙 바인딩:
     `Intro Values` → 같은 오브젝트의 **Animator**, `Intro Signals` → 같은 오브젝트의 **SignalReceiver**
   - Play On Awake 꺼짐 (스크립트가 Awake 에서도 끈다)
5. `Boss2IntroCamera` 가 꺼져 있는지 확인 (켜져 있어도 Awake 에서 끈다).
6. 씬 저장.

### 방법 B — 메뉴로 자동 생성
1. InGameScene 을 열어 **활성 씬**으로 둔다.
2. 메뉴 **Tools → Boss2 → Build Intro Cinematic**
   - 활성 씬에 `Boss2IntroCinematic` 오브젝트를 만들고 Boss2·Boss1 을 씬에서 찾아 자동 연결한다.
3. 씬 저장.

> ⚠ 방법 B 는 **Timeline(`Boss2Intro.playable`)을 지우고 새로 만든다.** Timeline 창에서 직접 고친 커브·시그널 위치가 초기화되고,
> 베기 정렬 순서 등 프리팹에서 손본 값도 빌더 기본값으로 돌아간다. 이미 손본 게 있다면 방법 A 를 쓴다.
> (같은 이유로 TestInGameScene 메뉴 **Build Intro Cinematic In TestInGameScene** 를 다시 실행할 때도 주의)

### 방법 C — TestInGameScene 에서 복사
TestInGameScene 과 InGameScene 을 함께 연 뒤(Additive) `Boss2IntroCinematic` 을 복사·붙여넣기 → 방법 A 의 3~6 단계와 같이 `Boss2`·`Boss1` 재연결 및 확인.
(다른 씬을 가리키던 참조는 복사 시 끊어진다)

---

## 3. 흐름 연결 (코드 — 담당자 확인 필요)

전체 흐름:
```
보스1 체력 0 → (Boss1DeathController) 중앙 이동 · 주저앉기 · 대기 → OnCollapseFinished
  → ① Phase1ToPhase2Acting.StartPhaseChange()    촛불 끄기 → 배경 전환 → 촛불 켜기
  → ② StartBossChange() 에서 Boss2IntroCinematic.Play(StartPhase2Game)
  → 시네마틱: 검은 기운 분출 · 어두워짐 · 구름 → X 베기 두 번
           → ③ 보스1 폭발 요청 (Timeline 일시정지, 폭발 끝 알림 오면 재개)
           → 완전 암전 → 보스2 등장(idle 정지, 얼굴 줌) → 카메라 대각선 낙하 → 줌아웃
  → StartPhase2Game() (플레이어 조작 복구) → 1초 뒤 보스2 FSM 시작
```

### ① 보스1 사망 → 페이즈 전환 시작 (현재 미연결)
현재 `StartPhaseChange()` 를 부르는 곳이 없다. 예: `Boss1DeathController.OnCollapseFinished` 를 구독해 호출.

### ② Phase1ToPhase2Acting → 시네마틱 재생 (`Assets/02.Scripts/Acting/Phase1ToPhase2Acting.cs`)
```csharp
[SerializeField] private Boss2IntroCinematic _boss2Intro;

private void StartBossChange()
{
    _boss2Intro.Play(StartPhase2Game);
}
```
- `Play(onFinished)` 의 콜백은 시네마틱 끝(줌아웃 블렌드가 거의 끝난 시점)에 불린다.
- 3초 대기를 남기려면 `DOVirtual.DelayedCall(3f, () => _boss2Intro.Play(StartPhase2Game));`
  (`DelayedCall(3f, _boss2Intro.Play(...))` 처럼 쓰면 `Play` 가 void 라 컴파일 오류 + 즉시 실행)
- 기존 `SlowFocus(FindAnyObjectByType<HitableObject>()...)` 는 플레이어나 숨겨진 보스2를 잡을 수 있으니 빼거나 대상을 보스1로 명시하는 것을 권장.
- ⚠ `Phase1ToPhase2Acting` 은 **Player 프리팹**에 붙어 있다. 프리팹은 씬 오브젝트를 참조할 수 없으므로
  `_boss2Intro` 는 **InGameScene 의 Player 인스턴스에서(오버라이드로)** 연결하거나, 코드에서
  `FindAnyObjectByType<Boss2IntroCinematic>()` 로 찾아 쓴다.

### ③ 보스1 폭발 (Boss1 담당)
- 시네마틱은 Timeline 3.95초에 `RequestBoss1Explosion()` 을 호출하고 **폭발 끝 알림이 올 때까지 멈춘다.**
- 현재는 `Use Temp Explosion` 이 켜져 있어 **임시 폭발(0.8초) 후 자동 진행** — 팀원 작업 전에도 연출은 끝까지 돈다.
- 실제 폭발 연결 시:
  1. `Boss2IntroCinematic` 의 `Use Temp Explosion` 끄기
  2. `On Request Boss1 Explosion` 이벤트에 보스1 폭발 시작 메서드 연결 (예: `Boss1DeathController.StartExplosion()`)
  3. 폭발 애니메이션 끝에서 `Boss1DeathController.OnExplosionFinished` 를 발생시키면 끝
     (시네마틱이 `Play()` 시점에 살아 있는 보스1의 이 이벤트를 자동 구독한다.
     코드로 직접 부르려면 `NotifyBoss1ExplosionFinished()`)
  4. 알림이 8초(`Explosion Timeout`) 안에 안 오면 경고 로그 후 그냥 진행 → 연출이 멈춰 있지 않는다.
- ⚠ 현재 `Boss1DeathController` 는 주저앉은 직후 `Destroy(gameObject)` 한다. 폭발까지 보스1이 남아 있도록
  Destroy 를 폭발 끝(`OnExplosionFinished` 발생 직후)으로 옮겨야 한다. 이때는 바로 다음 프레임이 완전 암전이라 사라지는 모습은 보이지 않는다.
- 보스1이 사라지기 전에 보스2가 켜져도 문제없다: 시네마틱이 보스2를 **숨김·무적·FSM 정지** 상태로 붙잡아 둔다.

---

## 4. 씬 확인 사항

| 확인 | 이유 |
|---|---|
| Boss2 인스턴스에 `Run On Start` 오버라이드가 **없을 것** (값 false) | true 면 시네마틱 전에 공격을 시작한다 |
| Main Camera 에 `CinemachineBrain` 이 있을 것 | 얼굴 줌 카메라 컷 전환 · 줌아웃 블렌드에 사용 |
| `Light2D` Global 라이트 (Backgrounds 안) | "화면이 서서히 어두워짐" 은 씬의 Global 라이트 강도를 낮춘다. 자동으로 찾는다 |
| 다른 Screen Space Overlay 캔버스의 Sort Order 가 500 이상이 아닐 것 | 암전·반짝임이 그 UI 를 덮지 못한다 |
| 정렬 레이어 `VfxBack`·`Boss` 존재 | DarkBurstVFX 는 VfxBack(캐릭터 뒤), X 베기는 Boss 레이어(연기 위) |

---

## 5. 동작 확인

1. **단독 확인**: 플레이 후 Hierarchy 의 `Boss2IntroCinematic` 선택 → 컴포넌트 우클릭(⋮) → **Test/Play Intro**
   (보스2가 켜져 있어야 등장 장면이 보인다. 본 게임 설정에서는 보스2가 꺼져 있으면 등장 시 자동으로 켠다)
2. **전체 흐름 확인**: 보스1 처치 → 촛불 전환 → 시네마틱 → 전투
3. 체크리스트
   - [ ] 시작하자마자 보스1 중심에서 검은 기운이 사방으로 터져 나오고, 갈수록 많이·빠르게
   - [ ] 화면이 서서히 어두워지고 구름이 나타나 떠다님
   - [ ] X 베기 두 번(0.5초 간격) — 획은 선명, 외곽은 붉게 반짝, 화면 반짝임, 연기 위에 그려짐
   - [ ] 보스1 폭발(현재 임시) 후 완전 암전
   - [ ] 불이 켜지면 보스2가 보스1 자리에 idle 정지, 얼굴 줌인 상태
   - [ ] 카메라가 오른쪽 아래로 뚝 떨어지고 미세하게 흔들린 뒤 멈춤 → 줌아웃
   - [ ] 시네마틱 중 보스2 공격·피격 없음, 끝난 뒤 플레이어 조작 복구, 1초 뒤 보스2 공격 시작
   - [ ] 끝난 뒤 카메라가 플레이어 추적·기본 줌으로 복귀, 콘솔 에러 없음

---

## 6. 조절 값 (Boss2IntroCinematic 인스펙터)

| 그룹 | 값 | 기본 | 의미 |
|---|---|---|---|
| 전투 시작 | Battle Start Delay | 1 | 시네마틱 후 FSM 시작까지 대기(초) |
| 가스 | Gas Rate Scale | 1 | 프리팹 방출량 배율 |
| | Gas Min / Max Speed Scale | 0.35 / 1.2 | 커브 0→1 동안 분출 속도 배율 |
| | Gas Start Burst | 0.5 | 시작 순간 한꺼번에 뿜는 양(초당 방출량의 몇 초 분량) |
| 화면 어둡게 | Dark Intensity Scale | 0.25 | 가장 어두울 때 Global 라이트 강도 배율 |
| 인트로 카메라 | Face Offset | (-0.05, 1.15) | 보스2 위치 기준 얼굴 위치 |
| | Face Orthographic Size | 1.1 | 얼굴 줌 크기 |
| | Drop Offset | (0.45, -0.45) | 카메라가 떨어지는 방향·거리 |
| | Zoom Out Duration | 1.2 | 메인 카메라로 돌아가는 블렌드 시간 |
| 보스1 폭발 | Use Temp Explosion / Temp Explosion Duration | 켬 / 0.8 | 임시 폭발 사용 여부·대기 시간 |

타이밍(가스 커브, 베기 3.0/3.5초, 폭발 3.95초, 암전, 등장, 낙하, 줌아웃, 전투 시작 7.0초)은
Timeline 창에서 `Boss2Intro.playable` 을 열어 커브·시그널 위치로 조절한다.
