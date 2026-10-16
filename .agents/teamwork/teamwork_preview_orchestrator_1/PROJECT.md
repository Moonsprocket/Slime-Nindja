# Project: Deep Research — Slime Ninja: от Game Jam к полноценной игре

## Architecture & Scope
Research and product-technical audit for transforming the Slime Ninja game jam prototype into a high-retention browser arcade on Yandex Games. Target deliverable: `/home/moonsprocket/Slime-Nindja-Research/RESEARCH_REPORT.md`.

## Feature Inventory / Research Deliverables
| # | Requirement | Description | Milestone | Specialist | Status |
|---|-------------|-------------|-----------|------------|--------|
| 1 | R1: Technical Codebase Audit | In-depth audit of GameManager.cs, Blade.cs, Sliceable.cs, Spawner.cs, Balance.cs, Sfx.cs, Hud.cs, ComboBar.cs, StreakBar.cs, Backdrop.cs, Balance.asset, UniversalRenderPipelineGlobalSettings.asset with line numbers, architecture risks, and WebGL readiness. | M1 | explorer_code_audit | DONE |
| 2 | R2: Market & Competitor Analysis | Analyze >=4 comparable games on Yandex Games with direct working URLs, retention drivers, player reviews, good practices, antipatterns. | M2 | explorer_market_research | DONE |
| 3 | R3: Game Design & Retention | Gameplay variability, risk/reward, lightweight meta-progression, daily challenges, adaptive difficulty, gameplay vs cosmetic mechanics. | M3 | explorer_game_design | DONE |
| 4 | R4: Yandex Games SDK & Platform Requirements | WebGL limits, auth & cloud saves, leaderboards, interstitial/rewarded ads rules & frequencies, mandatory vs recommended requirements. | M4 | explorer_platform_sdk | DONE |
| 5 | R5: Prioritization Matrix & Roadmap | Prioritization matrix (8 columns, 4 groups), 2-phase roadmap (MVP and Expanded), telemetry events & metric formulas. | M5 | worker_report_writer | DONE |
| 6 | Deliverable Assembly | Complete 9-section report in /home/moonsprocket/Slime-Nindja-Research/RESEARCH_REPORT.md meeting all acceptance criteria. | M5 | worker_report_writer | DONE |
| 7 | Rigorous Verification & Audit | Independent review by Reviewers and Forensic Auditor verifying exact code references, working links, absence of fabricated metrics, and complete 9 sections. | M6 | reviewer_1, reviewer_2, challenger_1, challenger_2, auditor_1 | DONE |

## Milestones
| # | Name | Scope | Dependencies | Status | Assigned Agent Conv ID |
|---|------|-------|-------------|--------|------------------------|
| M1 | R1 Code Audit | Technical analysis of all specified scripts & assets | none | DONE | 33877716-8649-4532-8fda-f2ebeeb45d78 |
| M2 | R2 Competitive Research | Live Yandex Games competitor analysis & review mining | none | DONE | b92ec15e-51b3-427d-b000-491070d41976 |
| M3 | R3 Game Design | Retention, replayability, meta-progression design | none | DONE | e343d3b7-f985-4e7e-a1cb-97cb70a6c49a |
| M4 | R4 Yandex SDK Audit | SDK technical constraints, ads policy, requirements | none | DONE | f7945f42-a071-4c8d-8384-f23976edb98d |
| M5 | Report Synthesis & Drafting | Author RESEARCH_REPORT.md with all 9 sections & R5 matrix | M1, M2, M3, M4 | DONE | 1420de25-092c-43eb-97cd-cedbd3f0f59a, 144c75a2 |
| M6 | Review & Audit Gate | Verification of factual accuracy, links, completeness | M5 | DONE (PASS) | 3db49148, 743317f9, d64c9c7d, 3ff7b865, 0d7f76cd |

## Code Layout & File Boundaries
- Input codebase (strictly READ-ONLY): `/home/moonsprocket/Slime-Nindja`
- Output target deliverable: `/home/moonsprocket/Slime-Nindja-Research/RESEARCH_REPORT.md`
