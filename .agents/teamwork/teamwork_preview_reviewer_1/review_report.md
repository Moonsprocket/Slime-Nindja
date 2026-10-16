# Review & Adversarial Challenge Report: Slime Ninja Deep Research
**Audited Document**: `/home/moonsprocket/Slime-Nindja-Research/RESEARCH_REPORT.md` (Sections 1, 2, 6, 7)  
**Reviewer**: Reviewer 1 (Roles: Quality Reviewer & Adversarial Critic)  
**Target Codebase (Strictly Read-Only)**: `/home/moonsprocket/Slime-Nindja`  
**Date of Audit**: October 15, 2026  
**Final Verdict**: **REQUEST_CHANGES**

---

## 1. Executive Summary & Verdict

### 1.1. Verdict
**REQUEST_CHANGES**

### 1.2. High-Level Assessment
The research report `/home/moonsprocket/Slime-Nindja-Research/RESEARCH_REPORT.md` demonstrates outstanding technical depth, strong architectural understanding of Unity 6.6 WebGL internals, and authentic static analysis of the Slime Ninja codebase. The identification of the fatal WebGL single-thread deadlock in `Sfx.cs:43, 88`, the evaluation of garbage collection spikes in `Sliceable.cs:30-65`, and the documentation of uncompressed assets in `AlchemiaTools.cs` represent exemplary engineering analysis.

However, the report fails strict compliance with explicit task requirements in two key areas and introduces a critical bug in its platform SDK reference code:
1. **R5 Prioritization Matrix Column Omission (Major/Contractual)**: The summary prioritization table in Section 7.1 contains only **6 columns** instead of the **8 mandatory columns** required by R5 of `ORIGINAL_REQUEST.md`. Specifically, the **«Риски» (Risks)** column is completely missing, and **«Проблема»** and **«Обоснование»** are collapsed into a single column.
2. **Defective C#/JS Bridge Implementation (Critical/Technical)**: In Section 6.8, `YandexSDK.jslib` invokes `SendMessage('YandexSDKManager', 'OnPlayerInitialized', ...)`, but `YandexSDKManager.cs` **does not implement `OnPlayerInitialized`**. Consequently, `IsAuthorized` remains permanently `false`, causing all high scores sent to Yandex Leaderboards (`SubmitScore:990`) to silently fail. Furthermore, the bridge provides no mechanism for loading cloud data (`getData()`), rendering cloud progression half-implemented.
3. **Incomplete Asset Audit in Section 2 (Major)**: While `UniversalRP.asset` is examined, `UniversalRenderPipelineGlobalSettings.asset` (explicitly mandated by R1 and DISPATCH) was not analyzed in Section 2.
4. **Minor Citation Inaccuracies (Minor)**: `GameManager.cs` is stated to have 742 lines (actual: 622 lines), and `Hud.cs` is cited as 1–664 lines (actual: 770 lines).

---

## 2. Integrity Verification (Anti-Cheat & Anti-Fabrication Audit)

Pursuant to the reviewer mandate, the work was audited against integrity violation patterns:
- **Hardcoded test results / expected outputs**: None found.
- **Dummy or facade implementations**: The C# code in Section 6.8 contains omission bugs, but is clearly structured as architectural guidance rather than a deceptive facade.
- **Shortcuts bypassing the intended task**: None. Detailed static analysis of all scripts and assets was genuinely conducted.
- **Fabricated verification outputs or logs**: None. All line citations correspond to genuine source code in `/home/moonsprocket/Slime-Nindja`.
- **Self-certifying work**: None. The report cites external official documentation and real source lines.

**Integrity Status**: **CLEARED** (No integrity violation tag. Findings are normal technical and compliance issues).

---

## 3. Detailed Review by Section

