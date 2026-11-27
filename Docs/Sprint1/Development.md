# Sprint 1 — 개발 팀 보고: 구조 분석과 NUnit 테스트 계획

작성: 개발 팀 (Claude Code) · 2026-11-27 · 대상: 감독, 프로듀서(Codex)
관련 TODO: `Docs/Tasks.md` "NUnit test suite"

> 이 문서는 **분석과 제안**만 담습니다. 코드·asmdef·.meta·씬·ProjectSettings는 수정하지 않았습니다.
> 클라우드 환경에는 Unity Editor와 dotnet이 없어 **컴파일과 테스트를 한 번도 실행하지 않았습니다.** 아래의 모든 테스트 코드 형태와 명령은 감독 PC에서 확인이 필요합니다.
> 줄 번호는 현재 작업 트리 기준입니다(`Assets/Scripts/LastStopPrototype.cs` 1641줄).

---

## 1. 현재 스크립트 구조

### 1.1 파일별 책임

| 파일 | 줄 수 | 책임 | 게임 규칙 상태 보유 |
|---|---|---|---|
| `Assets/Scripts/LastStopPrototype.cs` | 1641 | 부트스트랩, 월드·카메라·HUD 런타임 생성, 외부/내부 시뮬레이션, 피해, 레벨업, 결과, 재시작 | **예 (거의 전부)** |
| `Assets/Scripts/LastStopContentDefinitions.cs` | 112 | 콘텐츠 ID, 캐릭터/버스/적 정의, 카탈로그, `LastStopRunState` | 예 (HP·레벨·XP) |
| `Assets/Scripts/LastStopAudio.cs` | 109 | 오디오 이벤트 계약(`LastStopAudioEvent`), 쿨다운, 절차 생성 효과음, `CueRequested` 이벤트 | 오디오 쿨다운만 |
| `Assets/Scripts/LastStopProjectileVisual.cs` | 54 | 투사체 이동. 도착 시 콜백으로 피해 적용, 대상 무효 시 자기 파괴 | 아니오 (콜백 경유) |
| `Assets/Scripts/LastStopUnscaledLifetime.cs` | 46 | unscaled 시간 기준 연출 오브젝트 수명, 히트 스파크 위치 | 아니오 |
| `Assets/Scripts/LastStopSpriteMotion.cs` | 190 | 숨쉬기·피격·공격 포즈 등 표현 전용 모션 | 아니오 |
| `Assets/Scripts/LastStopBusTrail.cs` | 66 | 버스 배기 연출 | 아니오 |
| `Assets/Scripts/LastStopRoadMotion.cs` | 34 | 배경 스크롤 | 아니오 |
| `Assets/Scripts/LastStopBusWheelAnimation.cs` | 34 | 바퀴 프레임 애니메이션 | 아니오 |

- asmdef가 하나도 없습니다. 모든 스크립트는 `Assembly-CSharp`에 컴파일됩니다.
- `SampleScene.unity`, 프리팹, 에셋 어디에서도 위 스크립트의 GUID를 참조하지 않습니다(`.cs.meta` GUID 전수 검색 결과 0건). 프로토타입은 씬 직렬화 없이 런타임에 만들어집니다.
- 패키지: `com.unity.test-framework` 1.6.0, `com.unity.inputsystem` 1.19.0, `com.unity.ugui` 2.0.0이 이미 `Packages/manifest.json`에 있습니다. 테스트 프레임워크 추가 설치는 필요 없습니다.

### 1.2 실행 흐름

| 위치 | 내용 |
|---|---|
| `LastStopPrototype.cs:8-32` `LastStopBootstrap` | `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`로 **모든 씬 로드 시** `LastStopPrototype`가 없으면 `"LAST STOP MVP"` GameObject를 생성. Single 모드 `sceneLoaded`마다 반복. |
| `:155` `Start()` | 오디오 소스, 카메라(`:230`), 스프라이트 로드(`:275`), 월드(`:257`, 외부 `:317`, 내부 `:422`), HUD(`:461`) 생성 후 외부 시점. 외부 적 3마리 초기 스폰(`:351-354`). |
| `:169` `Update()` | 아래 순서의 한 프레임 루프. |

`Update()` 순서 (`:169-228`):
1. `gameOver`이면 R 입력만 확인 → `Restart()` (`:175-183`)
2. `levelUpOpen`이면 1/2/3 입력만 처리하고 반환 (`:185-189`)
3. Tab → `SetView` (`:191-195`)
4. `SimulateExterior` → `SimulateBubbleLauncher` → `SimulateBusFlame` → `UpdateCorePresentation` → `SimulateInteriorThreats` (`:197-201`)
5. 패배 시 HUD 갱신 후 반환 (`:202-206`)
6. 내부 시점일 때만 `MoveInteriorPlayerAndAttack` (`:208-211`)
7. `runElapsedSeconds += Time.deltaTime`, 480초 이상이면 `EndArrival` (`:219-224`)
8. 프레임 끝에서 `TryOfferLevelUp` (`:226`), `RefreshHud` (`:227`)

