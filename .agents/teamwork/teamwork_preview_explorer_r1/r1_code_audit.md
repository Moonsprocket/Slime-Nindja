# R1 Technical Codebase Audit Report: Slime Ninja (Alchemia)

**Дата проведения**: 2026-10-15  
**Аудитор**: Codebase Technical Auditor (R1 Explorer)  
**Репозиторий**: `/home/moonsprocket/Slime-Nindja` (Strictly Read-Only)  
**Версия Unity**: Unity 6.6 (`6000.6.5f1`), URP 17.6.0 (`com.unity.render-pipelines.universal`), Input System 1.20.0 (`com.unity.inputsystem`)  
**Целевая платформа трансформации**: Яндекс Игры (WebGL, Desktop + Mobile)

---

## 1. Введение и Архитектурный Обзор

Проект **Slime Ninja** (внутреннее название в коде — `Alchemia`) представляет собой аркадный 2D-слэшер механики Fruit Ninja в стилистике алхимической лаборатории.

### Ключевая особенность архитектуры
В сцене `Assets/Scenes/SampleScene.unity` находится минимальный набор объектов:
1. `Main Camera` (`Assets/Scenes/SampleScene.unity:123-260`) с `Camera`, `AudioListener`, `CinemachineBrain` и `UniversalAdditionalCameraData`.
2. `Global Light 2D` (`Assets/Scenes/SampleScene.unity:293-400`).
3. `CinemachineCamera` (`Assets/Scenes/SampleScene.unity:468-574`).
4. `GameManager` (`Assets/Scenes/SampleScene.unity:401-467`) с компонентами `GameManager` и `Spawner`.

Все остальные системы игры — клинок (`Blade`), интерфейс (`Hud`), полосы комбо и серий (`ComboBar`, `StreakBar`), окружение лаборатории (`Backdrop`), визуальные эффекты (`Fx`), звуковая подсистема (`Sfx`) и постобработка (`Volume`) — создаются и конфигурируются **динамически из C# кода** в `GameManager.Awake()` (`Assets/Scripts/GameManager.cs:67-99`).

---

## 2. Игровой Цикл, Механика Нарезки, Физика и Ввод

### 2.1. Игровой цикл и порядок выполнения (Execution Order)
Порядок обновления кадров строго детерминирован атрибутами `DefaultExecutionOrder`:
1. `GameManager` (`Assets/Scripts/GameManager.cs:11`): `[DefaultExecutionOrder(-100)]` — инициализирует глобальные параметры, координаты видимости `View`, масштаб `Unit`, таймскейл и подсистемы до любых игровых сущностей.
2. `Sliceable` (`Assets/Scripts/Sliceable.cs:6`): порядок выполнения по умолчанию (`0`) — интегрирует скорость и перемещает слаймы/бомбы в кадре `Update()`.
3. `Blade` (`Assets/Scripts/Blade.cs:8`): `[DefaultExecutionOrder(50)]` — запускается **после** того, как все слаймы переместились на текущий шаг кадра, что гарантирует точную проверку пересечений траекторий.

### 2.2. Обработка ввода мыши и тача (New Input System)
Обработка ввода реализована в классе `Blade` (`Assets/Scripts/Blade.cs`):
- **Система ввода**: используется Unity New Input System (`UnityEngine.InputSystem`, `Assets/Scripts/Blade.cs:3`).
- **Сбор суб-кадровых событий**: в `Blade.OnEnable()` (`Assets/Scripts/Blade.cs:57-65`):
  ```csharp
  InputSystem.settings.disableRedundantEventsMerging = true;
  InputSystem.onEvent += OnInputEvent;
  ```
  Отключение склейки избыточных событий позволяет перехватывать все промежуточные аппаратные позиции курсора между кадрами (до `MaxSamples = 64`, `Assets/Scripts/Blade.cs:16, 78`). В `Blade.OnInputEvent()` (`Assets/Scripts/Blade.cs:75-82`) события `StateEvent` и `DeltaStateEvent` мыши и пера сохраняются в список `samples`.
- **Чтение указателя**: `Blade.ReadPointer()` (`Assets/Scripts/Blade.cs:267-284`):
  1. Приоритетно опрашивается `Touchscreen.current.primaryTouch.press.isPressed`.
  2. При отсутствии тача опрашивается `Mouse.current.leftButton.isPressed`.
- **Изоляция от тряски камеры**: метод `GameManager.ScreenToWorld(screen)` (`Assets/Scripts/GameManager.cs:614-620`) транслирует экранные пиксели в мировые координаты относительно базовой точки `camRigBase`, исключая влияние дрожания камеры при взрывах на позиционирование лезвия.
- **Адаптивный масштаб мира**: `GameManager.UpdateView()` (`Assets/Scripts/GameManager.cs:606-612`):
  ```csharp
  Unit = Mathf.Min(View.height, View.width * 1.3f) / 20f;
  ```
  На узких вертикальных (смартфонных) экранах масштаб рассчитывается от ширины, чтобы слаймы не перекрывали весь экран.

### 2.3. Механика нарезки и порог скорости
- **Порог скорости**: `Blade.Update()` (`Assets/Scripts/Blade.cs:112-114`):
  ```csharp
  speed = Mathf.Max(dist / dt, speed * Mathf.Exp(-14f * dt));
  Cutting = speed > MinCutSpeed * u; // MinCutSpeed = 8f (Line 12)
  ```
  Медленное перетаскивание курсора или удержание пальца на месте не режет объекты.
