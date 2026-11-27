# LAST STOP — Sprint 1 아트 사양서 (탑다운 / 고해상도 픽셀아트 개정판)

작성: Art 팀 (텍스트 전용 에이전트), 개정 2026-11-27
상태: **제안 / 디렉터 검토 대기.** 이 문서는 사양과 생성 프롬프트만 담는다. 실제 이미지는 만들지 않았다. "신규 에셋"으로 적은 파일은 모두 아직 없다.

참조: `AGENTS.md`, `Docs/GameDesign.md`("Director decisions (2026-11-27)"), `Docs/Tasks.md`, `Docs/Teams/Art.md`, `Docs/Media/StyleReference/A_HD_PixelArt_Interior_Mockup.webp`(승인된 스타일 기준), `B_HD_Illustration_Interior_Mockup_rejected.webp`(거절안, 대비용), `Assets/Scripts/LastStopPrototype.cs`, `Assets/Scripts/LastStopBusWheelAnimation.cs`, `Packages/manifest.json`.

### 이번 개정에서 바뀐 것

| 이전 판 | 이번 판 | 근거 |
|---|---|---|
| 내부 = 측면 시점, 캐릭터 약 48 아트 px | 내부와 외부 모두 **3/4 탑다운**(Vampire Survivors류, 캐릭터는 정면 쪽으로 약간 기울여 보임) | 디렉터 결정: 카메라 |
| 저해상도 픽셀아트, 가상 480x270, PPU 24 | **고해상도 픽셀아트**, 캐릭터 64–96 아트 px, 가상 **960x540**, PPU **48** 제안 | 디렉터 결정: 아트 스타일 A |
| 측면 플레이어/Rusher/코어 시트, 4방향 측면 지붕포 | 탑다운 방향별 시트, 내부 타일·소품, 탑다운 외부 버스, 8방향 지붕 무기 | 위와 같음 |
| 측면 프롬프트 | 목업 기반 공통 스타일 블록 + 에셋별 프롬프트 | 위와 같음 |

유지한 것: 현재 에셋 인벤토리, 코드 경로 사실(내부 적 스프라이트 복사 `LastStopPrototype.cs:807`, 없는 `BusBody_v2`/`BusWheels_v2` 등), 알파 이진화·크로마·팔레트 매핑 후처리 원칙, 봉인 모티프 금지.

**레이아웃 의존 표시:** Planning의 탑다운 내부 사양(레이아웃, 조작, 적 경로, 카메라)이 아직 없다(Tasks.md TODO). 내부 배치에 따라 바뀌는 항목에는 **[잠정]**을 붙였다. 목업의 배치(가운데 코어, 한쪽 끝 문, 반대쪽 끝 운전석, 양쪽 벽 좌석)는 출발점일 뿐 승인된 배치가 아니다.

---

## 0. 기준 수치 (코드에서 확인)

| 항목 | 현재 값 | 출처 |
|---|---|---|
| 카메라 | 외부/내부 모두 Orthographic size **5.5** → 화면 세로 11 wu | `ConfigureCamera` (`LastStopPrototype.cs:251`) |
| 렌더 파이프라인 | URP `17.3.0`. `com.unity.2d.aseprite 3.0.1`, `2d.tilemap`, `2d.tilemap.extras`, `2d.animation` 설치됨 | `Packages/manifest.json` |
| 스프라이트 크기 결정 | `CreateBlock`이 원본 크기와 무관하게 지정 world 크기로 **비균등 스케일**. 예: 버스 4.8x1.8 wu(`:344`), 코어 0.96x1.05 wu(`:443`) | `CreateBlock`, `ConfigureSprite` |
| 로드 경로 | `Resources.LoadAll<Sprite>("Art/...")` → 실제 파일은 `Assets/Resources/Art/`. `Assets/Art/`는 비어 있음 | `LoadArtSprites` (`:280-284`) |
| 런타임 필터 | 배경과 스윙 시트는 코드에서 `FilterMode.Point`로 강제 | `LoadBackdropSprite`, `ConfigureInteriorSwing` (`:948`) |
| 내부 적 스프라이트 | 실제 침입 경로 `TransferEnemyInside`(`:767`) → `SeparateInteriorEnemyVisual`(`:792`)이 **외부 적 스프라이트를 그대로 복사**(`:807` `visualRenderer.sprite = originalRenderer.sprite`). `Enemy_Interior_SideView`는 쓰이지 않는다. `SpawnInteriorEnemy`(`:1125`)는 호출처가 없다 | 코드 |
| 버스 바퀴 애니 계약 | `Bus_Body_NoWheels` 1장 + `Bus_Wheels_0..3` 4장. **네 장 모두 body와 rect 크기, pivot, PPU가 같아야** 한다. 12 fps, 바퀴는 body보다 sortingOrder −1 | `TryGetAnimatedBusArt` (`:357`), `LastStopBusWheelAnimation.cs` |
| 버스 포구 | 방향과 무관하게 버스 중심 + (0.4, 0.7) 고정 | `FireBusProjectile` (`:1010`) |
| 플레이어 공격 | 입력 간격 0.18초, 포즈 0.16초, 타격 판정은 **입력 즉시**, 3타 콤보 강조 | `InteriorAttackInterval`, `LastStopSpriteMotion` |
| 내부 적 공격 | 0.8초 간격, 1회 7 HP | Tasks.md 검증 기록 |
| UI 기준 | CanvasScaler 1280x720, match 0.5 | `Docs/Sprint1/UI.md` |

---

## 1. 현재 에셋 인벤토리 (전부 `Assets/Resources/Art/`)

| 파일 | 픽셀 크기 | 내용 | 사용처 | 탑다운 전환 후 판정 |
|---|---|---|---|---|
| `LastStop_MvpSpritesheet_v1.png` | 1774x887 | `Player_Exterior_TopDown` 294x313, `Player_Interior_SideView` 322x330, `Enemy_Exterior_TopDown` 162x292, `Enemy_Interior_SideView` 340x235, `Bus_Exterior_Yellow` 1230x511, `Bus_Interior_Door_Seal` 342x466 | 내부 플레이어, 외부 적, 내부 적(외부 스프라이트 복사), 외부 버스 | `Player_Exterior_TopDown`(현재 미사용)과 `Enemy_Exterior_TopDown`은 **탑다운 캐릭터 디자인 참조**로 쓸 수 있다. `Player_Interior_SideView`, `Enemy_Interior_SideView`는 폐기 후보. `Bus_Exterior_Yellow`는 측면 3/4 시점이라 탑다운 외부와 맞지 않는다. `Bus_Interior_Door_Seal`은 봉인 메커닉이 없으므로 폐기 후보. |
| `LastStop_GuardSwing_v6.png` | 896x330 | 448x330 2프레임(contact/follow-through), 자동 슬라이스 잔여 조각 15개 | `ConfigureInteriorSwing`이 직접 자름(가로 448 하드코딩) | 측면 전용이라 폐기 후보. 교체 시 코드의 하드코딩도 제거해야 한다. |
| `LastStop_InteriorCore_v1.png` | 1254x1254 | 코어 본체 1185x1207 + 잔여 조각 19개 | 내부 코어(0.96x1.05 wu) | 정면 단말기 디자인. 탑다운에서도 정면 3/4로 쓸 수 있으나 25배 축소로 읽히지 않는다. A3로 교체. |
| `LastStop_CombatVfx_v1.png` | 1774x887 | `Vfx_Bean_Projectile`, `Vfx_Bubble_Projectile`, `Vfx_Flame_Loop`, `Vfx_Hit`, `Vfx_Enemy_Death`, `Vfx_Seal_Complete`, 코드가 별도 생성하는 `Vfx_Bus_Muzzle` | 탄환, 화염, 피격, 사망, 버스 궤적, 포구 섬광 | 다프레임 스트립이 1장으로 슬라이스되어 애니메이션이 없다. 색번짐. `Vfx_Seal_Complete`는 폐기 후보. 시점 중립적이라 재제작 전까지 유지 가능. |
| `LastStop_NightBusInterior_v1.png` | 1672x941 | 측면 내부 배경 1장 | 내부 배경 | 측면 시점이라 탑다운 내부에서 쓸 수 없다. A4 타일·소품으로 교체. |
| `LastStop_NightRoad_v1.png` | 1672x941 | 측면 거리 배경 1장 | 외부 배경 3장 반복 | 측면 시점이라 탑다운 외부와 맞지 않는다. 탑다운 도로 타일은 다음 우선순위(7절). |
| (없음) `LastStop_BusBody_v2`, `LastStop_BusWheels_v2` | — | 코드가 로드를 시도함(`:281-282`) | 바퀴 애니 버스 | 파일이 없어 `Bus_Exterior_Yellow`로 대체된다. A5에서 **같은 파일 이름과 슬라이스 계약**으로 탑다운 버전을 사양화했다. |
| (없음) 지붕 무기 | — | — | — | Tasks.md BLOCKED(측면 4방향의 위/아래 가독성 실패). 탑다운 지붕 시점에서는 포신이 지붕 평면에 누워 있어 위/아래 문제가 구조적으로 줄어든다. A6로 재사양. |

