# Challenger Report: Empirical Verification of URLs and Platform Claims

- **Target Document**: `/home/moonsprocket/Slime-Nindja-Research/RESEARCH_REPORT.md`
- **Agent**: Challenger 1 (Empirical Challenger: Critic, Specialist)
- **Date**: 2026-10-15
- **Verdict**: **REQUEST_CHANGES** (5 broken HTTP 404 links require URL correction; 20 URLs and all competitor metadata empirically validated)

---

## 1. Executive Summary

An exhaustive empirical verification of all 65 URL occurrences (25 unique URLs), competitor game metadata, developer names, ratings, and platform documentation claims cited in `RESEARCH_REPORT.md` was conducted via automated HTTP request harnesses, DOM/JSON-LD metadata extraction, and comparison against official Yandex Games and Unity documentation.

### Key Verification Metrics
| Category | Total Checked | Verified Valid (200 OK / Match) | Failed / Broken (404) | Status |
|---|---|---|---|---|
| **Yandex Games Competitor Cards** | 8 | 8 (100%) | 0 | **PASS** |
| **Competitor Developer Names & IDs** | 8 | 8 (100%) | 0 | **PASS** |
| **Competitor Ratings & Review Counts** | 8 | 8 (100% exact match) | 0 | **PASS** |
| **Yandex SDK Documentation Links** | 7 | 7 (100%) | 0 | **PASS** |
| **Yandex Requirements Links** | 7 | 3 (42.9%) | 4 (57.1% 404) | **DEFECT** |
| **Unity Technical Documentation Links** | 3 | 2 (66.7%) | 1 (33.3% 404) | **DEFECT** |
| **Platform Quotas & Ad Specifications** | 100% | 100% verified verbatim | 0 | **PASS** |

---

## 2. Empirical Verification of Competitor Games (Yandex Games)

All 8 games referenced in Section 3 and Section 9 were queried directly against the live Yandex Games platform via HTTP GET. Each game resolved to an active, playable web game on Yandex Games. Metadata was parsed from Schema.org JSON-LD and page hydration scripts.

| App ID | Report Game Title | Live Schema.org Game Title | Live Developer Name & ID | Report Rating & Reviews | Live Rating & Review Count | HTTP Status & Canonical URL |
|---|---|---|---|---|---|---|
| `278766` | Fruit Ninja | Fruit Ninja | Y2G Pte. Ltd (`75425`) | 4.4 ★ (1 236) | 4.4 ★ (1 236) | **200 OK** (`/games/app/fruit-ninja-278766`) |
| `193579` | Mr. Slice | Mr. Slice | DUCKY LTD (`37746`) | 4.4 ★ (17 054) | 4.4 ★ (17 054) | **200 OK** (`/games/app/mr-slice-193579`) |
| `377691` | Мастер меча: Разруби врагов! | Мастер меча: Разруби врагов! | Eccentric Studio (`131139`) | 4.6 ★ (18 931) | 4.6 ★ (18 931) | **200 OK** (`/games/app/master-mecha-razrubi-vragov-377691`) |
| `232881` | Взмах Мечом: Руби врагов на части! | Взмах Мечом: Руби врагов на части! | Eccentric Studio (`131139`) | 4.4 ★ (17 317) | 4.4 ★ (17 317) | **200 OK** (`/games/app/vzmakh-mechom-rubi-vragov-na-chasti-232881`) |
| `171204` | Порежь фрукты 2 | Порежь фрукты 2 | truelisgames (`390`) | 3.7 ★ (271) | 3.7 ★ (271) | **200 OK** (`/games/app/porezh-frukty-2-171204`) |
| `201627` | Порыв ниндзя | Порыв ниндзя | Ohayo (`38959`) | N/A (Sec 9) | 4.3 ★ (10 119) | **200 OK** (`/games/app/poryv-nindzia-201627`) |
| `508563` | Ультра Разрезатель | Ультра Разрезатель | kozlovGames (`138591`) | N/A (Sec 9) | 3.9 ★ (1 001) | **200 OK** (`/games/app/ultra-razrezatel-508563`) |
| `101502` | Knife Hit | Knife Hit | Onduck Games (`299`) | N/A (Sec 9) | 3.6 ★ (452) | **200 OK** (`/games/app/knife-hit-101502`) |

**Finding**: There are zero fabricated competitors, zero invalid IDs, and zero fabricated review metrics. The review counts and ratings in `RESEARCH_REPORT.md` match live platform data with 100% precision.

---

## 3. Empirical Verification of Platform Documentation & SDK Claims

