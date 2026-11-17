# LAST STOP - work queue (portfolio snapshot)

Updated: 2026-11-17. This document distinguishes **code present in the repository** from **runtime verification**. A playable Unity prototype is committed to `codex/netmarble-submission`; this review has not run Unity locally.

## Implemented in source / awaiting runtime verification (REVIEW)
- [ ] P-001 | Design baseline: exterior automatic combat, interior manual combat, 1:1 enemy breach, shared HP, 8-minute stop goal. Confirmed in `Docs/GameDesign.md`; review completeness and changes with human director.
- [ ] D-001 | Unity 6 project baseline and scene entry point. Verify clean project import and Play mode.
- [ ] D-002 | Exterior combat prototype: auto attack, enemy spawning, progress/upgrade flow. Verify in Unity editor.
- [ ] D-003 | Interior combat prototype: Tab view switch, bat attacks, one-for-one breaches, shared HP, restart. Verify in Unity editor.
- [ ] U-001 | HUD and level-up choices: source assets and UI code present; verify layout/readability at target resolution.
- [ ] A-001 | Pixel-art sprite resources included; verify sprite slicing, visibility, and licenses.
- [ ] S-001 | Audio feedback code/resources: verify sound mixing and absence of missing references.
- [ ] QA-001 | Human playtest of the complete loop; **not yet independently verified**.

## Known design rule (not an open task)
- **There is no sealing mechanic.** Any earlier mention of "sealing" in D-003 was outdated and has been removed.
- An interior enemy count of **50** is a warning threshold, **not** an automatic loss.
- The game ends only when the shared survival HP reaches zero; enemy count alone does not cause damage.
- For the demo, reaching the next stop is an **8-minute** objective. Full release scope may differ.

## Verification checklist for the human reviewer
1. Open the project in **Unity 6000.3.15f1**, load `Assets/Scenes/SampleScene.unity`, and press Play.
2. Confirm outdoor automatic combat and incoming enemies; switch view with Tab.
3. Confirm indoor movement (A/D), bat attack (Space), and that breached enemies attack player/core as specified.
4. Confirm one-to-one breach handling and that exterior combat continues while indoor view is active.
5. Confirm shared HP decreases only on successful enemy hits; HP <= 0 opens game over, and R restarts.
6. Confirm upgrade card inputs (1/2/3 or mouse) and game sound.
7. Confirm 8-minute stop objective, monitor Unity Console for errors, capture tested commit, date and evidence.

## Acceptance log (fill after actual test)
- Tested commit: TBD
- Editor/OS: TBD
- Playtest date: TBD
- Pass/fail and reproducible bugs: TBD
- Evidence (screenshots/video): TBD

Status: `TODO` = not started, `IN PROGRESS` = underway, `REVIEW` = code/artifact exists but acceptance evidence pending, `DONE` = tested and verified, `BLOCKED` = waiting for dependency.