내부 위협 시뮬레이션은 시점과 관계없이 매 프레임 진행됩니다(GameDesign "both simulations continue"와 일치). 플레이어 이동·공격만 내부 시점에서 동작합니다.

### 1.3 규칙별 상태와 핵심 메서드

#### 공유 HP
- 상태: `LastStopRunState.SharedHealth / SharedMaxHealth` (`LastStopContentDefinitions.cs:106-107`, 초기값 `YellowBus.MaxHealth` = 100, `:81`), `IsGameOver => SharedHealth <= 0f` (`:111`).
- 프로토타입이 `private readonly LastStopRunState runState` (`LastStopPrototype.cs:57`)로 보유.
- 감소 지점은 **단 한 곳** `DamageSharedHealth(Enemy, bool playerHit)` (`:1461-1503`):
  - `enemy.DamageTimer += Time.deltaTime`, 0.8 미만이면 반환, 이상이면 0으로 리셋 (`:1464-1466`). 초과분은 버려집니다(프레임 양자화로 실측 0.80~0.81초).
  - 피해는 고정 7, 0 미만으로 내려가지 않음 (`:1467-1468`).
  - HUD 피드백 0.35초 합산 창 (`:1471-1486`), 오디오 `PlayerDamaged/CoreDamaged` (`:1493`).
  - `runState.IsGameOver`이면 `EndGameOver` (`:1499-1502`).
- 증가 지점: 레벨업 3번 "차체 보강" `SharedMaxHealth += 25; SharedHealth += 25` (`:1428-1432`).
- 수치 0.8, 7은 메서드 내부 리터럴입니다(상수 아님).

#### 1:1 침입
- 외부 목록 `exteriorEnemies`, 내부 목록 `interiorEnemies` (`:52-53`), 공용 클래스 `private sealed class Enemy` (`:143-153`, `IsInside`, `Health`, `DamageTimer`, `CurrentAttackTarget`, `DoorwayStaggerTimer` 등).
- `MoveExteriorEnemyTowardsBus` (`:753-765`): 버스 기준 |dx|<2.15, |dy|<0.85이면 접촉으로 보고 `TransferEnemyInside` 호출.
- `TransferEnemyInside` (`:767-790`): 외부 목록에서 해당 인덱스 1개 제거 → `IsInside = true` → 같은 `Enemy` 인스턴스를 내부 목록에 추가. 체력은 그대로 보존. 내부 출입구(-5.75, -1.55)로 재배치, 0.6초 `DoorwayStaggerTimer`.
- 외부 투사체 추적 중단: `IsActiveExteriorEnemy` (`:1279-1282`)가 `!enemy.IsInside && exteriorEnemies.Contains(enemy)`를 검사하고, `LastStopProjectileVisual.Update` (`LastStopProjectileVisual.cs:29-34`)가 이 검사 실패 시 자기 파괴.
- 참고: `SpawnInteriorEnemy` (`:1125`)와 `DamageNearest` (`:1199`)는 호출처가 없는 코드입니다. 테스트 픽스처로 쓸 수 있지만 현재는 private.

#### 내부 적 타겟팅
- `private enum InteriorAttackTarget { None, Player, Core }` (`:141`).
- `SimulateInteriorThreats` (`:831-877`):
  - 출입구 경직 중이면 공격·이동 생략 (`:844-845`).
  - 플레이어 거리 ≤ 0.58 → Player, 아니면 코어 거리 ≤ 0.70 → Core, 아니면 None. **플레이어 우선** (`:846-849`).
  - 타깃이 바뀌면 `DamageTimer = 0` (`:850-854`).
  - 타깃이 있으면 `DamageSharedHealth`, 없으면 `MoveInteriorEnemyTowardsCore` (`:879-885`, x축만 이동).
- 플레이어 공격: `MoveInteriorPlayerAndAttack` (`:887-936`), 공격 간격 `InteriorAttackInterval` 0.18초 (`:46`), 3타 콤보 `AdvanceInteriorCombo` (`:938-942`, 창 0.55초), 3타는 `DamageInteriorSweep` (`:1159`, 범위 2, 최대 3마리), 1·2타는 `DamageNearestInFacing` (`:1131`).

#### 50 경고 임계
- `BreachWarningThreshold = 50` (`:42`).
- `breaches`는 매 프레임 `SimulateInteriorThreats`에서 0부터 다시 셉니다 (`:833`, `:843`). 경직 중인 적도 포함.
- `breaches >= 50`이면 `breachWarningActive = true`와 `BreachWarningOn` 오디오 1회, 50 미만으로 내려가면 해제 (`:867-875`). **HP와 `gameOver`에는 영향 없음** — 규칙과 일치.
- `peakBreaches` (`:865`)는 결과 화면용.
- HUD 문구 단계: 0 / 1~15 / 16~39 / 40~49 / 50+ (`:1602-1607`).

