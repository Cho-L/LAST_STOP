# LAST STOP

달리는 버스를 지키는 2D 픽셀 아트 생존 로그라이크 프로토타입입니다. 외부에서는 버스 장비가 자동으로 공격하고, 내부에서는 플레이어가 직접 방망이를 휘둘러 코어를 지킵니다.

버스 접촉부터 내부 전환과 방망이 반격까지 담은 실제 Unity 플레이입니다.

![LAST STOP 플레이 미리보기](Docs/Media/last-stop-preview.gif)

[게임 소리를 포함한 영상 다운로드](Docs/Media/last-stop-preview.mp4?raw=true)

## Windows 실행 파일

[제출용 Windows 빌드 다운로드](https://github.com/Cho-L/LAST_STOP/releases/tag/portfolio-2026-11-19)

ZIP을 모두 압축 해제한 뒤 `LAST_STOP.exe`를 실행합니다. 같은 폴더의 `LAST_STOP_Data`와 DLL 파일도 필요합니다. 실행 파일은 Unity 설치 없이 사용할 수 있습니다.

## Unity에서 실행

1. 이 저장소를 내려받고 Unity Hub에서 프로젝트 폴더를 추가합니다.
2. Unity **6000.3.15f1**로 열고 에셋 가져오기가 끝날 때까지 기다립니다.
3. `Assets/Scenes/SampleScene.unity`를 열고 Play를 누릅니다.

| 키 | 동작 |
| --- | --- |
| Tab | 버스 외부와 내부 전환 |
| A / D 또는 ← / → | 버스 내부에서 좌우 이동 |
| Space | 방망이 공격. 길게 누르면 연속 공격 |
| 1 / 2 / 3 또는 마우스 | 레벨업 카드 선택 |
| R | 게임 종료 후 재시작 |

## 플레이 흐름

- 버스에 닿은 적은 한 마리씩 내부로 들어옵니다. 내부를 조작하는 동안에도 외부 전투는 계속됩니다.
- 플레이어와 코어는 생존력을 공유합니다. 적의 공격이 명중하면 생존력이 줄고, 0이 되면 게임이 끝납니다. 내부 적 50마리부터 경고가 표시됩니다.
- 레벨업 카드를 골라 장비를 강화하며 8분 동안 버티면 다음 정류장에 도착합니다.

Unity 6, URP 2D, C#, Unity Input System을 사용합니다. 게임 규칙은 [GameDesign.md](Docs/GameDesign.md)에 정리했습니다. Noto Sans CJK KR 글꼴의 라이선스는 [동봉한 라이선스](Assets/Resources/Fonts/NotoSansCJKkr-LICENSE.txt)에서 확인할 수 있습니다.

## 현재 범위와 검증

현재 제출본은 8분 생존과 도착, HP 0 패배, R 재시작을 구현한 프로토타입입니다. 기본 적 한 종류와 콩알포·비눗방울·화염기, 분열 효과·차체 보강이 들어 있습니다. 외부 버스는 자동 주행을 연출하며 수동 조향은 후속 기획입니다.

정류장 정비, 노선 선택, 엔드리스와 영구 해금은 후속 개발 항목입니다. 차내 화염기 간격, 일시정지 피해 차단, 공유 HP와 1:1 침입을 검증한 기록은 [제출 검증 기록](Docs/SubmissionVerification.md)에 있습니다. 아트와 오디오는 프로토타입 단계입니다.
