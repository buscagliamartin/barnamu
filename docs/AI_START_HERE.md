# AI_START_HERE

## Purpose

This is the first file future AI sessions should read for the BarnaMu / OpenMU / MUnique documentation system. It prevents token waste from scanning scattered old Markdown files or unrelated repository areas.

## Current Clean Baseline

As of 2026-06-07, use `C:\MuDev\_clean\OpenMU` as the clean server and `C:\MuDev\_clean\MuMain` as the clean client. Do not use `C:\MuDev\_clean_wrong_base_20260607`.

Before porting OpenMU features, read `docs/PROJECTS/UPSTREAM_MIGRATION.md` for the aligned upstream baseline checkpoint, selected upstream commit, preserved DB rule, smoke test result, and next BarnaMu custom migration order.

## Required Reading Order

1. Read `docs/AI_START_HERE.md` first.
2. Read `docs/DOCUMENTATION_INDEX.md` second.
3. Read only the relevant project file under `docs/PROJECTS/`.
4. Read only the relevant general changelog files:
   - `docs/CHANGELOG_CLIENT.md` when touching `MuMain`, C++, UI, rendering, protocol, textures, or client build notes.
   - `docs/CHANGELOG_SERVER.md` when touching `OpenMU`, C#, game logic, packets, persistence, configuration, plugins, database, or server build notes.
   - `docs/CHANGELOG_WEB.md` when touching `BarnaMuWeb`, Razor Pages, web APIs, rankings, registration, website content, or web deploy.
   - `docs/CHANGELOG_BAT.md` when touching scripts, `.bat`, `.cmd`, `.ps1`, build/start/restart workflows, backups, push helpers, or automation.
5. Do not scan the full repository unless the user explicitly asks.
6. Do not read archived docs unless a missing historical detail is specifically needed.
7. Do not inspect source code unless explicitly asked.
8. When finishing work, update:
   - the relevant project `.md`;
   - every affected general changelog `.md`;
   - the update log / chronological change log sections.

## Area Map

| Area | Meaning | Canonical changelog |
| ---- | ------- | ------------------- |
| `MuMain` | Client, C++, UI, rendering, textures, protocol | `docs/CHANGELOG_CLIENT.md` |
| `OpenMU` | Server, C#, game logic, packets, persistence, plugins, DB/config | `docs/CHANGELOG_SERVER.md` |
| `BarnaMuWeb` | Website / web panel / Razor Pages / APIs / deploy | `docs/CHANGELOG_WEB.md` |
| `Bat` / root scripts | Build/start/stop/restart/backup/push scripts | `docs/CHANGELOG_BAT.md` |

## Project Update Rule

For every task, update both layers:

1. **Project file:** feature-specific status, scope, risks, pending tasks, testing checklist, and update log.
2. **Area changelog:** Client / Server / Web / Bat changelog for each affected repository area.

Example: Duel Ladder client UI work should update `docs/PROJECTS/DUEL_LADDER.md` and `docs/CHANGELOG_CLIENT.md`. If server packets or ladder persistence are touched, also update `docs/CHANGELOG_SERVER.md`.

## Do Not Assume

- Do not invent implementation facts.
- If something is unclear, write `Needs verification`.
- If a file path, class, function, config, packet, or feature status is uncertain, write `TBD - verify during implementation`.
- Do not assume access to `.cpp`, `.h`, `.cs`, web, config, or asset files unless explicitly asked.
- Do not mix third-party/generated documentation into project changelogs.

## Standard Prompt For Future Work

```text
Read docs/AI_START_HERE.md first. We are working on <PROJECT>.
Then read docs/PROJECTS/<PROJECT>.md and only the required changelog files.
Do not scan the repository. Do not inspect unrelated files.
Continue from the documented status.
```