#### 타이머
| 타이머 | 위치 | 기준 시간 |
|---|---|---|
| 런 경과 `runElapsedSeconds`, 480초 도착 | `:101`, `:219-224`, `DemoDurationSeconds` `:44` | scaled (`Time.deltaTime`) |
| 외부 스폰 `exteriorSpawnTimer`, 간격 4/2.6/1.8/1.5초, 상한 6/10/14 | `:688-722` | scaled |
| 중반 웨이브 `nextMiddleWaveAt` (195초부터 45초 간격) | `:724-731` | scaled |
| 외부 자동 공격 0.55초 / 배율 | `:45`, `:733-738` | scaled |
| 비눗방울 1.35초 | `:979-984` | scaled |
| 버스 화염 0.7초 | `:1062-1067` | scaled |
| 내부 화염 꼬치 준비 0.7초 | `:909-910`, `:1078-1082` | scaled |
| 적별 공격 `DamageTimer` 0.8초 | `:1464-1466` | scaled |
| 출입구 경직 0.6초, 감속 `SlowTimer` | `:785`, `:756`, `:844` | scaled |
| 콤보 창 `interiorComboExpiresAt` | `:940-941` | `Time.time` (scaled 누적) |
| HUD 피드백, 힌트, 펄스, 오디오 덕킹 | `:1471-1489`, `:1458` | **unscaled** |
| 연출 수명 | `LastStopUnscaledLifetime.cs:41-44` | **unscaled** |
| 오디오 쿨다운 | `LastStopAudio.cs:51-53` | **unscaled** |

#### 레벨업 일시정지
- 경험치: 처치 시 `GainExperience(2)` (`:1263`, `:1334-1338`).
- `TryOfferLevelUp` (`:1340-1348`)는 프레임 끝에서만 호출. 필요 XP는 `Ceil(x * 1.35)`.
- `ShowLevelUp` (`:1350-1371`): `levelUpOpen = true`, `Time.timeScale = 0`.
- 정지 보장 장치는 세 겹입니다:
  1. `Update()`가 `levelUpOpen`이면 시뮬레이션 전에 반환 (`:185-189`).
  2. `Time.timeScale = 0`으로 scaled 타이머 정지, 투사체는 `Time.deltaTime <= 0`이면 이동·도착 생략 (`LastStopProjectileVisual.cs:36`).
  3. `DamageEnemy` (`:1233`)와 `TryFlameSkewer` (`:1080`)가 `levelUpOpen || gameOver`를 확인.
- `ChooseLevelUp` (`:1393-1445`): 효과 적용 → 연속 레벨업이면 다시 제안, 아니면 `timeScale = 1`.

#### 패배·도착·재시작
- 패배 `EndGameOver` (`:1505-1516`), 도착 `EndArrival` (`:1518-1530`): 둘 다 `gameOver = true`, `Time.timeScale = 0`.
- 주의: 런 종료 플래그가 두 개입니다. 프로토타입의 `gameOver` (`:72`, 도착 포함)와 `runState.IsGameOver` (HP 기반, 패배만).
- `Restart` (`:1626-1630`): `timeScale = 1` 후 `SceneManager.LoadScene(활성 씬 buildIndex)`. 상태 초기화는 **씬 재로드와 부트스트랩 재생성에 의존**합니다. 필드를 직접 리셋하지 않습니다.
- 씬 재로드를 넘어 남는 정적 상태: `uiFont` (`:41`, 재사용해도 무해), `LastStopAudio`의 `source`, `Clips`, `LastPlayed`, `gameOverPlayed` (`LastStopAudio.cs:23-26`; `sceneLoaded`에서 `LastPlayed`, `gameOverPlayed` 리셋 `:38-42`).

#### 기타 관찰 (동작 변경 제안 아님)
- `exteriorPlayer` (`:63`)는 할당되지 않습니다. `TryFlameSkewer`의 `exteriorPlayer.localPosition` (`:1084`)은 내부 시점에서만 호출되므로 현재 도달 불가이지만, 외부 시점에서 호출하도록 바뀌면 NullReferenceException이 납니다.
- `RecordPlaytestEvent` (`:1632-1637`)는 Editor/Development 빌드에서 `LAST_STOP_QA event=...` 로그를 남깁니다. PlayMode 테스트에서 `LogAssert` 대신 로그 수집으로 검증 보조에 쓸 수 있습니다.

---

## 2. 테스트 가능성 분석

### 2.1 지금(코드 변경 없이) 가능한 것

**전제 조건:** 테스트 asmdef는 `Assembly-CSharp`를 참조할 수 없습니다. 따라서 런타임 asmdef(3장)가 없으면 asmdef 기반 테스트에서 `LastStop` 타입에 접근할 수 없습니다. 런타임 asmdef 추가는 **코드 변경 없이** 가능하지만, 파일 추가와 .meta 생성이 있으므로 감독 승인이 필요합니다.

