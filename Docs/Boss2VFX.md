# 보스2 연출 파티클 사용 설명서

대상 프리팹: `Assets/03.Prefabs/Boss2/Effect/`

| 프리팹 | 내용 | 재생 방식 |
|---|---|---|
| `DarkEngulfVFX2` | 블랙홀처럼 주변(반경 4.4)의 검은 입자를 소용돌이치며 빨아들임 | 1회 (약 1.2초) |
| `DarkFogVFX` | 검은 안개가 맵에 서서히 깔림 | 반복 (Stop 하면 서서히 걷힘) |
| `MoonlightFogVFX` | 검은 안개 + 오른쪽 위에서 왼쪽 아래로 떨어지는 달빛이 반시계로 30° 돌아감 | 안개 반복 / 달빛 1회 6초 |

---

## 1. 만들기 / 다시 만들기

Unity 메뉴 **Tools → Boss2 VFX**

| 메뉴 | 하는 일 |
|---|---|
| Build All | 아래 세 개 모두 실행 |
| Update DarkEngulfVFX2 | 기존 `DarkEngulfVFX2` 의 SuckIn 값을 바꾸고 `Core` 자식을 추가 (Cover 는 그대로) |
| Build DarkFogVFX | `DarkFogVFX.prefab` 생성 (있으면 덮어씀) |
| Build MoonlightFogVFX | `MoonlightFogVFX.prefab` 과 빛줄기 머티리얼·텍스처 생성 |

- 값은 절대값으로 넣으므로 몇 번 실행해도 결과가 같다.
- **주의:** 다시 실행하면 인스펙터에서 손으로 바꾼 값이 빌더 값으로 덮인다. 손으로 다듬은 뒤에는 다시 실행하지 말고, 바꾼 값을 `Assets/02.Scripts/Boss2/Editor/Boss2VfxBuilder.cs` 에도 반영해 둔다.
- 함께 생성되는 에셋
  - `Assets/08.Materials/Boss2/M_Boss2MoonBeamAdditive.mat` (빛줄기, 가산)
  - `Assets/08.Materials/Boss2/M_Boss2MoonDustAdditive.mat` (빛 속 먼지, 가산)
  - `Assets/04.Sprites/Boss2/VFX/T_Boss2MoonBeam.png` (빛줄기 텍스처, 위가 밝고 아래로 흐려짐)

---

## 2. DarkEngulfVFX2 (블랙홀 흡수)

구성: `SuckIn`(빨려 드는 입자) · `Core`(가운데 검은 핵) · `Cover`(몸을 덮는 입자, 변경 없음)

| 항목 | 이전 | 지금 | 조절 위치 |
|---|---|---|---|
| 흡수 반경 | 2.2 | **4.4** | SuckIn ▸ Shape ▸ Radius |
| 생성 위치 | 가장자리 10% 띠 | 가장자리 15% 띠 | Shape ▸ Radius Thickness |
| 입자 수 | 140/초 | 280/초 | Emission ▸ Rate over Time |
| 빨려 드는 속도 | 일정 -4.5 | -8 × (0.4 → 1.6) 점점 빨라짐 | Velocity over Lifetime ▸ Radial |
| 소용돌이 | 일정 1.5 rad/s | 1 → 6 rad/s 가운데로 갈수록 빨라짐 | Velocity over Lifetime ▸ Orbital Z |
| 크기 | 일정 | 가운데로 갈수록 20% 까지 작아짐 | Size over Lifetime |
| 검은 핵 | 없음 | 0 → 1.6 으로 커졌다 사라짐 | Core |

- 반경을 바꾸면 **Radial 값도 같이** 바꿔야 입자가 가운데까지 온다. 대략 `Radial = -반경 ÷ 수명(0.55)`.
- 즉사기(`Boss2SkillPattern._engulfEffect`)에 쓰려면 보스 자식으로 넣고 그 필드에 연결한다.
  즉사기는 `InstantKillTeleport`(StartInstantKillSkill 0.83초) 순간에 이 이펙트를 지우므로, 흡수는 0.83초 안에 끝나게 맞춰 두었다.

---

## 3. DarkFogVFX (검은 안개)

- 맵 크기(약 18×8)에 맞춘 16×7 상자 범위에 큰 연기 입자를 초당 12개 뿌린다.
- 입자가 투명하게 생겨 4~6초 동안 천천히 짙어졌다 사라지므로, **켜고 몇 초에 걸쳐 서서히 깔린다.**
- 정렬 레이어 `VfxBack` → 캐릭터 발밑에 깔린다.