### Section 1: Executive Summary
- **Strategic Opportunity (1.1–1.2)**: Accurately frames Slime Ninja as a Fruit Ninja-style arcade in an alchemical setting. Identifies the "disposable arcade" jam syndrome (lack of meta-progression, repetitive early game, flat skill curve). Competitor benchmarking (Fruit Ninja, Mr. Slice, Master of the Sword) is solidly integrated.
- **Core Technical Pivot (1.3)**: Correctly identifies the 4 essential engineering pillars:
  1. Resolving the WebGL single-thread deadlock in `Sfx.cs`.
  2. Zero-GC discipline (Object Pooling for slimes and slice pieces).
  3. Bundle size reduction (21 MB Resources down to <8 MB via ASTC/Crunch and Vorbis).
  4. Platform lifecycle integration (audio mute on blur/ads, dual-layer cloud saves).
- **Business Targets (1.4)**: Projections ($D1 > 35\%$, $D7 > 12\%$, session length $> 8$ min, rating $\ge 4.5$) are ambitious but well-defined.
- **Adversarial Critique**:
  - *Risk*: A $D1 > 35\%$ retention target on Yandex Games WebGL for an arcade without social/multiplayer loops relies heavily on daily rewards and cosmetic collection. On mobile web, cache clearances and casual bounce rates frequently cap arcade D1 at 22–26%. The report should note that hitting $>35\%$ requires aggressive push-notification integration (shortcut prompt + browser notification if available) and immediate Day 2 hook delivery.

---

### Section 2: Codebase Audit & Technical Limitations
All files cited were cross-checked against `/home/moonsprocket/Slime-Nindja`:

#### 1. File Coverage & Line Citations
| File / Asset | Cited in Report | Actual File Status | Verification Method & Evidence | Status |
|---|---|---|---|---|
| `GameManager.cs` | Execution order:11, Awake:67-99, Mult:48, Pause:114-122, GameOver:229-252, PlayerPrefs:243, 261-265, Combo:401, Streak:424-445, Frenzy:448-470, Transmute:473-482, PostFx:568-605, View/Unit:606-612, ScreenToWorld:614-620 | Exists, 622 lines | Verified via `view_file`. All line citations match verbatim. | **PASS** (Minor note: total lines stated as 742 in text, actual is 622) |
| `Blade.cs` | Order:8, TrailTime:11, MinCutSpeed:12, Padding:14, Smoothing:15, MaxSamples:16, OnEnable:57-65, Cut:168-186, CatmullRom:204-228, Paint:238-246, ReadPointer:267-284 | Exists, 329 lines | Verified via `view_file`. Exact match across all constants, logic, and CCD mathematics. | **PASS** |
| `Sliceable.cs` | Order:6, BombReach:9, CanCut:21, Reach:22, Spawn:30-65, Ballistics:113-114, Squash:117-118, 151-157, Slice/Cut:181, ShellBreak:193-194, Mitosis:206-208, 218-236 | Exists, 273 lines | Verified via `view_file`. Math formulas, shell guard time, and mitosis arcs verified. | **PASS** |
| `Spawner.cs` | 9 patterns:114-129, Anti-crowding:412-465, Release coroutine:474, 480 | Exists, 592 lines | Verified via `view_file`. Exact match for pattern rules and trajectory overlap logic. | **PASS** |
| `Balance.cs` & `Balance.asset` | comboMin:56, frenzyCombo:57, comboBonus:58, comboWindow:59, frenzySeconds:60, frenzyRest:61, slowSeconds:62 (3.2s vs 3.15s in asset:21), slowScale:63, missGrace:64, peak:86, kingChance:94, kingCooldown:96 | Exists, 235 lines / 511 lines | Verified via `view_file`. Discrepancy between C# default (3.2s) and asset (3.15s) correctly caught. | **PASS** |
| `Sfx.cs` | Ladder pentatonic:19, Task.Run:43, job.Wait:88, Synthesizer:165-401 | Exists, 601 lines | Verified via `view_file`. Exact match for deadlock mechanism and DSP formula. | **PASS** |
| `Hud.cs` | Mentioned on line 120; line range 1-664 cited in Section 9 | Exists, 770 lines | Only briefly mentioned in Section 2.1. Procedural layout and popup allocations not analyzed. Line count 664 vs 770. | **NEEDS IMPROVEMENT** |
| `ComboBar.cs` | 5 Tiers:22-29 (Boil, Bubble, Resonance, Flame, Great Work) | Exists, 311 lines | Verified via `view_file`. Color and particle parameters match code. | **PASS** |
| `StreakBar.cs` | Tiers:21-28, Broken:31 | Exists, 246 lines | Verified via `view_file`. Matches exact code logic. | **PASS** |
| `Backdrop.cs` | Layers:1-40, Lamp coordinates:17, Overscan:9 | Exists, 146 lines | Verified via `view_file`. Matches scene setup. | **PASS** |
| `UniversalRP.asset` | HDR support: `m_SupportsHDR: 1` on line 26 | Exists, line 26 | Verified via `grep_search`. Exact match. | **PASS** |
| `UniversalRenderPipelineGlobalSettings.asset` | Not detailed in Section 2 | Exists, 468 lines | Verified via `view_file`. File exists in `Assets/Settings/`, but Section 2 contains no analysis of shader stripping (`m_StripUnusedPostProcessingVariantsAndResources: 0`). | **FAIL / GAP** |
| `PostFx.asset` | Bloom threshold: 0.78, intensity: 0.8, scatter: 0.62 on lines 19-27 | Exists, lines 19-27 | Verified via `view_file`. Matches exact asset values. | **PASS** |
| `AlchemiaTools.cs` | Uncompressed textures:132, PCM audio:154 | Exists, lines 132, 154 | Verified via `view_file`. Confirms uncompressed assets causing 21 MB Resources footprint. | **PASS** |