### 공통 기술 문제 (기존 에셋, 그대로 유효)

1. 진짜 픽셀아트가 아니다. 생성 이미지를 그대로 써서 픽셀 그리드가 없고 시트당 약 4.6만–31만 색이다.
2. 알파가 반투명이다. 보이는 픽셀 대부분이 alpha 192–254이고 외곽에 alpha 1–63 후광이 수만 픽셀 있다.
3. 임포트 설정이 섞여 있다. Default는 Compression None, **Standalone 오버라이드는 `textureCompression: 1`**. 배경 2장은 Bilinear(런타임 Point). PPU 100은 코드가 크기를 강제해 의미가 없다.
4. 비균등 스케일로 비율이 깨진다.
5. 프레임 애니메이션이 없다(움직임은 `LastStopSpriteMotion`의 스케일·기울기·틴트뿐).

---

## 2. 스타일 가이드 (승인 목업 A에서 도출)

목업은 AI가 만든 분위기 이미지이며 실제 픽셀 그리드가 없다(1672x941에서 "가짜 픽셀" 한 칸이 약 2.5–3 px로 일정하지 않다). 아래 규칙은 목업의 **색, 빛, 실루엣, 밀도 느낌**을 가져오고, 그리드·색 수·알파는 우리가 강제한다.

### 2.1 목업에서 읽은 특징

- 밤, 비 젖은 아스팔트. 전체는 짙은 남색·청보라, 따뜻한 빛은 국소적이다.
- 버스 지붕을 걷어낸 "돌하우스" 3/4 탑다운. 위쪽 벽은 안쪽 면(창문, 앰버 천장등)이 보이고, 아래쪽 벽은 윗면 가장자리만 보인다.
- 캐릭터는 위에서 약 30–40도로 내려다보며 얼굴과 몸 앞면이 보인다. 머리 약 1/5.5 비율(치비 아님).
- 주요 대비 3축: **앰버 실내등**(따뜻함, 안전), **시안 코어 발광**(지켜야 할 대상), **빨강**(위험: 적 상처, 피, HP, 후미등).
- 노란 손잡이 기둥이 강한 수직 리듬을 만든다. 좌석은 남색 천에 주황 점무늬.
- 캐릭터는 두꺼운 어두운 외곽선으로 배경에서 분리된다. 바닥 아래에는 짧은 어두운 그림자가 있다.

거절안 B와의 차이(피해야 할 것): 부드러운 그라데이션, 페인터리 붓질, 블룸 번짐, 사진 같은 질감, 얇거나 없는 외곽선.

### 2.2 마스터 팔레트 (hex)

목업에서 샘플링한 값(예: 바닥 `#2F314C`/`#414465`, 선체 `#1E2447`/`#35427B`, 기둥 `#FBD631`, 앰버 `#FB9E3A`/`#FFC260`, 코어 하이라이트 `#D3FFFF`)을 기준으로 램프를 정리했다. **모든 신규 에셋은 이 표의 색으로 매핑**한다. 에셋별 색 수 상한은 3절 각 항목에 있다.

| 램프 | 어두움 → 밝음 | 용도 |
|---|---|---|
| 외곽선 | `#07080F` (주 외곽선), `#141526` (내부 선) | 모든 캐릭터·소품 외곽 |
| 밤 환경 (차가움) | `#0E162D` `#151C34` `#21293B` `#30436B` `#405F82` `#6E86B0` | 도로, 그림자, 림 하이라이트 |
| 실내 바닥 | `#1D1E32` `#2F314C` `#414465` `#53506E` | 바닥 타일, 통로 |
| 선체·벽 | `#1E2447` `#2A3563` `#35427B` `#5A6AA8` | 버스 벽, 창틀, 내부 금속 |
| 좌석 | `#1A2C5B` `#25305A` `#3A4C8C` + 무늬 `#E07A3A` | 좌석 천 |
| 노란 기둥 | `#8A6A12` `#D9A521` `#FBD631` | 손잡이 기둥, 바닥 안내선 |
| 앰버 빛 | `#7A4A1E` `#C9781F` `#FB9E3A` `#FFC260` `#FFE9A8` `#FFFBDF` | 천장등, 창문 반사, 문 조명, 가로등 |
| 시안 코어 | `#0B3A4A` `#127C8C` `#2EC4D6` `#7EF2F5` `#D3FFFF` | 코어, 코어 발광, 선택 링 |
| 위험 빨강 | `#3A0E16` `#7A1622` `#C02A34` `#E03A3A` `#FF7A6A` | 피, 상처, 후미등, 저HP 경고 |
| 해저드 | `#C9A227` + `#07080F` 줄무늬 | 코어 받침, 문턱 |
| 식생 | `#0C1118` `#16302A` `#24503C` | 외부 나무 |

캐릭터 고유 램프(기존 디자인 유지, 마스터 팔레트와 함께 사용):

| 대상 | 색 |
|---|---|
| 플레이어 | 머리 `#4A221E` `#6A302B` `#844030` `#A1683E`, 피부 `#8E5A3C` `#D98E5A` `#F1B276`, 재킷 `#7A3414` `#B8581C` `#E07A26` `#F6B04A`(기둥 노랑 `#FBD631`과 구분되도록 붉은 주황), 털 깃 `#C9B89A` `#F3E6CF`, 바지 `#1E2238` `#2E3550` `#46507A`, 부츠 `#3A2826` `#5A3F38`, 배낭 `#3F4A20` `#5E6B2E` `#7E8C40`, 곤봉 `#4E2E1A` `#7A4A2A` `#A87048`, 못 `#B8C0CC` |
| 기본 적 (기존 Rusher, 기본안) | 몸 `#2E1828` `#4B293D` `#703950` `#834859`, 림 `#A7678A`, 줄무늬 `#D99688` `#FBC9B3`, 눈 `#FFD23A`, 입 `#C0303A`, 이빨·발톱 `#F3E6CF` |
| 기본 적 (목업 좀비, 대안) | 피부 `#5E6B4A` `#8A9670` `#B4BC98`, 옷 `#3A3550` `#6A607E` `#BEB7D3`, 상처 마스터 빨강, 눈 `#FFE9A8` |

### 2.3 외곽선 규칙

- 캐릭터·적·코어·소품: 실루엣 바깥쪽 **1 아트 px 외곽선 `#07080F`** (1080p에서 2 화면 px로 보여 목업의 굵은 선과 같다).
- 내부 형태선: 외곽선 색 대신 해당 재질의 가장 어두운 색. 얼굴·손 같은 핵심 부위만 `#141526`.
- 빛을 받는 윗면 외곽(머리, 어깨)은 선택적으로 램프 가장 어두운 색으로 바꿀 수 있다(sel-out). 아래쪽과 그림자 쪽 외곽은 항상 `#07080F`.
- 타일(바닥, 벽): 외곽선 없음. 경계는 명도 차이로 나눈다.
- 발광체(코어 빛, 램프, VFX): 외곽선 없음.
- 실루엣 바깥 안티앨리어싱 금지. 실루엣 **안쪽**의 곡선에만 중간색 1단계 수동 AA를 허용한다(고해상도 픽셀아트 품질용).

### 2.4 빛과 음영

- **고정(그려 넣는) 빛:** 주광은 화면 위쪽(천장 앰버등)에서 내려온다. 머리·어깨·윗면은 따뜻한 쪽, 아래·뒷면은 남보라 쪽으로 **색조를 이동한 3–4단 램프**. 그라데이션 없음.
- **림:** 캐릭터 아래쪽/뒤쪽 가장자리에 차가운 `#6E86B0` 1 px 림(어두운 바닥과 분리용, 목업 특징).
- **시안 코어광과 국소 앰버광은 스프라이트에 굽지 않는다.** 위치에 따라 바뀌므로 Development가 URP 2D Light(2D Renderer 사용 시) 또는 가산 혼합 글로우 스프라이트로 처리하도록 제안한다. Art는 글로우 스프라이트(A3-G)만 제공한다.
- **그림자:** 캐릭터 시트에 넣지 않는다. 공용 바닥 그림자 스프라이트(타원, `#07080F` 단색, 알파는 0/255이고 SpriteRenderer 색 알파로 반투명 처리) 별도 제공.
- **디더링:** 넓은 면(바닥, 벽)에서만 2x2 체크 디더 허용. 캐릭터에는 쓰지 않는다.
- 외부: 차가운 달빛/가로등 키. 버스 전조등·후미등은 별도 가산 스프라이트.

### 2.5 픽셀 밀도 규칙

- 모든 신규 에셋은 **같은 밀도(PPU 48, 4절)**로 그린다. 한 화면에서 픽셀 크기가 섞이면 안 된다(업스케일·다운스케일 원본 금지, 런타임 비균등 스케일 금지).
- 스프라이트 회전으로 방향을 표현하지 않는다(회전하면 그리드가 깨진다). 방향은 그려서 만든다. 예외: 0.1초 이하로 사라지는 VFX(포구 섬광, 타격 섬광)는 회전 허용.
- 캐릭터 키 기준: 플레이어 **80 px**, 기본 적 **64–72 px**, 코어 **약 104 px**(디렉터 범위 64–96 px 안, 코어는 오브젝트라 예외).
- 디테일 하한: 1 px 단독 점은 눈 하이라이트, 못, 점무늬에만. 나머지 형태는 2 px 이상 폭.