- **Учёт взмахов**: каждый раз при нажатии инкрементируется `Stroke++` (`Assets/Scripts/Blade.cs:141`). При отпускании вызывается `GameManager.Instance.OnStrokeEnd()` (`Assets/Scripts/Blade.cs:149`).
- **Шлейф клинка**: точки траектории сохраняются в структуру `Point` со временем `now` и удаляются по истечении `TrailTime = 0.16f` (`Assets/Scripts/Blade.cs:11, 160`). Отрисовка выполняется через 2 `LineRenderer` (`glow` и `core`, `Assets/Scripts/Blade.cs:50-51`) со сглаживанием сплайном Катмулла — Рома (`Smoothing = 3`, `Assets/Scripts/Blade.cs:15, 204-228`).

### 2.4. Непрерывное обнаружение столкновений (Continuous Collision Detection)
В `Blade.Cut()` (`Assets/Scripts/Blade.cs:168-186`) реализован алгоритм непрерывного пересечения отрезка движения лезвия и траектории слайма:
1. Для каждого активного слайма `s` из `Sliceable.Active` проверяется `s.CanCut` (`Alive && guard <= 0f`, `Assets/Scripts/Sliceable.cs:21`).
2. Вектор движения лезвия интерполируется в относительной системе отсчёта цели:
   ```csharp
   Vector2 ra = a - Vector2.Lerp(s.Prev, s.Pos, t0);
   Vector2 rab = b - Vector2.Lerp(s.Prev, s.Pos, t1) - ra;
   float along = -Vector2.Dot(ra, rab) / Mathf.Max(rab.sqrMagnitude, 1e-8f);
   float t = Mathf.Clamp01(along);
   ```
3. Радиус поражения `reach`:
   ```csharp
   float reach = s.Def.IsBomb ? s.Reach : s.Reach + padding;
   ```
   где для бомбы `BombReach = 0.82f` (`Assets/Scripts/Sliceable.cs:9, 22`), а для слаймов добавляется запас `padding = HitPadding * u` (`HitPadding = 0.12f`, `Assets/Scripts/Blade.cs:14`). Таким образом, бомба прощает касание шипов, требуя попадания по корпусу, а слаймы засчитывают даже касания на грани.
4. **Сортировка попаданий**: задетые цели сортируются вдоль вектора удара:
   ```csharp
   hits.Sort((x, y) => x.along.CompareTo(y.along));
   for (int i = 0; i < hits.Count; i++) hits[i].target.Slice(dir, hits[i].offset);
   ```
   Цели разрубаются в строгом физическом порядке их пересечения лезвием.

### 2.5. Кинематика и физика полета (`Sliceable.cs`)
- **Полное отсутствие PhysX / Rigidbody**: движение рассчитывается детерминированно в `Sliceable.Update()` (`Assets/Scripts/Sliceable.cs:113-114`):
  ```csharp
  vel.y -= gravity * dt;
  transform.position += (Vector3)(vel * dt);
  ```
- **Деформация желе (Squash & Stretch)** (`Assets/Scripts/Sliceable.cs:117-118, 151-157`): тело слайма ориентируется по направлению скорости `heading = Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg` и вытягивается пропорционально скорости с затухающими колебаниями `wobble`.
- **Поведение лиц**: слаймы с мордочками покачиваются в пределах $\pm 20^\circ$ (`Assets/Scripts/Sliceable.cs:91`), сохраняя читаемость мимики, а бомбы и скорлупа жеод непрерывно вращаются (`spin`, `Assets/Scripts/Sliceable.cs:92`).
- **Специфика разрезания видов**:
  - `Kind.Geode`: первый удар раскалывает скорлупу (`Shelled = false`), накладывает `guard = SplitGuard (0.16s)` (`Assets/Scripts/Sliceable.cs:193-194`) и придаёт восходящий импульс (`Assets/Scripts/Sliceable.cs:198`), второй удар разрубает аметистовое желе.
  - `Kind.Blue`: митоз (`Assets/Scripts/Sliceable.cs:206-208, 218-236`) — спавнит два `Art.Mini` слайма, рассчитывая параболы так, чтобы они гарантированно приземлились в пределах видимого экрана.
  - `Kind.King`: срез вызывает `Transmutation()` (`Assets/Scripts/GameManager.cs:473-482`) — замедление времени на 3.15 с и мгновенное превращение всех бомб на экране в золотые самородки `Art.Nugget`.
  - `Kind.Bomb`: мгновенная детонация, вызов `GameManager.Instance.OnBombHit(pos)` (`Assets/Scripts/Sliceable.cs:186`).

---

## 3. Логика Волн, Спавнер и Кривые Сложности

Конфигурация параметров баланса хранится в `Assets/Resources/Balance.asset` (класс `Balance.cs`), а логика генерации реализована в `Assets/Scripts/Spawner.cs`.