#### 2. Verification of WebGL Blockers & Architecture
- **Single-Thread WebGL Deadlock in `Sfx.cs`**:
  - `Assets/Scripts/Sfx.cs:43`: `job = Task.Run(Synthesize);`
  - `Assets/Scripts/Sfx.cs:88`: `job.Wait();`
  - `ProjectSettings/ProjectSettings.asset:577`: `webGLThreadsSupport: 0`
  - *Finding*: Fully confirmed. In a single-threaded WebGL build, calling `Task.Run` followed by `job.Wait()` causes an immediate synchronous lockup of the browser thread on initialization.
- **Zero-GC Discipline & Memory Thrashing**:
  - Confirmed: `Sliceable.Spawn()` dynamically calls `new GameObject()` and instantiates multiple SpriteRenderers every spawn (`Sliceable.cs:30-65`).
  - Confirmed: `Fx.cs:541-560` creates ephemeral GameObjects on every slice (`Half`, `Slash`, `Flash`, `Splat`, `Drip`).
  - Confirmed: Lambda delegate allocations occur in `Blade.Cut()` (`hits.Sort`, line 183) and `Spawner.Release()` (`wave.Sort`, line 474).
- **URP Settings & WebGL Build Flags**:
  - Confirmed: HDR is enabled (`UniversalRP.asset:26`), causing FP16 framebuffer allocation.
  - Confirmed: Heavy Bloom is active (`PostFx.asset:19-27`).
  - Confirmed: WebGL compression format is disabled in settings (`ProjectSettings.asset:574`: `webGLCompressionFormat: 0`).

---

### Section 6: Yandex Games Requirements & SDK

#### 1. Platform Requirements Compliance
- **Blockers vs Recommended Features (6.1)**: Clearly delineated in a structured table.
  - *Blockers*: Audio mute outside focus (1.3), audio mute during ads (4.7), prohibition of mid-game ads (4.4), 2s warning for scheduled ads, zip $\le 100\text{ MB}$ (1.21), `index.html` at zip root (1.22), no scrollbars (1.10), icons 512x512.
  - *Recommended*: `LoadingAPI.ready()`, `GameplayAPI.start()/stop()`, Cloud saves, Leaderboards, Review prompt, Shortcut prompt.
