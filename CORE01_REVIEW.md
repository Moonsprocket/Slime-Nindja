# Независимое ревью задачи CORE-01: Устранение WebGL-дедлока в Sfx.cs

**Дата проверки**: 16 октября 2026 г.  
**Целевой коммит**: `bffa61746126f72b411b8c28f040eca5b979e13f`  
**Ветка**: `feature/CORE-01-sfx-webgl-init`  
**Репозиторий**: `/home/moonsprocket/Slime-Nindja`  
**Статус ревью**: **PASS WITH LIMITATIONS** (Пройдено с документированными ограничениями)

---

## 1. Исходное состояние репозитория и сохранность данных

Перед проведением аудита зафиксировано полное состояние рабочей копии и Git-указателей:

- **Текущая ветка**: `feature/CORE-01-sfx-webgl-init`
- **Текущий HEAD**: `bffa61746126f72b411b8c28f040eca5b979e13f`
- **Базовый коммит (upstream/origin main)**: `8ccf8f3` (`Fin`)
- **Состав коммита `bffa617`**:
  ```
   Assets/Scripts/Sfx.cs | 42 +++++++++++++++++++++---------------------
   1 file changed, 21 insertions(+), 21 deletions(-)
  ```
  В коммит входит **строго один файл** `Assets/Scripts/Sfx.cs`. Никаких посторонних файлов или мета-файлов не затронуто.

