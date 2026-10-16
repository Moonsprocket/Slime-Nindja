# Slime Ninja — Статус реализации бэклога (Implementation Status)

**Ветка разработки**: `ai/slime-ninja-development`  
**Базовый коммит**: `bffa617` (CORE-01)  
**Дата актуализации**: 16 октября 2026 г.  

---

## Матрица задач и зависимостей

| ID | Название | Зависимости | Статус | Коммит | Исполнитель | Ревью / Аудит |
|---|---|---|:---:|:---:|:---:|:---:|
| **CORE-01** | Устранение дедлока в `Sfx.cs` | — | **DONE** | `bffa617` | Worker-Core | PASS WITH LIMITATIONS |
| **CORE-02** | Пауза звука и потеря фокуса окна | CORE-01 | **DONE** | `f8ebb62` | Worker-Core | PASS |
| **CORE-03** | Мобильная кнопка паузы и тач-UI | CORE-02 | **DONE** | `79b9025` | Worker-UI | PASS |
| **CORE-04** | Настройки WebGL-сборки и сжатия | CORE-01 | **DONE** | `9d2373c` | Worker-Build | PASS |
| **PERF-03** | Стриппинг шейдеров URP и сжатие аудио | CORE-04 | **DONE** | `feat(meta-ads)` | Worker-Perf | PASS |
| **PERF-01** | ObjectPool для слаймов (`Sliceable`) | CORE-01 | **DONE** | `087c02b` | Worker-Perf | PASS |
| **PERF-02** | ObjectPool для частиц и всплывающего текста | PERF-01 | **DONE** | `45a701d` | Worker-Perf | PASS |
| **YSDK-01** | Мост C#/JS (LoadingAPI & GameplayAPI) | CORE-02, CORE-04 | **DONE** | `feat(ysdk)` | Worker-YSDK | PASS |
| **YSDK-02** | Двухуровневые облачные сохранения | YSDK-01 | **DONE** | `feat(ysdk)` | Worker-YSDK | PASS |
| **YSDK-03** | Лидерборд рекордов (Leaderboards API) | YSDK-02 | **DONE** | `feat(ysdk)` | Worker-YSDK | PASS |
| **GAME-01** | Идеальный срез (Bullseye Precision Cut) | CORE-01 | **DONE** | `f882ffb` | Worker-Game | PASS |
| **GAME-02** | Динамические микро-события волн | GAME-01 | **DONE** | `f5997af` | Worker-Game | PASS |
| **GAME-03** | Мета-валюта Эссенция и Кузница клинков | YSDK-02 | **DONE** | `feat(meta-ads)` | Worker-Game | PASS |
| **ADS-01** | Межстраничная реклама (Interstitial) | YSDK-01 | **DONE** | `feat(meta-ads)` | Worker-Monetization | PASS |
| **ADS-02** | Вознаграждаемая реклама (Rewarded Video) | YSDK-01, GAME-03 | **DONE** | `feat(meta-ads)` | Worker-Monetization | PASS |

---

## Текущий фокус выполнения
1. **Фаза 0**: `CORE-02` -> `CORE-03` -> `CORE-04`.
2. **Фаза 2 (часть 1)**: `PERF-03` (сжатие и стриппинг под WebGL), `PERF-01` и `PERF-02` (ObjectPool).
3. **Фаза 1**: `YSDK-01` -> `YSDK-02` -> `YSDK-03`.
4. **Фаза 3**: `GAME-01` -> `GAME-02` -> `GAME-03`.
5. **Фаза 4**: `ADS-01` -> `ADS-02`.
6. **Финализация**: Сквозное тестирование, финальный аудит и сборка релизного кандидата.
