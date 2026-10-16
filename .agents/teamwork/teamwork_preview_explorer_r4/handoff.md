# Handoff Report: R4 Yandex Games SDK Technical Analysis & Platform Specification

**Role**: Platform & SDK Technical Analyst (R4)  
**Deliverable Artifact**: `/home/moonsprocket/Slime-Nindja/.agents/teamwork/teamwork_preview_explorer_r4/r4_yandex_sdk.md`  
**Date**: October 2026  
**Parent Agent**: 928cc3d5-dc43-41e4-b8b8-18238c16c57b  

---

## 1. Observation

1. **Official Yandex Games Documentation Content**:
   - Evaluated official documentation at `https://yandex.ru/dev/games/doc/ru/`:
     - **Archive & Sizing**: Requirement 1.21 mandates total uncompressed contents of ZIP <= 100 MB. Requirement 1.22 mandates `index.html` at the archive root.
     - **Sound & Audio Muting**: Requirement 1.3 states: *"При потере фокуса окном игры (например, переключение вкладки, открытие другого окна или вызов контекстного меню браузера) звук в игре должен отключаться"*. Requirement 4.7 mandates complete game audio muting during ad playback.
     - **Ad Placement**: Requirement 4.4 states: *"Полноэкранная реклама не должна показываться во время активного игрового процесса в играх с быстрыми сессиями (раннеры, аркады, шутеры)"*. If a round lasts > 5 minutes, a 2-second visual countdown with frozen gameplay is mandatory before showing an interstitial.
     - **SDK Lifecycle Events**: `ysdk.on('game_api_pause')` and `ysdk.on('game_api_resume')` are triggered by platform overlays and automatic launch interstitials. `ysdk.features.LoadingAPI?.ready()` and `ysdk.features.GameplayAPI?.start() / stop()` are required for featuring and session analytics.
     - **Player & Cloud Storage Quotas**: `ysdk.getPlayer()` allows max 20 req / 5 min. `player.setData(data, flush)` allows max 200 KB per player, max 100 req / 5 min. `player.setStats(stats)` allows max 10 KB per player (numbers only), max 60 req / 1 min.
     - **Leaderboards**: Direct API `ysdk.leaderboards.setScore` (rate limit 1 req / sec) and `getEntries` (max 20 req / 5 min). Authorized players only.
     - **Retention Prompts**: `ysdk.feedback.canReview()` / `requestReview()` and `ysdk.shortcut.canShowPrompt()` / `showPrompt()`.

2. **Existing Slime Ninja Codebase Inspection**:
   - `Assets/Scripts/GameManager.cs`: Currently uses `PlayerPrefs.GetInt("Score", 0)` and `PlayerPrefs.SetInt("Score", score)`. Lacks any Yandex SDK initialization, cloud synchronization, or WebGL `.jslib` bridge.
   - `Assets/Scripts/GameManager.cs`: Modifies `Time.timeScale` for pause menus, but does not listen to browser window blur, document visibility changes, or `game_api_pause` events.
   - Project currently has no WebGL template configured for Yandex Games SDK script inclusion (`https://yandex.ru/games/sdk/v2`), resulting in default Unity WebGL templates with browser scrollbars or unhandled resize logic.

---

## 2. Logic Chain

1. **Moderation Blocker Avoidance**:
   - Yandex Games automated and manual moderation immediately rejects builds that fail Requirements 1.3 (sound leaks when switching tabs/apps), 1.21 (>100 MB uncompressed zip), 1.22 (`index.html` not at root), 1.10 (browser scrollbars), or 4.4 (mid-game interstitial interruptions).
   - Therefore, the Unity engine must hook into WebGL browser DOM events (`blur`, `focus`, `visibilitychange`) and SDK events (`game_api_pause`, `game_api_resume`), setting `AudioListener.pause = true` and `Time.timeScale = 0`.