| 하고 싶은 것 | 조절 위치 |
|---|---|
| 범위 | Shape ▸ Scale (맵 중앙에 프리팹을 놓는다) |
| 짙기 | Main ▸ Start Color 알파, Emission ▸ Rate |
| 깔리는 속도 | Main ▸ Start Lifetime (짧을수록 빨리 깔림), Color over Lifetime 앞쪽 알파 키 |
| 흐르는 정도 | Velocity over Lifetime ▸ X/Y, Noise ▸ Strength |
| 캐릭터 위로 덮기 | Renderer ▸ Sorting Layer 를 `Vfx` 로 |

켜기 / 끄기
```csharp
fog.Play();                                                   // 서서히 깔림
fog.Stop(true, ParticleSystemStopBehavior.StopEmitting);      // 새로 안 만들고 남은 안개가 서서히 걷힘
```

---

## 4. MoonlightFogVFX (검은 안개 + 달빛)

```
MoonlightFogVFX
 ├ Fog              검은 안개 (DarkFogVFX 와 같은 설정, 반복)
 └ MoonBeamPivot    빛의 축 (오른쪽 위, 로컬 (7, 5)) + Boss2LightSweep
     ├ Beam         넓고 흐린 빛줄기 (폭 2.4, 길이 16)
     ├ BeamCore     가운데 밝은 줄기 (폭 0.8)
     └ MoonDust     빛 속에서 떠다니는 먼지
```

- 빛은 축에서 **아래쪽(-Y)** 으로 뻗는다. 축의 Z 각도가 -45° 이면 오른쪽 위 → 왼쪽 아래.
- `Boss2LightSweep` 가 6초 동안 축을 **-45° → -15° (반시계 30°)** 로 부드럽게 돌린다.
- 빛줄기는 6초 동안 한 번 나온다: 0~1.2초 밝아짐 → 유지 → 5.1~6초 사라짐. 안개는 계속 남는다.
- 정렬 레이어 `Light` (캐릭터 위).

`Boss2LightSweep` (MoonBeamPivot)

| 필드 | 기본값 | 설명 |
|---|---|---|
| Start Angle | -45 | 시작 각도. 0 = 수직으로 아래, -45 = 오른쪽 위 → 왼쪽 아래 |
| Sweep Angle | 30 | 돌릴 각도. 양수 = 반시계 |
| Duration | 6 | 다 돌기까지 초. 빛줄기 수명(6초)과 맞춘다 |
| Ease In Out | 켬 | 시작·끝을 부드럽게 |

- Scene 뷰에서 MoonBeamPivot 을 선택하면 시작(하늘색)·끝(노란색) 방향이 선으로 보인다.
- 빛 위치는 MoonBeamPivot 의 Position, 길이·폭은 Beam/BeamCore ▸ Main ▸ 3D Start Size (Y = 길이, X = 폭). 길이를 바꾸면 Beam·BeamCore·MoonDust 의 Position Y 를 `-길이/2` 로 같이 바꾼다.
- 빛 색은 Beam/BeamCore ▸ Start Color.

다시 재생
```csharp
// 오브젝트를 껐다 켜면 빛 회전도 처음부터 다시 돈다.
moonlight.SetActive(false);
moonlight.SetActive(true);

// 켠 상태에서 다시 재생할 때 (루트에는 파티클이 없으므로 자식들을 각각 재생)
foreach (ParticleSystem ps in moonlight.GetComponentsInChildren<ParticleSystem>())
{
    ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
    ps.Play(false);
}
moonlight.GetComponentInChildren<Boss2LightSweep>().Play();
```

---

## 5. 문제 해결

| 증상 | 확인 |
|---|---|
| 빛줄기가 안 돌고 제자리 | Beam ▸ Main ▸ Simulation Space = **Local**, Renderer ▸ Render Alignment = **Local** 인지 |
| 빛줄기가 네모로 보임 | Beam 머티리얼의 Base Map 에 `T_Boss2MoonBeam` 이 있는지 (Tools ▸ Build MoonlightFogVFX 다시 실행) |
| 빛줄기가 너무 밝음 / 어두움 | Beam·BeamCore ▸ Start Color 알파 |
| 광역기 어둠 연출 중에 안개·빛이 안 보임 | 머티리얼이 Unlit 인지 (빌더가 만든 것은 Unlit). Lit 이면 2D 조명에 가려진다 |
| 안개가 화면 일부에서 잘림 | Renderer ▸ Max Particle Size 를 키운다 (화면 대비 비율) |
