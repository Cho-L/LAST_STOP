# Claude × Codex 협업 규칙

## 역할
- 감독(사용자): 게임 규칙 승인, PR 최종 머지 승인, Unity 에디터에서 실제 플레이 확인.
- Claude Code (작성자): 코드·문서 변경을 작성하고 `claude/...` 브랜치에 푸시한 뒤 PR을 연다.
- Codex (리뷰어): PR을 리뷰한다. 직접 같은 브랜치에 커밋하지 않고 코멘트로 지적한다.
  수정이 필요하면 Claude가 반영해 같은 PR에 다시 푸시한다.

## 흐름
1. 감독이 작업을 지시한다 (또는 `Docs/Tasks.md`의 TODO에서 고른다).
2. Claude가 작업 설명·변경 파일·의존성·확인 기준을 먼저 적고 구현한다.
3. Claude가 PR을 열고 `.github/pull_request_template.md` 양식을 채운다.
   실행하지 못한 검증(Unity 컴파일, Play Mode, 빌드)은 "미실행"으로 명시한다.
4. 감독이 PR에 `@codex review` 코멘트를 남긴다 (또는 Codex 자동 리뷰 설정).
5. Codex는 `AGENTS.md`의 Review guidelines 기준으로 리뷰한다.
6. Claude가 지적 사항을 반영하거나, 반영하지 않는 이유를 PR 코멘트로 답한다.
7. 감독이 Unity에서 컴파일·플레이를 확인하고 머지한다. 어떤 AI도 스스로 머지하지 않는다.

## 규칙
- 한 PR은 한 가지 목적. 작고 리뷰 가능한 크기로 유지한다.
- 씬(.unity), 프리팹, ProjectSettings, .meta 파일 변경은 PR 본문에 따로 표시하고 이유를 적는다.
  .meta GUID는 보존한다. Library, Temp, Logs, obj는 커밋하지 않는다.
- 미승인 게임 규칙 변경 금지. 필요하면 PR이 아닌 제안으로 감독에게 올린다.
- 클라우드 환경에는 Unity 에디터가 없으므로 Claude·Codex 모두 컴파일/플레이 결과를
  추정으로 "통과"라고 쓰지 않는다. 실제 실행 결과만 기록한다.
- 의견이 갈리면 양쪽 근거를 PR에 남기고 감독이 결정한다.