| 범위 | 대상 | 방법 |
|---|---|---|
| EditMode | `LastStopRunState` 기본값(HP 100/100, 레벨 1, XP 0/10), `IsGameOver`(0과 음수에서 true, 0.01에서 false) | 공개 클래스를 직접 생성 |
| EditMode | `LastStopContentCatalog` 값(버스 MaxHealth 100, Rusher 체력 3, ID 문자열 고정) | 공개 정적 필드 |
| EditMode | `LastStopAudio.TryPlay`가 Edit 모드에서 `CueRequested`를 발생시키고 AudioSource를 만들지 않음 (`LastStopAudio.cs:44-46`) | 공개 이벤트 구독 |
| PlayMode | 부트스트랩이 프로토타입을 정확히 1개 생성, 수 프레임 동안 예외 없음, HUD 오브젝트 존재 | `FindObjectsByType<LastStopPrototype>` |
| PlayMode (리플렉션) | 공유 HP, 침입, 50 경고, 0.8초 간격, 레벨업 정지 | private 필드를 `BindingFlags.NonPublic`으로 읽고 쓰기. 기존 Play Mode 프로브(`Docs/Verification/SubmissionRegressionResults.txt`)와 같은 접근입니다. |

### 2.2 테스트를 막는 요소

1. **로직이 전부 private 인스턴스 메서드이고 MonoBehaviour 안에 있음.** `DamageSharedHealth`, `TransferEnemyInside`, `SimulateInteriorThreats`, `TryOfferLevelUp`, `ChooseLevelUp`, `EndGameOver`이 모두 private입니다. `Enemy`(`:143`)와 `InteriorAttackTarget`(`:141`)은 private 중첩 타입이라 테스트에서 인스턴스를 만들려면 리플렉션(`Activator.CreateInstance` + 중첩 타입 조회)이 필요합니다.
2. **`Time.deltaTime`, `Time.time`, `Time.unscaledTime` 직접 참조.** 시간을 주입할 수 없어 EditMode에서 타이머 로직을 실행할 수 없습니다. PlayMode에서는 `Time.captureDeltaTime` 또는 `Time.captureFramerate`로 고정 프레임을 만들 수 있습니다.
3. **입력을 `Keyboard.current`로 직접 읽음** (`:177`, `:191`, `:911`, `:1103-1116`, `:1373-1391`). 입력 없이 R 재시작, Tab, Space, 1/2/3를 검증하려면 Input System 테스트 픽스처(`InputTestFixture`)가 필요하고, 이것은 `manifest.json`의 `"testables": ["com.unity.inputsystem"]` 추가가 필요합니다(ProjectSettings급 변경, 승인 필요). 대안은 리플렉션으로 `Restart`/`ChooseLevelUp`를 직접 호출하는 것입니다.
4. **`Start()`가 월드 전체를 생성.** 카메라, Resources 스프라이트, HUD, Canvas, EventSystem을 모두 만듭니다. 분리된 로직 단위 테스트가 불가능하고 PlayMode에서만 의미 있게 동작합니다.
5. **위치 기반 판정.** 접촉(|dx|<2.15, |dy|<0.85)과 타깃(0.58/0.70)이 Transform 위치로 계산되어 GameObject가 필요합니다.
6. **부트스트랩이 테스트 씬에도 개입.** `AfterSceneLoad`로 PlayMode 테스트 러너의 임시 씬에도 프로토타입이 자동 생성됩니다. 장점(스모크 테스트가 쉬움)이자 단점(스폰 랜덤, 다른 테스트의 간섭)입니다. 테스트마다 정리(`Object.Destroy` + `Time.timeScale = 1`)가 필요합니다.
7. **`Restart`가 활성 씬의 `buildIndex`로 재로드.** 테스트 러너 임시 씬은 빌드 설정에 없어 buildIndex가 -1이라 실패할 가능성이 높습니다. 재시작 테스트는 먼저 `SceneManager.LoadScene("SampleScene")`(빌드 설정 0번, `ProjectSettings/EditorBuildSettings.asset`에 등록됨)으로 진입한 뒤 수행해야 합니다.
8. **`Time.timeScale` 전역 상태.** 레벨업·결과에서 0으로 만든 값이 다음 테스트로 새어 나갑니다. `[TearDown]`/`[UnityTearDown]`에서 1로 복원해야 합니다.
9. **랜덤 스폰** (`:1120`, `Random.insideUnitCircle`). 재현성이 필요하면 `Random.InitState(seed)`를 테스트 시작 시 호출합니다. 초기 3마리 스폰은 `Start()`에서 일어나므로 부트스트랩 전에 시드를 고정하기 어렵습니다. 리플렉션 테스트에서는 기존 적을 제거하고 테스트용 적을 배치하는 편이 안정적입니다.

### 2.3 결론
- 리팩터 없이: EditMode는 데이터·오디오 계약 수준만, 핵심 규칙은 **PlayMode + 리플렉션**으로만 검증 가능합니다. 필드 이름에 묶여 깨지기 쉽지만, 리팩터 전 회귀 기준선(characterization test)으로 가치가 있습니다.
- 4장의 최소 리팩터 후: 공유 HP, 공격 타이머, 침입 카운트, 경고, 런 시계, 레벨업 XP가 순수 C#이 되어 **EditMode에서 밀리초 단위로** 검증 가능합니다.

