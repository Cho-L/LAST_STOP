# LAST STOP Codex studio setup (Windows PowerShell)
Prereqs: Git, Unity Hub + Unity 6 (2D project), Codex CLI.
Install: `npm install -g @openai/codex` OR official Windows installer.
Verify: `codex --version`.
1. Create a Unity 6 **2D** project named `LAST-STOP` via Unity Hub.
2. Copy this starter's `AGENTS.md`, `.codex/`, `Docs/`, `.gitignore` into the Unity project root. Merge Assets subfolders; DO NOT replace Unity-generated `Packages/` or `ProjectSettings/`.
3. In PowerShell: `cd C:\path	o\LAST-STOP`; `git init`; `git add .`; `git commit -m "chore: initialize LAST STOP"` (set git user.name/email first if needed).
4. `codex` then choose **Sign in with ChatGPT**. If multi-agent unavailable, check CLI version and enable in `/experimental` then restart.
5. Paste the manager prompt from `PROMPT_MANAGER.md`.
6. Review each team's proposed outputs. Do not start 5 simultaneous edits to same Unity files. Use branches/worktrees for separate implementation tasks.
This zip is a scaffold, NOT a complete Unity project: Unity Hub generates the engine-specific files.
