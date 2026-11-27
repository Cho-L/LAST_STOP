# LAST STOP — shared instructions
You are working on LAST STOP, a Unity 6 2D roguelike. The human user is game director; root Codex session is producer.
Read Docs/GameDesign.md, Docs/Tasks.md and your team guide before work. Never silently invent approved mechanics. Propose design changes to director.
Core loop: exterior top-down bus auto-combat; each enemy touching the bus enters its interior one-for-one; simultaneous side-view manual combat defends a visible interior core. Player and core share one survival HP pool, damaged only by actual enemy hits; HP zero ends the run. Switch views and repeat. There is no sealing mechanic. 50 interior enemies is an alert threshold, NOT automatically game over.
Do not overwrite Unity scenes, prefabs, .meta files or ProjectSettings without coordination. Preserve Unity .meta GUIDs. Never edit Library, Temp, Logs, obj. Keep scene ownership with Development team; UI owns UI prefabs only when delegated.
Before writing: describe task, touched files, dependencies, acceptance tests. When done: changed files, test commands/results, assumptions, blockers. Do not claim tests passed unless run.
Use small reviewable changes. Avoid destructive commands, force pushes, unattended merges, or adding secrets. Human approves final integration. Work on distinct branches/worktrees for simultaneous writes; subagents in one worktree should research/review or use exclusive file ownership.
MVP first. Art/audio generators require separate tools; text-only agents should output specifications/placeholders, not pretend to have generated assets.