### 3.1. SDK Documentation URLs
The following official Yandex SDK documentation links were tested and returned **HTTP 200 OK**:
1. `https://yandex.ru/dev/games/doc/ru/sdk/sdk-about` (Init SDK v2) — **200 OK**
2. `https://yandex.ru/dev/games/doc/ru/sdk/sdk-game-events` (LoadingAPI & GameplayAPI) — **200 OK**
3. `https://yandex.ru/dev/games/doc/ru/sdk/sdk-player#data` (Player Data & Stats) — **200 OK**
4. `https://yandex.ru/dev/games/doc/ru/sdk/sdk-leaderboard` (Leaderboards API) — **200 OK**
5. `https://yandex.ru/dev/games/doc/ru/sdk/sdk-adv` (Fullscreen & Rewarded Ads) — **200 OK**
6. `https://yandex.ru/dev/games/doc/ru/sdk/sdk-review` (Feedback / Review API) — **200 OK**
7. `https://yandex.ru/dev/games/doc/ru/sdk/sdk-shortcut` (Shortcut API) — **200 OK**

### 3.2. Platform Specifications & Quota Claims Verification
Cross-referencing the report claims against official documentation pages revealed complete accuracy of numbers and constraints:
- **`player.setData()` Limits**: Report claims max 200 KB per player, max 100 calls per 5 minutes. Official doc (`sdk-player`): *"player.setData() Сохраняет данные пользователя. Максимальный размер данных на игрока — 200 КБ. Запрос можно отправлять не чаще, чем 100 раз за 5 минут"*. **VERIFIED**.
- **`player.setStats()` Limits**: Report claims max 10 KB per player, max 60 calls per 1 minute. Official doc (`sdk-player`): *"player.setStats() Сохраняет численные данные пользователя. Лимит 10 КБ. 60 запросов за 1 минуту"*. **VERIFIED**.
- **`ysdk.leaderboards.setScore()` Limits**: Report claims 1 call per second. Official doc (`sdk-leaderboard`): *"1 запрос за 1 секунду"*. **VERIFIED**.
- **`ysdk.leaderboards.getEntries()` Limits**: Report claims 20 calls per 5 minutes. Official doc (`sdk-leaderboard`): *"20 запросов за 5 минут"*. **VERIFIED**.
- **iOS LocalStorage Wipe Prevention (`safeStorage`)**: Report specifies `ysdk.getStorage()` for iOS Safari. Official doc (`sdk-player`): *"safeStorage, у которого такой же интерфейс, как у localStorage: const safeStorage = await ysdk.getStorage()"*. **VERIFIED**.
- **Ad Frequency & Background Timer Prohibition**: Report notes that ads are platform-throttled (60–90s) and warns against background interval timers. Official doc (`sdk-adv`): *"Частота вызова полноэкранного блока рекламы управляется платформой... Пример неправильного вызова: setInterval(() => ysdk.adv.showFullscreenAdv(), 180000); onClose передает wasShown: false при слишком частом вызове"*. **VERIFIED**.
- **Ad Warning & 5-Minute Rule**: Report specifies 2-second countdown warning before mid-game ads and pausing time/sound. Official doc (`requirements/4/4`): *"Предупреждения о рекламе должны длиться 2 секунды. Во время предупреждения о рекламе и самой рекламы игра должна быть на паузе. Для игр в реальном времени с уровнями длиннее 5 минут реклама по таймеру с паузой и предупреждением"*. **VERIFIED**.

---

## 4. Challenges & Defects Identified (Broken 404 Links)

During automated HTTP verification, 5 URLs returned **HTTP 404 Not Found**. While the underlying regulatory and technical content is genuine, the specific URLs in the report point to outdated or non-existent paths.

### Defect 1: Dead Link `yandex.ru/dev/games/doc/ru/requirements/`
- **Report Occurrence**: Lines 711, 1207.
- **HTTP Result**: `HTTP Error 404: Not Found`.
- **Root Cause**: Yandex Games restructured its developer documentation. The root index for game requirements is `/concepts/requirements`, not `/requirements/`.
- **Working Verified Replacement**: `https://yandex.ru/dev/games/doc/ru/concepts/requirements` (Status: 200 OK).

### Defect 2: Dead Link `yandex.ru/dev/games/doc/ru/requirements/1/21`
- **Report Occurrence**: Lines 711, 1210.
- **HTTP Result**: `HTTP Error 404: Not Found`.
- **Root Cause**: Requirement 1.21 (*"Размер всех файлов игры не превышает 100 МБ в разархивированном виде"*) is located on the consolidated requirements page under anchor `#1-21`, rather than a separate child page.
- **Working Verified Replacement**: `https://yandex.ru/dev/games/doc/ru/concepts/requirements#1-21` (Status: 200 OK, anchor confirmed).

### Defect 3: Dead Link `yandex.ru/dev/games/doc/ru/requirements/1/22`
- **Report Occurrence**: Lines 712, 1211.
- **HTTP Result**: `HTTP Error 404: Not Found`.
- **Root Cause**: Requirement 1.22 (*"В корень архива добавлен файл index.html. В названиях файлов и папок не используются пробелы и русские символы"*) is located under anchor `#1-22`.
- **Working Verified Replacement**: `https://yandex.ru/dev/games/doc/ru/concepts/requirements#1-22` (Status: 200 OK, anchor confirmed).