### 3.1. Параметры Balance.asset vs Balance.cs
| Параметр | По умолчанию в C# (`Balance.cs`) | Значение в ассете (`Balance.asset`) | Описание |
|---|---|---|---|
| `comboMin` | 3 | 3 | Мин. число слаймов во взмахе для комбо (`Balance.cs:56`) |
| `frenzyCombo` | 4 | 4 | Порог комбо для активации режима Frenzy (`Balance.cs:57`) |
| `comboBonus` | 10 | 10 | Базовый бонус за каждый слайм комбо (`Balance.cs:58`) |
| `comboWindow` | 0.35s | 0.35s | Окно между срезами одного взмаха (`Balance.cs:59`) |
| `frenzySeconds` | 8.0s | 8.0s | Базовая длительность перегрузки (`Balance.cs:60`) |
| `frenzyRest` | 10.0s | 10.0s | Передышка после перегрузки (`Balance.cs:61`) |
| `slowSeconds` | 3.2s | **3.15s** | Длительность замедления после короля (`Balance.asset:21`) |
| `slowScale` | 0.4 | 0.4 | Масштаб времени в замедлении (`Balance.cs:63`) |
| `missGrace` | 0.6s | 0.6s | Окно защиты от потери нескольких жизней (`Balance.cs:64`) |
| `peak` | 270.0s | 270.0s | Секунда выхода сложности на пик (`Balance.cs:86`) |
| `phraseChance` | 0.3 | 0.3 | Вероятность музыкальной серии волн (`Balance.cs:92`) |
| `kingChance` | 0.14 | 0.14 | Базовый шанс короля в тесной волне (`Balance.cs:94`) |
| `kingCooldown` | 25.0s | 25.0s | Кулдаун между вызовами короля (`Balance.cs:96`) |

### 3.2. Кривые сложности (Animation Curves)
Все кривые интерполируются по времени попытки `Elapsed` от 0 до 270 секунд (`Assets/Resources/Balance.asset:24-392`):
1. **`airTime`** (время полета слайма в воздухе):
   - $0\,\text{с} \to 2.7\,\text{с}$; $45\,\text{с} \to 2.5\,\text{с}$; $100\,\text{с} \to 2.3\,\text{с}$; $170\,\text{с} \to 2.05\,\text{с}$; $270\,\text{с} \to 1.6\,\text{с}$.
   - Сверх пика ($>270\,\text{с}$): `Mathf.Max(1.15f, airTime - 0.2f * over)` (`Assets/Scripts/Spawner.cs:91`).
2. **`load`** (среднее количество объектов в волне):
   - $0\,\text{с} \to 1.0$; $6\,\text{с} \to 1.5$; $15\,\text{с} \to 2.0$; $45\,\text{с} \to 2.6$; $100\,\text{с} \to 3.3$; $170\,\text{с} \to 4.4$; $270\,\text{с} \to 6.0$.
   - Формула: `count = Mathf.Min(7, Mathf.FloorToInt(load + over + Random.value))` (`Assets/Scripts/Spawner.cs:93`).
3. **`interval`** (пауза между волнами):
   - $0\,\text{с} \to 1.4\,\text{с}$; $6\,\text{с} \to 1.3\,\text{с}$; $45\,\text{с} \to 1.2\,\text{с}$; $100\,\text{с} \to 1.1\,\text{с}$; $170\,\text{с} \to 0.95\,\text{с}$; $270\,\text{с} \to 0.7\,\text{с}$.
4. **`bombChance`** (вероятность появления первой бомбы в волне):
   - $10\,\text{с} \to 0.15$; $45\,\text{с} \to 0.25$; $100\,\text{с} \to 0.32$; $170\,\text{с} \to 0.42$; $270\,\text{с} \to 0.55$.
5. **`secondBomb`** (вероятность второй бомбы в волне):
   - До $90\,\text{с} \to 0.0$; от $90\,\text{с}$ растёт линейно до $0.30$ к $270\,\text{с}$.
6. **`plainWeight`** (вес базового кислотного слайма):
   - Падает с $10.0$ ($10\,\text{с}$) до $5.0$ ($270\,\text{с}$) (`Assets/Scripts/Spawner.cs:494`), повышая долю редких слаймов (Магма: 3.0, Синий: 2.2, Жеода: 2.2, Ртуть: 2.0).

### 3.3. Рисунки волн (Patterns)
В игре 9 паттернов (`Assets/Scripts/Spawner.cs:114-129`, `Assets/Scripts/Balance.cs:99-110`):
1. **Scatter** (россыпь, с $0\,\text{с}$, вес 0.6, 1–7 слаймов): вылетают одновременно из случайных точек.
2. **Stream** (очередь, с $4\,\text{с}$, вес 1.0, 2–5 слаймов): последовательный запуск слева направо или наоборот с задержкой $0.26$–$0.40\,\text{с}$.
3. **Cross** (ножницы, с $7\,\text{с}$, вес 0.9, 2–4 слайма): пары летят навстречу и сходятся в верхней точке траектории.
4. **Row** (строй, с $10\,\text{с}$, вес 1.0, 2–6 слаймов): параллельные дуги, в Frenzy бомба может занять слот внутри строя.
5. **Column** (змейка, с $13\,\text{с}$, вес 0.7, 2–5 слаймов): вылет из одной точки по общей дуге с шагом $0.2\,\text{с}$.
6. **Fan** (веер, с $17\,\text{с}$, вес 1.0, 3–7 слаймов): запуск из одной точки веером в стороны.
7. **Arc** (радуга/чаша, с $22\,\text{с}$, вес 0.9, 3–7 слаймов): строй с апогеем по дуге.
8. **Stairs** (лесенка, с $28\,\text{с}$, вес 0.8, 3–6 слаймов): косая шеренга со ступенчатым стартом $0.16\,\text{с}$.
9. **Figure** (фигура, с $34\,\text{с}$, вес 0.9, 3–7 слаймов): треугольник, ромб или кольцо слаймов, летящих как единое твердое тело.