- **WebGL Limits (6.2)**: 100 MB uncompressed limit accurately cited. CSS reset specifies `touch-action: none` and `overflow: hidden`, which is essential to prevent accidental pull-to-refresh on mobile browsers.
- **Audio & Lifecycle Pause (6.3)**: Correctly models the state machine distinguishing between `_isPlatformPaused`, `_isManualPaused`, and `_isAdPlaying`. Prevents the bug where returning to the tab unpauses a game manually paused by the user.
- **Cloud Save Quotas (6.5)**: Accurately breaks down `setData` (JSON, 200 KB, 100 calls/5 min) and `setStats` (numerical counters, 10 KB, 60 calls/1 min), along with iOS Safari `safeStorage` mitigation.
- **Ad Policies (6.7)**: Accurately reflects platform restrictions (no mid-slashing interstitials, 60–90s cooldown, 2s countdown before forced ads in long rounds, Rewarded Video reward given strictly in `onRewarded`).

#### 2. Adversarial Code Review of C#/JS Bridge (Section 6.8)
A deep code trace of `YandexSDK.jslib` and `YandexSDKManager.cs` reveals three significant bugs:
1. **Critical Defect — Missing `OnPlayerInitialized` Handler**:
   - In `YandexSDK.jslib:813-816`:
     ```javascript
     SendMessage('YandexSDKManager', 'OnPlayerInitialized', JSON.stringify({
         isAuthorized: window.player.isAuthorized() ? 1 : 0,
         name: window.player.getName() || "Guest"
     }));
     ```
   - In `YandexSDKManager.cs`:
     `public bool IsAuthorized { get; private set; }`
     `public string PlayerName { get; private set; } = "Guest";`
     **`OnPlayerInitialized` is completely absent!**
   - *Impact*: `IsAuthorized` remains `false` forever. In `SubmitScore(int score)`:
     ```csharp
     if (IsAuthorized) YandexSetLeaderboardScore("SlimeNinja_Leaderboard", score, "");
     ```
     Because `IsAuthorized` is never updated, leaderboard scores are **never submitted**.
2. **Missing Cloud Data Loading (`getData`)**:
   - The bridge includes `YandexSaveCloudData` (`setData`), but provides **no `YandexLoadCloudData` (`player.getData()`)**.
   - *Impact*: Cloud data can be saved, but can never be loaded on startup or after page refresh, breaking cross-device and cross-session progress.
3. **Missing Public C# Save Wrapper**:
   - `[DllImport("__Internal")] private static extern void YandexSaveCloudData(...)` is declared at line 899, but no public C# method wraps it for external invocation.
4. **Unity 6 WebGL Asynchronous Callback Scoping Risk**:
   - In Unity WebGL, calling `SendMessage` inside an asynchronous Promise (`then(function() { ... })`) or event listener (`window.ysdk.on(...)`) can throw `ReferenceError: SendMessage is not defined` if `SendMessage` is not bound to `Module.SendMessage` or `window.unityInstance.SendMessage`. The `.jslib` must safely reference `Module.SendMessage` or verify instance binding.

---

### Section 7: Prioritization Matrix & Roadmap

#### 1. Prioritization Matrix Compliance Check (Section 7.1)
The mandate in R5 of `ORIGINAL_REQUEST.md` specifically requires:
> «Составить сводную таблицу приоритизации со столбцами: «Идея», «Проблема», «Обоснование», «Ожидаемый эффект», «Стоимость», «Риски», «Приоритет», «Как проверить». Разбить инициативы на 4 группы: (1) Критично до первой публикации, (2) Быстрые победы, (3) Эксперименты после релиза, (4) Идеи, от которых стоит отказаться.»

**Compliance Evaluation**:
- **4 Groups Represented**: **YES** (Group 1: Blockers, Group 2: Quick Wins, Group 3: Post-Release, Group 4: Dropped / Anti-Patterns).
- **8 Required Columns**: **NO (FAILED)**.
  - The table on lines 1013–1082 has only **6 columns**:
    1. `Группа / Идея`
    2. `Проблема / Обоснование` (Merged)
    3. `Эффект`
    4. `Стоимость`
    5. `Приоритет`
    6. `Метод проверки (Verification)`
  - **The «Риски» (Risks) column is missing entirely.**
  - **«Проблема» and «Обоснование» are combined into a single column.**
  - *Severity*: Major contractual deviation. While Section 5 contains a separate table with a `Риски` column, Section 7.1 is the formal R5 deliverable and must strictly adhere to the 8-column layout.

