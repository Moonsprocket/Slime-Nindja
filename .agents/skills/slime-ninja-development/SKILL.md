---
name: slime-ninja-development
description: Workflow and protocol for autonomous multi-agent development on the Slime Ninja (Alchemia) Unity project.
---

# Slime Ninja Autonomous Development Protocol

## 1. Overview
This skill governs the autonomous multi-agent engineering workflow for the Unity project **Slime Ninja** (`Alchemia`), targeting deployment to Yandex Games (WebGL Desktop and Mobile Web).

## 2. Agent Roles
- **Project Orchestrator**: Manages the dependency graph, selects next tasks, coordinates workers and reviews, and handles integration into `ai/slime-ninja-development`.
- **Explorers**: Inspect codebase, architecture, documentation, and dependencies without modifying code.
- **Workers**: Implement isolated single-task modifications with exclusive file ownership.
- **Critic**: Performs independent code review checking architectural soundness, zero-GC, null-safety, and regressions.
- **Challenger**: Tests edge cases, boundary conditions, platform constraints (WebGL single thread, mobile touch, focus loss, network failure).
- **Auditor**: Verifies that acceptance criteria are backed by real command output and execution logs, not assumptions.
- **Success Auditor**: Performs end-to-end regression testing and final release readiness checks.

## 3. Safe Workspace & Git Guidelines
- All development takes place in the dedicated Teamwork workspace `/home/moonsprocket/Slime-Nindja-Teamwork` on branch `ai/slime-ninja-development`.
- The main branch (`main`) and the primary repository (`/home/moonsprocket/Slime-Nindja`) are protected.
- Atomic commits with task IDs: `feat(scope): description [TASK-ID]` or `fix(scope): description [TASK-ID]`.
- No destructive git commands (`git reset --hard`, `git clean -fd`, `git push --force`).
- No commits of untracked machine-generated artifacts (font cache, crash dumps, local browser data).
- Verified commits in `ai/slime-ninja-development` are pushed to the local origin repository `/home/moonsprocket/Slime-Nindja`.

## 4. Execution Cycle per Task
1. **Pre-flight**: Inspect dependencies, target files, and acceptance criteria in `IMPLEMENTATION_BACKLOG.md`.
2. **Implementation**: Worker implements minimal, targeted changes without unrelated refactoring.
3. **Automated Verification**: Run `unity run . -- -nographics` or automated tests. Ensure exit code 0.
4. **Independent Review**: Critic & Challenger evaluate code changes against edge cases.
5. **Audit**: Auditor verifies actual logs against acceptance criteria.
6. **Commit & Integration**: Stage only modified task files. Commit with task ID.
7. **Tracking**: Update `IMPLEMENTATION_STATUS.md` and append entry to `DEVELOPMENT_LOG.md`.
8. **Progression**: Advance automatically to the next unlocked task in the dependency graph.