*Правило исключения*: повтор одного и того же паттерна подряд штрафуется: `weight * 0.3f` (`Assets/Scripts/Spawner.cs:202`). В режиме Frenzy паттерны Scatter, Stream и Column запрещены (`weight = 0`).

### 3.4. Алгоритм размещения бомб (Anti-Crowding)
В методе `Spawner.AddBombs()` (`Assets/Scripts/Spawner.cs:412-433`):
- Для каждой бомбы генерируется до `BombTries = 10` случайных траекторий.
- Траектория дискретизируется на `FlightSamples = 16` точек во времени.
- Рассчитывается метрика `Crowd(shot, wave)` (`Assets/Scripts/Spawner.cs:436-465`) — доля времени, когда бомба находится ближе `reach + NearBomb * Unit` (`NearBomb = 1.1f`) к любому слайму.
- Принимается траектория с минимальной теснотой, не превышающей порог `balance.crowding.Evaluate(Elapsed)` ($0.1$ на $10\,\text{с} \to 0.5$ на $270\,\text{с}$).

### 3.5. Алгоритм появления Золотого Короля и Эскорта
- Король появляется не по таймеру, а в моменты максимального давления (`Spawner.Pressure`, `Assets/Scripts/Spawner.cs:161-172`):
  $$\text{crowd} = \sum \text{объекты в волне} + \sum \text{активные объекты в воздухе} \quad (\text{бомбы считаются за 2})$$
- Если $\text{crowd} \ge \text{kingCrowd}(\text{Elapsed})$ ($5$ на $0\,\text{с} \to 9$ на $270\,\text{с}$):
  $$\text{Вероятность} = \text{kingChance} \cdot (1 + 0.5 \cdot (\text{crowd} - \text{need}))$$
- При успешном броске после $95\,\text{с}$ с вероятностью `escortChance` ($0.35 \to 0.70$) король выпускается под конвоем двух бомб спереди и сзади с зазором `EscortGap = 0.8f` (`Assets/Scripts/Spawner.cs:581-586`).

---

## 4. Система Комбо, Серий, Множителей и Очков

### 4.1. Стоимость слаймов
Определена в `Art.BuildSlimes()` (`Assets/Scripts/Art.cs:107-124`):
- `Acid`: 10 очков (штраф при упущении: да)
- `Magma`: 20 очков (штраф: да)
- `Geode`: 30 очков (раскол скорлупы: 0 очков; желе: 30 очков; штраф: да)
- `Mercury`: 40 очков (штраф: да, летит на 26% быстрее: `airTime * 0.74`)
- `Blue`: 10 очков (при срезе делится на 2 Mini; штраф: да)
- `Mini`: 15 очков (штраф: да)
- `King`: 100 очков (штраф при упущении: **нет**)
- `Nugget`: 25 очков (штраф при упущении: **нет**)
- `Bomb`: 0 очков (отнимает жизнь, штраф при упущении: **нет**)

### 4.2. Множитель очков (Mult)
Свойство `GameManager.Mult` (`Assets/Scripts/GameManager.cs:48`):
```csharp
public int Mult { get { return FrenzyLeft > 0f ? 2 : 1; } }
```
В режиме Alembic Frenzy множитель равен **×2** для всех очков, бонусов комбо и бонусов вех серии.

### 4.3. Очки за срез слайма
Формула в `GameManager.OnSlimeSliced()` (`Assets/Scripts/GameManager.cs:317`):
$$\text{gain} = \text{def.points} \times \text{Mult}$$

### 4.4. Бонус за Комбо (Combo Bonus)
Формула в `GameManager.EndChain()` (`Assets/Scripts/GameManager.cs:400`):
Если число слаймов в одном взмахе $n \ge 3$ (`comboMin`):
$$\text{bonus} = \text{comboBonus} \times n \times \text{Mult} = 10 \times n \times \text{Mult}$$
*Примеры*:
- 3 слайма: $10 \times 3 \times 1 = 30$ очков (в Frenzy: $60$).
- 5 слаймов: $10 \times 5 \times 1 = 50$ очков (в Frenzy: $100$).
- 10 слаймов (Ультракомбо): $10 \times 10 \times 1 = 100$ очков (в Frenzy: $200$).

### 4.5. Режим перегрузки (Alembic Frenzy)
Активируется при $n \ge 4$ (`frenzyCombo`, `Assets/Scripts/GameManager.cs:419, 448-470`):
- Длительность: $\text{FrenzySpan} = 8 + \min(2, n - 4)$ секунд (от 8 до 10 секунд).
- Кулдаун после завершения: $10\,\text{с}$ (`frenzyRest = 10f`).
- Уровень полоски `FrenzyTier = n - frenzyCombo + 1` (Тьеров всего 5, `Assets/Scripts/ComboBar.cs:22-29`):
  1. Кипение (Tier 1): цвет голубой (`0.3, 0.75, 1.0`), частицы капель.
  2. Бурление (Tier 2): цвет зелёный (`0.35, 1.0, 0.5`), капли + звёзды.
  3. Резонанс (Tier 3): цвет жёлтый (`1.0, 0.82, 0.22`), капли + звёзды + кристаллы.
  4. Пламя (Tier 4): цвет оранжевый (`1.0, 0.45, 0.15`), звёзды + кристаллы + угли.
  5. Великое делание (Tier 5): цвет радужный перелив, все виды частиц.

