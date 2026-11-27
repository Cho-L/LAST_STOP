# LAST STOP — Sprint 1 아트 사양서

작성: Art 팀 (텍스트 전용 에이전트, 2026-11-27)
상태: 제안 / 디렉터 검토 대기. 이 문서는 사양과 생성 프롬프트이며, 실제 이미지 파일은 만들지 않았다. 아래 "신규 에셋"은 모두 아직 존재하지 않는다.

참조: `AGENTS.md`, `Docs/GameDesign.md`, `Docs/Tasks.md`(BLOCKED 루프 무기 4방향, REVIEW 내부 스프라이트 크기/프레임 애니메이션 부재), `Docs/Teams/Art.md`, `Assets/Scripts/LastStopPrototype.cs`, `Assets/Scripts/LastStopSpriteMotion.cs`, `Docs/Media/last-stop-preview.gif`.

---

## 0. 기준 수치 (코드에서 측정)

| 항목 | 값 | 출처 |
|---|---|---|
| 카메라 | 외부/내부 모두 Orthographic, size 5.5 → 화면 세로 11 world unit | `ConfigureCamera` |
| 960x540에서 1 world unit | 540 / 11 = **49.09 화면 px** (정수 아님) | 계산 |
| 스프라이트 크기 결정 방식 | `CreateBlock`이 원본 크기와 무관하게 지정한 world 크기로 **비균등 스케일** (`size.x / spriteSize.x`, `size.y / spriteSize.y`) | `CreateBlock`, `ConfigureSprite` |
| 로드 경로 | `Resources.LoadAll<Sprite>("Art/...")` → 실제 파일은 `Assets/Resources/Art/` (`Assets/Art/`는 현재 `.gitkeep`만 있음) | `LoadArtSprites` |
| 런타임 필터 | 배경과 스윙 시트는 코드에서 `FilterMode.Point`로 강제 | `LoadBackdropSprite`, `ConfigureInteriorSwing` |
| 플레이어 공격 | 입력 간격 0.18초, 포즈 표시 0.16초 (0–0.055초 contact, 이후 follow-through), 타격 판정은 **입력 즉시** | `InteriorAttackInterval`, `LastStopSpriteMotion` |
| 내부 적 공격 | 0.8초 간격, 1회 7 HP | `DamageTimer < 0.8f`, Tasks.md |
| 포구 위치 | 방향과 무관하게 버스 중심 + (0.4, 0.7) 고정 | `FireBusProjectile` |

---

## 1. 현재 에셋 인벤토리

모든 PNG는 `Assets/Resources/Art/`에 있다. "화면 크기"는 960x540 Game view 기준 계산값이며 `Docs/Media/last-stop-preview.gif` 프레임으로 육안 확인했다.