### Состояние незакоммиченных пользовательских файлов (`git status --short`)
Все локальные изменения пользователя, созданные до начала задачи, бережно сохранены в рабочей директории в неизменном виде:
```
 M Assets/Resources/Art/Effects/Coin.png.meta
 M Assets/Resources/Art/Effects/Crystal.png.meta
 M Assets/Resources/Art/Effects/Dot.png.meta
 M Assets/Resources/Art/Effects/Drip.png.meta
 M Assets/Resources/Art/Effects/Flame.png.meta
 M Assets/Resources/Art/Effects/Rays.png.meta
 M Assets/Resources/Art/Effects/Ring.png.meta
 M Assets/Resources/Art/Effects/Shard.png.meta
 M Assets/Resources/Art/Effects/Shock.png.meta
 M Assets/Resources/Art/Effects/Soft.png.meta
 M Assets/Resources/Art/Effects/Splat1.png.meta
 M Assets/Resources/Art/Effects/Splat2.png.meta
 M Assets/Resources/Art/Effects/Splat3.png.meta
 M Assets/Resources/Art/Effects/Splat4.png.meta
 M Assets/Resources/Art/Effects/Star.png.meta
 M Assets/Resources/Art/Effects/Streak.png.meta
 M Assets/Resources/Art/Effects/White.png.meta
 M Assets/Resources/Art/Interface/Badge.png.meta
 M Assets/Resources/Art/Interface/Crown.png.meta
 M Assets/Resources/Art/Interface/Frame.png.meta
 M Assets/Resources/Art/Interface/Heart.png.meta
 M Assets/Resources/Art/Interface/Mascot1.png.meta
 M Assets/Resources/Art/Interface/Mascot2.png.meta
 M Assets/Resources/Art/Interface/Mascot3.png.meta
 M Assets/Resources/Art/Interface/Panel.png.meta
 M Assets/Resources/Art/Interface/Pill.png.meta
 M Assets/Resources/Art/Interface/PillGlow.png.meta
 M Assets/Resources/Art/Interface/PillRing.png.meta
 M Assets/Resources/Art/Interface/Plate.png.meta
 M Assets/Resources/Art/Interface/Rank1.png.meta
 M Assets/Resources/Art/Interface/Rank2.png.meta
 M Assets/Resources/Art/Interface/Rank3.png.meta
 M Assets/Resources/Art/Interface/Rank4.png.meta
 M Assets/Resources/Art/Interface/Rank5.png.meta
 M Assets/Resources/Art/Interface/Rank6.png.meta
 M Assets/Resources/Art/Interface/Rank7.png.meta
 M Assets/Resources/Art/Interface/Ribbon.png.meta
 M Assets/Resources/Art/Interface/Vignette.png.meta
 M Assets/Resources/Art/Scene/Brim.png.meta
 M Assets/Resources/Art/Scene/Cat.png.meta
 M Assets/Resources/Art/Scene/CatBlink.png.meta
 M Assets/Resources/Art/Scene/Lab.png.meta
 M Assets/Resources/Art/Scene/Potion1.png.meta
 M Assets/Resources/Art/Scene/Potion2.png.meta
 M Assets/Resources/Art/Scene/Potion3.png.meta
 M Assets/Resources/Art/Scene/Rim.png.meta
 M Assets/Resources/Art/Scene/Shelves.png.meta
 M Assets/Resources/Art/Scene/Wall.png.meta
 M Assets/Resources/Art/Slimes/Acid.png.meta
 M Assets/Resources/Art/Slimes/Acid_blink.png.meta
 M Assets/Resources/Art/Slimes/Acid_half.png.meta
 M Assets/Resources/Art/Slimes/Bead.png.meta
 M Assets/Resources/Art/Slimes/Blue.png.meta
 M Assets/Resources/Art/Slimes/Blue_blink.png.meta
 M Assets/Resources/Art/Slimes/Bomb.png.meta
 M Assets/Resources/Art/Slimes/Geode.png.meta
 M Assets/Resources/Art/Slimes/Geode_blink.png.meta
 M Assets/Resources/Art/Slimes/Geode_half.png.meta
 M Assets/Resources/Art/Slimes/Geode_shell.png.meta
 M Assets/Resources/Art/Slimes/King.png.meta
 M Assets/Resources/Art/Slimes/King_blink.png.meta
 M Assets/Resources/Art/Slimes/King_half.png.meta
 M Assets/Resources/Art/Slimes/Magma.png.meta
 M Assets/Resources/Art/Slimes/Magma_blink.png.meta
 M Assets/Resources/Art/Slimes/Magma_half.png.meta
 M Assets/Resources/Art/Slimes/Mercury.png.meta
 M Assets/Resources/Art/Slimes/Mercury_blink.png.meta
 M Assets/Resources/Art/Slimes/Mini.png.meta
 M Assets/Resources/Art/Slimes/Mini_blink.png.meta
 M Assets/Resources/Art/Slimes/Mini_half.png.meta
 M Assets/Resources/Art/Slimes/Nugget.png.meta
 M Assets/Resources/Art/Story/Burst.png.meta
 M Assets/Resources/Art/Story/Cauldron.png.meta
 M Assets/Resources/Art/Story/Sickle.png.meta
 M Assets/Resources/Art/Story/Wizard.png.meta
 M Assets/Resources/Art/Story/WizardShock.png.meta
 M Assets/Resources/Balance.asset
 M Assets/Settings/UniversalRenderPipelineGlobalSettings.asset
?? .agents/
?? AGENTS.md
?? Assets/Resources/Music/LOCAL_APPDATA_FONTCONFIG_CACHE.meta
?? Assets/Resources/Music/LOCAL_APPDATA_FONTCONFIG_CACHE/
?? Assets/Resources/Music/~~watch_later.meta
?? Assets/Resources/Music/~~watch_later/
?? mono_crash.mem.10599.1.blob
```

---

## 2. Анализ реализации и трассировка вызовов