### 2.6 AI 생성물 일관성 유지 방법

1. **공통 스타일 블록(5.1절)을 모든 프롬프트 앞에 그대로 붙인다.** 단어를 바꾸지 않는다.
2. **참조 이미지 2장을 항상 첨부:** (a) 승인 목업 A(스타일), (b) 해당 캐릭터의 승인된 모델 시트(디자인). 모델 시트가 없으면 먼저 모델 시트(정지 포즈 3방향)를 생성·승인한 뒤 애니메이션 행을 만든다.
3. **한 번에 한 애니메이션 × 한 방향(1행)만 생성.** 전체 시트를 한 번에 요청하지 않는다.
4. 같은 대화 스레드 안에서 이어서 생성하고, 앞서 승인된 행을 다시 첨부해 "same character, same proportions, same palette"를 지시한다.
5. 팔레트 hex를 프롬프트에 적고, 후처리에서 **마스터 팔레트로 강제 매핑**한다(생성 색은 믿지 않는다).
6. 배경은 단색 크로마. 마젠타 `#FF00FF` 기본, 분홍·자주가 들어간 대상(기본 적)은 초록 `#00FF00`.
7. 거절 기준(재생성): 외곽선 없음, 그라데이션·블룸, 프레임마다 키가 3 px 이상 다름, 방향이 틀림, 배경에 그림자·질감, 텍스트·격자선 포함.
8. 생성 결과는 밑그림이다. **사람이 Aseprite에서 얼굴, 손, 무기, 발 기준선을 반드시 정리**한 뒤 납품한다.

---

## 3. 목표 해상도와 픽셀 배율 (Development 결정 요청)

### 3.1 제안 (권장안)

| 항목 | 값 | 비고 |
|---|---|---|
| 가상(아트) 해상도 | **960x540 아트 px** | 16:9 |
| 기준 출력 해상도 | **1920x1080** (아트 1 px = 화면 2x2 px) | 2560x1440은 비정수(2.67), 3840x2160은 4x |
| 리뷰 해상도 | 960x540 Game view = 1x (기존 리뷰 관행 유지), 최종 판정은 1920x1080 | |
| Sprite PPU | **48** | 1 wu = 48 아트 px |
| 카메라 ortho size | 5.5 → **5.625** (화면 11.25 x 20 wu) | 현재 11 wu에서 약 2%만 늘어 스폰·배치 좌표 변경이 거의 없다 |
| Pixel Perfect Camera (URP) | Assets PPU 48, Reference Resolution 960x540, Grid Snapping: Upscale Render Texture(또는 Pixel Snapping), Crop Frame 없음 | 내부·외부 카메라 모두 |
| 배치 | 신규 스프라이트는 **스케일 1**(원본 px / PPU). `CreateBlock`의 강제 크기 사용 중지 | Development 소관 |
| 정렬 | Transparency Sort Mode Custom Axis (0, 1, 0), 캐릭터·소품 피벗은 발/바닥 | y-정렬 탑다운 |

환산: 플레이어 80 px = 1.67 wu = 1080p에서 160 화면 px(화면 높이의 약 15%, 목업의 플레이어 비율 약 16%와 비슷).

### 3.2 대안과 단점

- **1280x720 창모드:** 960x540 기준으로 1.33배라 비정수. Pixel Perfect Camera의 Upscale Render Texture를 쓰면 화면 전체가 같은 비율로 확대되어 일관성은 유지되지만 픽셀 크기가 1–2 px로 섞인다. 720p를 주 목표로 할 경우 가상 640x360(PPU 32, 720p 2x/1080p 3x)도 가능하나, 캐릭터 64–96 px가 화면 높이의 18–27%가 되어 내부가 좁아진다. 권장하지 않는다.
- **PPU 64 / ortho 4.22:** 같은 960x540이지만 화면이 8.4 wu로 줄어 기존 좌표를 다시 잡아야 한다.
- **외부 카메라만 줌아웃(ortho 11.25):** 외부 군중을 더 보여주지만 외부 픽셀이 1080p에서 1x가 되어 내부(2x)와 밀도가 달라진다. 디렉터 질문 3 참고. 권장안은 외부도 같은 밀도를 쓰고 **외부 전용 작은 스프라이트**(A2-E)로 군중감을 낸다.

---

## 4. 공통 규격 (신규 에셋 전체)

### 4.1 방향 규약

- 탑다운 방향 이름: `D`(아래, 화면을 향함), `U`(위), `R`(오른쪽), `L`(왼쪽), 대각선 `DR` `UR` `DL` `UL`.
- **4방향 권장안:** `D`, `U`, `R` 3개를 그리고 `L`은 `flipX`. 8방향은 `D` `DR` `R` `UR` `U` 5개를 그리고 `DL` `L` `UL`은 `flipX`(디렉터 질문 1).
- `flipX`는 피벗 기준 반전이므로 피벗 x가 몸통 중심에 정확히 있어야 한다. 반전하면 곤봉을 든 손이 바뀌지만 이 장르에서 허용되는 수준으로 본다.
- 빛은 위쪽에서 오므로 좌우 반전해도 조명이 깨지지 않는다(이전 판의 "왼쪽 위 광원" 규칙을 바꾼 이유).

### 4.2 파일 이름, 위치, 슬라이스 이름

- 이름: `LastStop_<대상>_TopDown_<내용>_v<N>.png`. 기존 파일을 같은 이름으로 덮어쓰지 않는다. 기존 `.meta` GUID는 건드리지 않는다.
- 예외: A5 외부 버스는 코드가 이미 찾는 `LastStop_BusBody_v2` / `LastStop_BusWheels_v2` 이름과 슬라이스 이름을 쓴다.
- 목표 경로: `Assets/Art/<분류>/...`. 현재 코드는 `Resources/Art`만 로드하므로 Resources로 옮길지 직렬화 참조로 바꿀지는 **Development가 결정**한다.
- 슬라이스 이름: `<접두어>_<애니>_<방향>_<두 자리 번호>`, 예: `Player_Walk_D_00`.
- 원본 작업 파일(`.aseprite`)은 Art 작업 폴더에 보관한다. Unity에 `.aseprite`를 직접 넣을지(Aseprite Importer 설치됨)는 Development와 협의하고, 기본 납품은 PNG 시트이다.

### 4.3 Unity 임포트 설정 (신규 시트 공통)

| 설정 | 값 |
|---|---|
| Texture Type | Sprite (2D and UI) |
| Sprite Mode | Multiple (Grid By Cell Size, 항목별 셀 크기, Offset 0, Padding 0). 타일셋도 동일 |
| Pixels Per Unit | **48** |
| Mesh Type | Full Rect |
| Extrude Edges | 0 |
| Pivot | Custom, 항목별 값 (같은 시트 모든 슬라이스 동일) |
| Filter Mode | **Point (no filter)** |
| Compression | **None — Default와 Standalone 등 모든 플랫폼 오버라이드** |
| Generate Mip Maps | Off |
| Wrap Mode | Clamp |
| sRGB | On |
| Alpha Is Transparency | On |
| Max Size | 4096 (시트보다 크게) |
| Read/Write | Off |

---

## 5. 우선순위 에셋 사양

프레임 속도는 표시 목표값이며 재생 연결은 Development 일이다. 모든 피벗은 셀 왼쪽 아래 (0,0) 기준 픽셀 좌표와 정규화 값을 같이 적었다.

| ID | 에셋 | 우선순위 | 레이아웃 의존 |
|---|---|---|---|
| A1 | 플레이어 (곤봉 경비원) | P1 | 공격 방향 수는 조작 사양 의존 |
| A2 | 기본 적 (내부) | P1 | 진입 애니는 문 위치 의존 [잠정] |
| A3 | 코어 + 발광 | P1 | 없음 |
| A4 | 내부 타일·소품 | P1 [잠정] | 전부 |
| A5 | 외부 버스 탑다운 | P2 | 앞 방향 |
| A6 | 지붕 무기 + 포구 섬광 | P2 | 장착점은 A5 의존 |
| A2-E | 외부 적 (작은 변형) | P3 | 디렉터 질문 3 |
| SH | 공용 바닥 그림자 | P1 | 없음 |

### A1. 플레이어 — 곤봉 경비원 (탑다운)