### 4.6. Серия без промаха (Streak) и вехи
Серия растёт при каждом разрубленном слайме (`GameManager.GrowStreak()`, `Assets/Scripts/GameManager.cs:424-438`):
- Вехи: $10, 30, 50, 100$ слаймов; далее каждые $50$ слаймов ($150, 200, 250\dots$).
- Бонус за взятие вехи (`Assets/Scripts/GameManager.cs:430`):
  $$\text{bonus} = \text{streakBonus} \times \text{Streak} \times \text{Mult} = 10 \times \text{Streak} \times \text{Mult}$$
  - Серия ×10: $+100$ очков ($+200$ в Frenzy).
  - Серия ×30: $+300$ очков ($+600$ в Frenzy).
  - Серия ×50: $+500$ очков ($+1000$ в Frenzy).
  - Серия ×100: $+1000$ очков ($+2000$ в Frenzy).
- Обрыв серии: `GameManager.BreakStreak()` (`Assets/Scripts/GameManager.cs:442-445`) сбрасывает `Streak = 0` при упущении слайма (`OnSlimeMissed:369`) или подрыве бомбы (`OnBombHit:386`). Полоса серии вздрагивает красным контуром (`Broken`, `Assets/Scripts/StreakBar.cs:31, 142, 171`).

### 4.7. Звания игрока (Ranks)
Таблица званий (`Assets/Scripts/Balance.cs:126-135`, `Assets/Resources/Balance.asset:485-506`):
1. «Ученик» — 0 очков
2. «Подмастерье» — 1 000 очков
3. «Зельевар» — 3 000 очков
4. «Алхимик» — 7 000 очков
5. «Магистр» — 15 000 очков
6. «Архимаг» — 30 000 очков
7. «Легенда лаборатории» — 60 000 очков

Когда текущий счёт в бою превышает порог следующего звания, объявление о повышении выводится прямо во время игры: `Hud.Hint("Новое звание: «...»!", 2.6f); Sfx.Record();` (`Assets/Scripts/GameManager.cs:490-496`).

---

## 5. Пауза, Game Over, Звук и Сцена

### 5.1. Логика Паузы
- **Активация**: правая кнопка мыши `Blade.PausePressed()` (`Assets/Scripts/Blade.cs:304-308`, `Assets/Scripts/GameManager.cs:114`).
- **Состояние паузы**: `GameManager.SetPause()` (`Assets/Scripts/GameManager.cs:159-169`):
  - `Paused = on;`
  - `Hud.ShowPause(on);`
  - Музыка приглушается: `Sfx.Music(on ? 0.4f : 1f);`
  - Время останавливается: `Time.timeScale = 0f;` (`Assets/Scripts/GameManager.cs:122`).
  - При снятии с паузы: `Time.timeScale = flow; PlayerPrefs.Save();`.
  - В `Blade.Update()` (`Assets/Scripts/Blade.cs:95`): при `Paused == true` флаг касания сбрасывается `down = false`, клинок не оставляет следа и не режет.
  - На паузе активен интерактивный ползунок громкости `pauseVolume` (`Assets/Scripts/Hud.cs:544`), поддерживающий перетаскивание мышью и колесико `Blade.Wheel()`.
  - Выход в меню по `Escape`: `SaveProgress(); ToMenu();` (`Assets/Scripts/GameManager.cs:117-121`).

### 5.2. Поток поражения (Game Over Flow)
- **Условия потери жизни** (`Lives` инициализируется 3, `MaxLives = 3`, `Assets/Scripts/GameManager.cs:14, 201`):
  1. Упущен слайм с `penalty == true` (`Assets/Scripts/Sliceable.cs:140-143`): срабатывает окно защиты `graceLeft = missGrace (0.6s)` (`Assets/Scripts/GameManager.cs:362`), очищается поле `ClearField()`, спавнер ставится на удержание `spawner.Hold(1.4f)`, отнимается 1 жизнь (`LoseLife()`).
  2. Разрублена бомба (`Assets/Scripts/GameManager.cs:376-393`): сброс комбо и серий, сброс Frenzy и Slow, провал времени `TimeDip(0.22f, 0.45f)`, очистка поля `ClearField()`, пауза спавнера на 1.5 с, потеря 1 жизни.
- **Экран GameOver** (`Assets/Scripts/GameManager.cs:229-252`):
  - `State = GameState.GameOver;`
  - Фиксация времени поражения: `stateSince = Time.unscaledTime;`
  - Защита от случайного рестарта: `RestartLock = 0.7f;` (`Assets/Scripts/GameManager.cs:15`). Первые 0.7 секунды нажатия игнорируются (`Assets/Scripts/GameManager.cs:128`), защищая от рестарта тем же движением, которым игрок нажал на бомбу.
  - Сохранение рекордов в PlayerPrefs (`SaveProgress()`, `Assets/Scripts/GameManager.cs:255-266`).
  - Звук поражения: `Sfx.GameOver()`, музыка приглушается до 0.3 (`Assets/Scripts/GameManager.cs:249-250`).
  - Если побит рекорд (`newBest`): периодический запуск праздничных салютов `Celebrate(dt)` (`Assets/Scripts/GameManager.cs:269-279`).