2. **Arcade-Specific Ad Timing**:
   - Because Slime Ninja is a high-speed arcade game where a single mistimed freeze breaks player combos and causes instant deaths, interstitials during active slicing violate Requirement 4.4.
   - Therefore, interstitials must be strictly restricted to: (1) Game Over screen, (2) Restart button, or (3) Main Menu transitions.
   - Rewarded video must provide high perceived value (1x Revive per run, or 2x Score/Coins on Game Over) with zero cooldown, awarding perks strictly in the `onRewarded` callback.

3. **Storage & Data Integrity**:
   - Apple's Safari ITP deletes `localStorage` and `IndexedDB` after 7 days of inactivity. Unity's default `PlayerPrefs` in WebGL maps to `IndexedDB`, making long-term retention fragile for iOS web users.
   - Therefore, data must be bifurcated: numerical stats (coins, best score, slimes sliced) synced via `player.setStats()` / `incrementStats()`, and complex JSON data (unlocked blades, skins, settings) synced via `player.setData()`. For guest players on iOS, `ysdk.getStorage()` (`safeStorage`) must be utilized.

4. **Leaderboard & Auth Flow**:
   - Yandex leaderboards reject submissions from unauthorized users.
   - Therefore, score submission should check `player.isAuthorized()`. If false, trigger `openAuthDialog()` or cache high score locally until auth is completed.

---

## 3. Caveats

1. **Unity Build Pipeline**: Slime Ninja build size has not yet been measured with WebGL compression enabled. Asset stripping, texture compression (ASTC/Crunch or WebP), and WebAssembly memory limits (<= 512 MB) must be benchmarked upon the first WebGL test build.
2. **Sticky Banner Placement**: While sticky banners are configured via Developer Console, bottom sticky banners may interfere with bottom swipe gestures. Physical testing on Android/iOS mobile devices is required to determine whether top-anchored banners or in-game safe-area padding is superior.
3. **Draft Metadata & Assets**: Promotional graphics (Icon 512x512, Cover 800x470, Showcase Hero Cover 1560x520, 16:9 and 9:16 videos) are mandatory for release and featuring, but must be produced by the art/creative pipeline.

---

## 4. Conclusion

Slime Ninja is exceptionally well-suited for the Yandex Games arcade audience, provided the integration strictly adheres to the technical architecture established in `r4_yandex_sdk.md`:
1. Implement `Assets/Plugins/WebGL/YandexSDK.jslib` and `YandexSDKManager.cs` to manage lifecycle, sound muting, and ads.
2. Replace isolated `PlayerPrefs` calls with a hybrid `SaveManager` bridging local cache to Yandex Cloud (`setData` and `setStats`).
3. Enforce zero mid-game interstitials, restricting ads to Game Over, Restart, and Rewarded Revive (1x per run).
4. Utilize a responsive, zero-scrollbar WebGL template with `LoadingAPI.ready()` and `GameplayAPI.start() / stop()` calls to qualify for platform featuring.

---

## 5. Verification Method

To independently verify the technical findings and compliance rules:

1. **Documentation Verification**:
   - Review official Yandex Games developer portal:
     - `https://yandex.ru/dev/games/doc/ru/requirements/1/3` (Audio muting)
     - `https://yandex.ru/dev/games/doc/ru/requirements/4/4` (Ad placement)
     - `https://yandex.ru/dev/games/doc/ru/sdk/sdk-player#data` (Cloud storage limits)
     - `https://yandex.ru/dev/games/doc/ru/sdk/sdk-game-events` (GameplayAPI / LoadingAPI)
2. **File Inspection**:
   - Inspect the complete technical report at:
     `/home/moonsprocket/Slime-Nindja/.agents/teamwork/teamwork_preview_explorer_r4/r4_yandex_sdk.md`
3. **Pre-Moderation Test Steps (Post-Implementation)**:
   - Build WebGL zip and verify uncompressed size: `unzip -l SlimeNinja.zip | tail -n 1` (< 100 MB).
   - Test tab switching and window minimizing in browser to verify immediate sound muting.
   - Test Rewarded Video callback flow to ensure rewards are not granted upon early close or error.