| 항목 | 값 |
|---|---|
| 파일 | `Assets/Art/Characters/Player/LastStop_Player_TopDown_Sheet_v1.png` |
| 셀 | **128x128 px** |
| 캐릭터 크기 | 머리부터 발까지 **80 px**, 어깨 폭 약 32 px. 위·아래·옆으로 곤봉 휘두를 여백 포함 |
| 피벗 | 발 중앙 **(64, 24) → (0.5, 0.1875)**. 아래(D) 스윙이 발 아래 24 px까지 내려갈 공간 |
| 방향 | **8방향 (감독 결정 2026-11-27)**: D, DR, R, UR, U 5방향을 그리고 DL, L, UL은 flipX |
| 시트 배치 | 6열. 행 = 애니 x 방향(아래 표 순서, 방향 D→U→R) |
| 시트 크기 | 8방향: 6열 x 30행 = **768x3840** (참고: 4방향이었다면 768x2304) |
| 슬라이스 접두어 | `Player` |
| 색 상한 | 32색 + 외곽선 |

| 행 묶음 | 애니 | 프레임 | 속도 | 비고 |
|---|---|---|---|---|
| 0–2 | Idle (D/U/R) | 4 | 6 fps | 호흡 1 px, 곤봉 어깨에 걸침 |
| 3–5 | Walk (D/U/R) | 6 | 10 fps | 루프, 접지 프레임 0과 3. 이동 5 wu/s 기준 보폭은 Dev와 맞춘다 |
| 6–8 | AttackA 1타 (D/U/R) | 3 | 약 19 fps (0.16초) | **0 = contact**(판정이 입력 즉시라 예비동작 없음), 1 = follow-through, 2 = recover. 바라보는 방향 앞쪽으로 수평 휘두르기 |
| 9–11 | AttackB 2타 | 3 | 약 19 fps | 반대 방향 되휘두르기 — P2 |
| 12–14 | AttackC 3타 마무리 | 4 | 약 20 fps | 내려찍기, 가장 큰 실루엣 — P2 |
| 15–17 | Hurt (D/U/R) | 2 | 12 fps | 뒤로 젖힘. 흰 플래시는 코드가 처리 |

- **최소 납품(MVP):** Idle, Walk, AttackA, Hurt 각 3방향 = 45프레임. 콤보 B/C는 탑다운 조작 사양(Planning)이 3타 콤보를 유지할 때 만든다.
- 스윙 궤적(smear)은 시트에 넣지 않는다. 별도 VFX `LastStop_Vfx_TopDown_BatArc_v1.png`(128x128 x 3프레임, 오른쪽 기준, 코드 회전 허용 — 2.5절 예외)로 만든다. 곤봉 끝 2–3 px 단색 잔상까지만 시트에 허용.
- 디자인: 기존 `Player_Exterior_TopDown`/`Player_Interior_SideView`의 경비원(갈색 올림머리, 붉은 주황 패딩, 크림색 털 깃, 남색 바지, 갈색 부츠, 올리브 배낭, 못 박힌 나무 곤봉)을 목업 스타일로 다시 그린다. 목업의 남성 생존자 디자인으로 바꿀지는 디렉터 질문 2.

### A2. 기본 적 — 내부 (탑다운)

| 항목 | 값 |
|---|---|
| 파일 | `Assets/Art/Characters/Enemies/LastStop_Rusher_TopDown_Sheet_v1.png` |
| 셀 | **96x96 px** |
| 크기 | 키 약 **64 px**(사족 짐승은 머리 높이 기준), 몸길이(꼬리 포함) 약 70 px. 플레이어보다 확실히 작게 |
| 피벗 | 발 중앙 **(48, 12) → (0.5, 0.125)** |
| 방향 | **8방향 (감독 결정 2026-11-27)**: D, DR, R, UR, U를 그리고 DL, L, UL은 flipX. Death와 Breach는 D 1방향. 시트 행 수는 8방향 기준으로 다시 계산 필요 |
| 시트 배치 | 6열 x 11행 = **576x1056** |
| 슬라이스 접두어 | `Rusher` |
| 크로마 | **초록 `#00FF00`** (분홍 줄무늬 보호) |
| 색 상한 | 20색 + 외곽선 |

| 행 | 애니 | 프레임 | 속도 | 비고 |
|---|---|---|---|---|
| 0–2 | Walk (D/U/R) | 6 | 12 fps | 루프, 꼬리 흔들림 |
| 3–5 | Attack (D/U/R) | 4 | 12 fps (0.33초) | 0 = 웅크림, 1 = 덤벼듦, **2 = hit 프레임**, 3 = 복귀. 0.8초 주기 안. hit 프레임과 데미지 동기화는 Development 요청 |
| 6–8 | Hit (D/U/R) | 2 | 12 fps | 넉백 자세. 흰 플래시는 코드 |
| 9 | Death (D) | 6 | 12 fps | 쓰러짐 → 보라 연기 → 마지막 프레임은 연기 2–3점. 모든 방향 공용 |
| 10 | Breach (D) [잠정] | 3 | 10 fps | 문에서 뛰어들어 착지. 문 위치가 정해지면 방향 추가 가능. `TriggerEntry`와 연결 |

- 50마리가 겹치는 경보 상황을 위해 실루엣을 단순하게: 큰 꼬리, 노란 눈, 분홍 줄무늬. 위쪽 림 `#A7678A`.
- 코드 연동: `SeparateInteriorEnemyVisual`(`LastStopPrototype.cs:807`)에서 외부 스프라이트 복사 대신 이 시트의 애니메이션을 써야 한다(Development).
- 목업의 좀비 디자인으로 바꿀지는 디렉터 질문 2. 바꾸면 크기를 키 72 px, 2족 보행으로 하고 크로마는 마젠타로 돌린다.

### A3. 코어 (탑다운 3/4 정면)

| 항목 | 값 |
|---|---|
| 파일 | `Assets/Art/Interior/LastStop_Core_TopDown_Sheet_v1.png` |
| 셀 | **128x128 px** |
| 크기 | 본체 약 96x104 px(바닥 받침 포함). 플레이어(80 px)보다 크게 |
| 피벗 | 받침 바닥 중앙 **(64, 12) → (0.5, 0.09375)** |
| 방향 | 1방향(정면 3/4) |
| 시트 배치 | 6열 x 4행 = **768x512** |
| 슬라이스 접두어 | `Core` |
| 색 상한 | 24색 + 외곽선 |

| 행 | 상태 | 프레임 | 속도 | 비고 |
|---|---|---|---|---|
| 0 | Idle | 6 | 8 fps | 시안 캡슐 안 에너지가 1–2 px씩 오르내림, 받침 램프 깜빡임 |
| 1 | Hit | 3 | 15 fps | 0: 캡슐 흰색, 본체 1 px 아래로 눌림, 1: 원래 색 + 금 빛, 2: 복귀 |
| 2 | LowHP (공유 HP 30% 이하) | 6 | 10 fps | 캡슐 빛이 시안 ↔ 빨강 `#E03A3A`로 깜빡임, 캡슐에 금 1개, 경고 램프 |
| 3 | Destroyed | 1 | — | 캡슐 깨짐, 빛 꺼짐. 불꽃 없음(VFX 담당) |

- 디자인: 목업의 **시안 캡슐 반응로**(어두운 금속 하우징, 양옆 장갑판, 노랑 해저드 줄무늬 받침)를 기본으로 하고, 기존 코어의 녹색/시안/빨강 버튼 3개를 받침 앞면에 남긴다(디렉터 질문 7).
- **A3-G 발광(별도):** `LastStop_Core_TopDown_Glow_v1.png`, **192x96 x 4프레임 = 768x96**, 바닥 동심원 링(목업의 시안 원), 피벗 (0.5, 0.5), 색 `#127C8C` `#2EC4D6` `#7EF2F5` 3색만, 외곽선 없음. 가산 혼합 또는 낮은 알파로 쓰는 것은 Development 결정. LowHP 변형 1줄(빨강) 추가 가능.

### A4. 내부 타일·소품 [잠정 — 전체가 Planning 레이아웃 의존]

목업처럼 지붕을 걷어낸 3/4 탑다운. 위쪽 벽은 안쪽 면, 아래쪽 벽은 윗면 가장자리만 보인다. 기본 단위 **48x48 px = 1 wu**.

**A4-T 타일셋** `Assets/Art/Interior/LastStop_Interior_TopDown_Tiles_v1.png`, 셀 48x48, 8열 x 8행 = **384x384**, 피벗 (0.5, 0.5), Tilemap/Rule Tile 용도, 크로마 불필요(불투명). 색 상한 32색.

| 행 | 내용 |
|---|---|
| 0 | 바닥 기본 4종(미세 변형), 바닥 이음매 2종, 배수 그레이트 1, 빈칸 1 |
| 1 | 노란 바닥 안내선(직선 가로/세로, 모서리 4) |
| 2 | 위쪽 벽 안쪽 면 하단(창문 아래 패널) 4종 |
| 3 | 위쪽 벽 안쪽 면 상단(창문 + 밖의 앰버 빛 반사) 2종, 천장등 붙은 창 2종 |
| 4 | 아래쪽 벽 윗면 가장자리(높이 48 중 아래 16 px만 선체, 나머지 투명) 4종 |
| 5 | 앞·뒤 끝 모서리(둥근 선체) 4종 |
| 6 | 문턱(해저드 줄무늬) 2종, 계단 2종 [잠정] |
| 7 | 예비 |