### 5.3. Звуковая обратная связь (`Sfx.cs`)
- **Пул аудиоисточников**: 16 источников `AudioSource` в круговом буфере (`Voices = 16`, `Assets/Scripts/Sfx.cs:15, 59-65, 538-543`).
- **Специализированные источники**:
  - `loop`: фоновый эмбиент лаборатории (зацикленный 12-секундный буфер, `AmbientLevel = 0.5f`).
  - `wind`: непрерывный свист клинка, модулируемый скоростью взмаха (`Assets/Scripts/Sfx.cs:437-443`).
  - `music`: трек `Music/Gameplay`, громкость плавно интерполируется в `Tick(dt)` (`Assets/Scripts/Sfx.cs:427-434`).
- **Музыкальная пентатоника для комбо**:
  Мажорная пентатоника в полутонах `Ladder = { 0, 2, 4, 7, 9, 12, 14, 16, 19, 21, 24 }` (`Assets/Scripts/Sfx.cs:19`). Метод `Sfx.Step(chain)` (`Assets/Scripts/Sfx.cs:528-532`) повышает питч звука ноты `Mathf.Pow(2f, Ladder[i] / 12f)` с каждым ударом серии.
- **Встроенный DSP-синтезатор**: в `Sfx.Synthesize()` (`Assets/Scripts/Sfx.cs:165-401`) процедурно синтезируются 17 звуковых клипов (Swoosh, Squelch, Fizz, Crack, Thud, Magma, Ding, Bells, Note, Combo, Start, GameOver, Bomb, Miss, Toss, Split, Frenzy, Transmute, Ambient).

---

## 6. Архитектурные Риски и Готовность к WebGL (Unity 6.6)

### 6.1. Архитектурные антипаттерны и связанность
1. **Глобальные синглтоны и статические классы**:
   - `GameManager.Instance` (`Assets/Scripts/GameManager.cs:24`) — монолитный God Object, управляющий состояниями, UI, эффектами, временем, счётом и сохранением.
   - `Sliceable.Active` (`Assets/Scripts/Sliceable.cs:13`) — публичный статический список без инкапсуляции.
   - `Art`, `Sfx`, `Fx` — статические классы с жесткой взаимной зависимостью.
2. **Отсутствие событийной шины (Event Bus)**:
   Компоненты жестко вызывают друг друга напрямую (`Sliceable` напрямую дергает методы `GameManager.Instance`, `Blade` дергает `GameManager.Instance`). Любая модификация одной подсистемы требует правок во всех зависимых скриптах.

### 6.2. GC-аллокации и утечки производительности (GC Pressure)
1. **Полное отсутствие Object Pooling для слаймов**:
   - В `Sliceable.Spawn()` (`Assets/Scripts/Sliceable.cs:30-65`) на каждый объект создаётся `new GameObject(def.name)` и 2 дочерних GameObject (`Glow` и `Body`) со `SpriteRenderer`.
   - При уничтожении вызывается `Destroy(gameObject)` (`Assets/Scripts/Sliceable.cs:259`).
   - На поздних секундах (5–7 объектов в секунду) генерируются десятки GameObject в секунду с сопутствующими вызовами аллокатора Unity Native-to-Managed bridge.
2. **Отсутствие пулинга визуальных эффектов (`Fx.cs`)**:
   - На каждый срез слайма создаются новые GameObject через `NewSprite` и `NewPiece` (`Assets/Scripts/Fx.cs:541-560`): `Half` (2 шт.), `SlashGlow`, `Slash`, `Pulse`, `Flash`, `Splat` (с 2–4 дочерними `Drip`), а также осколки `Shard` и кристаллы `Crystal`.
   - На каждое комбо создаются десятки временных компонентов `Fade`, `Piece`, `Splat`, уничтожаемых через `Destroy(gameObject)`.
   - Это приводит к скачкообразным паузам сборщика мусора (GC Spike) каждые 5–10 секунд игры на мобильных браузерах.
3. **Аллокации лямбда-делегатов в цикле апдейта**:
   - `Blade.Cut()` (`Assets/Scripts/Blade.cs:183`):
     ```csharp
     hits.Sort((x, y) => x.along.CompareTo(y.along));
     ```
     Аллокация замыкания и делегата `Comparison<Hit>` при каждом ударе клинка по объектам.
   - `Spawner.Release()` (`Assets/Scripts/Spawner.cs:474`):
     ```csharp
     wave.Sort((a, b) => a.delay.CompareTo(b.delay));
     ```
     Аллокация делегата сортировки на каждую волну.
   - `Spawner.Release()` (`Assets/Scripts/Spawner.cs:480`):
     ```csharp
     yield return new WaitForSeconds(wave[i].delay - clock);
     ```
     Аллокация `new WaitForSeconds` на каждый шаг волны.
   - `Blade.Paint()` (`Assets/Scripts/Blade.cs:238-246`): аллокация `new Gradient()`, `new GradientColorKey[]`, `new GradientAlphaKey[]` при каждом переключении режима Empowered.
   - Конкатенация строк в `Hud.Update()`, `ComboBar.Update()`, `StreakBar.Update()`: генерация сотен мусорных строк `string` в секунду.