### Полный diff коммита `bffa617`
```diff
diff --git a/Assets/Scripts/Sfx.cs b/Assets/Scripts/Sfx.cs
index 4d07f28..0287168 100644
--- a/Assets/Scripts/Sfx.cs
+++ b/Assets/Scripts/Sfx.cs
@@ -1,5 +1,4 @@
 using System.Collections.Generic;
-using System.Threading.Tasks;
 using UnityEngine;
 
 // Звуки игры. Каждый звук — файл WAV в Assets/Resources/Sounds: его можно открыть, отредактировать или заменить своим.
@@ -33,14 +32,13 @@ public static class Sfx
     static System.Random rng;
     static Dictionary<string, float[]> baked;
     static Dictionary<string, AudioClip> clips;
-    static Task job;
 
-    static bool Ready { get { return clips != null && clips["Swoosh"] != null; } }
+    static bool Ready { get { return swoosh != null; } }
 
-    // Синтез — чистая математика, поэтому идёт в фоновом потоке, пока главный рисует графику.
+    // Подготовка звуков: загрузка аудиоклипов из Resources или процедурный синтез при их отсутствии.
     public static void Prepare()
     {
-        if (!Ready && job == null) job = Task.Run(Synthesize);
+        if (!Ready) Publish();
     }
 
     // Сырые буферы всех звуков — для выгрузки в WAV из меню редактора.
@@ -82,20 +80,6 @@ public static class Sfx
         AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, 0.8f);
         Slow = 1f;
 
-        if (!Ready)
-        {
-            Prepare();
-            job.Wait();
-            job = null;
-            clips = new Dictionary<string, AudioClip>();
-            foreach (var pair in baked)
-            {
-                var clip = AudioClip.Create(pair.Key, pair.Value.Length, 1, Rate, false);
-                clip.SetData(pair.Value, 0);
-                clips[pair.Key] = clip;
-            }
-            baked = null;
-        }
         // Свои файлы подхватываются при каждом запуске, поэтому замена звука не требует перезапуска редактора.
         Publish();
 
@@ -155,11 +139,27 @@ public static class Sfx
         return clip;
     }
 
-    // Свой звук из Resources/Sounds важнее синтезированного.
+    static void EnsureSynthesized()
+    {
+        if (clips != null) return;
+        Synthesize();
+        clips = new Dictionary<string, AudioClip>();
+        foreach (var pair in baked)
+        {
+            var clip = AudioClip.Create(pair.Key, pair.Value.Length, 1, Rate, false);
+            clip.SetData(pair.Value, 0);
+            clips[pair.Key] = clip;
+        }
+        baked = null;
+    }
+
+    // Свой звук из Resources/Sounds важнее синтезированного. Если файл не найден, используется синтезированный клип.
     static AudioClip Make(string name)
     {
         var custom = Resources.Load<AudioClip>(CustomFolder + "/" + name);
-        return custom != null ? custom : clips[name];
+        if (custom != null) return custom;
+        EnsureSynthesized();
+        return clips != null && clips.TryGetValue(name, out var clip) ? clip : null;
     }
 
     static void Synthesize()
```

### Трассировка вызовов и логики

1. **`Prepare()`**:
   - Вызывается в `GameManager.Awake()` (строка 83).
   - Проверяет `!Ready` (`swoosh == null`). Если звуки не загружены, синхронно вызывает `Publish()`.
   - Не порождает фоновых потоков `Task.Run` и не держит дескрипторов задач.
2. **`Build(Transform parent)`**:
   - Вызывается в `GameManager.Awake()` (строка 85).
   - Создаёт объект `SFX` и пул `AudioSource` (16 голосов + ambient loop + wind loop + music).
   - Вызывает `Publish()`.
   - Не вызывает блокирующих методов ожидания (`job.Wait()`).
   - Назначает зацикленные звуки: `loop.clip = ambient; loop.Play(); wind.clip = swoosh; wind.Play();`.
3. **`Publish()`**:
   - Для всех 19 звуков вызывает `Make(soundName)` и дубли через `Takes()`.
   - Назначает статические поля `swoosh`, `squelch`, `fizz`, `crack`, `thud`, `magma`, `ding`, `bells`, `note`, `combo`, `start`, `gameOver`, `bomb`, `miss`, `toss`, `split`, `frenzy`, `transmute`, `ambient`.
4. **`Make(string name)`**:
   - Сначала запрашивает ассет из ресурсов: `Resources.Load<AudioClip>("Sounds/" + name)`.
   - Если файл есть на диске (в проекте присутствуют все файлы `Acid1-3.wav`, `Ambient.wav`, `Bells.wav`, `Bomb.wav`, `Combo.wav`, `Crack.wav`, `Ding.wav`, `Fizz.wav`, `Frenzy.wav`, `GameOver.wav`, `Magma.wav`, `Magma1.wav`, `Mercury1-4.wav`, `Miss.wav`, `Note.wav`, `Split.wav`, `Squelch.wav`, `Start.wav`, `Swoosh.wav`, `Thud.wav`, `Toss.wav`, `Transmute.wav`), он возвращается **немедленно**.
   - `EnsureSynthesized()` вызывается **только** при отсутствии ассета на диске (`custom == null`).