**A4-P 소품** `Assets/Art/Interior/LastStop_Interior_TopDown_Props_v1.png`, 마젠타 크로마, 색 상한 32색, 모든 소품 피벗은 바닥 접점 중앙(y-정렬용).

| 슬라이스 | 셀 | 프레임 | 피벗 | 비고 |
|---|---|---|---|---|
| `Prop_SeatPair_FacingDown` | 96x80 | 1 | (0.5, 0.1) | 위쪽 줄 좌석(쿠션과 등받이 앞면이 보임) |
| `Prop_SeatPair_FacingUp` | 96x80 | 1 | (0.5, 0.1) | 아래쪽 줄 좌석(등받이 뒷면이 보임) |
| `Prop_Seat_Single` | 48x80 | 1 | (0.5, 0.1) | 끝자리 |
| `Prop_Pole` | 16x112 | 1 | (0.5, 0.05) | 노란 수직 기둥, 캐릭터와 겹칠 때 y-정렬 |
| `Prop_Strap` | 16x32 | 2 | (0.5, 1.0) | 천장 손잡이, 흔들림 2프레임 |
| `Prop_Door` [잠정] | 96x160 | 4 | (0.5, 0.05) | 닫힘, 열림 1–2, 열림+앰버 조명. **봉인 판자·자물쇠 없음** |
| `Prop_DriverCab` [잠정] | 160x240 | 1 | (0.5, 0.05) | 운전석, 계기판, 핸들 |
| `Decal_Blood` | 32x32 | 3 | (0.5, 0.5) | 바닥 데칼, 정렬 맨 아래 |
| `Decal_Paper` | 32x32 | 3 | (0.5, 0.5) | 신문지·영수증 |

시트 크기는 소품을 셀 높이 기준 행으로 묶어 1024x512 안에 배치하고, 실제 좌표는 납품 시 슬라이스 표로 함께 준다(크기가 섞여 Grid 자동 슬라이스 불가 → 수동 슬라이스).

### A5. 외부 버스 탑다운

| 항목 | 값 |
|---|---|
| 파일 | `Assets/Resources/Art/LastStop_BusBody_v2.png`, `Assets/Resources/Art/LastStop_BusWheels_v2.png` (코드가 이미 찾는 이름. 위치는 Development가 확정) |
| 셀 | **320x144 px** (body와 wheels 모든 슬라이스 동일 — 코드 계약) |
| 크기 | 차체 약 304x128: 지붕 304x80 + 화면 아래쪽 옆면 띠 약 40 px(창문, 문, 바퀴 아치). 6.67 x 3.0 wu |
| 방향 | **앞이 오른쪽**(현재 외부 진행 방향). 1방향 |
| 피벗 | 차체 바닥면 중앙 **(160, 72) → (0.5, 0.5)** (지붕 중심. 충돌 중심과 맞추기 쉬움) |
| 슬라이스 | Body 파일: `Bus_Body_NoWheels` 1장. Wheels 파일: `Bus_Wheels_0` … `Bus_Wheels_3` 4장(1x4 = 1280x144), 바퀴 외 투명 |
| 색 상한 | 32색 + 외곽선 |

- 지붕: 기존 버스 노랑을 마스터 팔레트에 맞춘 램프 `#7A5418` `#B98320` `#E8B230` `#F6D365`(기존 버스 정체성 유지). 에어컨 박스, 비상 해치, **지붕 무기 장착점**(A6 피벗이 올 곳, 지붕 중앙에서 앞쪽으로 약 40 px)을 표시해 둔다. 장착점 좌표는 납품 데이터로 준다.
- 옆면 띠: 앰버로 빛나는 창문 열, 문 1–2개, 바퀴 아치(바퀴 레이어가 보이도록 비워 둠). 앞 전조등, 뒤 빨간 후미등 `#E03A3A`.
- 바퀴: 2개(앞, 뒤)만 옆면 띠 아래로 보인다. 4프레임 회전(휠 캡 무늬 45°씩).
- 코드 연동(Development): `CreateBlock`이 버스를 4.8x1.8 wu로 강제하므로(`:344`) 스케일 1 배치로 바꿔야 한다. 피격 빨강 틴트는 코드 그대로.
- 전조등 빛(선택): `LastStop_Bus_TopDown_Headlight_v1.png` 192x96 1장, 외곽선 없음, 앰버 3색.
- 목업의 남색 선체를 외부에도 쓸지는 디렉터 질문 2.

### A6. 지붕 무기 (콩알포, 8방향) + 포구 섬광

탑다운에서 포탑은 지붕 위에서 내려다보이므로 포신이 지붕 평면에 눕는다. 이전 BLOCKED 원인(측면 3/4의 위/아래 원근 단축)이 크게 줄어든다.

| 항목 | 값 |
|---|---|
| 파일 | `Assets/Art/Vehicles/Weapons/LastStop_RoofWeapon_Bean_TopDown_Sheet_v1.png` |
| 셀 | **48x48 px** |
| 방향 | 8방향: R, UR, U, DR, D 5행을 그림 + L, UL, DL은 flipX |
| 프레임 | 3열(Idle, Recoil — 포신 2 px 후퇴, Settle) |
| 시트 크기 | 3열 x 5행 = **144x240** |
| 피벗 | 회전축 = 장착점 **(24, 24) → (0.5, 0.5)** |
| 크기 | 돔 지름 약 20 px, 포신 피벗에서 끝까지 18 px |
| 슬라이스 접두어 | `RoofBean` (예: `RoofBean_UR_01`) |
| 색 상한 | 14색 + 외곽선 |

**포구 좌표 (필수 납품 데이터).** 셀 왼쪽 아래 (0,0) 기준. 포신 끝 개구부 중앙이 이 점에서 1 px 이내. 3프레임 모두 Idle 값을 쓴다.

| 방향 | 포구 점 (px) | 피벗 기준 (px) | world 오프셋 (PPU 48) |
|---|---|---|---|
| R | (42, 24) | (+18, 0) | (+0.375, 0) |
| UR | (37, 37) | (+13, +13) | (+0.271, +0.271) |
| U | (24, 42) | (0, +18) | (0, +0.375) |
| DR | (37, 11) | (+13, −13) | (+0.271, −0.271) |
| D | (24, 6) | (0, −18) | (0, −0.375) |
| L (flip R) | (6, 24) | (−18, 0) | (−0.375, 0) |
| UL (flip UR) | (11, 37) | (−13, +13) | (−0.271, +0.271) |
| DL (flip DR) | (11, 11) | (−13, −13) | (−0.271, −0.271) |

- 가독성 규칙: 포신은 가장 밝은 금속색 `#D5DBEA`, 돔은 버스 노랑, 포신 끝 2 px은 밝은 링 `#F6D365` + 구멍 `#07080F`. 네 방향 모두 포신 길이는 같다(원근 단축 금지). U 방향은 포신이 돔 뒤(위)로, D 방향은 돔 앞(아래)으로 그려 겹침 순서를 맞춘다.
- 8방향 사이 조준은 가장 가까운 방향 스프라이트를 쓴다(회전 금지, 2.5절). 더 부드럽게 하려면 16방향(9장 그림)으로 확장 가능 — 디렉터 질문 1.
- **포구 섬광:** `LastStop_RoofWeapon_Bean_MuzzleFlash_v1.png`, **24x24 x 3프레임 = 72x24**, 오른쪽 향함, 피벗 **(0, 0.5)**(왼쪽 가장자리 중앙 = 포구 점), 30 fps. 코드 회전 허용(2.5절 VFX 예외). 반짝이 별이 아니라 **포신 방향 원뿔**.
- 검수용 가이드(임포트 금지): 각 셀 포구 점을 `#FF00FF` 1 px로 찍은 `..._MuzzleGuide_v1.png`, Art 작업 폴더에 보관. 위 표가 정본.
- 팔레트: 버스 노랑 `#7A5418` `#B98320` `#E8B230`, 금속 `#2A3563` `#3C4163` `#5A6390` `#9AA3C0` `#D5DBEA`, 링 `#F6D365`, 구멍 `#07080F`, 콩알 `#C9D94A`.
- 비눗방울 발사기, 화염 꼬치 등 다른 무기도 같은 48x48 셀·피벗·포구 표 형식을 쓰면 장착점을 공유할 수 있다(디렉터 질문 10).

### A2-E. 외부 적 (작은 변형) — P3, 디렉터 질문 3 결과에 따름

- `Assets/Art/Characters/Enemies/LastStop_Rusher_TopDown_Exterior_Sheet_v1.png`, 셀 **64x64**, 키 약 40 px, 피벗 (32, 8) → (0.5, 0.125).
- 4방향(D, U, R + flip) Walk 4프레임 + Death(D) 4프레임 = 4열 x 4행 = 256x256. 색 상한 12색. 같은 디자인을 단순화(꼬리와 눈만 강조).
- 외부 적이 버스에 닿아 내부로 들어가면 내부 표시는 A2를 쓴다(1:1 침입 규칙은 그대로, 그림만 다름).

### SH. 공용 바닥 그림자