### 6.3. Критические риски готовности к Unity 6.6 WebGL для Яндекс Игр
1. **КРИТИЧЕСКИЙ РИСК: `Task.Run` в `Sfx.Prepare()` на WebGL**:
   - В `Assets/Scripts/Sfx.cs:43`:
     ```csharp
     public static void Prepare()
     {
         if (!Ready && job == null) job = Task.Run(Synthesize);
     }
     ```
     и в `Assets/Scripts/Sfx.cs:88`:
     ```csharp
     job.Wait();
     ```
   - В `ProjectSettings/ProjectSettings.asset:577` установлено:
     ```yaml
     webGLThreadsSupport: 0
     ```
   - **Диагноз**: В WebGL без поддержки многопоточности (Wasm Threads / SharedArrayBuffer) `Task.Run` не может выделить фоновый рабочий поток. Вызов блокирующего `job.Wait()` на главном потоке браузера в ряде версий Emscripten/Unity WebGL приводит либо к **полному зависанию (deadlock) при старте игры**, либо к `PlatformNotSupportedException`.
2. **Несжатые текстуры и аудио в памяти**:
   - В `Assets/Editor/AlchemiaTools.cs:132`:
     ```csharp
     importer.textureCompression = TextureImporterCompression.Uncompressed;
     ```
     Все 77 текстур в `Assets/Resources/Art` импортируются как `RGBA32` без сжатия! В VRAM мобильного браузера распакованные текстуры занимают свыше 40 МБ.
   - В `Assets/Editor/AlchemiaTools.cs:154`:
     ```csharp
     settings.compressionFormat = AudioCompressionFormat.PCM;
     ```
     Все звуки импортируются в несжатом PCM.
   - Папка `Assets/Resources` весит 21 МБ на диске. В Unity WebGL всё содержимое папки `Resources` вшивается в основной файл данных `data.unityweb`, что приводит к огромному размеру первой загрузки (превышающему лимиты мобильных сетей) и риску аварийного закрытия вкладки по Out-Of-Memory (OOM) на смартфонах с 2–3 ГБ ОЗУ (особенно iOS Safari).
3. **Отсутствие интеграции SDK Яндекс Игр**:
   - Отсутствуют JS-библиотеки (`.jslib`) для вызова `ysdk.adv.showFullscreenAdv()`, `ysdk.adv.showRewardedVideo()`, `ysdk.getPlayer()`.
   - В `ProjectSettings.asset:571` используется шаблон `webGLTemplate: APPLICATION:Default`, не содержащий скриптов инициализации Yandex SDK, адаптивного масштабирования холста под размер экрана мобильного браузера и заглушек потери фокуса.
4. **Хранилище PlayerPrefs вместо Cloud Storage**:
   - В коде прогресс сохраняется исключительно через `PlayerPrefs.SetInt` / `PlayerPrefs.Save` (`Assets/Scripts/GameManager.cs:243, 261-265`).
   - На WebGL `PlayerPrefs` сохраняется в локальный IndexedDB браузера (`/idbfs/`). При очистке кэша или смене устройства прогресс игрока сгорает. Для соответствия требованиям Яндекс Игр требуется синхронизация с облачным хранилищем `ysdk.getPlayer().setData()`.
5. **Отсутствие паузы при потере фокуса окна**:
   - Требование модерации Яндекс Игр: игра и звук должны гарантированно вставать на паузу при потере фокуса вкладки.
   - В `GameManager.cs` обработчики `OnApplicationFocus(bool focus)` и `OnApplicationPause(bool pause)` **полностью отсутствуют**.
6. **Блокировка автовоспроизведения Web Audio (Autoplay Policy)**:
   - В `Sfx.Build()` (`Assets/Scripts/Sfx.cs:103-105`) сразу вызывается `loop.Play()` и `wind.Play()`. Современные браузеры блокируют AudioContext до первого пользовательского клика, выводя ошибки в консоль.

---

## 7. Рендеринг URP, Шейдеры и Постобработка на Мобильном WebGL

### 7.1. Конфигурация UniversalRP и Renderer2D
1. **Конфигурация ассета URP** (`Assets/Settings/UniversalRP.asset`):
   - `m_SupportsHDR: 1` (`Line 26`) — включен HDR буфер кадра (`FP16`).
   - `m_MSAA: 1` (`Line 28`) — MSAA отключен (значение 1 означает 1x / Off).
   - `m_RenderScale: 1` (`Line 29`).
   - `m_UseSRPBatcher: 1` (`Line 72`).
2. **Конфигурация Renderer2D** (`Assets/Settings/Renderer2D.asset`):
   - `m_LightRenderTextureScale: 0.5` (`Line 32`).
   - `m_UseDepthStencilBuffer: 1` (`Line 46`).
   - `Global Light 2D` присутствует в сцене (`SampleScene.unity:318`), но все игровые спрайты используют материал `SpriteUnlit` (`Assets/Scripts/Art.cs:1330`), который игнорирует освещение 2D Light.

