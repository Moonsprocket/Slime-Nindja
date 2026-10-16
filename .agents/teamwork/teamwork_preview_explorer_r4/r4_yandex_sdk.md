# Yandex Games (Яндекс Игры) Platform & SDK Technical Specification Report
**Project**: Slime Ninja (Unity WebGL)  
**Author**: Platform & SDK Technical Analyst (R4)  
**Date**: October 2026  
**Reference Base**: Official Yandex Games Developer Documentation (`https://yandex.ru/dev/games/doc/ru/`)  
**Status**: Completed Technical Research & Architectural Specification

---

## Executive Summary

Slime Ninja is a high-speed arcade slicing game built in Unity. Porting and publishing this title to Yandex Games requires strict compliance with platform-specific technical constraints, lifecycle events, ad placement policies, and cloud storage specifications. Non-compliance with audio muting, ad timing, archive sizing, or canvas responsiveness leads to **immediate moderation rejection**.

This report establishes the complete technical blueprint for the Yandex Games release, detailing:
1. **WebGL Build Specifications**: 100 MB uncompressed archive limit, canvas viewport scaling, responsive mobile/desktop layout, and mandatory lifecycle hooks (`LoadingAPI`, `GameplayAPI`, `game_api_pause/resume`, startup ads).
2. **SDK Features & Quotas**: Authentication flow, dual-layer cloud saves (`setData` vs `setStats`), iOS Safari ITP storage mitigations, leaderboards, in-game ratings (`requestReview`), and desktop/mobile shortcuts (`showPrompt`).
3. **Monetization & Ad Rules**: Strict prohibition of mid-game interstitials during active slashing, ad cooldown mechanics, 2-second countdown rules for long sessions, rewarded video architecture (Revive & 2x Multiplier), and sticky banner clearance zones.
4. **Moderation Compliance Matrix**: Blockers vs. Featuring Requirements vs. Performance Optimizations with exact official documentation citations.
5. **Codebase Gap Analysis & Architecture**: Direct bridge between Unity C# and JavaScript (`.jslib`), state-resilient lifecycle management, and a hybrid local/cloud save system.

---

## 1. WebGL Build Technical Requirements