- `Assets/Art/Common/LastStop_Shadow_TopDown_v1.png`, 3장: 48x16(적), 64x20(플레이어), 112x28(코어). 단색 `#07080F`, 알파 0/255, 피벗 (0.5, 0.5). 반투명은 SpriteRenderer 색 알파(예: 0.45)로 Development가 처리.

---

## 6. 이미지 생성 프롬프트 (ChatGPT 이미지 생성, 영어 그대로 붙여넣기)

### 6.0 사용 원칙

- 매번 **6.1 공통 스타일 블록 + 에셋 블록**을 이어 붙인다. `{...}`만 바꾼다.
- 첨부: 승인 목업 A(스타일) + 해당 대상의 승인 모델 시트(없으면 6.2의 모델 시트부터). 플레이어·적 모델 시트의 1차 참조로 기존 `LastStop_MvpSpritesheet_v1.png`의 `Player_Exterior_TopDown`, `Enemy_Exterior_TopDown` 영역을 잘라 첨부한다.
- 캔버스는 가로형 1536x1024 권장. 6프레임 x 128 셀이면 가짜 픽셀 약 2 px라 후처리가 쉽다.
- 결과는 밑그림이다. 7절 후처리와 사람 수정 없이 임포트하지 않는다.

### 6.1 공통 스타일 블록 (모든 프롬프트 맨 앞)

```
STYLE: high-resolution pixel art for a 2D top-down survival game, matching the attached style reference (a night bus interior seen from a 3/4 top-down camera, roof removed). Camera looks down at about 35 degrees, so characters are seen slightly from the front and their faces are visible. Grounded proportions, about 5.5 heads tall, not chibi.
PIXEL RULES: true pixel art on an exact grid. Every art pixel is a crisp square block of identical size, enlarged with nearest-neighbor. No anti-aliasing on the outer silhouette, no blur, no soft brushes, no gradients, no bloom, no glow halo, no motion blur, no painterly texture, no photorealism.
OUTLINE: 1-art-pixel near-black outline (#07080F) around every character and object silhouette. Interior lines use the darkest shade of the local color.
LIGHT: night scene. Warm amber key light from above (top of the screen) on heads, shoulders and top surfaces; shadows hue-shifted toward dark blue-violet; a thin cool blue rim (#6E86B0) on the lower and back edges. Do not paint cyan or colored glow onto the character. 3 to 4 flat tones per material.
PALETTE: dark navy and blue-violet environment (#0E162D, #21293B, #30436B, #405F82), amber lights (#C9781F, #FB9E3A, #FFC260, #FFE9A8), yellow handrails (#D9A521, #FBD631), cyan core energy (#127C8C, #2EC4D6, #7EF2F5, #D3FFFF), danger red (#7A1622, #C02A34, #E03A3A). Use a limited palette.
SHEET RULES: equal-size frame cells in a strict grid, the subject the same size and at the same position in every cell, feet or base on the same baseline in every cell. No drop shadow, no floor, no text, no labels, no numbers, no frame borders, no grid lines.
BACKGROUND: perfectly flat solid {#FF00FF magenta} filling the entire image, no texture, no vignette, no shadow on the background.
```

(기본 적 Rusher는 `{#FF00FF magenta}` → `#00FF00 pure green`.)

### 6.2 모델 시트 (애니메이션 전에 1회, 대상별)

```
Create a character model sheet: the same character standing idle in 3 views side by side in equal cells: facing down toward the camera, facing up away from the camera, facing right. Each cell {128x128} art pixels, character about {80} art pixels tall, feet on the bottom baseline at the same height in every cell.
CHARACTER: {character description from the asset block below}.
```

### 6.3 A1 플레이어 (애니메이션 1행 x 1방향씩)

```
SUBJECT: one horizontal row of {6} equal frames, each cell 128x128 art pixels, character about 80 art pixels tall, feet centered horizontally at the same point near the bottom of each cell with about 24 pixels of empty space below the feet and generous empty space around the body for the club swing.
CHARACTER: a young female night bus guard, brown hair tied in a messy bun, red-orange puffy winter jacket (#B8581C, #E07A26, #F6B04A) with a cream fur collar, dark navy pants, brown boots, olive green backpack with a rolled bedroll, holding a wooden club studded with nails. Identical design, proportions and colors to the attached model sheet.
DIRECTION: facing {down toward the camera}.
ANIMATION: {walk cycle, 6 frames, looping, contact poses on frames 1 and 4, body bobs by 1 pixel}.
```

`{ANIMATION}` 대체 문구:
- Idle: `idle breathing, 4 frames, looping, club resting on the shoulder, chest rises by 1 pixel` (`{6}` → `4`)
- AttackA: `first combo strike, 3 frames: frame 1 the club already hitting in front of the character at full extension (contact, no wind-up), frame 2 follow-through across the body, frame 3 recovering to guard. Club stays fully visible, no smear trail` (`{6}` → `3`)
- AttackB: `second combo strike, 3 frames, backhand swing in the opposite direction: frame 1 contact, frame 2 follow-through, frame 3 recover` (`{6}` → `3`)
- AttackC: `third finishing strike, 4 frames: frame 1 overhead club slamming down in front at contact, frame 2 crouched impact pose with the club touching the floor, frame 3 follow-through, frame 4 recovering. Biggest silhouette of the set` (`{6}` → `4`)
- Hurt: `hurt reaction, 2 frames, leaning back away from the facing direction, eyes squeezed shut` (`{6}` → `2`)

`{DIRECTION}` 대체: `down toward the camera` / `up away from the camera, back of the head and backpack visible` / `right, three-quarter side view`.

### 6.4 A2 기본 적 Rusher

```
SUBJECT: one horizontal row of {6} equal frames, each cell 96x96 art pixels, creature about 64 art pixels tall at the head and about 70 pixels long including the tail, paws on the same baseline near the bottom of each cell with about 12 pixels below, body at the same position in every cell.
CHARACTER: a feral skunk-like night critter, dark purple fur (#4B293D, #703950, #834859) with a lighter purple rim on top (#A7678A), huge bushy tail with pale pink stripes (#D99688, #FBC9B3), glowing yellow eyes (#FFD23A), red mouth, small cream claws. Aggressive low stance. Simple bold silhouette that stays readable when fifty copies overlap. Identical design to the attached model sheet.
DIRECTION: facing {down toward the camera}.
ANIMATION: {running cycle, 6 frames, looping, tail bouncing}.
```

대체 문구:
- Attack: `attack, 4 frames: frame 1 crouching and coiling, frame 2 lunging forward, frame 3 biting and clawing at full extension (hit frame), frame 4 landing back` (`{6}` → `4`)
- Hit: `hit reaction, 2 frames, knocked backward, eyes shut` (`{6}` → `2`)
- Death (D만): `death, 6 frames: flinch, collapse, body dissolving into dark purple smoke, smoke spreading, smoke thinning, only two or three small smoke specks left`
- Breach (D만) [잠정]: `entering through a doorway, 3 frames: leaping in mid-air, landing crouched, standing ready` (`{6}` → `3`)

대안(좀비 디자인 채택 시) CHARACTER 줄: `a shambling zombie bus passenger, grey-green skin (#5E6B4A, #8A9670), torn shirt (#6A607E, #BEB7D3) stained with red, hunched posture, arms reaching forward, pale yellow eyes. About 72 art pixels tall.` + 배경 마젠타.

### 6.5 A3 코어

```
SUBJECT: one horizontal row of {6} equal frames, each cell 128x128 art pixels, a single machine about 96 pixels wide and 104 pixels tall standing on the bottom of each cell, same position in every frame, seen from the same 3/4 top-down camera, front face visible.
OBJECT: a compact power core reactor in the middle of a bus: a vertical glass capsule filled with glowing cyan energy (#2EC4D6, #7EF2F5, #D3FFFF) held by dark gunmetal armored housing (#182C3C, #2A3563, #3C4163, #5A6390) with side armor plates, a low base with yellow and black hazard stripes, three small square buttons on the front of the base (green, cyan, red) and one amber indicator light. Simple large shapes, no tiny bolts. The capsule is the brightest element; light inside the capsule only, no glow outside the outline.
STATE: {idle, 6 frames, looping, energy level in the capsule rises and falls by 1 to 2 pixels, base lamp blinks}.
```

대체 문구:
- Hit: `taking a hit, 3 frames: frame 1 capsule flashes white and the machine is squashed down by 1 pixel, frame 2 normal colors with a small crack flicker, frame 3 back to normal` (`{6}` → `3`)
- LowHP: `critical damage, 6 frames, looping, capsule light alternates between cyan and red (#E03A3A), one crack across the glass, amber warning lamp flashing`
- Destroyed: `destroyed, 1 frame, capsule glass shattered and dark, housing dented, no fire, no smoke` (`{6}` → `1`)

A3-G 발광:
```
SUBJECT: one horizontal row of 4 equal frames, each cell 192x96 art pixels, flat concentric elliptical rings on the floor seen from a 3/4 top-down camera, centered in each cell, made of only three colors (#127C8C, #2EC4D6, #7EF2F5), thin 1 to 2 pixel ring lines and a few broken arc segments, rings slowly expanding outward across the 4 frames. No outline, no solid fill, no blur.
```