### Defect 4: Dead Link `yandex.ru/dev/games/doc/ru/requirements/4/7`
- **Report Occurrence**: Lines 730, 1213.
- **HTTP Result**: `HTTP Error 404: Not Found`.
- **Root Cause**: Requirement 4.7 (*"При показе полноэкранной рекламы (interstitial или rewarded video) звук в игре и игровой процесс должны ставиться на паузу"*) is located under anchor `#4-7`.
- **Working Verified Replacement**: `https://yandex.ru/dev/games/doc/ru/concepts/requirements#4-7` (Status: 200 OK, anchor confirmed).

### Defect 5: Dead Link `docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/2D/renderer-2d.html`
- **Report Occurrence**: Line 1253.
- **HTTP Result**: `HTTP Error 404: Not Found`.
- **Root Cause**: Unity 6 URP package documentation restructured 2D renderer paths in the 17.x documentation tree.
- **Working Verified Replacement**: `https://docs.unity3d.com/6000.0/Documentation/Manual/urp/2d-index.html` (or `https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/2d-index.html` which redirects to it with Status 200 OK).

---

## 5. Required Modifications Checklist for Writer Agent

To achieve complete verification compliance, the writer agent must make the following line replacements in `/home/moonsprocket/Slime-Nindja-Research/RESEARCH_REPORT.md`:

1. **Line 711**:
   - *Current*: `([Требования: 1.21](https://yandex.ru/dev/games/doc/ru/requirements/1/21))`
   - *Replace with*: `([Требования: 1.21](https://yandex.ru/dev/games/doc/ru/concepts/requirements#1-21))`
2. **Line 712**:
   - *Current*: `([Требования: 1.22](https://yandex.ru/dev/games/doc/ru/requirements/1/22))`
   - *Replace with*: `([Требования: 1.22](https://yandex.ru/dev/games/doc/ru/concepts/requirements#1-22))`
3. **Line 730**:
   - *Current*: `[Пункт требований 4.7](https://yandex.ru/dev/games/doc/ru/requirements/4/7)`
   - *Replace with*: `[Пункт требований 4.7](https://yandex.ru/dev/games/doc/ru/concepts/requirements#4-7)`
4. **Line 1207**:
   - *Current*: `[https://yandex.ru/dev/games/doc/ru/requirements/](https://yandex.ru/dev/games/doc/ru/requirements/)`
   - *Replace with*: `[https://yandex.ru/dev/games/doc/ru/concepts/requirements](https://yandex.ru/dev/games/doc/ru/concepts/requirements)`
5. **Line 1210**:
   - *Current*: `[https://yandex.ru/dev/games/doc/ru/requirements/1/21](https://yandex.ru/dev/games/doc/ru/requirements/1/21)`
   - *Replace with*: `[https://yandex.ru/dev/games/doc/ru/concepts/requirements#1-21](https://yandex.ru/dev/games/doc/ru/concepts/requirements#1-21)`
6. **Line 1211**:
   - *Current*: `[https://yandex.ru/dev/games/doc/ru/requirements/1/22](https://yandex.ru/dev/games/doc/ru/requirements/1/22)`
   - *Replace with*: `[https://yandex.ru/dev/games/doc/ru/concepts/requirements#1-22](https://yandex.ru/dev/games/doc/ru/concepts/requirements#1-22)`
7. **Line 1213**:
   - *Current*: `[https://yandex.ru/dev/games/doc/ru/requirements/4/7](https://yandex.ru/dev/games/doc/ru/requirements/4/7)`
   - *Replace with*: `[https://yandex.ru/dev/games/doc/ru/concepts/requirements#4-7](https://yandex.ru/dev/games/doc/ru/concepts/requirements#4-7)`
8. **Line 1253**:
   - *Current*: `[https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/2D/renderer-2d.html](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/2D/renderer-2d.html)`
   - *Replace with*: `[https://docs.unity3d.com/6000.0/Documentation/Manual/urp/2d-index.html](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/2d-index.html)`

---

## 6. Final Adversarial Assessment

- **Authenticity of Competitor Research**: **100% Robust**. The competitor analysis is grounded in real, verifiable games with genuine audience metrics and actual user reviews from Yandex Games.
- **Accuracy of Platform Constraints**: **100% Robust**. Every API name, parameter, limitation, quota, and ad rule corresponds exactly to Yandex Games Developer Documentation.
- **Hyperlink Integrity**: **80% (20/25 valid, 5 broken 404)**. Blocked strictly due to the 5 dead links identified above.
- **Verdict**: **REQUEST_CHANGES**. Once the 5 URLs are updated to their active counterparts, the report will be 100% verified.