### 1.1 Archive Structure & File Size Limits
*Official Citation*: [Добавление черновика игры](https://yandex.ru/dev/games/doc/ru/console/add-new-game/draft) | [Требования к играм: 1.21, 1.22](https://yandex.ru/dev/games/doc/ru/requirements/1/21)

| Parameter | Specification | Platform Rule / Moderation Impact |
| :--- | :--- | :--- |
| **Archive Format** | Standard `.zip` archive | Must contain all build files directly or with relative paths. Nested root folders must not isolate `index.html`. |
| **Root File** | `index.html` at archive root | **Blocker (1.22)**: Submitting a zip where `index.html` is in a subfolder (e.g. `SlimeNinja_Build/index.html`) is an instant automated rejection. |
| **Total Archive Size** | **<= 100 MB uncompressed** | **Blocker (1.21)**: The total size of all extracted files inside the zip must not exceed 100 MB. Unity `.data`, `.wasm`, `.framework.js`, and audio/texture assets must fit in this envelope. |
| **Filename Encoding** | ASCII only, no spaces, no Cyrillic | Files containing Cyrillic characters, spaces, or non-standard symbols will fail CDN upload or cause 404 errors during asset streaming. |
| **Compression** | Brotli or Gzip with decompression fallback | Brotli is strongly recommended for Unity WebGL (`.unityweb`). Ensure the web server or Unity template provides client-side decompression if CDN headers are stripped. |

### 1.2 Viewport, Scaling & Responsiveness (Mobile vs Desktop)
*Official Citation*: [Требования к играм: 1.10](https://yandex.ru/dev/games/doc/ru/requirements/1/10) | [Адаптивность верстки](https://yandex.ru/dev/games/doc/ru/concepts/scaling)

* **No Browser Scrollbars**: The game canvas must fit 100% of the iframe/window. The presence of horizontal or vertical scrollbars is an explicit **Blocker (1.10)**.
* **CSS Reset Requirements**:
  ```css
  html, body {
      margin: 0;
      padding: 0;
      width: 100%;
      height: 100%;
      overflow: hidden;
      background: #000;
      -webkit-touch-callout: none;
      -webkit-user-select: none;
      user-select: none;
      touch-action: none; /* Prevents mobile pull-to-refresh and gesture zooms */
  }
  #unity-container, #unity-canvas {
      width: 100%;
      height: 100%;
      position: absolute;
      top: 0;
      left: 0;
  }
  ```
* **Mobile vs Desktop Adaptability**:
  * **Mobile (Smartphones/Tablets)**: Slime Ninja runs natively in portrait orientation (or landscape depending on design). Input must support single-touch and multi-touch slicing without initiating browser gestures (swipe navigation, refresh).
  * **Desktop**: The canvas must scale dynamically to the browser container maintaining aspect ratio or dynamically adjusting camera FOV/orthographic size without clipping the blade cutting zone or UI headers.
  * **Safe Zones**: Ensure game UI (score, combo counter, lives) is padded away from mobile notch areas and platform overlay buttons (Yandex Games top-right close/profile bar).

### 1.3 Lifecycle Event Handling & Audio Policy
*Official Citation*: [Звук вне игры: 1.3](https://yandex.ru/dev/games/doc/ru/requirements/1/3) | [Запуск и остановка геймплея: 1.16](https://yandex.ru/dev/games/doc/ru/requirements/1/16) | [События в игре](https://yandex.ru/dev/games/doc/ru/sdk/sdk-game-events)

#### The Sound Muting Rule (Blocker 1.3 & 4.7)
Moderation strictly tests audio behavior under three specific scenarios:
1. **Minimizing the browser window** or switching to another application.
2. **Switching browser tabs** away from the game.
3. **Triggering the browser tab context menu** or platform navigation.
4. **During Ad Playback**: Any game audio playing during an interstitial or rewarded video is an instant rejection.

**Requirement**: When the game loses focus or the platform triggers a pause, audio must be **completely muted** (`AudioListener.pause = true` in Unity, plus Web Audio context suspension) and game logic must freeze (`Time.timeScale = 0`).

#### Mandatory Platform Event Listeners
The Yandex Games SDK provides platform-level hooks via `ysdk.on()` that govern automatic platform actions (such as the automatic startup interstitial):

```javascript
// Platform events dispatched by Yandex Games iframe wrapper
ysdk.on('game_api_pause', () => {
    // 1. Mute all Unity audio
    // 2. Pause timeScale
    // 3. Mark background pause state in C#
    SendMessage('YandexSDKManager', 'OnPlatformPause');
});

ysdk.on('game_api_resume', () => {
    // 1. Unmute Unity audio (if not muted by user in settings)
    // 2. Restore timeScale (only if game was NOT manually paused by player)
    SendMessage('YandexSDKManager', 'OnPlatformResume');
});
```

#### Window Blur/Focus Redundancy
Because some browser configurations do not fire iframe events consistently, the WebGL template must also bind native DOM events:
```javascript
window.addEventListener('blur', () => SendMessage('YandexSDKManager', 'OnWindowBlur'));
window.addEventListener('focus', () => SendMessage('YandexSDKManager', 'OnWindowFocus'));
document.addEventListener('visibilitychange', () => {
    if (document.hidden) {
        SendMessage('YandexSDKManager', 'OnWindowBlur');
    } else {
        SendMessage('YandexSDKManager', 'OnWindowFocus');
    }
});
```

### 1.4 LoadingAPI & GameplayAPI Integration
*Official Citation*: [События в игре](https://yandex.ru/dev/games/doc/ru/sdk/sdk-game-events)

#### 1. `LoadingAPI.ready()`
* **Purpose**: Informs the Yandex Games shell that the game has finished compiling WebGL shaders, loading assets, and is ready for player interaction.
* **Timing**: Call immediately after the Unity splash screen/loader finishes and the first interactive screen (Main Menu) is displayed.
* **Syntax**:
  ```javascript
  ysdk.features.LoadingAPI?.ready();
  ```
* **Impact**: Failure to call `ready()` causes platform analytics to flag the game as stuck in loading, damaging catalog ranking.

#### 2. `GameplayAPI.start()` and `GameplayAPI.stop()`
* **Purpose**: Signals active gameplay rounds for platform session analytics, screen time tracking, and smart ad serving.
* **Rules**:
  * Call `ysdk.features.GameplayAPI?.start()` when:
    * The player presses "Play" and slimes begin spawning.
    * The player unpauses the game.
    * The player revives via Rewarded Video.
  * Call `ysdk.features.GameplayAPI?.stop()` when:
    * Game Over occurs (lives depleted or bomb sliced).
    * Player presses Pause to open the pause modal.
    * Player exits to Main Menu.
    * Before initiating any ad display (interstitial or rewarded).

### 1.5 Automatic Startup Interstitial Ad
* **Platform Behavior**: On initial game launch, the Yandex platform automatically attempts to serve an interstitial ad before or during the initial loading phase.
* **Handling**:
  * The SDK does **not** provide a direct callback for the startup ad; instead, it triggers `game_api_pause` and `game_api_resume`.
  * The game must initialize with audio muted until `game_api_resume` fires or until user interaction occurs (complying with modern Web Audio autoplay restrictions).

---

## 2. Yandex Games SDK Features & Technical Quotas

### 2.1 Initialization & SDK Script Embedding
*Official Citation*: [Инициализация SDK](https://yandex.ru/dev/games/doc/ru/sdk/sdk-about)

In `index.html`:
```html
<head>
    <!-- Yandex Games SDK v2 -->
    <script src="https://yandex.ru/games/sdk/v2"></script>
    <script>
        let ysdk = null;
        let player = null;
        let lb = null;

        YaGames.init().then(_ysdk => {
            ysdk = _ysdk;
            console.log('Yandex SDK initialized successfully');
            // Notify Unity
            window.ysdk = ysdk;
        }).catch(err => {
            console.error('Failed to initialize Yandex SDK:', err);
        });
    </script>
</head>
```

---

### 2.2 Player Authentication & Identification
*Official Citation*: [Авторизация пользователя](https://yandex.ru/dev/games/doc/ru/sdk/sdk-player)

#### Guest vs. Authorized Players
* When initialized without authorization, players are in Guest mode.
* **Check Status**: `player.isAuthorized()` (boolean). Note: `player.getMode()` is deprecated.
* **Unique Identification**:
  * `player.getUniqueID()`: Returns a persistent unique identifier for the user scoped to this developer account.
  * `player.getName()` and `player.getPhoto("medium")`: Returns name and avatar URL (empty string or generic avatar for guests).

#### Requesting Authorization
When a guest clicks "Sign In" or tries to submit a competitive leaderboard score:
```javascript
ysdk.auth.openAuthDialog().then(() => {
    // Player successfully logged in
    return ysdk.getPlayer();
}).then(_player => {
    player = _player;
    SendMessage('YandexSDKManager', 'OnAuthSuccess', player.getName());
}).catch(err => {
    // User cancelled or dialog failed
    SendMessage('YandexSDKManager', 'OnAuthFailed', err.message || 'Cancelled');
});
```

#### Quotas:
* `ysdk.getPlayer()`: Maximum **20 requests per 5 minutes**. Must be cached in memory upon initialization.

---

### 2.3 Cloud Saves: `setData` vs. `setStats`
*Official Citation*: [Внутриигровые данные](https://yandex.ru/dev/games/doc/ru/sdk/sdk-player#data)

Yandex Games provides two distinct mechanisms for persisting player progress to the cloud:

```
+-------------------------------------------------------------------------+
|                         YANDEX CLOUD STORAGE                            |
+------------------------------------+------------------------------------+
|           player.setData()         |          player.setStats()         |
+------------------------------------+------------------------------------+
| Payload: Any JSON Object/String    | Payload: Key-Value NUMBERS ONLY    |
| Limit: 200 KB per player           | Limit: 10 KB per player            |
| Rate: Max 100 requests / 5 minutes | Rate: Max 60 requests / 1 minute   |
| Use Case: Unlocks, blade skins,    | Use Case: High-frequency counters: |
| inventory, sound/haptic settings,  | coins, best_score, total_sliced,   |
| quest progress                     | games_played                       |
+------------------------------------+------------------------------------+
```

#### Cloud Save Methods & Constraints:
1. **`player.setData(data, flush)`**:
   * `data`: JSON object (max **200 KB** total across all keys).
   * `flush`: Boolean (`true` forces immediate server write; `false` batches locally in SDK memory). Use `flush: true` sparingly (e.g. after purchasing a skin or ending a play session) to avoid exhausting the 100 req/5 min limit.
2. **`player.setStats(stats)` / `player.incrementStats(increments)`**:
   * Numerical counters only (`{ coins: 450, highScore: 1280 }`).
   * Max **10 KB** total payload.
   * Rate: **60 requests per 1 minute**.
   * Atomic operations: `incrementStats({ coins: 25 })` ensures currency is safely added without race conditions across tabs.
3. **iOS Safari ITP Mitigations (`safeStorage`)**:
   * *Critical Platform Quirk*: On iOS Safari (especially within Yandex App WebViews), Apple's Intelligent Tracking Prevention (ITP) wipes `localStorage` and `IndexedDB` after 7 days of inactivity.
   * *Solution*: For guest players on iOS, use `ysdk.getStorage()` (`safeStorage` API) which bridges directly to native app preferences or server-side cookies, preventing progress loss on device restarts.

---

### 2.4 Leaderboards API
*Official Citation*: [Лидерборды](https://yandex.ru/dev/games/doc/ru/sdk/sdk-leaderboard)

#### Technical Rules & Syntax
* **Access**: Direct access via `ysdk.leaderboards` (the older `ysdk.getLeaderboards()` method is deprecated).
* **Authorized Players Only**: Guests cannot submit scores to Yandex public leaderboards. If `player.isAuthorized() == false`, prompt `openAuthDialog()` before `setScore()`.

#### Methods & Parameters:
1. **`setScore(leaderboardName, score, extraData)`**:
   * `leaderboardName`: Technical ID defined in Yandex Developer Console (e.g. `SlimeNinja_HighScore`).
   * `score`: Positive integer.
   * `extraData`: Optional string (max 1024 characters), can store metadata such as blade used or slimes sliced.
   * **Rate Limit**: Maximum **1 request per second** per player.
2. **`getEntries(leaderboardName, options)`**:
   * Options:
     ```javascript
     {
         includeUser: true,          // Include current user position
         quantityAround: 5,          // Number of entries around user (max 10)
         quantityTop: 10,            // Number of top entries (max 20)
         avatarSize: 'm'             // Avatar size: 's', 'm', 'l'
     }
     ```
   * **Rate Limit**: Maximum **20 requests per 5 minutes**. Must be cached in memory while the player is viewing the leaderboard UI.

---

### 2.5 Feedback & Shortcut Prompts (Retention APIs)
*Official Citation*: [Оценка игры](https://yandex.ru/dev/games/doc/ru/sdk/sdk-review) | [Иконка на рабочий стол](https://yandex.ru/dev/games/doc/ru/sdk/sdk-shortcut)

#### 1. In-Game Rating Dialog (`FeedbackAPI`)
* Allows players to leave a star rating and text review directly inside the game iframe.
* **Method Flow**:
  1. Check availability: `const { canReview, reason } = await ysdk.feedback.canReview();`
  2. If `canReview === true`, display in-game trigger or invoke:
     ```javascript
     const { value } = await ysdk.feedback.requestReview();
     if (value) {
         console.log('Player submitted review');
     }
     ```
* **Reasons `canReview` may be false**: Player is not logged in, player has already reviewed the game, or platform cooldown is active.
* **Best Practice**: Trigger after a positive milestone (e.g., reaching a new high score or playing 3 complete sessions). Never trigger during active slicing!

#### 2. Add Shortcut to Desktop / Mobile Home Screen (`ShortcutAPI`)
* Prompts the player to add a direct launcher icon to their device desktop or mobile home screen.
* **Method Flow**:
  1. Check capability: `const { canShowPrompt } = await ysdk.shortcut.canShowPrompt();`
  2. If `canShowPrompt === true`:
     ```javascript
     const { outcome } = await ysdk.shortcut.showPrompt();
     if (outcome === 'accepted') {
         // Reward player with bonus coins
         SendMessage('YandexSDKManager', 'OnShortcutAccepted');
     }
     ```

---

## 3. Monetization Policies, Timing & Limits

### 3.1 Interstitial Ads (Full-Screen)
*Official Citation*: [Реклама: 4.4](https://yandex.ru/dev/games/doc/ru/requirements/4/4) | [Полноэкранная реклама](https://yandex.ru/dev/games/doc/ru/sdk/sdk-adv#fullscreen)

#### The Real-Time Arcade Rule (Mandatory Blocker 4.4)
In games with short, high-intensity rounds (such as Slime Ninja, where rounds typically last between 30 seconds and 3 minutes):
* **STRICT PROHIBITION**: Interstitial ads are **strictly forbidden** during active gameplay. Showing an ad while slimes are in the air or while the player is actively swiping is an instant moderation rejection.
* **Permitted Placement Points**:
  1. **Game Over Screen**: When all lives are lost and the final score is presented.
  2. **Restart Button**: Immediately upon clicking restart, before the new round spawns.
  3. **Return to Main Menu**: When transitioning from game over back to the title screen.
  4. **Pause Screen**: Only if explicitly requested when resuming, provided game audio and timers remain frozen.

#### The 5-Minute Session & 2-Second Warning Rule
If an endless session or practice run exceeds 5 continuous minutes without reaching Game Over:
* The game must display a clear visual warning: **"Реклама через: 2... 1..."** (or localized equivalent).
* During the 2-second countdown, gameplay must freeze (`Time.timeScale = 0`) and audio must mute.
* Only then can `ysdk.adv.showFullscreenAdv()` be executed.

#### Platform Cooldowns & Fraud Prevention
* Yandex Games enforces an automatic platform cooldown on interstitials (typically 60 to 120 seconds depending on player profile and geo).
* If `showFullscreenAdv()` is called during a cooldown period, the SDK simply invokes `onClose(false)` without showing an ad.
* **ANTI-FRAUD WARNING**: The game must **never** create a manual `setInterval` or loop calling `showFullscreenAdv()` in the background to detect when the cooldown expires. This pattern is flagged as ad spam / fraudulent activity.

#### Interstitial Implementation Blueprint:
```javascript
function ShowFullscreenAd() {
    // 1. Inform platform gameplay has stopped
    ysdk.features.GameplayAPI?.stop();

    // 2. Mute sound and freeze engine in C#
    SendMessage('YandexSDKManager', 'OnAdStarted');

    ysdk.adv.showFullscreenAdv({
        callbacks: {
            onOpen: () => {
                console.log('Interstitial ad opened');
            },
            onClose: (wasShown) => {
                console.log('Interstitial closed. Was shown:', wasShown);
                // Resume game audio and engine
                SendMessage('YandexSDKManager', 'OnAdClosed');
            },
            onError: (error) => {
                console.error('Interstitial error:', error);
                SendMessage('YandexSDKManager', 'OnAdClosed');
            }
        }
    });
}
```

---

### 3.2 Rewarded Video Ads
*Official Citation*: [Реклама за вознаграждение](https://yandex.ru/dev/games/doc/ru/sdk/sdk-adv#rewarded)

#### Monetization Architecture for Slime Ninja
1. **Revive / Second Chance (Once per Run)**:
   * When player loses their last life or slices a bomb, present a 3-second revive opportunity: "Watch ad to revive!".
   * If rewarded, clear all active slimes/bombs on screen, restore 1 or 3 lives, reset blade combo, and call `GameplayAPI.start()`.
   * Strictly limited to **1 revive per run** to preserve competitive game balance and leaderboard integrity.
2. **2x Score / Coins Multiplier on Game Over**:
   * On the Game Over summary screen, offer a button: "Watch ad to double coins collected!".
   * Delivers an instant numerical boost via `player.incrementStats({ coins: collectedCoins })`.

#### Critical Implementation Rules:
* **Explicit User Intent**: Rewarded ads must only trigger upon direct user action (clicking a button marked with a video icon / reward badge). Never auto-trigger a rewarded ad.
* **Reward Verification**: Reward logic must execute **only** inside the `onRewarded` callback. Never award rewards in `onClose` or `onError`.
* **Zero Cooldown**: Unlike interstitials, Rewarded Videos have no platform cooldown. A player may watch multiple rewarded ads sequentially if they choose to double different rewards.

#### Rewarded Video Blueprint:
```javascript
function ShowRewardedVideo(rewardType) {
    ysdk.features.GameplayAPI?.stop();
    SendMessage('YandexSDKManager', 'OnAdStarted');

    let rewardGiven = false;

    ysdk.adv.showRewardedVideo({
        callbacks: {
            onOpen: () => {
                console.log('Rewarded video opened');
            },
            onRewarded: () => {
                rewardGiven = true;
                console.log('User completed rewarded video for:', rewardType);
                SendMessage('YandexSDKManager', 'OnRewardEarned', rewardType);
            },
            onClose: () => {
                SendMessage('YandexSDKManager', 'OnAdClosed');
                if (!rewardGiven) {
                    SendMessage('YandexSDKManager', 'OnRewardCancelled', rewardType);
                }
            },
            onError: (e) => {
                console.error('Rewarded video error:', e);
                SendMessage('YandexSDKManager', 'OnAdClosed');
                SendMessage('YandexSDKManager', 'OnRewardCancelled', rewardType);
            }
        }
    });
}
```

---

### 3.3 Sticky Banner Ads (RTB)
*Official Citation*: [Баннерная реклама](https://yandex.ru/dev/games/doc/ru/sdk/sdk-adv#banner)

* **Configuration**: Sticky banners are enabled and configured in the Yandex Developer Console.
* **Runtime Control**:
  * `ysdk.adv.showBannerAdv()`: Displays the banner.
  * `ysdk.adv.hideBannerAdv()`: Hides the banner (e.g. during active gameplay if it impedes swiping).
* **Screen Placement & Slime Ninja Swipe Clearance**:
  * On mobile devices, sticky banners appear anchored to the **bottom** or **top** of the screen (typically 50px or 90px height).
  * **Critical UX Hazard for Slicing**: In Slime Ninja, slimes spawn from the bottom of the screen and fly upward. Players swipe aggressively across the bottom third of the screen.
  * **Recommendation**:
    1. Either configure sticky banners to anchor at the **top** of the viewport in Developer Console.
    2. Or implement a **100px bottom safe margin** in the camera viewport and UI canvas so swipe trails do not accidentally tap the banner ad (which would trigger accidental ad clicks and cause high player churn).
    3. Alternatively, hide the sticky banner during active rounds (`hideBannerAdv()` on `GameplayAPI.start()`) and show it only in the Main Menu and Game Over screens (`showBannerAdv()` on `GameplayAPI.stop()`).

---

## 4. Moderation Compliance Matrix

| Category | Requirement Item | Severity | Doc Citation | Verification & Acceptance Criteria |
| :--- | :--- | :--- | :--- | :--- |
| **Audio** | Sound Muting on Background & Tab Switch | **BLOCKER** | [Требования 1.3](https://yandex.ru/dev/games/doc/ru/requirements/1/3) | Sound must mute instantly when minimizing window, switching tabs, or clicking outside iframe. `AudioListener.pause = true`. |
| **Audio** | Sound Muting during Ads | **BLOCKER** | [Требования 4.7](https://yandex.ru/dev/games/doc/ru/requirements/4/7) | All game BGM and SFX must be silent while any fullscreen or rewarded ad is active. |
| **Monetization** | Ad Placement Policy (No Mid-Game Interstitial) | **BLOCKER** | [Требования 4.4](https://yandex.ru/dev/games/doc/ru/requirements/4/4) | No interstitial ads during active gameplay/slashing. Allowed only on GameOver, Restart, Menu. |
| **Monetization** | 2-Second Warning for Long Sessions | **BLOCKER** | [Требования 4.4](https://yandex.ru/dev/games/doc/ru/requirements/4/4) | If round > 5 min, must show visual 2s warning countdown and freeze game before interstitial. |
| **Technical** | Archive Size <= 100 MB | **BLOCKER** | [Требования 1.21](https://yandex.ru/dev/games/doc/ru/requirements/1/21) | Total uncompressed extracted contents of ZIP <= 100 MB. |
| **Technical** | Root `index.html` Structure | **BLOCKER** | [Требования 1.22](https://yandex.ru/dev/games/doc/ru/requirements/1/22) | `index.html` must be in root folder of zip, not in a subfolder. |
| **UI / Layout** | No Browser Scrollbars | **BLOCKER** | [Требования 1.10](https://yandex.ru/dev/games/doc/ru/requirements/1/10) | Game canvas must fit 100% of iframe. No horizontal or vertical scrollbars permitted. |
| **UI / Layout** | Touch & Mobile Responsiveness | **BLOCKER** | [Требования 1.10](https://yandex.ru/dev/games/doc/ru/requirements/1/10) | Touch inputs must be responsive; no mobile gestures (pull-to-refresh) triggered by swiping. |
| **Persistence** | Progress Saved on Refresh | **BLOCKER** | [Требования 1.9](https://yandex.ru/dev/games/doc/ru/requirements/1/9) | High scores, unlocked skins, and currency must persist when reloading the page. |
| **Promotional** | Standard Promotional Media | **BLOCKER** | [Требования 2.1](https://yandex.ru/dev/games/doc/ru/requirements/2/1) | Icon 512x512 PNG, Cover 800x470 PNG, Screenshots min 2 (1280-2560px, gameplay >= 70%). |
| **Platform** | `LoadingAPI.ready()` Call | **FEATURING** | [События в игре](https://yandex.ru/dev/games/doc/ru/sdk/sdk-game-events) | Must be called when menu is ready. Prerequisite for featuring and catalog boost. |
| **Platform** | `GameplayAPI.start() / stop()` | **FEATURING** | [События в игре](https://yandex.ru/dev/games/doc/ru/sdk/sdk-game-events) | Required for platform session analytics and smart ad frequency calculation. |
| **Promotional** | Showcase Hero Cover (Витрина) | **FEATURING** | [Обложка для витрины](https://yandex.ru/dev/games/doc/ru/console/add-new-game/draft) | 1560x520 PNG/JPG. Mandatory for main page banner featuring. |
| **Promotional** | Gameplay Video Trailers | **FEATURING** | [Видеоролики](https://yandex.ru/dev/games/doc/ru/console/add-new-game/draft) | 16:9 horizontal + 9:16 vertical MP4 (<=28 sec, <=100 MB). Boosts conversion by 30-50%. |
| **Retention** | Cloud Saves (`setData` / `setStats`) | **FEATURING** | [Внутриигровые данные](https://yandex.ru/dev/games/doc/ru/sdk/sdk-player#data) | Synchronize player data across devices for authorized users. |
| **Retention** | In-Game Review (`requestReview`) | **RECOMMENDED** | [Оценка игры](https://yandex.ru/dev/games/doc/ru/sdk/sdk-review) | Prompts positive players for 5-star ratings; boosts organic search ranking. |
| **Retention** | Shortcut Prompt (`showPrompt`) | **RECOMMENDED** | [Иконка на рабочий стол](https://yandex.ru/dev/games/doc/ru/sdk/sdk-shortcut) | Encourages desktop / mobile home screen install, increasing D1/D7 retention. |
| **Performance** | WebGL 2.0 & Brotli Compression | **RECOMMENDED** | [Оптимизация сборки](https://yandex.ru/dev/games/doc/ru/concepts/optimization) | Reduces initial download payload from ~25MB to ~7MB, accelerating first load. |
| **Performance** | WebAssembly Memory <= 512 MB | **RECOMMENDED** | Unity WebGL Spec | Prevents Out-Of-Memory crashes on low-end Android mobile browsers. |

---

## 5. Slime Ninja Codebase Gap Analysis & Architecture

### 5.1 Existing Codebase Audit (`/home/moonsprocket/Slime-Nindja`)

Inspection of current C# scripts reveals the following technical gaps:

1. **Persistence Mechanism (`GameManager.cs`)**:
   * *Current*: Uses standard `PlayerPrefs.GetInt("Score", 0)`.
   * *Gap*: `PlayerPrefs` in Unity WebGL writes to browser `IndexedDB`. On iOS Safari, Apple ITP wipes `IndexedDB` after 7 days of inactivity. Furthermore, `PlayerPrefs` does not sync across devices or support Yandex public leaderboards.
   * *Required Solution*: Build a `SaveManager` that wraps `PlayerPrefs` for offline/fallback caching and mirrors data to Yandex `player.setData()` and `player.setStats()` when authorized.
2. **Audio & Lifecycle Handling**:
   * *Current*: No global pause hook. The game relies on `Time.timeScale = 0` when entering the internal pause menu, but does not monitor browser window blur or iframe messages.
   * *Gap*: When a player minimizes the browser or an ad plays, background audio continues playing. This is an immediate **Moderation Blocker (1.3 & 4.7)**.
   * *Required Solution*: Implement a centralized `AudioManager` and `YandexSDKManager` that enforces `AudioListener.pause = true` whenever platform pause or ad playback occurs.
3. **State Resiliency on Resume**:
   * *Current*: If `Time.timeScale` is restored unconditionally on ad completion, a game that was **manually paused by the player** prior to the ad would accidentally unpause and drop slimes immediately!
   * *Required Solution*: Maintain two distinct flags: `isManuallyPaused` (player UI) and `isPlatformPaused` (ad / window blur). `Time.timeScale` must only restore to `1` if *neither* flag is active.

---

### 5.2 Proposed Unity-to-JavaScript Bridge Architecture

To bridge Unity C# with Yandex Games SDK, we specify two files:
1. `Assets/Plugins/WebGL/YandexSDK.jslib` (C-linkage JavaScript bridge for Unity WebGL).
2. `Assets/Scripts/Managers/YandexSDKManager.cs` (C# Singleton managing SDK state, audio hooks, and events).

#### 1. JavaScript Library Bridge (`YandexSDK.jslib`)
```javascript
mergeInto(LibraryManager.library, {
    YandexInitSDK: function () {
        if (typeof YaGames === 'undefined') {
            console.warn('YaGames SDK script not found!');
            return;
        }
        YaGames.init().then(function (_ysdk) {
            window.ysdk = _ysdk;
            console.log('Yandex SDK initialized in jslib');

            // Hook platform lifecycle pause/resume
            window.ysdk.on('game_api_pause', function () {
                SendMessage('YandexSDKManager', 'OnPlatformPause');
            });
            window.ysdk.on('game_api_resume', function () {
                SendMessage('YandexSDKManager', 'OnPlatformResume');
            });

            // Initialize player
            return window.ysdk.getPlayer();
        }).then(function (_player) {
            window.player = _player;
            var isAuth = window.player.isAuthorized() ? 1 : 0;
            var playerName = window.player.getName() || "Guest";
            SendMessage('YandexSDKManager', 'OnPlayerInitialized', JSON.stringify({
                isAuthorized: isAuth,
                name: playerName
            }));
        }).catch(function (err) {
            console.error('Yandex SDK Init Error:', err);
        });
    },

    YandexLoadingReady: function () {
        if (window.ysdk && window.ysdk.features && window.ysdk.features.LoadingAPI) {
            window.ysdk.features.LoadingAPI.ready();
            console.log('Yandex LoadingAPI.ready() called');
        }
    },

    YandexGameplayStart: function () {
        if (window.ysdk && window.ysdk.features && window.ysdk.features.GameplayAPI) {
            window.ysdk.features.GameplayAPI.start();
        }
    },

    YandexGameplayStop: function () {
        if (window.ysdk && window.ysdk.features && window.ysdk.features.GameplayAPI) {
            window.ysdk.features.GameplayAPI.stop();
        }
    },

    YandexShowInterstitial: function () {
        if (!window.ysdk) return;
        window.ysdk.features.GameplayAPI && window.ysdk.features.GameplayAPI.stop();
        SendMessage('YandexSDKManager', 'OnAdStarted');

        window.ysdk.adv.showFullscreenAdv({
            callbacks: {
                onClose: function (wasShown) {
                    SendMessage('YandexSDKManager', 'OnAdClosed');
                },
                onError: function (err) {
                    console.error('Interstitial error:', err);
                    SendMessage('YandexSDKManager', 'OnAdClosed');
                }
            }
        });
    },

    YandexShowRewarded: function (rewardTypePtr) {
        if (!window.ysdk) return;
        var rewardType = UTF8ToString(rewardTypePtr);
        window.ysdk.features.GameplayAPI && window.ysdk.features.GameplayAPI.stop();
        SendMessage('YandexSDKManager', 'OnAdStarted');

        var rewarded = false;
        window.ysdk.adv.showRewardedVideo({
            callbacks: {
                onRewarded: function () {
                    rewarded = true;
                    SendMessage('YandexSDKManager', 'OnRewardEarned', rewardType);
                },
                onClose: function () {
                    SendMessage('YandexSDKManager', 'OnAdClosed');
                    if (!rewarded) {
                        SendMessage('YandexSDKManager', 'OnRewardCancelled', rewardType);
                    }
                },
                onError: function (err) {
                    console.error('Rewarded error:', err);
                    SendMessage('YandexSDKManager', 'OnAdClosed');
                    SendMessage('YandexSDKManager', 'OnRewardCancelled', rewardType);
                }
            }
        });
    },

    YandexSetLeaderboardScore: function (lbNamePtr, score, extraDataPtr) {
        if (!window.ysdk || !window.player || !window.player.isAuthorized()) return;
        var lbName = UTF8ToString(lbNamePtr);
        var extra = UTF8ToString(extraDataPtr);

        window.ysdk.leaderboards.setScore(lbName, score, extra).then(function () {
            console.log('Leaderboard score submitted:', score);
        }).catch(function (err) {
            console.error('Leaderboard submit error:', err);
        });
    },

    YandexSaveCloudData: function (jsonPtr, flush) {
        if (!window.player) return;
        var jsonStr = UTF8ToString(jsonPtr);
        try {
            var dataObj = JSON.parse(jsonStr);
            window.player.setData(dataObj, flush === 1).then(function () {
                console.log('Cloud data saved successfully');
            });
        } catch (e) {
            console.error('SaveCloudData JSON parse error:', e);
        }
    }
});
```

#### 2. C# Singleton Controller (`YandexSDKManager.cs`)
```csharp
using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class YandexSDKManager : MonoBehaviour
{
    public static YandexSDKManager Instance { get; private set; }

    [DllImport("__Internal")] private static extern void YandexInitSDK();
    [DllImport("__Internal")] private static extern void YandexLoadingReady();
    [DllImport("__Internal")] private static extern void YandexGameplayStart();
    [DllImport("__Internal")] private static extern void YandexGameplayStop();
    [DllImport("__Internal")] private static extern void YandexShowInterstitial();
    [DllImport("__Internal")] private static extern void YandexShowRewarded(string rewardType);
    [DllImport("__Internal")] private static extern void YandexSetLeaderboardScore(string lbName, int score, string extra);
    [DllImport("__Internal")] private static extern void YandexSaveCloudData(string jsonData, int flush);

    public bool IsAuthorized { get; private set; }
    public string PlayerName { get; private set; } = "Guest";

    private bool _isPlatformPaused = false;
    private bool _isManualPaused = false;
    private bool _isAdPlaying = false;

    public event Action<string> OnRewardSuccess;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_WEBGL && !UNITY_EDITOR
        YandexInitSDK();
#endif
    }

    private void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        YandexLoadingReady();
#endif
    }

    // --- Lifecycle Management ---

    public void OnPlatformPause()
    {
        _isPlatformPaused = true;
        UpdateGameState();
    }

    public void OnPlatformResume()
    {
        _isPlatformPaused = false;
        UpdateGameState();
    }

    public void OnWindowBlur()
    {
        _isPlatformPaused = true;
        UpdateGameState();
    }

    public void OnWindowFocus()
    {
        _isPlatformPaused = false;
        UpdateGameState();
    }

    public void SetManualPause(bool isPaused)
    {
        _isManualPaused = isPaused;
        UpdateGameState();
    }

    public void OnAdStarted()
    {
        _isAdPlaying = true;
        UpdateGameState();
    }

    public void OnAdClosed()
    {
        _isAdPlaying = false;
        UpdateGameState();
    }

    private void UpdateGameState()
    {
        bool shouldMute = _isPlatformPaused || _isAdPlaying;
        AudioListener.pause = shouldMute;

        bool shouldFreeze = _isPlatformPaused || _isManualPaused || _isAdPlaying;
        Time.timeScale = shouldFreeze ? 0f : 1f;
    }

    // --- Gameplay Analytics ---

    public void NotifyGameplayStart()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        YandexGameplayStart();
#endif
    }

    public void NotifyGameplayStop()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        YandexGameplayStop();
#endif
    }

    // --- Monetization ---

    public void ShowInterstitialAd()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        YandexShowInterstitial();
#else
        Debug.Log("[Editor] Simulating Interstitial Ad");
#endif
    }

    public void ShowRewardedAd(string rewardType)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        YandexShowRewarded(rewardType);
#else
        Debug.Log($"[Editor] Simulating Rewarded Ad for: {rewardType}");
        OnRewardEarned(rewardType);
#endif
    }

    public void OnRewardEarned(string rewardType)
    {
        OnRewardSuccess?.Invoke(rewardType);
    }

    public void OnRewardCancelled(string rewardType)
    {
        Debug.Log($"Reward cancelled for: {rewardType}");
    }

    // --- Cloud Saves & Leaderboards ---

    public void SubmitScore(int score)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (IsAuthorized)
        {
            YandexSetLeaderboardScore("SlimeNinja_Leaderboard", score, "");
        }
#endif
    }
}
```

---

## 6. Verification & Test Plan

To independently verify moderation compliance prior to release, execute the following protocol:

### Test 1: Audio Muting Verification (Blocker 1.3 & 4.7)
1. Run game in Google Chrome or Yandex Browser.
2. Start an active game round with background music and slicing sound effects playing.
3. Switch to another browser tab. **Expected Result**: Audio cuts off immediately with 0 ms leak.
4. Minimize browser window. **Expected Result**: Audio cuts off immediately.
5. Trigger an interstitial ad or rewarded video. **Expected Result**: All Unity audio mutes completely while the ad is playing.

### Test 2: Ad Placement Compliance (Blocker 4.4)
1. Play a standard arcade round. Verify that **no** interstitial ad is triggered while slimes are being sliced or during combos.
2. Die and reach Game Over. Verify that an interstitial ad is triggered cleanly, with `GameplayAPI.stop()` called first.
3. Verify that the Revive button only awards the extra life when the video is watched to completion (`onRewarded`).

### Test 3: Uncompressed Archive Size (Blocker 1.21)
1. Build the Unity WebGL project using Brotli compression.
2. Package all files directly into a `.zip` archive.
3. Execute verification command in shell:
   ```bash
   unzip -l SlimeNinja_Release.zip | tail -n 1
   ```
4. Confirm total uncompressed size is strictly `< 100,000,000 bytes` (100 MB). Verify that `index.html` is in the root directory.

### Test 4: Persistence Verification (Blocker 1.9)
1. Slice slimes, set a high score, collect 500 coins.
2. Reload browser tab (`Ctrl + F5`).
3. Verify that high score and currency are restored.
4. Log into Yandex passport, verify data synchronizes to `player.setData()` / `player.setStats()`.

---
*Report compiled and certified for Slime Ninja production pipeline.*