### 6.6 A4 내부 타일·소품 [잠정]

타일셋 (크로마 대신 불투명 타일이므로 BACKGROUND 줄을 `BACKGROUND: none, tiles fill their cells completely.`로 바꾼다):
```
SUBJECT: a tileset grid of 8 columns by 8 rows, every tile exactly 48x48 art pixels, seamless edges, for the floor and walls of a city bus interior seen from a 3/4 top-down camera with the roof removed. No outlines on tiles.
ROW CONTENT: row 1 dark blue-violet anti-slip floor (#1D1E32, #2F314C, #414465) in 4 subtle variations, 2 seam tiles, 1 drain grate; row 2 yellow floor guide line pieces (straight and corners, #D9A521); row 3 lower panels of the far wall's inner face; row 4 the far wall's windows showing amber city light reflections (#FB9E3A, #FFC260) and two with a warm ceiling lamp; row 5 the near wall's top rim seen from above (dark navy hull #1E2447, #35427B, only the bottom 16 pixels filled, rest flat magenta); row 6 rounded front and rear corner pieces; row 7 a hazard-striped door threshold and step; row 8 empty magenta.
```

소품 (1회에 같은 크기 소품만):
```
SUBJECT: one row of {4} equal cells, each {96x80} art pixels, {pairs of bus seats facing down toward the camera so the seat cushions and the front of the backrests are visible}, standing on the bottom edge of each cell.
OBJECT DETAILS: navy blue fabric seats (#1A2C5B, #25305A, #3A4C8C) with a small orange dot pattern (#E07A3A), dark metal frames, a yellow grab handle on top of the backrest (#FBD631). Slight wear variations between cells.
```
`{OBJECT}` 대체: `pairs of bus seats seen from behind, backrests facing the camera` / `a single vertical yellow handrail pole (16x112 cells)` / `a bus side door, closed, half open, fully open, fully open with warm amber light spilling onto the floor; no boards, no locks, no barricades (96x160 cells)` / `small floor decals: three blood splatters (#7A1622, #C02A34) and three crumpled newspapers (32x32 cells)`.

### 6.7 A5 외부 버스 탑다운

```
SUBJECT: a single city bus seen from a 3/4 top-down camera at night, driving to the RIGHT. Canvas cell 320x144 art pixels, bus body about 304 pixels long, centered. Mostly the roof is visible (about 80 pixels tall) plus a thin strip of the near (bottom) side about 40 pixels tall showing a row of amber-lit windows, one or two doors and two empty wheel arches.
DETAILS: yellow roof (#7A5418, #B98320, #E8B230, #F6D365) with an air-conditioning unit and an emergency hatch, a small round empty turret mount ring on the roof slightly toward the front, headlights at the right end, red tail lights (#E03A3A) at the left end, dark windshield at the right end. NO wheels drawn (leave the wheel arches dark and empty).
```

바퀴(별도 생성 후 같은 셀에 맞춰 배치):
```
SUBJECT: one row of 4 equal frames, each 320x144 art pixels, showing ONLY two dark bus wheels with grey hubcaps at the bottom edge positions matching the attached bus (front wheel near the right, rear wheel near the left), everything else flat magenta. Each frame rotates the hubcap pattern by 45 degrees.
```

### 6.8 A6 지붕 무기 콩알포 (방향 1행씩)

```
SUBJECT: one row of 3 equal frames, each cell 48x48 art pixels, a small roof-mounted cannon turret seen from directly above at a slight 3/4 angle. The rotation center (a round yellow dome about 20 pixels wide, #B98320, #E8B230, on a dark metal ring) is exactly at the center of every cell. One long bright steel barrel (#9AA3C0, #D5DBEA) points {RIGHT}, 18 pixels from the center to the tip, never shortened by perspective. The barrel tip has a bright yellow ring (#F6D365) around a dark bore hole (#07080F).
FRAMES: idle, recoil (barrel pulled back toward the dome by 2 pixels), settling.
LAYERING: {for UP the barrel goes behind the dome; for DOWN the barrel lies in front of the dome}.
NO muzzle flash, no projectile.
```
`{RIGHT}` 대체: `RIGHT` / `diagonally UP-RIGHT at 45 degrees` / `UP toward the top of the screen` / `diagonally DOWN-RIGHT at 45 degrees` / `DOWN toward the bottom of the screen`.

포구 섬광:
```
SUBJECT: one row of 3 equal frames, each 24x24 art pixels. A muzzle flash pointing RIGHT that starts exactly at the middle of the left edge of each cell: a short cone-shaped burst of white and yellow with a tiny round bean-shaped spark. Frame 1 small and bright, frame 2 largest cone, frame 3 fading to two small sparks. Colors only #FFFFFF, #F6D365, #FB9E3A, #C9D94A. No outline, no star shapes, no glow.
```

---

## 7. 후처리 파이프라인 (모든 에셋 공통)

1. **크로마 제거:** 배경색과 RGB 맨해튼 거리 90 이내 → alpha 0. 경계에 남은 분홍/초록 띠는 1 px 침식 또는 수작업.
2. **가짜 픽셀 크기 측정:** 생성 이미지에서 외곽선 두께나 평탄 영역 블록 크기로 한 칸 크기(예: 2.0, 2.67 px)를 잰다. 균등 분할을 가정하지 않는다.
3. **프레임 잘라내기:** 크로마 제거 후 연결 영역(bounding box)으로 프레임을 찾는다. 생성기는 셀 간격을 자주 어긴다.
4. **최근접 축소:** 각 프레임을 목표 셀 크기의 아트 해상도로 Nearest Neighbor 축소(블록 중앙 샘플링, 반 칸 오프셋).
5. **팔레트 매핑:** 2.2절 마스터 팔레트(+캐릭터 램프)에서 에셋별 상한 이하로 가장 가까운 색 매핑, 디더링 없음.
6. **알파 이진화:** alpha < 128 → 0, 나머지 255.
7. **외곽선 보정:** 실루엣 바깥 1 px 외곽선 `#07080F` 끊긴 곳을 채운다(스크립트로 자동 추가 후 사람 확인).
8. **정렬:** 발/바닥 기준선을 피벗 y에, 몸통 중심을 피벗 x에 맞춘다. 같은 행 프레임 사이 기준선 차이 0 px, 몸통 x ±1 px.
9. **사람 수정(Aseprite):** 얼굴, 눈, 손, 곤봉 못, 포신 끝, 포구 링, 좌우 반전 시 어색한 부분. A6은 가이드 PNG로 포구 좌표 1 px 이내 확인.
10. **시트 조립:** 5절 셀 크기와 행 순서로 여백 없이 붙인다.
11. **자동 검증:** 시트 크기, alpha ∈ {0,255}, 색 수, 크로마 잔여 0, 행별 기준선 일치.
12. **Unity 임포트·슬라이스:** 4.3절 설정, Grid By Cell Size, Custom Pivot, 슬라이스 이름 규칙, 빈 셀 삭제.
13. **화면 리뷰:** 960x540과 1920x1080 실제 플레이 캡처(Development/QA가 생성)로 8절 체크리스트.

참고용 Pillow 스크립트(실행 검증하지 않음, Art 작업 폴더에서 사용):

```python
from PIL import Image

def clean_frame(src, box, cell, palette_png, chroma=(255, 0, 255), tol=90):
    """box: 생성 이미지에서 잘라낼 프레임 영역, cell: (w, h) 목표 아트 px 크기,
    palette_png: 2.2절 팔레트 색만 담은 작은 PNG"""
    im = Image.open(src).convert("RGBA").crop(box)
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if abs(r - chroma[0]) + abs(g - chroma[1]) + abs(b - chroma[2]) < tol:
                px[x, y] = (0, 0, 0, 0)
    small = im.resize(cell, Image.NEAREST)
    alpha = small.getchannel("A").point(lambda v: 255 if v >= 128 else 0)
    pal = Image.open(palette_png).convert("P")
    rgb = small.convert("RGB").quantize(palette=pal, dither=Image.Dither.NONE).convert("RGBA")
    rgb.putalpha(alpha)
    return rgb

def validate_sheet(path, size, max_colors):
    im = Image.open(path).convert("RGBA")
    assert im.size == size, f"size {im.size} != {size}"
    alphas = {a for *_, a in im.getdata()}
    assert alphas <= {0, 255}, f"semi-transparent alpha: {sorted(alphas)[:10]}"
    colors = {p[:3] for p in im.getdata() if p[3] == 255}
    assert len(colors) <= max_colors + 1, f"{len(colors)} colors"
    assert (255, 0, 255) not in colors and (0, 255, 0) not in colors, "chroma left"
```

---

## 8. 리뷰 체크리스트

리뷰 조건: 960x540(1x)과 1920x1080(2x) 실제 플레이 캡처(정지와 실시간 영상). Art는 Unity를 실행하지 않으므로 캡처는 Development/QA가 만들고 통과 여부는 디렉터가 판정한다.

