# Dispatch: Forensic Auditor 1 — Integrity Verification

## Objective
Perform an exhaustive forensic audit on the research deliverable `/home/moonsprocket/Slime-Nindja-Research/RESEARCH_REPORT.md` and verify complete adherence to all constraints and acceptance criteria.

## Key Inputs
- Target Report: `/home/moonsprocket/Slime-Nindja-Research/RESEARCH_REPORT.md`
- Original Request: `/home/moonsprocket/Slime-Nindja/.agents/teamwork/ORIGINAL_REQUEST.md`
- Local Repository: `/home/moonsprocket/Slime-Nindja`

## Audit Checks:
1. **Read-Only Codebase Integrity**:
   - Check `git status` in `/home/moonsprocket/Slime-Nindja`. Are any project files modified, created, or deleted? (Must be completely clean).
2. **Zero Fabrication & Authenticity**:
   - Are any fake or non-existent URLs present?
   - Are any competitor ratings, review quotes, or developer identities fabricated?
   - Are line numbers, classes, or methods hallucinated?
3. **Acceptance Criteria Verification**:
   - [ ] Is the final report in `/home/moonsprocket/Slime-Nindja-Research/RESEARCH_REPORT.md`?
   - [ ] Does it contain all 9 required sections?
   - [ ] Does Section 2 cite actual classes, methods, and line numbers in `/home/moonsprocket/Slime-Nindja`?
   - [ ] Does Section 3 cite real games with working Yandex Games links?
   - [ ] Are recommendations evidence-based?
   - [ ] Does Section 6 separate mandatory blockers from recommended features?
   - [ ] Does Section 7 contain the 8-column matrix in 4 groups and 2-phase roadmap?
   - [ ] Does Section 8 define telemetry events and metric formulas?
   - [ ] Does Section 9 list sources with access dates?
4. **Substance vs Placeholder / Facade Check**:
   - Check whether the document contains superficial bullet points, TODOs, or dummy placeholders.

## Output Requirements
Produce forensic report in:
`/home/moonsprocket/Slime-Nindja/.agents/teamwork/teamwork_preview_auditor_1/audit_report.md`
and write `handoff.md` with explicit verdict: `CLEAN` or `INTEGRITY VIOLATION`.

## 2026-10-15T15:22:50Z
User Request received:
You are Forensic Auditor 1 for Slime Ninja deep research.
Your working directory is: /home/moonsprocket/Slime-Nindja/.agents/teamwork/teamwork_preview_auditor_1/

MANDATORY INPUTS:
- Read ORIGINAL_REQUEST.md at: /home/moonsprocket/Slime-Nindja/.agents/teamwork/ORIGINAL_REQUEST.md
- Read your task assignment at: /home/moonsprocket/Slime-Nindja/.agents/teamwork/teamwork_preview_auditor_1/DISPATCH.md
- Read target report at: /home/moonsprocket/Slime-Nindja-Research/RESEARCH_REPORT.md
- Check codebase at: /home/moonsprocket/Slime-Nindja (STRICTLY READ-ONLY)

MISSION:
Perform an exhaustive forensic audit on /home/moonsprocket/Slime-Nindja-Research/RESEARCH_REPORT.md.
Check:
1. git status in /home/moonsprocket/Slime-Nindja is clean (zero modifications).
2. Zero fabrication: all competitor URLs, developer names, review quotes, code line citations, and platform quotas are authentic and verified.
3. Complete fulfillment of all 9 required sections and all Acceptance Criteria in ORIGINAL_REQUEST.md.
4. Substance check: no dummy text, no placeholders, no skipped sections.
Write your audit report to:
/home/moonsprocket/Slime-Nindja/.agents/teamwork/teamwork_preview_auditor_1/audit_report.md
Write your completion handoff.md with explicit verdict: CLEAN or INTEGRITY VIOLATION.
When done, notify the orchestrator with send_message.

