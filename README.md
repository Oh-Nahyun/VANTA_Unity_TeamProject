# VANTA : LAST LIGHT

> **보스의 패턴을 파악하고, 회피와 반격으로 돌파하는 2D 액션 보스 러쉬 게임**

**VANTA : LAST LIGHT**는 보스의 공격 패턴을 관찰하고 학습하며,  
정확한 회피와 근거리 공격을 통해 보스를 공략하는 **2D 액션 보스 러쉬 게임**입니다.

빠른 전투 흐름 속에서 **묵직한 타격감과 속도감**을 전달하는 것을 핵심 목표로 합니다.


## 🎮 Game Overview

| 항목 | 내용 |
| :--- | :--- |
| **게임명** | VANTA : LAST LIGHT |
| **장르** | 2D Action / Boss Rush |
| **핵심 플레이** | 패턴 파악 → 회피 → 반격 |
| **Game Feel** | 묵직한 타격감 / 속도감 |
| **플레이 구조** | Boss Battle → Clear / Retry |
<img width="1671" height="941" alt="image" src="https://github.com/user-attachments/assets/aab8f8c6-5e39-4c17-a67f-6377c3f44baf" />
<img width="1670" height="942" alt="image" src="https://github.com/user-attachments/assets/5f641739-b44f-49b9-9591-fb343af1388a" />
<img width="1672" height="941" alt="image" src="https://github.com/user-attachments/assets/a303ac37-4d4b-4a70-a06d-7b13d8fdfbb8" />


## ⚔️ Core Gameplay

플레이어는 보스의 공격 패턴을 **관찰하고 학습**하며 대응 방법을 찾아야 합니다.

공격을 회피하여 빈틈을 만들고, 근거리 무기와 스킬을 이용해 반격합니다.  
또한 **펫의 보조 능력**을 활용하여 전투를 유리하게 이끌 수 있습니다.

### 🔄 Core Loop

```text
보스 조우
   ↓
패턴 관찰 및 파악
   ↓
회피 / 위치 선정
   ↓
반격
   ↓
보스 피해
   ↓
다음 패턴
   ↺
```

**Boss HP = 0** → 전투 승리  
**Player HP = 0** → 재도전


## 🕹️ Controls

| 입력 | 동작 |
| :---: | :--- |
| `WASD` | 캐릭터 이동 |
| `Space Bar` | 구르기 / 회피 |
| `Mouse Click` | 무기 공격 / 스킬 |


## 👹 Boss Battle

### ⚔️ Boss Encounter

전투 공간에 진입하면 **보스 등장 연출**과 함께 전투가 시작됩니다.

전투 시작 시 보스 HP와 전투 UI가 활성화되며,  
등장 연출을 통해 보스의 특징과 전투 분위기를 전달합니다.

### 👁️ Pattern Learning

보스는 **2개의 페이즈**를 가지며 각 페이즈에서 여러 공격 패턴을 반복하거나 조합합니다.

플레이어는 전투를 반복하며 다음 요소를 학습합니다.

- 공격 방향
- 공격 범위
- 공격 타이밍
- 안전한 위치
- 반격 가능한 타이밍

전투가 진행될수록 보스의 패턴에 익숙해지고 대응 능력이 향상됩니다.

### 🌀 Dodge & Counter

보스의 공격 방향과 범위를 파악하여 회피하고,  
이동과 스킬을 활용해 유리한 위치를 확보한 뒤 반격합니다.

**정확한 회피 → 공격 기회 확보 → 반격**

회피는 단순한 생존 수단이 아니라 공격 기회를 만들어내는 핵심 플레이 요소입니다.

### ☠️ Boss Clear

보스의 HP가 `0`이 되면 전투가 종료됩니다.

보스 처치 연출과 함께 클리어 화면으로 전환하여  
플레이어에게 전투의 종료와 승리를 명확하게 전달합니다.


## 💥 Game Feel

VANTA : LAST LIGHT는 단순히 데미지를 주는 것을 넘어  
**공격이 실제로 적에게 꽂히는 느낌**을 전달하는 것을 목표로 합니다.

| Feel | 구현 방식 | 적용 상황 |
| :--- | :--- | :--- |
| **Hit Stop** | 적중 순간 게임 시간을 짧게 정지 | 공격 적중 |
| **Camera Shake** | 공격 강도에 따라 흔들림 조절 | 강공격 / 피격 / 처치 |
| **Slow Motion** | 결정적인 순간 게임 속도 감소 | 회피 / 보스 처치 |
| **Trail / AfterImage** | 빠른 움직임에 궤적과 잔상 생성 | 공격 / 대시 / 회피 |
| **Knockback / Recoil** | 피격 대상에 짧은 밀림과 반동 적용 | 보스 / 플레이어 피격 |
| **Hit Effect** | 스파크 및 충격파 VFX 생성 | 공격 적중 |
| **Attack / Hit SFX** | 상황별 타격 및 행동 효과음 적용 | 공격 / 회피 / 피격 |


## 🖥️ UI

전투 흐름을 방해하지 않으면서 필요한 정보를 빠르게 확인할 수 있도록 구성합니다.

### 상단

`Boss HP` · `Stage Info`

### 하단

`Player Skill Stack` · `Player Status`


## 🎨 Art Direction

**Dark Fantasy Pixel Art**

어둡고 차가운 분위기의 픽셀 아트를 기반으로  
캐릭터와 보스의 **실루엣, 조명, 공격 이펙트**를 강조합니다.

어두운 배경에서도 공격과 캐릭터의 움직임을 명확하게 인식할 수 있도록 구성합니다.

### Reference Assets

- Luneblade Bundle
- The Dark Series - Boss & Decorations


## 👥 Team — 오구오구

| 이름 | 역할 | 담당 |
| :---: | :--- | :--- |
| 👑 **오나현** | PM / Programmer | 프로젝트 총괄 · 기획 조율 · 펫 시스템 · UMG |
| 💻 **박철규** | Programmer | Boss Phase 1 |
| 💻 **송재욱** | Programmer | Boss Phase 2 |
| 💻 **최재원** | Programmer | Player System |


## 🎯 Development Goal

> ### **패턴을 읽고 → 피하고 → 정확한 순간에 반격한다.**

복잡한 시스템을 늘리는 것보다 **보스와 플레이어 사이의 전투 상호작용**에 집중합니다.

반복되는 도전을 통해 보스의 패턴을 학습하고,  
처음에는 피하기 어려웠던 공격을 자연스럽게 대응하게 되는

**「숙련 과정 자체가 재미가 되는 보스전」**

을 만드는 것을 목표로 합니다.
