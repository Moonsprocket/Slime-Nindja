# Dispatch: R4 Yandex Games SDK & Technical Requirements Audit

## Objective
Analyze official documentation and technical specifications for publishing Unity WebGL games on Yandex Games (Яндекс Игры).

## Key Inputs
- Official docs: https://yandex.ru/dev/games/doc/
- Original request: `/home/moonsprocket/Slime-Nindja/.agents/teamwork/ORIGINAL_REQUEST.md`
- Project scope: `/home/moonsprocket/Slime-Nindja/.agents/teamwork/teamwork_preview_orchestrator_1/PROJECT.md`

## Specific Areas to Investigate & Document
1. WebGL Build Technical Requirements:
   - Target bundle size limits (initial download size, uncompressed vs gzip/brotli limits).
   - Mobile vs Desktop browser compatibility and responsiveness (canvas sizing, orientation, high-DPI scaling).
   - Audio and gameplay lifecycle handling: mandatory pausing of AudioListener and timeScale when game loses focus (tab switch, window blur, ad display).
   - Unity WebGL template recommendations and loading bar requirements.
2. SDK Features & Integration Points:
   - Player Authorization: guest vs logged-in players, Yandex Passport auth flow.
   - Cloud Saves (`ysdk.getPlayer().setData()` / `getData()`): payload size limits, sync strategies, offline fallback.
   - Leaderboards (`ysdk.getLeaderboards()`): public vs player scores, score formats, refresh intervals, multiple boards support.
   - Review & Shortcut prompt API (`ysdk.feedback.canReview()`, `ysdk.shortcut.canShowPrompt()`).
3. Monetization Policies & Limits:
   - Interstitial Ads (Full-screen ads): minimum cooldown rules (e.g. 60-180 seconds between impressions), rules for showing (never during active slashing/action, only at natural pauses/game over).
   - Rewarded Video Ads: placement ideas (revive once per run, 2x end-of-run score/essence, unlocking premium trail/skin trial), cooldowns, reward fulfillment reliability.
   - Banner Ads (Sticky banner / RTB): supported devices, positioning, impact on mobile touch zones.
   - Monetization requirements to pass Yandex Games moderation.
4. Compliance Matrix:
   - Mandatory Requirements (Strict moderation blockers — game will be rejected if missing).
   - Strongly Recommended Features (Direct impact on catalog algorithms, featuring, and CPM/eCPM).
   - Nice-to-have features (Future expansion).

## Output Requirements
Produce a comprehensive report in:
`/home/moonsprocket/Slime-Nindja/.agents/teamwork/teamwork_preview_explorer_r4/r4_yandex_sdk.md`
and write a standard `handoff.md`. Include direct links to Yandex Games documentation sections.

## Hard Constraints

## 2026-10-15T14:58:48Z
You are the Platform & SDK Technical Analyst (R4) for Slime Ninja deep research.
Your working directory is: /home/moonsprocket/Slime-Nindja/.agents/teamwork/teamwork_preview_explorer_r4/

MANDATORY INPUTS:
- Read ORIGINAL_REQUEST.md at: /home/moonsprocket/Slime-Nindja/.agents/teamwork/ORIGINAL_REQUEST.md
- Read your task assignment at: /home/moonsprocket/Slime-Nindja/.agents/teamwork/teamwork_preview_explorer_r4/DISPATCH.md
- Read project scope at: /home/moonsprocket/Slime-Nindja/.agents/teamwork/teamwork_preview_orchestrator_1/PROJECT.md
- Official documentation: https://yandex.ru/dev/games/doc/

HARD CONSTRAINT:
- The codebase at /home/moonsprocket/Slime-Nindja is STRICTLY READ-ONLY.

MISSION:
Analyze official documentation and technical specifications for publishing Unity WebGL games on Yandex Games (Яндекс Игры):
1. WebGL Build Technical Requirements: bundle size limits, mobile vs desktop responsiveness, lifecycle event handling (AudioListener/timeScale pause on blur/ad), WebGL template requirements.
2. SDK Features: Player Authorization, Cloud Saves (Player Data limits, synchronization), Leaderboards (scores, formats, multiple tables), Review prompt & Shortcut APIs.
3. Monetization Policies & Limits: Interstitial ads (minimum cooldowns, placement rules - never during gameplay, only at game over/pause), Rewarded Video ads (cooldowns, placements, revive/2x rewards), Sticky Banners.
4. Compliance Matrix: Categorize all platform items into Mandatory (moderation rejection blockers) vs Recommended (featuring & algorithmic boost) vs Nice-to-have. Cite exact doc sections.

OUTPUT:
Write your full detailed report to:
/home/moonsprocket/Slime-Nindja/.agents/teamwork/teamwork_preview_explorer_r4/r4_yandex_sdk.md
Write your completion handoff to:
/home/moonsprocket/Slime-Nindja/.agents/teamwork/teamwork_preview_explorer_r4/handoff.md
Update progress.md throughout your work.
When done, send a message to orchestrator with your summary.