#### 2. Roadmap Feasibility & Pacing (Section 7.2 & 7.3)
- **Phase 1: MVP for Publication (12 working days, 2.5 weeks)**:
  - *Sprint 1 (Days 1–5)*: Sfx coroutine, AudioListener pause, WebGL canvas CSS, Asset compression, ObjectPool.
  - *Sprint 2 (Days 6–10)*: JS bridge, Lifecycle APIs, Cloud saves, Leaderboards, Interstitial ads.
  - *Sprint 3 (Days 11–12)*: Rewarded video (Revive/2x), store metadata.
  - *Assessment*: Well-sequenced. Core WebGL blockers are resolved before platform APIs are added.
- **Phase 2: Expanded Roadmap (20 working days, 4 weeks)**:
  - *Sprint 4 (Days 13–17)*: Bullseye cut, slow-mo, 5 micro-events, 5 wave formations.
  - *Sprint 5 (Days 18–22)*: Soft currency, ScriptableObject blade system, 12 blades, shop UI.
  - *Sprint 6 (Days 23–27)*: 7-day calendar, streak freeze, 3 daily quests, 24 achievements, DDA controller.
  - *Sprint 7 (Days 28–32)*: High-risk targets, lab themes, feedback dialog, shortcut, marketing assets.
  - *Adversarial Challenge*:
    - **Asset Production Bottleneck (Sprint 5)**: Days 20–21 allocate only 2 days for creating, balancing, drawing sprites, configuring trail particle effects, and sound design for 12 distinct blades. For an indie developer, this is an aggressive underestimate.
    - **Micro-Event Complexity (Sprint 4)**: Implementing 5 complex micro-events (e.g. "Plague Alert" with cascading spores and "Catalyst" with reaction chemistry) in 2 days (Days 15–16) creates a testing deficit.

---

## 4. Adversarial Challenge & Failure Mode Analysis

| # | Subsystem / Assumption | Attack Scenario / Failure Mode | Blast Radius | Recommended Mitigation |
|---|---|---|---|---|
| 1 | **SFX Async Synthesis** | Replacing `Task.Run` with a coroutine still performs heavy mathematical DSP on the main thread during gameplay if `Prepare()` is deferred. | WebGL main thread frame drops (100–300 ms stutter) during startup or first slice. | **Pre-bake all 17 procedural sounds into WAV files** inside `Resources/Sounds` during editor build time, eliminating runtime synthesis entirely in WebGL. |
| 2 | **iOS Safari AudioContext** | iOS Safari suspends `AudioContext` until the first explicit user gesture (`touchend`). If `Sfx.Build()` attempts to play ambient audio before user interaction, audio enters an unrecoverable muted state. | Complete audio silence on iOS devices. | Implement an explicit Web Audio unlock overlay in `index.html` on first user tap before initializing game sound. |
| 3 | **Leaderboard Authorization** | Due to missing `OnPlayerInitialized`, `IsAuthorized` stays `false`. Even if implemented, guest users clicking leaderboard will be blocked without an authorization prompt. | 0% leaderboard participation from unauthorized players; lost viral retention. | Add guest fallback or trigger `ysdk.auth.openAuthDialog()` when a guest attempts to view or submit scores. |
| 4 | **Cloud Save Desynchronization** | If a player plays offline or has a spotty mobile connection, `player.setData()` fails. If the game doesn't cache changes in local `PlayerPrefs`/IndexedDB and retry, progress earned during the session is discarded. | Loss of currency/blades, leading to 1-star reviews. | Enforce a strict **dual-layer write-through cache**: write to local memory/PlayerPrefs first, then push to Yandex Cloud asynchronously with a dirty-flag retry queue. |