### 7.2. Нагрузка постобработки (`PostFx.asset` & `GameManager.BuildPostFx`)
В `GameManager.BuildPostFx()` (`Assets/Scripts/GameManager.cs:568-605`) создаются два глобальных профиля постобработки:
1. **Профиль "PostFx"** (`Assets/Resources/PostFx.asset`):
   - **Bloom**: `threshold = 0.78f`, `intensity = 0.8f`, `scatter = 0.62f` (`PostFx.asset:19-27`).
   - **Vignette**: `intensity = 0.26f`, `smoothness = 0.5f` (`PostFx.asset:73-77`).
2. **Профиль "PostFxShock"** (`Assets/Scripts/GameManager.cs:591-595`):
   - **ChromaticAberration**: динамически меняет интенсивность `chroma.intensity.value = shock` (до 1.0 при взрыве бомбы и до 0.6 при комбо $\ge 5$).

### 7.3. Анализ рисков производительности на бюджетном мобильном WebGL
- **Bloom Pass Overhead**:
  Эффект свечения (Bloom) в URP рассчитывается через пирамиду даунсэмплинга и апсэмплинга текстур с размытием Гаусса (до 6 проходов брэнд-буфера кадра). На встроенных мобильных GPU (ARM Mali-G52/G57, Adreno 610) при разрешении рендера 1080p/720p задержка рендеринга Bloom может составлять от $8$ до $16\,\text{мс}$ на кадр, что делает поддержание стабильных 60 FPS невозможным.
- **HDR & Текстуры глубины на WebGL 2.0**:
  При включенном HDR (`UniversalRP.asset:26`) движок выделяет рендертаргет формата `R11G11B10` или `RGBA16F`. На дешевых мобильных процессорах мобильный веб-драйвер часто не поддерживает нативное переключение таких буферов без дорогостоящего копирования памяти (Copy Color Pass), вызывая просадки FPS при взрывах и вспышках.
- **Динамическое инстанцирование материалов**:
  В `Art.BuildMaterials()` (`Assets/Scripts/Art.cs:1346-1348`):
  ```csharp
  SoftMat = new Material(Mat) { mainTexture = Soft.texture };
  DotMat = new Material(Mat) { mainTexture = Dot.texture };
  StarMat = new Material(Mat) { mainTexture = Star.texture };
  ```
  Создание отдельных экземпляров материалов `new Material(...)` ломает батчинг SRP Batcher между спрайтами с разными текстурами, заставляя Unity делать отдельные Draw Calls для каждого типа частиц.

---

## 8. Итоговый Чек-лист и Рекомендации по Доработке (R1 Summary)

| № | Область | Обнаруженный дефект / Риск | Файл и строки | Рекомендуемое исправление |
|---|---|---|---|---|
| 1 | **Критично (WebGL)** | `Task.Run` + `job.Wait()` зависает в однопоточном WebGL | `Assets/Scripts/Sfx.cs:43, 88` | Заменить фоновый Task на корутину с распределением расчета по кадрам либо использовать предварительно запеченные аудиофайлы из `Resources/Sounds` |
| 2 | **Критично (WebGL)** | Нет паузы звука и логики при потере фокуса вкладки браузера | `Assets/Scripts/GameManager.cs:107-155` | Реализовать `OnApplicationFocus(bool)` и `OnApplicationPause(bool)` с вызовом `SetPause(true)` и глушением `AudioListener.pause` |
| 3 | **Критично (WebGL)** | Использование PlayerPrefs вместо облака Яндекс Игр | `Assets/Scripts/GameManager.cs:243, 261-265` | Интегрировать YandexGames SDK (Bridge) для облачных сохранений `PlayerAccount.SetData` / `GetData` |
| 4 | **Производительность** | Отсутствие пулинга для слаймов (Spawn/Destroy спам) | `Assets/Scripts/Sliceable.cs:30-65, 259` | Внедрить пул объектов (`UnityEngine.Pool.ObjectPool<Sliceable>`) |
| 5 | **Производительность** | Спам объектами FX (Half, Slash, Splat, Drip) | `Assets/Scripts/Fx.cs:541-560, 703` | Заменить разовые GameObject на частицы ParticleSystem или централизованный пул спрайтов |
| 6 | **GC-аллокации** | Лямбда-сортировка `hits.Sort` и `wave.Sort` в рантайме | `Assets/Scripts/Blade.cs:183`, `Assets/Scripts/Spawner.cs:474` | Реализовать неаллоцирующую сортировку (IComparer) или вставку в упорядоченный список |
| 7 | **Память / Загрузка** | Несжатые текстуры (RGBA32) и аудио (PCM) в Resources | `Assets/Editor/AlchemiaTools.cs:132, 154` | Включить сжатие текстур ASTC/ETC2/Crunch и сжатие аудио Vorbis; исключить неиспользуемые ассеты из `Resources` |
| 8 | **Рендеринг (Mobile)** | Тяжелый Bloom и HDR на слабых мобильных устройствах | `Assets/Settings/UniversalRP.asset:26`, `Assets/Scripts/GameManager.cs:580` | Добавить профиль низкого качества качества графики без Bloom для слабых мобильных браузеров |
| 9 | **Платформа** | Стандартный шаблон Unity WebGL вместо респонсивного | `ProjectSettings/ProjectSettings.asset:571` | Использовать специализированный шаблон WebGL с автоматической подгонкой под размеры экрана и интеграцией SDK |

---

*Аудит выполнен на основе строгого статического анализа кодовой базы без внесения изменений в файлы проекта.*
