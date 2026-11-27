# Sprint 1 — 팀 현황 점검 요약 (2026-11-27)

프로듀서(Claude)가 기획·아트·UI·사운드·개발 하위 에이전트를 병렬로 돌려 코드와 문서를 읽고 작성한 보고서입니다.
모든 내용은 코드 읽기 기반이며 Unity 컴파일·플레이·청취 검증은 하지 않았습니다. 수치와 제안은 감독 검토 전까지 확정이 아닙니다.

| 팀 | 문서 | 핵심 |
|---|---|---|
| 기획 | [Planning.md](Planning.md) | GameDesign 규칙 9개 모두 코드에 구현. 설계에 없는 동작(레벨업 중 8분 시계 정지 등)과 감독 질문 10개 |
| 개발 | [Development.md](Development.md) | 로직 대부분이 LastStopPrototype.cs private 필드. asmdef 3개 + 테스트 27개(T01–T27) 계획, Windows batchmode 명령 |
| UI | [UI.md](UI.md) | 13.5–15px 작은 텍스트, HP 박스 잘림 위험, 50 경고가 색 변경뿐. P0 개선안 |
| 사운드 | [Sound.md](Sound.md) | 동시 피격 시 0.35s 쿨다운으로 소리 1회. 누락 큐(도착·레벨업·저HP) |
| 아트 | [Art.md](Art.md) | 현 스프라이트가 진짜 픽셀아트 아님. 플레이어/적/지붕포/코어 명세와 ChatGPT용 이미지 프롬프트 |

## 프로듀서가 직접 확인한 코드 문제
- `LastStopPrototype.cs:281-282`가 `Art/LastStop_BusBody_v2`, `Art/LastStop_BusWheels_v2`를 로드하지만 `Assets/Resources/Art`에 해당 파일이 없음.
- `LastStopPrototype.cs:63`의 `exteriorPlayer`는 할당되지 않는데 `:1084`에서 사용됨 (현재 도달하지 않는 경로).

## 감독 결정이 필요한 우선 질문
1. 레벨업 중 8분 도착 시계를 멈출지 (현재 멈춤).
2. 외부 시점: 설계 문서는 탑다운, 현재 빌드는 측면에 가까움 — 어느 쪽이 기준인지.
3. "콩알 연발포" 카드: 이름대로 연사속도를 올릴지, 이름을 바꿀지.
4. 동시 피격 사운드 강조안(S-B) 채택 여부.
5. 테스트 착수 순서: asmdef + 스모크 테스트 PR부터 시작할지.
6. 아트 픽셀 스케일 기준(PPU 24, ortho 5.625 제안) 채택 여부.

나머지 질문은 각 팀 문서의 "감독 질문" 절을 참고하세요.