---

## 5. Summary of Findings

### Critical Findings (Must Fix)
1. **[Section 6.8] Functional Defect in `YandexSDKManager.cs` — Missing `OnPlayerInitialized`**:
   - *Location*: `/home/moonsprocket/Slime-Nindja-Research/RESEARCH_REPORT.md`, lines 883–994.
   - *Issue*: `YandexSDK.jslib` sends `OnPlayerInitialized`, but `YandexSDKManager.cs` lacks the method. `IsAuthorized` is never set to `true`, completely disabling Leaderboard submissions (`SubmitScore:990`).
   - *Fix*: Add `public void OnPlayerInitialized(string json)` to `YandexSDKManager.cs` with JSON deserialization.
2. **[Section 6.8] Incomplete Cloud Storage Bridge — Missing `getData()`**:
   - *Location*: lines 873–878, 899.
   - *Issue*: The bridge implements `player.setData()`, but provides no `player.getData()` mechanism to retrieve saved progress upon game launch.
   - *Fix*: Add `YandexLoadCloudData` in `YandexSDK.jslib` and a corresponding callback `OnCloudDataLoaded(string json)` in `YandexSDKManager.cs`.

### Major Findings (Should Fix)
3. **[Section 7.1] Contractual Non-Compliance — Prioritization Matrix Missing Required Columns**:
   - *Location*: lines 1013–1082.
   - *Issue*: R5 of `ORIGINAL_REQUEST.md` mandates an 8-column table («Идея», «Проблема», «Обоснование», «Ожидаемый эффект», «Стоимость», «Риски», «Приоритет», «Как проверить»). The matrix has only 6 columns: «Риски» is missing, and «Проблема»/«Обоснование» are merged.
   - *Fix*: Expand the table to all 8 explicit columns, restoring the dedicated `Риски` and separating `Проблема` from `Обоснование`.
4. **[Section 2.8] Missing Audit of `UniversalRenderPipelineGlobalSettings.asset`**:
   - *Location*: Section 2.8, lines 327–331.
   - *Issue*: The asset is cited in Section 9, but omitted from technical analysis in Section 2. Its shader stripping settings (`m_StripUnusedPostProcessingVariantsAndResources: 0`) materially affect WebGL shader build size.
   - *Fix*: Add a short technical note in Section 2.8 analyzing `UniversalRenderPipelineGlobalSettings.asset`.

### Minor Findings (Nice to Fix)
5. **[Section 2.1 & Section 9] Script Line Count Miscounts**:
   - `GameManager.cs`: cited as 742 lines (actual: 622 lines).
   - `Hud.cs`: cited as 1–664 lines (actual: 770 lines).
   - *Fix*: Update numbers to reflect exact file counts.
6. **[Section 2] Shallow Coverage of `Hud.cs`**:
   - *Location*: Section 2.1, line 120.
   - *Issue*: `Hud.cs` is only mentioned in a single bullet point despite building the entire UI procedurally.
   - *Fix*: Add 1–2 paragraphs detailing `Hud.cs` procedural canvas construction and string/popup garbage allocations.

---

## 6. Actionable Recommendations for Author

To transition the document from **REQUEST_CHANGES** to **APPROVE**:
1. In Section 7.1, format the Prioritization Matrix with all 8 explicit columns:
   `| Идея | Проблема | Обоснование | Ожидаемый эффект | Стоимость | Риски | Приоритет | Как проверить |`
2. In Section 6.8, update `YandexSDKManager.cs` to implement `OnPlayerInitialized(string json)` and add cloud loading (`YandexLoadCloudData` / `OnCloudDataLoaded`).
3. In Section 2.8, add the analysis of `UniversalRenderPipelineGlobalSettings.asset`.
4. Correct script line counts for `GameManager.cs` (622) and `Hud.cs` (770).

---

*Report compiled and verified by Reviewer 1 (Quality Reviewer & Adversarial Critic).*
