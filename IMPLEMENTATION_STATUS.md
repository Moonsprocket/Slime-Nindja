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
| **GAME-04** | Отдельная вкладка Магазина клинков (12 скинов, карточки) | GAME-03 | **DONE** | `32bf344` | Worker-UI/Game | PASS |
| **GAME-05** | Рекалибровка формулы Эссенции и каталога цен | GAME-03 | **DONE** | `32bf344` | Worker-Game | PASS |
| **GAME-06** | Бейдж количества нарезанных слаймов в блоке Ранга | GAME-03 | **DONE** | `32bf344` | Worker-UI | PASS |
| **GAME-07** | Расширение маскотов (5 образов), серия x98+ (7 вех, 8 уровней) | GAME-01 | **DONE** | `5afc1a5` | Worker-Game/Art | PASS |
| **GAME-08** | Калибровка тач/мышь ввода и тюнинг Slime Rush | CORE-03, GAME-02 | **DONE** | `5afc1a5` | Worker-Game | PASS |
| **GAME-09** | 12 тематических косметических VFX эффектов клинков | GAME-04 | **DONE** | `5afc1a5` | Worker-Art | PASS |
| **AUDIO-01** | Редизайн звукового окружения меню (без бурления, G-dur чил) | CORE-01 | **DONE** | `5afc1a5` | Worker-Audio | PASS |

---

## Текущий фокус выполнения
1. **Фаза 0**: Базовая стабильность и WebGL-блокеры (`CORE-01` .. `CORE-04`) — **DONE**.
2. **Фаза 1**: Интеграция Yandex Games SDK v2 (`YSDK-01` .. `YSDK-03`) — **DONE**.
3. **Фаза 2**: Оптимизации производительности и Zero-GC (`PERF-01` .. `PERF-03`) — **DONE**.
4. **Фаза 3**: Геймплей, комбо, события, монетизация (`GAME-01` .. `GAME-03`, `ADS-01`, `ADS-02`) — **DONE**.
5. **Фаза 4 (Продуктовый редизайн по Research Report)**:
   - Этап 1: Магазин клинков, формула Эссенции, бейдж нарезки — **DONE** (`32bf344`).
   - Этап 2: Балансировка и динамика (Slime Rush, тач-калибровка, нелинейные комбо x3-x6) — **DONE** (`5afc1a5`).
   - Этап 3: Маскоты, расширенные серии до x500 (8 тиров), 12 уникальных VFX клинков, чистое G-dur меню-аудио — **DONE**.
6. **Финализация**: Автономные тесты batchmode пройдены, релизный WebGL билд собран, форк синхронизирован.