**파일과 임포트**
- [ ] 시트 크기가 5절과 같다(768x2304, 576x1056, 768x512, 768x96, 384x384, 144x240, 72x24, Bus body 320x144 / wheels 1280x144 등).
- [ ] alpha는 0과 255만 있다(`validate_sheet`).
- [ ] 색 수가 항목별 상한 이하이고 모든 색이 마스터 팔레트(+캐릭터 램프)에 있다.
- [ ] `.meta`: `filterMode: 0`, **모든 플랫폼** `textureCompression: 0`, `spritePixelsToUnits: 48`(또는 합의값), mipmap off, 자동 잔여 슬라이스 없음.
- [ ] 슬라이스 이름과 피벗이 5절과 같다. `Bus_Body_NoWheels`와 `Bus_Wheels_0..3`은 rect·pivot·PPU가 모두 같다.
- [ ] 기존 파일과 기존 `.meta` GUID는 변경하지 않았다.
- [ ] 봉인(판자, 자물쇠, 바리케이드) 모티프가 없다.

**스타일 일관성 (목업 A와 나란히 놓고)**
- [ ] 밤 남색 환경 + 앰버 실내등 + 시안 코어 + 빨강 위험의 색 역할이 목업과 같다.
- [ ] 외곽선이 1 아트 px `#07080F`이고 실루엣 바깥에 AA·후광이 없다.
- [ ] 그라데이션·블룸·페인터리 질감이 없다(거절안 B 특징 없음).
- [ ] 한 화면 안에서 픽셀 크기가 모두 같다(400% 확대 확인, 1080p에서 2x2 일정). Pixel Perfect Camera 미적용이면 "보류"로 기록.
- [ ] 캐릭터 시점(위에서 약 35°, 얼굴 보임)이 플레이어·적·코어·소품에서 같다.

**가독성 (100% 크기, 리뷰어 2명 이상이 설명 없이 판단)**
- [ ] 플레이어 키 80 아트 px(1080p 160 px)이고 노란 기둥 앞, 좌석 사이에서도 실루엣이 분리된다.
- [ ] 플레이어·적의 바라보는 방향(4 또는 8)을 리뷰어가 맞힌다. flipX한 L 방향이 어색하지 않다.
- [ ] 걷기가 미끄러지지 않는다(접지 프레임과 이동 속도, Dev와 확인).
- [ ] 곤봉 공격의 방향과 타격 순간이 첫 프레임에 보인다. 콤보를 만들면 3타가 가장 크다.
- [ ] 적 공격의 hit 프레임이 실제 HP 감소 시점과 ±1프레임 이내.
- [ ] 50마리 경보 상황 캡처에서 플레이어와 코어 위치를 2초 안에 찾는다.
- [ ] 코어 Idle / Hit / LowHP / Destroyed가 코드 틴트 없이도 구분된다.
- [ ] y-정렬: 캐릭터가 좌석·기둥 앞뒤를 지날 때 겹침 순서가 맞다(피벗이 발/바닥).
- [ ] 외부: 버스 앞(오른쪽)이 바로 읽힌다. 바퀴 4프레임이 body와 정확히 겹친다.
- [ ] 지붕 무기: 8방향 조준을 리뷰어가 맞힌다. 포구 섬광과 탄환 생성 위치가 포구 표에서 아트 1 px 이내(Dev 연동 후).
- [ ] 지붕 무기가 버스 실루엣을 과하게 가리지 않는다(지붕 폭의 1/3 이하).

---

## 9. 의존성과 담당 (Art가 하지 않는 일)

| 필요 작업 | 담당 | 내용 |
|---|---|---|
| 해상도·픽셀 배율 | Development + 디렉터 승인 | 3절 권장안(960x540 가상, PPU 48, ortho 5.625, Pixel Perfect Camera) 결정, 신규 스프라이트 스케일 1 배치 |
| 탑다운 내부 사양 | Planning + 디렉터 | 레이아웃, 조작(공격 방향 수), 적 경로, 카메라. A1 콤보·방향, A2 Breach, A4 전체가 이것에 의존 |
| 프레임 재생 | Development | 시트를 애니메이션으로 재생, 방향 선택과 flipX |
| 내부 적 스프라이트 교체 | Development | `SeparateInteriorEnemyVisual`(`LastStopPrototype.cs:807`)의 외부 스프라이트 복사를 A2 애니메이션으로 교체. `SpawnInteriorEnemy`(`:1125`)는 호출처가 없어 고쳐도 효과 없음 |
| 외부 버스 | Development | `CreateBlock` 강제 크기(`:344`) 대신 스케일 1, 지붕 무기 장착점 데이터 사용 |
| 포구 위치 | Development | A6 포구 표를 데이터로 사용, `(0.4, 0.7)` 고정값(`:1010`) 대체 |
| y-정렬과 조명 | Development | Custom Axis 정렬, 코어 시안광·앰버광을 2D Light 또는 가산 스프라이트로 |
| 로드 경로 | Development | `Assets/Art/...` 납품물을 Resources로 옮길지 참조로 바꿀지 |
| 공격 타이밍 | Development / Planning | 적 hit 프레임과 0.8초 데미지 동기화. 밸런스 변경 없음 |
| 실제 이미지 생성과 픽셀 수정 | 이미지 생성 도구 + 사람 아티스트 | 6절 프롬프트와 7절 후처리 |

## 10. 이번 스프린트에서 제외 (참고)

- 외부 탑다운 도로 타일셋(아스팔트, 차선, 비 웅덩이, 가로등, 나무): 다음 우선순위. 6.1 스타일 블록 그대로 사용.
- VFX 시트 재제작(프레임 분리, 색번짐 제거, 피격·사망 탑다운화, BatArc). 포구 섬광은 A6에 포함.
- HUD 초상화·아이콘(목업의 HP 초상화, 타이머 아이콘): UI 팀 위임 시.
- 폐기 후보(삭제는 디렉터 승인 후, 지금은 아무것도 지우지 않음): `Player_Interior_SideView`, `Enemy_Interior_SideView`, `LastStop_GuardSwing_v6.png`, `LastStop_NightBusInterior_v1.png`, `LastStop_NightRoad_v1.png`, `Bus_Interior_Door_Seal`, `Vfx_Seal_Complete`. 측면 3/4 `Bus_Exterior_Yellow`는 A5 납품 후 폐기 후보.

## 11. 디렉터 확인 질문

1. **방향 수 — 결정됨 (2026-11-27): 플레이어와 적 모두 8방향.** 지붕 무기 16방향 확장 여부만 남음. 원래 질문: 플레이어·적은 **4방향(3장 그림 + flipX)**을 권장한다(프레임 약 45개 vs 8방향 약 75개). 지붕 무기는 **8방향(5장 그림)**을 권장한다. 승인하는가, 플레이어만 8방향으로 할 것인가? 지붕 무기를 16방향으로 늘릴 것인가?
2. **캐릭터 정체성:** 목업은 남성 생존자, 좀비, 남색 버스를 보여주지만 현재 에셋은 여성 경비원, 스컹크형 짐승, 노란 버스이다. 이 문서는 **기존 디자인을 목업 스타일로 다시 그리는 것**을 기본으로 했다. 목업 디자인으로 바꿀 것인가(대안 프롬프트 6.4 포함)?
3. **외부 픽셀 밀도:** 외부도 내부와 같은 밀도(PPU 48)를 쓰고 외부 적은 작은 변형(키 약 40 px, A2-E)으로 할지, 외부 카메라를 줌아웃해 밀도가 달라지는 것을 허용할지?
4. **내부 앞뒤 방향:** 목업은 운전석이 왼쪽(앞이 왼쪽)이지만 외부 버스는 오른쪽으로 달린다. 내부도 앞을 오른쪽으로 뒤집을 것인가? (Planning 레이아웃과 함께 결정)
5. **해상도:** 1920x1080 기준(가상 960x540, 2x)을 승인하는가? 1280x720은 비정수 배율 확대를 허용하는가?
6. **조명:** 코어 시안광과 앰버 국소광을 런타임 2D 조명(또는 가산 스프라이트)으로 처리하고 스프라이트에는 굽지 않는 방식을 승인하는가?
7. **코어 디자인:** 목업의 시안 캡슐 반응로에 기존 버튼 3개를 남기는 절충안을 승인하는가, 기존 화면 단말기 디자인을 유지할 것인가?
8. **플레이어 재킷 색:** 노란 기둥 `#FBD631`과 구분하려고 붉은 주황(`#E07A26`)으로 옮기는 것을 승인하는가?
9. **판정 크기:** 캐릭터 그림이 커진다(플레이어 1.67 wu). 충돌·공격 판정은 그대로 두고 그림만 키울지 함께 조정할지(메커닉 변경은 제안만 한다)?
10. **다른 무기:** 비눗방울 발사기, 화염 꼬치도 지붕 포탑 스프라이트가 필요한가, 같은 장착점을 공유하는가?
11. **곤봉 콤보:** 탑다운에서도 3타 콤보(A/B/C)를 유지하는가? 유지하지 않으면 AttackB/C 행을 만들지 않는다(Planning 사양 대기).