---

## 3. NUnit 테스트 계획

### 3.1 어셈블리 정의 (승인 필요, 아직 생성하지 않음)

```
Assets/Scripts/LastStop.Runtime.asmdef            (신규)
Assets/Tests/EditMode/LastStop.Tests.EditMode.asmdef  (신규)
Assets/Tests/PlayMode/LastStop.Tests.PlayMode.asmdef  (신규)
```

**A. `LastStop.Runtime` (Assets/Scripts)**
- 기존 9개 스크립트를 `Assembly-CSharp`에서 이 어셈블리로 옮깁니다. 파일 자체는 이동하지 않습니다.
- 필요한 참조: `Unity.InputSystem` (`Keyboard`, `InputSystemUIInputModule`), `UnityEngine.UI` (Canvas, Text, Image). `autoReferenced: true`.
- 확인 사항: `com.coplaydev.unity-mcp` 등 다른 코드가 `LastStop` 타입을 참조하지 않는지 감독 PC에서 컴파일 결과로 확인합니다(리포지토리의 `Assets` 안에는 `Assets/Scripts` 외 .cs 파일이 없습니다).

예시 (감독 확인 전 초안):
```json
{
  "name": "LastStop.Runtime",
  "rootNamespace": "LastStop",
  "references": ["Unity.InputSystem", "UnityEngine.UI"],
  "autoReferenced": true
}
```

**B. `LastStop.Tests.EditMode` (Assets/Tests/EditMode)**
```json
{
  "name": "LastStop.Tests.EditMode",
  "references": ["LastStop.Runtime", "UnityEngine.TestRunner", "UnityEditor.TestRunner"],
  "includePlatforms": ["Editor"],
  "overrideReferences": true,
  "precompiledReferences": ["nunit.framework.dll"],
  "autoReferenced": false,
  "defineConstraints": ["UNITY_INCLUDE_TESTS"]
}
```

**C. `LastStop.Tests.PlayMode` (Assets/Tests/PlayMode)**
- B와 같되 `includePlatforms: []`(전체), `UnityEditor.TestRunner` 제외 가능.
- 입력 시뮬레이션을 쓸 때만 `Unity.InputSystem`, `Unity.InputSystem.TestFramework` 추가 + `manifest.json`에 `"testables": ["com.unity.inputsystem"]`. 1차 범위에서는 **사용하지 않는 것**을 권장합니다(리플렉션으로 `Restart`·`ChooseLevelUp` 직접 호출).

**.meta 관련 영향**
- 새 asmdef 3개, 새 폴더(`Assets/Tests`, `EditMode`, `PlayMode`), 새 테스트 .cs 파일마다 Unity가 첫 임포트 시 `.meta`를 생성합니다. **감독 PC에서 Unity가 생성한 .meta를 그대로 같은 PR에 커밋**해야 합니다. 클라우드에서 손으로 .meta를 만들지 않습니다(GUID 충돌 위험).
- 기존 `.cs.meta` 9개의 GUID는 변하지 않습니다. 씬·프리팹이 이 스크립트를 직렬화 참조하지 않으므로 어셈블리 이동으로 끊기는 참조도 없습니다.
- asmdef 추가 직후 전체 재컴파일이 발생합니다. `Library`는 커밋하지 않습니다.
- 빌드 영향: 테스트 asmdef는 `UNITY_INCLUDE_TESTS` 제약으로 플레이어 빌드에 포함되지 않습니다. 런타임 asmdef는 빌드에 `LastStop.Runtime.dll`로 포함되며 Windows 빌드 재검증이 필요합니다.

### 3.2 테스트 케이스

단계 구분: **A** = 리팩터 없이(런타임 asmdef만) 작성 가능, **B** = 4장 리팩터 후 EditMode로 전환.

