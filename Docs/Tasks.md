# Work queue

## Collaboration (2026-11-27)
- REVIEW - Claude writes code on claude/* branches, Codex reviews PRs, director merges. See Docs/Collaboration.md.

## Current integration work (2026-11-11)
- REVIEW - aligned the design baseline with approved exterior bus weapons, interior-only character combat, one-for-one intrusion, actual-hit shared HP damage, 50-enemy warning, no sealing, eight-minute demo, and planned endless full release.
- DONE - reset an interior enemy's attack timer when its target changes between core, player, and none; live single-enemy Play Mode logs confirm 0.80-0.81-second hit spacing and 7 HP per hit.
- REVIEW - captured 960x540 exterior and interior sequences. Background scrolling and enemy approach are visible, but roof muzzle/shot origin and character frame animation remain unclear. Real-time video and isolated game audio still need verification.
- DONE - aggregate actual shared HP damage within a fixed 0.35-second HUD feedback window, including hit count and player/core target names. Same-target, mixed-target, expired-window, and low-HP runtime probes passed.
- REVIEW - two same-time core hits reduced HP by 14 and requested two audio cues, but the audio cooldown allowed one playback. Isolated listening and a director-reviewed sound accent remain pending; no sound rule changed.
- BLOCKED - roof weapon four-direction art integration. Up/down readability and down muzzle placement did not pass the 960x540 concept review. Rework at display size before importing an asset.
- REVIEW - reconcile the existing modified and untracked project files before final integration. No item below is DONE without code and play evidence.

## Verification record (2026-11-11)
- Unity recompile: 0 errors, 0 warnings. Play mode advanced frames.
- Runtime probe: core-to-player, player-to-core, and core-to-none target changes reset the timer to 0 without reducing HP. A later live single-enemy run measured the 0.8-second interval; see follow-up record below.
- Runtime probe: one exterior enemy transfer changed outside 1 to 0 and inside 0 to 1 without HP loss. Fifty interior enemies activated the warning while HP remained 100 and gameOver remained false. One direct hit reduced HP from 100 to 93.
- 960x540 captures: exterior, intrusion, interior, interior with an intruder, level-up, and hit feedback were inspected. Continuous combat readability and audio listening were not verified.
- Temporary probes and screenshots did not modify scenes or prefabs. The roof weapon concept remains unimported.

## Follow-up verification (2026-11-11)
- Unity recompile after HUD change: 0 errors, 0 warnings. Play Mode frame count advanced.
- Single enemy at the core: shared_hit times 10.50, 11.30, 12.10, 12.90, 13.71 and later hits differed by 0.80-0.81 seconds; each reduced shared HP by 7. HP 0 triggered game over.
- Before HUD change, two same-time core hits changed HP 100 to 86, but the visible message said -7. CueRequested fired twice while the CoreDamaged LastPlayed timestamp remained unchanged after the second cue.
- After HUD change: two core hits showed -14 and count 2; player plus core showed both targets and -14; an expired window showed a single -7; a final hit from HP 5 showed -5 and game over.
- 960x540 exterior/interior image sequences show scrolling background, enemy approach and interior targets. The roof muzzle/shot origin remains unclear at this size. Character and enemy sprite motion uses scale changes rather than frame animation; full-speed video readability was not established.
- Actual game-only audio was not recorded or judged by ear. OBS's existing scene contained unrelated browser/desktop audio, so no recording was made. Sound changes await an isolated listening check.

## Follow-up verification (2026-11-14)
- DONE - added a presentation-only interior attack pose: each combo strike briefly stretches, compresses, tilts toward the facing direction, and flashes the player sprite. The third strike has stronger emphasis. Attack range, damage, timing, and collision are unchanged.
- Unity recompile after the change: 0 errors, 0 warnings. Runtime probe of the third strike measured active scale 0.262 x 0.232 and 12-degree tilt; after the pose expired, rotation returned to 0 degrees. The probe also exposed and fixed a zero-quaternion initialization case after a Play Mode script reload.
- REVIEW - 960x540 Game view shows the interior player, core, and enemy remain very small relative to the detailed bus background. Drawn walk and attack frames remain an art priority; this pose is a temporary readability improvement.
- REVIEW - measured the mixed-hit HUD string in Play Mode: preferred width 276.8 against a 463.4 rect at the current Game view size, and the bottom message 465.1 against 1150. No clipping was observed, so no HUD code was changed. Isolated game-audio listening remains pending.

## Original MVP checklist (completion evidence pending)

- [ ] P-001 Planning: MVP mechanics spec, interfaces, controls, acceptance criteria
- [ ] D-001 Development: Unity project baseline and folder conventions (after P-001)
- [ ] D-002 Development: exterior prototype (after D-001)
- [ ] D-003 Development: interior prototype, repeatable 1:1 intrusion, shared HP, core defense, breach count and view switch (after D-002)
- [ ] U-001 UI: HUD wireframe and interface contract (after P-001)
- [ ] A-001 Art: pixel-art style sheet and placeholder sprite dimensions (after P-001)
- [ ] S-001 Sound: cue list and audio event naming contract (after P-001)
- [ ] QA-001 Producer: verify playable loop and report issues (after integration)
Status values: TODO / IN PROGRESS / BLOCKED / REVIEW / DONE. Only mark DONE after evidence.


## Submission candidate (2026-11-19)
- DONE - fixed interior flame timer, projectile pause guard, and frame-end level-up offers. Twenty-five live Play Mode checks passed.
- DONE - accelerated input-driven 480-second arrival, HP0 defeat, and R restart after both results passed. See SubmissionVerification.md and Verification/*.txt.
- DONE - clean Windows64 build succeeded; native startup, Korean HUD and advancing game clock observed. Native key-input automation remains unverified. See SubmissionVerification.md.
- TODO - NUnit test suite, normal-speed human playthrough, isolated audio listening and Player profiling.