| 파일 | 픽셀 크기 | 내용 (슬라이스) | 사용처 | 960x540 문제 |
|---|---|---|---|---|
| `LastStop_MvpSpritesheet_v1.png` | 1774x887 | `Player_Exterior_TopDown` 294x313, `Player_Interior_SideView` 322x330, `Enemy_Exterior_TopDown` 162x292, `Enemy_Interior_SideView` 340x235, `Bus_Exterior_Yellow` 1230x511, `Bus_Interior_Door_Seal` 342x466 | 내부 플레이어(0.70x0.90 wu), 외부 적(0.60x0.60 wu), **내부 적도 외부 탑다운 스프라이트 사용** (`SeparateInteriorEnemyVisual`, `LastStopPrototype.cs:807`에서 복사), 외부 버스(4.8x1.8 wu) | 내부 플레이어 약 **34x44 화면 px**, 가로 약 21% 눌림. 적 약 **29x29 px**, 162x292 원본을 정사각형에 넣어 가로 약 1.8배 늘어남. 내부에 탑다운 적이 나와 시점이 맞지 않음(`Enemy_Interior_SideView` 미사용). 버스 236x88 px, 약 11% 비균등. `Player_Exterior_TopDown`은 캐릭터가 외부에 나오지 않으므로 미사용. `Bus_Interior_Door_Seal`은 **봉인 메커닉이 없으므로** 폐기 대상. 원본이 약 10–25배 축소되어 디테일이 뭉개지고 픽셀 그리드가 없다. |
| `LastStop_GuardSwing_v6.png` | 896x330 | 448x330 2프레임 (contact / follow-through). 메타 자동 슬라이스에 7x6 같은 잔여 조각 15개 | `ConfigureInteriorSwing`이 직접 `Sprite.Create`로 자름 (가로 448 고정, idle 너비 322 검사) | 2프레임뿐이라 걷기/콤보 구분이 없다. 노란 스윙 궤적이 그림에 포함되어 있어 좌우 반전·콤보별 변화가 어렵다. 코드가 크기에 하드코딩되어 있어 교체 시 코드도 바꿔야 한다. |
| `LastStop_InteriorCore_v1.png` | 1254x1254 | 코어 본체 1185x1207 + 잔여 조각 19개 | 내부 코어 (0.96x1.05 wu) | 화면 약 **47x52 px**, 25배 축소로 화면 그래프·버튼이 읽히지 않는다. 저HP·피격 상태는 색 틴트로만 표현된다. |
| `LastStop_CombatVfx_v1.png` | 1774x887 | `Vfx_Bean_Projectile`, `Vfx_Bubble_Projectile`, `Vfx_Flame_Loop`, `Vfx_Hit`, `Vfx_Enemy_Death`, `Vfx_Seal_Complete`. 코드가 (310,632,150,175) 영역을 `Vfx_Bus_Muzzle`로 별도 생성 | 탄환, 화염, 피격, 사망, 버스 궤적, 포구 섬광 | 각 효과가 다프레임 스트립인데 슬라이스는 1개씩이라 애니메이션 없이 첫 묶음이 통째로 표시된다. 빨강·초록 색번짐(halo)이 보인다. `Vfx_Seal_Complete`(자물쇠)는 봉인 메커닉이 없으므로 폐기 대상. 포구 섬광이 반짝이 별 모양이라 포구 위치를 알려주지 못한다. |
| `LastStop_NightBusInterior_v1.png` | 1672x941 | 내부 배경 1장 | 내부 배경 (약 21.6x11.5 wu → 약 1058x565 화면 px) | 0.63배 Point 축소라 1–2 px 선(손잡이, 좌석 무늬)이 끊기고 스크롤 없이도 계단 현상이 보인다. 배경 디테일이 캐릭터보다 촘촘해 캐릭터가 묻힌다. 노란 기둥(#DF9717 계열)이 플레이어 주황 재킷과 색이 비슷하다. |
| `LastStop_NightRoad_v1.png` | 1672x941 | 외부 배경 1장 (측면 시점 거리) | 외부 배경 3장 반복 스크롤 (13.2 wu 높이 → 약 0.69배) | 축소 계단 현상(위와 같음). **배경은 측면 시점인데 GameDesign.md는 외부를 탑다운으로 정의**하고 적은 원형으로 스폰된다. 루프 무기 시점 결정에 영향을 준다(8절 질문 1). |
| (없음) `LastStop_BusBody_v2`, `LastStop_BusWheels_v2` | — | 코드가 로드를 시도함 | 바퀴 애니메이션 버스 | 파일이 없어 `Bus_Exterior_Yellow`로 대체된다. |
| (없음) 루프 무기 | — | — | — | Tasks.md BLOCKED: 4방향 컨셉이 960x540 검토에서 위/아래 가독성과 아래 방향 포구 위치를 통과하지 못해 임포트하지 않았다. |

### 공통 기술 문제

1. **진짜 픽셀아트가 아니다.** 이미지 생성 결과를 그대로 사용해 픽셀 그리드가 없고 색이 수만 개이다(시트당 약 4.6만–31만 색).
2. **알파가 반투명이다.** 보이는 픽셀 대부분이 alpha 192–254이고 완전 불투명 픽셀은 수백 개뿐이다. 외곽에 alpha 1–63 후광이 수만 픽셀 있다. 어두운 배경에서 테두리가 번지고 메타 자동 슬라이스에 잔여 조각이 생기는 원인이다.
3. **임포트 설정이 섞여 있다.** Default 플랫폼은 Compression None이지만 **Standalone 오버라이드가 `textureCompression: 1`(압축)**이다. 배경 2장은 `filterMode: 1`(Bilinear)이고 런타임에 Point로 바꾼다. PPU는 100이지만 코드가 크기를 강제하므로 의미가 없다.
4. **비균등 스케일**로 원본 비율이 깨진다. 프레임별로 크기가 다르면 같은 world 크기로 늘려지면서 출렁인다.
5. **프레임 애니메이션이 없다.** 움직임은 `LastStopSpriteMotion`의 스케일·기울기·틴트뿐이다(Tasks.md REVIEW).

---

## 2. 공통 규격 (신규 에셋 전체)

### 2.1 픽셀 배율 제안 (Development 결정 필요)

960x540에서 **아트 1픽셀 = 화면 2x2 픽셀**(가상 해상도 480x270)을 목표로 한다.

- **권장안 B (프레이밍 변화 최소):** 카메라 orthographic size 5.5 → **5.625**, 스프라이트 **PPU 24**. 1 wu = 48 화면 px = 24 아트 px이므로 정확히 2배이다. 화면 세로가 11 → 11.25 wu로 약 2%만 늘어난다.
- 대안 A: Pixel Perfect Camera(참조 해상도 480x270, PPU 32). 화면 세로가 8.4375 wu로 줄어 스폰·배치 좌표를 다시 잡아야 하므로 이번 스프린트에는 권장하지 않는다.
- 어느 쪽이든 신규 스프라이트는 `CreateBlock`의 강제 크기 대신 **스케일 1(원본 px / PPU)**로 배치해야 정수 배율이 유지된다. 이것은 Development 소관이며 Art는 코드나 씬을 수정하지 않는다.
- 이 문서의 모든 world 크기는 **안 B(PPU 24)** 기준이다.

### 2.2 그리기 규칙

- 원본은 목표 픽셀 크기 그대로 그린다(업스케일 원본 금지). 반투명 금지: alpha는 0 또는 255만 쓴다.
- 1px 외곽선은 공용 `#141019`를 쓴다. 안티앨리어싱 없음, 디더링은 최소화한다.
- 광원은 왼쪽 위(천장 형광등)에서 온다. 그림자는 스프라이트에 넣지 않는다(바닥 그림자는 필요하면 별도 스프라이트).
- 기본 방향은 **오른쪽을 본다**. 왼쪽은 `flipX`를 쓴다. Unity `flipX`는 피벗 기준으로 반전되므로 피벗이 발 위치에 정확히 있어야 한다.
- 모든 프레임의 발 기준선은 셀 맨 아래 행(y=0)이고, 몸통 중심 x는 피벗 x에 고정한다(idle/walk에서 몸통 흔들림은 ±1px 이내).
- 배경 대비: 내부 바닥은 `#12161F`–`#1C2026`, 벽은 `#252224`–`#352E30`, 기둥은 `#DF9717`이다. 캐릭터의 가장 밝은 면은 이보다 명도가 확실히 높아야 하고, 기둥색 `#DF9717`과 같은 색은 쓰지 않는다.

### 2.3 파일 이름, 위치, 슬라이스 이름

- 이름: `LastStop_<대상>_<시점>_<내용>_v<N>.png`. 기존 파일을 같은 이름으로 덮어쓰지 않는다. 새 파일에는 새 `.meta`가 생기고, 기존 `.meta` GUID는 건드리지 않는다.
- 목표 경로: `Assets/Art/<분류>/...` (아래 각 항목). 현재 코드는 `Resources/Art`만 로드하므로, **Resources로 옮길지 직렬화 참조로 바꿀지는 Development가 결정**한다(통합 시 협의).
- 슬라이스 이름: `<접두어>_<애니>_<두 자리 번호>`, 예: `PlayerInt_Walk_00`.

### 2.4 Unity 임포트 설정 (신규 시트 공통)

| 설정 | 값 |
|---|---|
| Texture Type | Sprite (2D and UI) |
| Sprite Mode | Multiple (Grid By Cell Size, 각 항목의 셀 크기, Offset 0, Padding 0) |
| Pixels Per Unit | **24** (안 A 채택 시 32) |
| Mesh Type | Full Rect |
| Extrude Edges | 0 |
| Pivot | Custom, 항목별 값 (모든 슬라이스 동일) |
| Filter Mode | **Point (no filter)** |
| Compression | **None** — Default와 Standalone 등 **모든 플랫폼 오버라이드**에서 |
| Generate Mip Maps | Off |
| Wrap Mode | Clamp |
| sRGB | On |
| Alpha Is Transparency | On |
| Max Size | 2048 (시트보다 크게) |
| Read/Write | Off |

---

## 3. 우선순위 신규 에셋 사양

프레임 속도는 표시 목표값이며 실제 재생 연결은 Development의 일이다.

### A1. 내부 플레이어 (Rookie Guard, 측면) — 우선순위 1

| 항목 | 값 |
|---|---|
| 파일 | `Assets/Art/Characters/Player/LastStop_Player_Interior_Sheet_v1.png` |
| 셀 | **96x64 px**, 6열 x 6행 → 시트 **576x384** |
| 캐릭터 크기 | 머리부터 발까지 약 48 px(2.0 wu, 화면 96 px), 몸통 폭 약 22–26 px. 현재 화면 44 px의 약 2.2배 |
| 피벗 | 발 중앙 (36, 0) → **(0.375, 0)**. 앞쪽 60 px는 공격 리치 공간 |
| 슬라이스 접두어 | `PlayerInt` |

| 행 | 애니 | 프레임 | 속도 | 비고 |
|---|---|---|---|---|
| 0 | Idle | 4 | 6 fps | 호흡 1px, 곤봉은 내린 상태 |
| 1 | Walk | 6 | 10 fps | 루프, 접지 프레임 0과 3 |
| 2 | AttackA (1타) | 3 | 약 19 fps (0.16초) | **0 = contact**(판정이 입력 즉시이므로 예비동작 없음), 1 = follow-through, 2 = recover. 수평 스윙 |
| 3 | AttackB (2타) | 3 | 약 19 fps | 0 = contact(올려치기), 1, 2 |
| 4 | AttackC (3타 마무리) | 4 | 약 20 fps (0.20초) | 0 = contact(내려찍기), 1 = 임팩트 스미어, 2, 3 = recover. 가장 큰 실루엣 |
| 5 | Hurt | 2 | 12 fps | 뒤로 젖힘. 흰색 플래시는 코드가 처리하므로 그림은 원색 |

- 스윙 궤적(smear)은 캐릭터 시트에 **넣지 않는다**. 필요하면 별도 VFX로 만든다. 곤봉 끝 스미어는 2–3px 단색 잔상까지만 허용한다.
- 팔레트(최대 20색 + 외곽선, 기존 디자인 유지): 머리 `#6A302B` `#844030` `#A1683E`, 피부 `#F1B276` `#D98E5A`, 재킷 `#E07A26` `#B8581C` `#F6B04A`(기둥색 `#DF9717`과 구분되도록 붉은 쪽으로 이동), 털 깃 `#F3E6CF`, 바지 `#2E3550` `#1E2238`, 부츠 `#5A3F38` `#3A2826`, 배낭 `#5E6B2E` `#3F4A20`, 곤봉 `#7A4A2A` `#4E2E1A`, 가시 `#B8C0CC`, 눈 `#141019`.

### A2. 내부 적 (Rusher, 측면) — 우선순위 1

| 항목 | 값 |
|---|---|
| 파일 | `Assets/Art/Characters/Enemies/LastStop_Rusher_Interior_Sheet_v1.png` |
| 셀 | **80x48 px**, 6열 x 5행 → 시트 **480x240** |
| 캐릭터 크기 | 몸길이 약 44 px(꼬리 포함), 키 약 30 px(1.25 wu, 화면 60 px). 플레이어보다 확실히 작게 |
| 피벗 | 앞발과 뒷발 사이 바닥 (32, 0) → **(0.4, 0)**. 오른쪽(코어 방향)을 보고, 앞쪽 공간은 덤벼들기용 |
| 슬라이스 접두어 | `RusherInt` |

| 행 | 애니 | 프레임 | 속도 | 비고 |
|---|---|---|---|---|
| 0 | Walk(질주) | 6 | 12 fps | 루프, 꼬리 흔들림 |
| 1 | Attack | 4 | 12 fps (약 0.33초) | 0 = 웅크림, 1 = 덤벼듦, **2 = 물기/할퀴기 hit 프레임**, 3 = 복귀. 0.8초 공격 주기 안에 끝난다. hit 프레임과 데미지 타이밍 동기화는 Development에 요청 |
| 2 | Hurt | 2 | 12 fps | 넉백 자세 |
| 3 | Death | 5 | 12 fps | 마지막 프레임은 거의 빈 연기 2–3점. 기존 `Vfx_Enemy_Death`와 겹쳐도 읽히게 |
| 4 | Breach(진입) | 3 | 10 fps | 문 쪽에서 튀어 들어와 착지. `TriggerEntry`와 연결 |

- 50마리가 겹치는 경보 상황을 고려해 실루엣을 단순하게 한다(큰 꼬리, 밝은 눈, 분홍 줄무늬).
- 팔레트(최대 14색 + 외곽선, 크로마는 분홍과 겹치지 않도록 **초록 `#00FF00`**): 몸 `#4B293D` `#703950` `#834859`, 윗면 림라이트 `#A7678A`(어두운 바닥과 분리), 줄무늬 `#D99688` `#FBC9B3`, 눈 `#FFD23A`, 입 `#C0303A`, 이빨·발톱 `#F3E6CF`, 외곽선 `#2B0C21` 또는 공용 `#141019`.

### A3. 루프 무기 (콩알포, 4방향) — 우선순위 1 (Tasks.md BLOCKED 해소용)

| 항목 | 값 |
|---|---|
| 파일 | `Assets/Art/Vehicles/Weapons/LastStop_RoofWeapon_Bean_Sheet_v1.png` |
| 셀 | **32x32 px**, 3열(Idle, Recoil, Settle) x 4행(R, L, U, D) → 시트 **96x128** |
| 화면 크기 | 셀 64x64 화면 px. 실제 포탑 몸체는 약 20x14 px(화면 40x28)이며 현재 버스(화면 236x88) 지붕 위에 올라간다 |
| 피벗 | 포탑 회전 중심/장착점 (16, 16) → **(0.5, 0.5)**. 아래 방향 포신이 장착점 아래로 내려갈 공간이 필요해서 중앙에 둔다 |
| 장착 위치 | 현재 코드의 포구 기준점과 맞춰 버스 중심 + 약 (0.4, 0.75) wu. 최종값은 Development가 정한다 |
| 슬라이스 접두어 | `RoofBean` (예: `RoofBean_R_00`, `RoofBean_D_01`) |

**포구 좌표 (필수 납품 데이터).** 셀 왼쪽 아래를 (0,0)으로 하는 픽셀 경계 좌표이다. 포신 끝 개구부 중앙이 이 점에서 1px 안에 있어야 한다. 3프레임 모두 같은 점이다(반동 프레임에서 포신이 1–2px 뒤로 물러나도 포구 기준점은 Idle 값을 쓴다).

| 방향 | 포구 점 (px) | 피벗 기준 오프셋 (px) | world 오프셋 (PPU 24) |
|---|---|---|---|
| R (오른쪽) | (31, 18) | (+15, +2) | (+0.625, +0.083) |
| L (왼쪽) | (1, 18) | (−15, +2) | (−0.625, +0.083) |
| U (화면 위쪽) | (16, 31) | (0, +15) | (0, +0.625) |
| D (화면 아래쪽) | (16, 1) | (0, −15) | (0, −0.625) |

- 지난 컨셉이 실패한 원인이 위/아래 가독성이므로 다음 규칙을 강제한다.
  - **U와 D도 포신을 실제 길이 12–14 px로 길게 그린다.** 원근 단축으로 짧게 줄이지 않는다.
  - **D**: 포신이 장착점에서 아래로 뻗고, 끝에 4x3 px 어두운 구멍 `#141019`과 그것을 둘러싼 밝은 링 `#F6D365`을 그린다. 장착부(포탑 돔)는 포신 **뒤쪽(위)**에 둬서 포신이 돔을 가리게 한다.
  - **U**: 포신이 돔 뒤로 올라간다. 돔이 포신 아래쪽을 가리고, 포신 끝에 밝은 링 2px.
  - R/L: 포신 끝 2px을 밝은 링색으로 칠한다. L은 R의 단순 반전이 아니라 광원(왼쪽 위)을 유지해 다시 칠한다.
  - 네 방향 모두 포신은 가장 밝은 금속색, 돔은 버스 노랑으로 해서 "어디를 겨누는지"가 색 대비로 먼저 읽히게 한다.
- 포구 섬광(별도 시트): `Assets/Art/Vehicles/Weapons/LastStop_RoofWeapon_Bean_MuzzleFlash_v1.png`, **16x16 x 3프레임 → 48x16**, 오른쪽을 향함, 피벗 **(0, 0.5)**(왼쪽 가장자리 중앙 = 포구 점). 코드가 목표 방향으로 회전시킨다. 30 fps, 3프레임(0.1초). 반짝이 별이 아니라 **포신 방향의 원뿔 모양**이어야 한다.
- 검수용 가이드(선택, 임포트 금지): 같은 크기 PNG에 각 셀의 포구 점을 `#FF00FF` 1px로 찍은 `..._MuzzleGuide_v1.png`. `Assets` 밖(예: Art 작업 폴더)에 두고, 위 표가 정본이다.
- 팔레트(최대 12색 + 외곽선, 크로마 `#FF00FF`): 버스 노랑 `#E8B230` `#B98320` `#7A5418`, 금속 `#3C4163` `#5A6390` `#9AA3C0` `#D5DBEA`, 포구 링 `#F6D365`, 구멍 `#141019`, 콩알 포인트 `#C9D94A`.

### A4. 내부 코어 — 우선순위 2

| 항목 | 값 |
|---|---|
| 파일 | `Assets/Art/Interior/LastStop_Core_Interior_Sheet_v1.png` |
| 셀 | **56x64 px**, 4열 x 4행 → 시트 **224x256** |
| 크기 | 본체 약 44x54 px(2.25 wu 높이, 화면 108 px). 플레이어(48 px)보다 약간 크게 |
| 피벗 | 바닥 중앙 (28, 0) → **(0.5, 0)** |
| 슬라이스 접두어 | `CoreInt` |

| 행 | 상태 | 프레임 | 속도 | 비고 |
|---|---|---|---|---|
| 0 | Idle (정상) | 4 | 6 fps | 화면 파형 2px 이동, 초록 램프 깜빡임 |
| 1 | LowHP (공유 HP 30% 이하) | 4 | 8 fps | 화면이 빨강 `#E0566A`, 경고 램프, 화면 금 1개 |
| 2 | Hit | 2 | 15 fps | 프레임 0: 화면 흰색·몸체 1px 아래로 눌림. 흰 플래시 틴트는 코드에도 있으므로 과하지 않게 |
| 3 | Destroyed | 1 (열 0만) | — | 게임오버 연출용. 화면 깨짐, 불꽃 없음(VFX가 담당) |

- 기존 디자인(화면 속 버스 아이콘, 3색 버튼, 해저드 줄무늬)을 유지하되 48px 높이에서 읽히도록 화면 아이콘은 버스 실루엣 1개와 파형만 남긴다. 볼트·통풍구는 제거한다.
- 팔레트(최대 16색 + 외곽선, 크로마 `#FF00FF`): 몸체 `#1A1D33` `#2A3354` `#3C4163` `#5A6390`(윗면 림라이트 필수, 어두운 벽과 분리), 화면 `#33688E` `#2EA8B7` `#7EF2F5`, 버튼 `#8FD13A` `#3FE0E8` `#E0566A`, 앰버 `#F2A03A`, 해저드 `#C9A227`.

---

## 4. 이미지 생성 프롬프트 (ChatGPT 이미지 생성용, 영어 그대로 붙여넣기)

### 4.0 사용 원칙

- 생성 모델은 정확한 픽셀 그리드와 균일한 셀을 보장하지 못한다. **애니메이션 1행씩 따로 생성**하고, 기존 스프라이트를 참조 이미지로 첨부한다(A1: `Player_Interior_SideView` 영역 잘라낸 것, A2: `Enemy_Interior_SideView`, A4: `LastStop_InteriorCore_v1.png`).
- 결과는 "컨셉 겸 밑그림"이다. 납품 전에 4.5절 후처리와 사람의 픽셀 수정(Aseprite 등)이 필요하다. 후처리 없이 임포트하지 않는다.
- 프롬프트 안의 `{...}` 부분만 행별로 바꾼다.

### 4.1 A1 내부 플레이어 (행별 생성)

```
Pixel art sprite sheet, single horizontal row of {6} equal-size frames, side view, character facing right.
Character: a young female bus guard in a chibi-proportioned but readable style (about 3 heads tall), brown hair in a messy bun, orange-red puffy winter jacket with a cream fur collar, dark navy pants, brown boots, olive green backpack with a bedroll, holding a wooden spiked club in both hands. Match the attached reference character exactly in design and colors.
Animation: {walk cycle, 6 frames, looping, contact poses on frames 1 and 4}.
Strict rules: true low-resolution pixel art drawn on a 96x64 pixel grid per frame, then shown enlarged with nearest-neighbor so every art pixel is a crisp square block of identical size. Each frame cell is the same size, character is the same height in every frame (about 48 art pixels tall), feet on the same baseline at the bottom of each cell, body centered at the same x position in every cell, extra empty space in front of the character for the club swing.
1-pixel dark outline (#141019) around the whole silhouette, no anti-aliasing, no soft shading, no gradients, no glow, no motion blur, no smear trails, no drop shadow, no text, no frame borders, no grid lines.
Limited palette of at most 20 colors. Light comes from the upper left.
Background: perfectly flat solid magenta #FF00FF filling the entire image, no texture, no vignette.
```

행별 `{Animation}` 대체 문구:
- Idle: `idle breathing, 4 frames, looping, club held low, chest rises by 1 pixel`
- AttackA: `first combo strike, 3 frames: frame 1 the club already hitting forward horizontally at full extension (contact, no wind-up), frame 2 follow-through past the body, frame 3 recovering to guard`
- AttackB: `second combo strike, 3 frames: frame 1 rising upward swing at contact, frame 2 follow-through above the head, frame 3 recovering`
- AttackC: `third finishing strike, 4 frames: frame 1 overhead club slamming down in front at contact, frame 2 impact pose crouched low with the club touching the floor, frame 3 follow-through, frame 4 recovering. Biggest and most dynamic silhouette of the set`
- Hurt: `hurt reaction, 2 frames, leaning backward, eyes squeezed shut`

(Idle 4프레임, Attack 3–4프레임일 때는 첫 줄의 `{6}`도 같은 수로 바꾼다.)

### 4.2 A2 내부 적 Rusher (행별 생성)

```
Pixel art sprite sheet, single horizontal row of {6} equal-size frames, side view, creature facing right.
Creature: a feral skunk-like night critter, dark purple fur, large bushy tail with pale pink stripes, glowing yellow eyes, small sharp claws, aggressive low stance. Match the attached reference creature in design and colors.
Animation: {running cycle, 6 frames, looping, tail bouncing}.
Strict rules: true low-resolution pixel art drawn on an 80x48 pixel grid per frame, shown enlarged with nearest-neighbor so every art pixel is a crisp square block of identical size. Every frame cell is the same size, the creature is about 30 art pixels tall and about 44 art pixels long including the tail, paws on the same baseline at the bottom of each cell, body at the same x position in every cell, empty space in front for lunging.
Simple bold silhouette that stays readable when 50 copies overlap. A thin lighter purple rim light on the top edge of the body. 1-pixel dark outline (#141019), no anti-aliasing, no gradients, no glow halo, no motion blur, no drop shadow, no text, no frame borders.
Limited palette of at most 14 colors. Light from the upper left.
Background: perfectly flat solid pure green #00FF00 filling the entire image, no texture.
```

행별 대체 문구:
- Attack: `attack, 4 frames: frame 1 crouching and coiling, frame 2 lunging forward, frame 3 biting and clawing at full extension (the hit frame), frame 4 landing back`
- Hurt: `hurt, 2 frames, knocked backward, eyes shut`
- Death: `death, 5 frames: flinch, collapse, puff into dark purple smoke, smoke thinning, only two or three small smoke specks left`
- Breach: `entering, 3 frames: leaping in from the left mid-air, landing crouched, standing ready`

### 4.3 A3 루프 무기 콩알포 (방향별 생성 권장, 4방향 한 장도 가능)

```
Pixel art sprite sheet for a small roof-mounted cannon turret on a yellow city bus, viewed from a 3/4 top-down angle matching a side-scrolling 2D game. Grid of 3 columns by 4 rows, every cell the same size, 32x32 art pixels per cell, shown enlarged with nearest-neighbor so each art pixel is a crisp square block.
Rows from top to bottom: barrel aiming RIGHT, barrel aiming LEFT, barrel aiming UP (toward the top of the screen), barrel aiming DOWN (toward the bottom of the screen, toward the viewer).
Columns from left to right: idle, recoil (barrel pulled back 2 pixels), settling.
Turret design: a rounded yellow dome (#E8B230) on a short dark metal ring mount, with one long bright steel barrel. The mount center is exactly at the center of every cell. The barrel is long and clearly visible in all four directions, about 13 pixels from the mount center to the tip, never shortened by perspective. The barrel tip has a bright yellow ring and a dark round bore hole.
For the DOWN row the barrel extends downward in front of the dome and the dark bore hole with a bright ring is clearly visible at the bottom tip. For the UP row the barrel extends upward behind the dome.
The barrel tip must touch: right edge center-right for RIGHT, left edge for LEFT, top edge center for UP, bottom edge center for DOWN.
Strict rules: 1-pixel dark outline (#141019), no anti-aliasing, no gradients, no glow, no muzzle flash, no projectile, no text, no grid lines, no drop shadow. Limited palette of at most 12 colors, light from the upper left.
Background: perfectly flat solid magenta #FF00FF.
```

포구 섬광(별도):

```
Pixel art muzzle flash animation, single row of 3 equal frames, each 16x16 art pixels, shown enlarged with nearest-neighbor. The flash points to the RIGHT and starts exactly at the middle of the left edge of each cell: a short cone-shaped burst of yellow and white with a tiny round bean-shaped spark, frame 1 small and bright, frame 2 largest cone, frame 3 fading to two small sparks. Colors: #FFFFFF, #F6D365, #F2A03A, #C9D94A, 1-pixel outline only where needed. No anti-aliasing, no glow, no gradients, no star shapes. Background: perfectly flat solid magenta #FF00FF.
```

### 4.4 A4 내부 코어 (상태별 생성)

```
Pixel art sprite sheet, single horizontal row of {4} equal-size frames, front-facing slightly from the side, a compact sci-fi power core terminal standing on a bus floor. Match the attached reference machine: dark navy metal housing, a large cyan screen showing a simple bus icon and a waveform line, three square buttons (green, cyan, red) below the screen, one amber indicator light, yellow hazard stripes at the base.
State: {normal idle, 4 frames, looping, the waveform shifts by 2 pixels each frame and the green button blinks}.
Strict rules: true low-resolution pixel art on a 56x64 pixel grid per frame, the machine about 44 pixels wide and 54 pixels tall, standing on the bottom edge of each cell, same position in every frame, shown enlarged with nearest-neighbor so every art pixel is a crisp square block. Very simple shapes, no tiny bolts or vents, a lighter blue-gray rim light on the top and left edges so it separates from a dark wall. 1-pixel dark outline (#141019), no anti-aliasing, no gradients, no glow halo, no drop shadow, no text, no frame borders.
Limited palette of at most 16 colors. Light from the upper left.
Background: perfectly flat solid magenta #FF00FF.
```

상태별 대체 문구:
- LowHP: `critical damage, 4 frames, looping, the screen is red with a warning symbol blinking, one crack across the screen, amber light flashing`
- Hit: `taking a hit, 2 frames: frame 1 screen flashes white and the body is squashed down by 1 pixel, frame 2 returning to normal`
- Destroyed: `destroyed, 1 frame, screen shattered and dark, housing dented, no fire` (`{4}` → `1`)

### 4.5 후처리 (모든 에셋 공통)

1. **크로마 제거**: 배경색(`#FF00FF` 또는 `#00FF00`)과 RGB 거리 80 이내인 픽셀을 alpha 0으로 만든다. 경계에 남은 분홍/초록 띠는 1px 침식하거나 수작업으로 지운다.
2. **유효 픽셀 크기 확인**: 생성 이미지의 "가짜 픽셀" 한 칸 크기를 잰다(예: 1536 px 폭에 6프레임 x 96 → 칸당 약 2.67 px). 셀 경계를 직접 측정해 프레임별로 잘라낸다. 생성기가 셀 간격을 지키지 않는 경우가 많으므로 균등 분할을 가정하지 않는다.
3. **최근접 축소**: 각 프레임을 목표 셀 크기(A1 96x64, A2 80x48, A3 32x32, 섬광 16x16, A4 56x64)로 **Nearest Neighbor**로 축소한다. 가짜 픽셀 블록의 중앙을 샘플링하도록 오프셋을 반 칸 둔다.
4. **팔레트 축소**: 항목별 최대 색 수로 줄인다(디더링 없음). 3절 지정 팔레트로 매핑(가장 가까운 색)하는 것을 권장한다.
5. **알파 이진화**: alpha < 128 → 0, 나머지 → 255. 외곽에 `#141019` 1px 외곽선이 끊긴 곳을 보정한다.
6. **정렬**: 발 기준선을 y=0에 맞추고, 피벗 x 열에 몸통 중심을 맞춘다. 프레임 사이 기준선 차이 0px, 몸통 x 차이 ±1px 이내.
7. **사람 수정**: 얼굴, 손, 포신 끝, 포구 링은 반드시 손으로 정리한다. A3은 포구 좌표표와 1px 이내인지 가이드 PNG로 확인한다.
8. **시트 조립**: 3절 셀 크기 x 행/열 순서로 여백 없이 붙인다(Offset 0, Padding 0).
9. **Unity 슬라이스**: Sprite Editor → Slice → Grid By Cell Size, 셀 크기 입력, Pivot Custom(3절 값) → 슬라이스 이름을 2.3절 규칙으로 바꾸고 빈 셀은 삭제한다.

참고용 Pillow 스크립트(검증하지 않음, Art 작업 폴더에서 사용):

```python
from PIL import Image

def clean_frame(src, box, cell, palette_png, chroma=(255, 0, 255), tol=80):
    """box: 생성 이미지에서 잘라낼 프레임 영역, cell: (w, h) 목표 셀 크기"""
    im = Image.open(src).convert("RGBA").crop(box)
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if abs(r - chroma[0]) + abs(g - chroma[1]) + abs(b - chroma[2]) < tol:
                px[x, y] = (0, 0, 0, 0)
    small = im.resize(cell, Image.NEAREST)
    alpha = small.getchannel("A").point(lambda v: 255 if v >= 128 else 0)
    pal = Image.open(palette_png).convert("P")  # 지정 팔레트를 담은 작은 PNG
    rgb = small.convert("RGB").quantize(palette=pal, dither=Image.Dither.NONE).convert("RGBA")
    rgb.putalpha(alpha)
    return rgb
```

---

## 5. 960x540 아트 리뷰 수락 체크리스트

리뷰 조건: Game view 960x540 고정, 실제 플레이 화면 캡처(정지 이미지와 실시간 영상)를 쓴다. Art 혼자서는 Unity를 실행하지 않으므로 캡처는 Development/QA가 만들고, 통과 여부는 디렉터가 판정한다.

**파일과 임포트**
- [ ] 파일 크기가 3절 시트 크기와 정확히 같다(576x384, 480x240, 96x128, 48x16, 224x256).
- [ ] alpha 값은 0과 255만 있다(스크립트로 히스토그램 확인).
- [ ] 색 수가 항목별 상한 이하이다(외곽선 포함 +1).
- [ ] `.meta`: `filterMode: 0`, 모든 플랫폼 `textureCompression: 0`, `spritePixelsToUnits: 24`(또는 합의값), mipmap off, 자동 잔여 슬라이스 없음.
- [ ] 슬라이스 이름과 피벗이 3절과 같다. 기존 파일과 기존 `.meta` GUID는 변경하지 않았다.
- [ ] 봉인(자물쇠, 판자 등) 모티프가 없다.

**픽셀 품질 (캡처를 400% 확대해서 확인)**
- [ ] 아트 1px가 모든 곳에서 화면 2x2 px이다(1px/3px 섞임 없음). 안 B 또는 A가 적용되지 않았다면 이 항목은 "보류"로 기록한다.
- [ ] 배경 위에서 외곽선이 끊기거나 후광(분홍/초록 띠)이 없다.

**가독성 (100% 크기, 리뷰어 2명 이상이 설명 없이 판단)**
- [ ] 내부 플레이어의 화면 키가 90–100 px이고, 노란 기둥 앞을 지날 때도 실루엣이 분리된다.
- [ ] 걷기가 미끄러지지 않는다(발 접지 프레임과 이동 속도 5 wu/s가 맞는지 Dev와 확인).
- [ ] 콤보 1·2·3타가 서로 다른 동작으로 보이고, 3타가 가장 크게 읽힌다. 입력 직후 첫 프레임에 이미 타격이 보인다.
- [ ] 내부 적이 **측면 스프라이트**로 나오고 오른쪽(코어)을 본다. 화면 높이 약 60 px이고 플레이어와 바로 구분된다.
- [ ] 적 공격의 hit 프레임이 실제 HP 감소 시점과 맞는다(±1프레임).
- [ ] 50마리 경보 상황 캡처에서 플레이어와 코어의 위치를 2초 안에 찾을 수 있다.
- [ ] 코어의 정상 / 저HP / 피격 상태가 색 틴트 없이도 구분된다.
- [ ] 루프 무기: 4방향 각각의 조준 방향을 리뷰어가 설명 없이 맞힌다. **특히 D(아래)와 U(위)**.
- [ ] 루프 무기: 포구 섬광과 탄환 생성 위치가 네 방향 모두 포신 끝에서 아트 1px 이내이다(포구 좌표표 기준, Dev 연동 후).
- [ ] 버스 위에서 포탑이 버스 실루엣을 과하게 가리지 않는다(버스 높이의 절반 이하가 지붕 위로 나온다).

---

## 6. 의존성과 담당 (Art가 하지 않는 일)

| 필요 작업 | 담당 | 내용 |
|---|---|---|
| 픽셀 배율 | Development + 디렉터 승인 | 안 B(ortho 5.625, PPU 24) 또는 안 A 결정, 신규 스프라이트를 스케일 1로 배치 |
| 프레임 재생 | Development | 시트 슬라이스를 애니메이션으로 재생(스케일 모션은 보조로 유지 가능) |
| 내부 적 스프라이트 버그 | Development | 실제 침입 경로 `TransferEnemyInside`(`LastStopPrototype.cs:767`) → `SeparateInteriorEnemyVisual`(`:807`)이 외부 탑다운 스프라이트를 그대로 복사함. 여기서 측면 내부 스프라이트로 교체해야 함. `SpawnInteriorEnemy`(`:1125`)는 호출처가 없으므로 고쳐도 효과 없음 |
| 포구 위치 | Development | 방향별 포구 오프셋 표를 데이터로 사용, `(0.4, 0.7)` 고정값 대체 |
| 로드 경로 | Development | `Assets/Art/...` 납품물을 Resources로 옮길지, 참조 방식으로 바꿀지 |
| 공격 타이밍 | Development / Planning | 적 hit 프레임과 0.8초 데미지 타이밍 동기화. 밸런스 변경 없음 |
| 실제 이미지 생성과 픽셀 수정 | 이미지 생성 도구 + 사람 아티스트 | 이 문서의 프롬프트와 후처리 사용 |

## 7. 이번 스프린트에서 제외 (참고)

- 배경 2장을 480x270(또는 960x540 정수 배율) 원본으로 다시 만들기: 축소 계단 현상 해결용이며 다음 우선순위이다.
- `LastStop_BusBody_v2` / `LastStop_BusWheels_v2`: 코드는 준비되어 있고 파일이 없다.
- VFX 시트 재제작(프레임 분리, 색번짐 제거, 원뿔형 포구 섬광은 A3에 포함).
- 폐기 후보(삭제는 디렉터 승인 후): `Bus_Interior_Door_Seal`, `Vfx_Seal_Complete`, 미사용 `Player_Exterior_TopDown`.

## 8. 디렉터 확인 질문

1. **외부 시점**: GameDesign.md는 외부를 탑다운으로 정의하지만 현재 빌드의 배경과 버스는 측면/3/4 시점이다. 루프 무기는 현재 화면에 맞춘 3/4 탑다운 기준으로 썼다. 어느 시점이 정본인가?
2. **캐릭터 크기 확대**(플레이어 화면 약 44 → 96 px, 적 29 → 60 px)는 내부 전투 공간과 충돌 판정에 영향을 줄 수 있다. 판정 크기는 그대로 두고 그림만 키울지, 함께 조정할지 결정이 필요하다(메커닉 변경은 제안만 한다).
3. **플레이어 재킷 색**을 기둥 노랑과 구분하려고 주황에서 붉은 주황(`#E07A26`)으로 조금 옮기는 것을 승인하는가?
4. 루프 무기 디자인은 "콩알포" 한 종류만 사양화했다. 비눗방울 발사기와 화염 꼬치도 지붕 포탑 스프라이트가 필요한가, 같은 장착점을 공유하는가?