| ID | GameDesign 규칙 | 종류 | 단계 | 준비 → 실행 → 기대 |
|---|---|---|---|---|
| T01 | 공유 HP 시작값 | EditMode | A | `new LastStopRunState()` → HP 100/100, `IsGameOver == false` |
| T02 | HP ≤ 0이면 게임오버 | EditMode | A | `SharedHealth = 0` → true; `-1` → true; `0.01` → false |
| T03 | 1:1 침입 (개수) | PlayMode 리플렉션 | A | 외부 적 1마리를 버스 위치로 이동, 1프레임 → 외부 -1, 내부 +1, 다른 외부 적 수 불변 |
| T04 | 1:1 침입 (동일 개체 보존) | PlayMode 리플렉션 | A | T03에서 이동한 `Enemy`가 같은 인스턴스, `Health` 보존, `IsInside == true`, 공유 HP 불변 |
| T05 | 같은 프레임 다중 접촉도 1:1 | PlayMode 리플렉션 | A | 외부 3마리 동시 접촉 → 내부 +3, 외부 -3 |
| T06 | 침입 후 외부 투사체 추적 중단 | PlayMode | A | 투사체 비행 중 대상 침입 → 투사체 파괴, 적 체력 불변 |
| T07 | 적 수만으로는 피해 없음 | PlayMode 리플렉션 | A | 내부 적 10마리를 공격 범위 밖(출입구)에 두고 3초 → HP 100 유지 |
| T08 | 50 경고는 게임오버가 아님 | PlayMode 리플렉션 | A | 내부 50마리, 공격 범위 밖 → `breachWarningActive == true`, `gameOver == false`, HP 100, `CueRequested`에 `BreachWarningOn` 1회 |
| T09 | 49 → 경고 없음, 50 → 49 → 해제 | PlayMode 리플렉션 / EditMode | A/B | 임계 경계 확인 |
| T10 | 실제 피격 1회 = 7 감소 | PlayMode 리플렉션 | A | 코어 0.70 안에 적 1마리, 고정 dt 1/60로 0.8초 → HP 93, `CoreDamaged` 1회 |
| T11 | 0.8초 공격 간격 | PlayMode / EditMode | A/B | 고정 dt로 5회 피격 시각 차이가 0.8 이상 0.8+dt 이하. B단계는 `AttackTimer.Tick` 반복으로 정확히 검증 |
| T12 | 0.8초 미만은 피해 없음 | 동일 | A/B | 0.79초 누적 → HP 100 |
| T13 | 타깃 변경 시 타이머 리셋 | 동일 | A/B | Core 0.5초 → Player 전환 → 0.5초 → 피해 없음(기존 2026-11-11 프로브와 동일 조건) |
| T14 | 플레이어 우선 타깃팅 | 동일 | A/B | 플레이어 0.58, 코어 0.70 모두 안 → Player, `PlayerDamaged` 이벤트 |
| T15 | 동시 2타 = 14 감소 | 동일 | A/B | 적 2마리 같은 프레임 타격 → HP 86 |
| T16 | HP 바닥 클램프 | EditMode | B | HP 5에서 1타 → 0, 실제 피해량 5 |
| T17 | 실제 피격으로 HP 0 → 게임오버 | PlayMode 리플렉션 | A | HP 7 설정 후 1타 → `gameOver == true`, `Time.timeScale == 0`, 결과 패널 활성 |
| T18 | 게임오버 후 추가 피해 없음 | PlayMode 리플렉션 | A | T17 이후 프레임 진행 → HP 0 유지, 적 체력 불변 |
| T19 | 레벨업 중 타이머 정지 | PlayMode 리플렉션 | A | `ShowLevelUp` 후 1초(unscaled) → `runElapsedSeconds`, 적 `DamageTimer`, 스폰 타이머, HP, 적 위치 모두 불변 |
| T20 | 레벨업 중 적 피해 차단 | PlayMode 리플렉션 | A | `levelUpOpen` 상태에서 `DamageEnemy` 호출 → 체력 불변 |
| T21 | 선택 후 재개 | PlayMode 리플렉션 | A | `ChooseLevelUp(3)` → `timeScale == 1`, HP·최대 HP +25 |
| T22 | 연속 레벨업 | PlayMode / EditMode | A/B | XP를 2레벨분 설정 → 선택 후 즉시 다음 제안, `timeScale` 0 유지 |
| T23 | 480초 도착 | PlayMode 리플렉션 / EditMode | A/B | `runElapsedSeconds = 479.99` → 1프레임 → `gameOver == true`, HP 0 아님(도착) |
| T24 | 재시작 상태 초기화 | PlayMode | A | `SampleScene` 로드 → HP·적·레벨 변경 → `gameOver` 후 `Restart` 호출 → 한 프레임 대기 → 프로토타입 1개, HP 100/100, 레벨 1, 내외부 적 수 초기값, `timeScale == 1`, 외부 시점 |
| T25 | 재시작 시 오디오 GameOver 재생 가능 | PlayMode | A | 재시작 후 `gameOverPlayed` 리셋 확인(`LastStopAudio` 리플렉션) |
| T26 | 스모크: 부트스트랩 1개, 300프레임 예외 없음 | PlayMode | A | `LogAssert.NoUnexpectedReceived()` |
| T27 | 오디오 계약 | EditMode | A | Edit 모드 `TryPlay` → `CueRequested` 1회, 예외 없음 |

PlayMode 공통 규칙:
- `[UnitySetUp]`: `Time.timeScale = 1`, `Time.captureDeltaTime = 1f/60f`(고정 dt), 필요 시 `SceneManager.LoadScene("SampleScene")`.
- `[UnityTearDown]`: 프로토타입 파괴, `Time.timeScale = 1`, `Time.captureDeltaTime = 0`, `LastStopAudio.CueRequested` 구독 해제.
- 리플렉션 헬퍼 하나(`PrototypeProbe`)에 필드 이름을 모아, 리팩터 시 한 곳만 고치도록 합니다.

### 3.3 감독 확인이 필요한 질문
- T11 허용 오차: 현재 구현은 초과분을 버리므로 실제 간격이 "0.8 + 최대 1프레임"입니다. 테스트 기준을 이대로 둘지, 0.8초 정확 간격을 규칙으로 할지 결정이 필요합니다(바꾸면 밸런스 변경).
- T23 도착과 같은 프레임의 HP 0: 현재는 패배가 먼저입니다(`Update` 순서). 이것을 규칙으로 고정할지.