5. **`EnsureSynthesized()`**:
   - Проверяет `if (clips != null) return;` (идемпотентность — процедурный синтез выполняется максимум один раз за сессию).
   - Синхронно вызывает `Synthesize()`, формируя 19 буферов в `baked`.
   - Создаёт `AudioClip.Create(...)`, заполняет данными через `clip.SetData` и сохраняет в словарь `clips`.
   - Очищает `baked = null` для освобождения оперативной памяти.
6. **`Synthesize()` и `Render()`**:
   - `Synthesize()` генерирует математические волны звуков (детерминированный PRNG `new Random(7)`).
   - `Render()` используется исключительно утилитой редактора `AlchemiaTools.ExportSounds(bool overwrite)` для генерации и перезаписи WAV-файлов на диск. Логика `Render()` полностью сохранена и не сломана.

---

## 3. Проверка критериев приёмки спецификации

| Критерий | Результат | Анализ и доказательства |
|---|:---:|---|
| **Отсутствие `Task.Run` и `.Wait()`** | **СОБЛЮДЕНО** | `git grep -n "Task" Assets/Scripts/Sfx.cs` и `git grep -n "Wait" Assets/Scripts/Sfx.cs` возвращают 0 совпадений. Библиотека `System.Threading.Tasks` удалена из `using`. |
| **Пропуск синтеза при наличии ассетов** | **СОБЛЮДЕНО** | `Make()` проверяет `Resources.Load` первой строкой. Если клип найден, происходит мгновенный `return custom`. `EnsureSynthesized()` и `Synthesize()` не вызываются. |
| **Корректность fallback при отсутствии файла** | **СОБЛЮДЕНО** | При отсутствии любого WAV-файла срабатывает `EnsureSynthesized()`, который наполняет словарь `clips`. Доступ выполняется безопасно через `clips.TryGetValue(name, out var clip)`. |
| **Корректность семантики `Ready`** | **СОБЛЮДЕНО** | `Ready` возвращает `swoosh != null`. Поскольку `swoosh` инициализируется первой строкой `Publish()`, после выполнения `Publish()` свойство `Ready` истинно, блокируя повторные вызовы в `Prepare()`. |
| **Устойчивость к повторным вызовам** | **СОБЛЮДЕНО** | `Prepare()` защищён guard-проверкой `if (!Ready)`. Повторные вызовы `Publish()` в `Build()` безопасны, так как лишь обновляют ссылки на кэшированные ассеты. |
| **Защита от NullReferenceException** | **СОБЛЮДЕНО** | В оригинале `clips[name]` бросал `KeyNotFoundException` при отсутствующем ключе. В новой версии используется `clips != null && clips.TryGetValue(name, out var clip) ? clip : null`. Метод `Play()` проверяет `sources == null || sources[0] == null`. |
| **Сохранение семантики звуковых событий** | **СОБЛЮДЕНО** | Все публичные методы `Sfx` (`Slice`, `Bomb`, `ShellCrack`, `GameOver`, `Combo`, `Wind`, `Tick`, `Music`, `Start`, `Split`, `Frenzy`, `Record`, `Transmute`, `Toss`, `Volume`) сохранили сигнатуры, уровни громкости, формулы высоты тона и порядок воспроизведения. |
| **Работоспособность редакторских инструментов** | **СОБЛЮДЕНО** | `AlchemiaTools.cs:83` вызывает `Sfx.Render()`. Метод `Render()` вызывает `Synthesize()`, возвращает `baked` и очищает его. В верификационном тесте получено ровно 19 буферов. |

---

## 4. Сверка утверждений итогового отчёта с фактически проверенным