---

## 4. 최소 리팩터 제안 (선택, 감독 승인 필요)

목표: **동작 변경 없이** 규칙 로직을 순수 C# 클래스로 꺼내 EditMode 테스트 대상으로 만듭니다. 수치·순서·프레임 의미는 그대로 둡니다.

### 4.1 추출 대상

| 신규 타입 (안) | 옮길 로직 | 원본 위치 | 주의 |
|---|---|---|---|
| `SharedHealth` (또는 `LastStopRunState` 확장 메서드) | `ApplyHit(7) → 실제 피해량`, 0 클램프, `AddMax(25)`, `IsDepleted` | `:1467-1468`, `:1429-1430` | `LastStopRunState` 직렬화 필드 이름 유지 |
| `EnemyAttackClock` | `Tick(dt, target) → bool hit`, 타깃 변경 시 0 리셋, 0.8 도달 시 **초과분 버림** | `:850-854`, `:1464-1466` | 순서 보존: 타깃이 바뀐 프레임에 먼저 0으로 리셋하고, 같은 프레임에 타깃이 있으면 곧바로 그 프레임 dt를 누적함. None 타깃 동안에는 누적하지 않음. 출입구 경직 중에는 리셋·누적 모두 생략(`:845`) |
| `InteriorTargetRule` (정적) | `Select(playerDist, coreDist) → None/Player/Core`, 0.58/0.70, 플레이어 우선 | `:846-849` | enum을 internal 이상으로 |
| `BreachMonitor` | `Update(count) → (warningActive, justActivated)`, 임계 50, peak | `:865-875` | 오디오 호출은 프로토타입에 남김 |
| `RunClock` | `Tick(dt) → arrived`, 480초, 남은 시간 표시용 계산 | `:219-224`, `:1598` | |
| `ExperienceTrack` | `Gain`, `TryConsumeLevel()`, `Ceil(×1.35)` | `:1334-1348` | `ShowLevelUp`은 프로토타입에 남김 |

- 모든 신규 타입은 `UnityEngine.Time`을 참조하지 않고 `deltaTime`을 인자로 받습니다. `Mathf` 사용은 EditMode에서도 문제없습니다.
- 접근 수준은 `internal` + 런타임 asmdef의 `AssemblyInfo.cs`에 `[assembly: InternalsVisibleTo("LastStop.Tests.EditMode")]`, 또는 단순하게 `public`. 감독 선택.
- 프로토타입은 기존 private 메서드에서 새 타입을 호출만 하도록 바꿉니다. 메서드 시그니처와 `Update` 순서는 유지합니다.

### 4.2 하지 않는 것
- 수치(7, 0.8, 50, 480, 25 등) 변경, 프레임 순서 변경, 입력 추상화, 씬·프리팹 변경, `Restart` 방식 변경, `exteriorPlayer` 수정(별도 이슈로 보고만).

### 4.3 변경 파일 (예상)
- 수정: `Assets/Scripts/LastStopPrototype.cs`, `Assets/Scripts/LastStopContentDefinitions.cs`(선택)
- 신규: `Assets/Scripts/Rules/*.cs` 5~6개 + Unity 생성 `.meta`, `Assets/Scripts/Rules.meta`, 선택 시 `Assets/Scripts/AssemblyInfo.cs`
- 신규(3장): asmdef 3개와 `.meta`, `Assets/Tests/**`

### 4.4 위험과 완화
| 위험 | 영향 | 완화 |
|---|---|---|
| 타이머 누적·리셋 순서가 미묘하게 달라짐 | 0.8초 간격, 첫 타격 시점 변화 | 리팩터 **전에** 단계 A의 T10~T15 PlayMode 테스트를 먼저 머지하고, 리팩터 후 같은 테스트 통과 확인 |
| 런타임 asmdef 참조 누락으로 컴파일 실패 | 프로젝트 전체 빌드 불가 | 작은 PR로 asmdef만 먼저 추가, 감독 PC에서 0 error 확인 |
| `RuntimeInitializeOnLoadMethod`가 다른 어셈블리에서 실행 순서 변화 | 부트스트랩·오디오 리셋 순서 | `SubsystemRegistration`(오디오) → `AfterSceneLoad`(부트스트랩)는 단계가 달라 영향 없을 것으로 예상. T26 스모크로 확인 |
| Windows 빌드 차이 | 제출 빌드 회귀 | 리팩터 후 `SubmissionVerification.md`와 같은 빌드·실행 확인 |
| 리플렉션 테스트가 필드 이름 변경에 깨짐 | 테스트 유지보수 | 헬퍼 한 곳에 집중, 단계 B로 EditMode 전환 후 해당 PlayMode 테스트 축소 |