| Утверждение из отчёта | Категория проверки | Фактическое подтверждение |
|---|---|---|
| Устранён дедлок WebGL в `Sfx.cs` | **Фактически подтверждено анализом кода** | Поток выполнения полностью линеаризован на главном потоке, блокировки монитора/потока (`.Wait()`) и фоновый планировщик (`Task.Run`) полностью удалены. |
| Компиляция скриптов в Unity без ошибок (Exit code 0) | **Фактически подтверждено запуском** | Запущен батчмод Unity 6.6 (`unity run . -- -nographics`), компиляция скриптов выполнена за 4026 мс, завершение с кодом 0. |
| Безопасная инициализация рантайма без исключений | **Фактически подтверждено запуском** | Выполнен вызов `Sfx.Prepare()`, `Sfx.Build()`, всех методов воспроизведения звуков и `Sfx.Render()`. Получен Exit code 0, 0 исключений. |
| «0 мс задержки на старте» | **Логически выведено, не замерено микропрофилировщиком** | Логически обосновано (пропуск циклов генерации 1.1 млн сэмплов в пользу загрузки ресурсов), но профилирование в микросекундах через Unity Profiler не проводилось. |
| «Звук воспроизводится на слух» | **Не подтверждено** | В батчмоде с флагом `-nographics` аудиоустройство не инициализируется и человеческий слуховой контроль не осуществлялся. |
| «Сборка WebGL работает» | **Не подтверждено (билд не выполнялся)** | Полноценный WebGL билд на данном этапе не собирался. |

---

## 5. Проверка возможностей сборки WebGL на данном этапе

### Анализ команд Unity CLI
В рамках аудита выполнен запрос `unity build --list-targets` и `unity build --list-profiles`:
- Для платформы WebGL выведено требование:
  `WebGL: Requires --execute-method, or a Unity 6+ build profile (--profile)`
  `No build profiles found in Assets/Settings/Build Profiles.`

### Почему WebGL билд не был выполнен:
1. В проекте на текущий момент отсутствуют C#-скрипты сборщика с точкой входа `BuildPipeline.BuildPlayer` (поиск `BuildPlayer` по проекту вернул 0 результатов).
2. В проекте отсутствуют профили сборки Unity 6 (`Assets/Settings/Build Profiles/`).
3. По инструкции аудита:
   > *«Не придумывай команды и не создавай постоянные Build Profiles или build-скрипты только ради получения положительного результата. Если сборка невозможна без изменения проекта, остановись и объясни, что именно требуется.»*
4. Настройка сборки WebGL, профилей и сжатия выделена в отдельную задачу бэклога: **`CORE-04: Настройки WebGL-сборки и сжатия`**.

---

## 6. Выявленные замечания и риски регрессии

### Критические дефекты
- **Не обнаружены.**

### Некритические замечания (наблюдения для последующих задач)
1. **Двойной вызов `Publish()` на старте игры**:
   В `GameManager.Awake()` строка 83 вызывает `Sfx.Prepare()`, который вызывает `Publish()`. Затем строка 85 вызывает `Sfx.Build(transform)`, который также содержит безусловный вызов `Publish()`. Это не приводит к ошибкам, так как `Resources.Load` возвращает ссылки из кэша памяти, но вызов дублируется.
2. **Отсутствие паузы звука при сворачивании окна**:
   В текущей версии `Sfx.Volume` управляет `AudioListener.volume`, но игра пока не перехватывает `OnApplicationFocus`/`OnApplicationPause` и не вызывает `AudioListener.pause = true`. Это зафиксировано как отдельная следующая задача **`CORE-02`**.

### Оценка риска регрессии
- **Низкий (Low)**. Изменения локализованы исключительно в классе `Sfx.cs`. Все внешние контракты и публичные интерфейсы сохранены.

---

## 7. Итоговое решение

### **ВЕРДИКТ: PASS WITH LIMITATIONS**

**Обоснование**:
- Требования задачи **`CORE-01`** выполнены полностью и корректно: WebGL-дедлок устранён, потокобезопасность обеспечена, фолбэки работают, компиляция и рантайм-вызовы подтверждены автоматическими тестами.
- Ограничение заключается в том, что физическая WebGL-компиляция (WASM) и проверка звука в браузере требуют выполнения задач **`CORE-02`** (пауза звука по требованиям платформы) и **`CORE-04`** (профиль сборки WebGL).

Переход к выполнению задачи **`CORE-02`** является безопасным и обоснованным.