### 4.5 권장 PR 순서
1. PR-1: `LastStop.Runtime.asmdef` + 테스트 asmdef 2개 + T01, T02, T27, T26 (코드 동작 변경 0)
2. PR-2: 리플렉션 기반 PlayMode 기준선 T03~T25
3. PR-3 (승인 시): 4장 리팩터 + 단계 B EditMode 테스트, PR-2 테스트 전부 통과 확인

---

## 5. 감독 PC 배치모드 실행 명령 (Windows)

전제:
- Unity 6000.3.15f1 Hub 기본 설치 경로를 가정합니다. 다르면 `$unity`를 바꿉니다.
- **같은 프로젝트를 Editor로 열어 둔 상태에서는 실행할 수 없습니다**(프로젝트 잠금). Editor를 닫고 실행합니다.
- `-runTests` 사용 시 `-quit`를 붙이지 않습니다. 테스트가 끝나면 Unity가 스스로 종료합니다.
- 종료 코드: 0 = 전부 통과, 2 = 테스트 실패 있음, 그 외 = 실행 오류(컴파일 실패, 라이선스 등). Unity.exe는 GUI 프로그램이라 PowerShell에서 `Start-Process -Wait -PassThru`로 종료 코드를 받습니다.

### PowerShell

```powershell
$unity   = "C:\Program Files\Unity\Hub\Editor\6000.3.15f1\Editor\Unity.exe"
$project = "C:\path\to\LAST_STOP"           # 실제 경로로 변경
$out     = "$project\TestResults"
New-Item -ItemType Directory -Force -Path $out | Out-Null

# EditMode
$p = Start-Process -FilePath $unity -Wait -PassThru -ArgumentList @(
  "-batchmode", "-nographics",
  "-projectPath", "`"$project`"",
  "-runTests", "-testPlatform", "EditMode",
  "-testResults", "`"$out\EditMode-results.xml`"",
  "-logFile", "`"$out\EditMode-log.txt`""
)
"EditMode exit code: $($p.ExitCode)"

# PlayMode (그래픽 장치 유지: -nographics 생략 권장)
$p = Start-Process -FilePath $unity -Wait -PassThru -ArgumentList @(
  "-batchmode",
  "-projectPath", "`"$project`"",
  "-runTests", "-testPlatform", "PlayMode",
  "-testResults", "`"$out\PlayMode-results.xml`"",
  "-logFile", "`"$out\PlayMode-log.txt`""
)
"PlayMode exit code: $($p.ExitCode)"
```

### cmd.exe

```bat
set UNITY="C:\Program Files\Unity\Hub\Editor\6000.3.15f1\Editor\Unity.exe"
set PROJECT=C:\path\to\LAST_STOP
if not exist "%PROJECT%\TestResults" mkdir "%PROJECT%\TestResults"

start "" /wait %UNITY% -batchmode -nographics -projectPath "%PROJECT%" -runTests -testPlatform EditMode -testResults "%PROJECT%\TestResults\EditMode-results.xml" -logFile "%PROJECT%\TestResults\EditMode-log.txt"
echo EditMode exit code: %ERRORLEVEL%

start "" /wait %UNITY% -batchmode -projectPath "%PROJECT%" -runTests -testPlatform PlayMode -testResults "%PROJECT%\TestResults\PlayMode-results.xml" -logFile "%PROJECT%\TestResults\PlayMode-log.txt"
echo PlayMode exit code: %ERRORLEVEL%
```

### 유용한 추가 옵션
- 특정 테스트만: `-testFilter "LastStop.Tests.EditMode.SharedHealthTests"` (세미콜론으로 여러 개, 정규식 가능)
- 카테고리: `-testCategory "Rules"`
- 어셈블리 지정: `-assemblyNames "LastStop.Tests.PlayMode"`

### 결과 확인
- `TestResults\*-results.xml`은 NUnit 3 형식입니다. 첫 줄의 `<test-run ... result="Passed" total=".." passed=".." failed="..">`를 보면 됩니다.
- PowerShell 요약:
  ```powershell
  [xml]$r = Get-Content "$out\EditMode-results.xml"
  $r.'test-run' | Select-Object result, total, passed, failed, skipped
  ```
- 컴파일 오류로 테스트가 실행되지 않으면 XML이 생성되지 않을 수 있습니다. 이때는 `*-log.txt`에서 `error CS`를 검색합니다.
- `TestResults/` 폴더는 커밋하지 않도록 `.gitignore` 추가를 별도 제안합니다(현재 `.gitignore`에 없음). 필요한 결과만 `Docs/Verification/`에 요약해 남기는 기존 방식을 따릅니다.

### 아직 확인되지 않은 것
- 위 명령은 이 클라우드 세션에서 실행하지 못했습니다. 첫 실행 결과(종료 코드, XML 생성 여부)를 이 문서나 `Docs/Tasks.md`에 기록해 주시면 다음 스프린트 계획에 반영합니다.
- PlayMode를 `-nographics`로 실행하면 카메라·Canvas 생성 코드가 문제없이 도는지 확인되지 않았습니다. 그래서 PlayMode는 그래픽 장치를 유지하는 명령을 기본으로 적었습니다.
