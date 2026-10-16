using System.Collections.Generic;
using UnityEngine;

// Звуки игры. Каждый звук — файл WAV в Assets/Resources/Sounds: его можно открыть, отредактировать или заменить своим.
// Формулы ниже — запасной синтезатор: если файла нет, звук создаётся кодом. Исходные файлы создаются из этих же
// формул через меню редактора «Алхимия → Звуки».
public static class Sfx
{
    public const int Rate = 44100;
    public const string CustomFolder = "Sounds";
    const string VolumeKey = "alchemia.volume";
    const float MusicLevel = 0.5f, AmbientLevel = 0.5f;   // громкость музыки и фона относительно эффектов
    const float WindLoop = 2f, WindSeam = 0.12f;
    const int Voices = 16;
    const float Tau = Mathf.PI * 2f;

    // Мажорная пентатоника (C–D–E–G–A) в полутонах: каждый следующий срез в серии звучит на ступень выше.
    static readonly int[] Ladder = { 0, 2, 4, 7, 9, 12, 14, 16, 19, 21, 24 };

    public static float Slow = 1f; // в замедлении звук «проседает» вместе со временем

    static AudioClip swoosh, squelch, fizz, crack, thud, magma, ding, bells, note, combo, bomb, miss, toss, split,
        start, gameOver, frenzy, transmute, ambient;
    static AudioSource[] sources;
    static AudioSource loop, wind, music;
    static float musicTarget;
    // Дубли: несколько записей одного и того же звука. Игра берёт случайный, поэтому срезы подряд не звучат одинаково.
    static AudioClip[] acidTakes, mercuryTakes, magmaTakes;
    static AudioClip lastTake;
    static int next, tossFrame;
    static float windLevel;
    static System.Random rng;
    static Dictionary<string, float[]> baked;
    static Dictionary<string, AudioClip> clips;
    static bool isPaused;

    static bool Ready { get { return swoosh != null; } }

    // Подготовка звуков: загрузка аудиоклипов из Resources или процедурный синтез при их отсутствии.
    public static void Prepare()
    {
        if (!Ready) Publish();
    }

    // Сырые буферы всех звуков — для выгрузки в WAV из меню редактора.
    public static Dictionary<string, float[]> Render()
    {
        Synthesize();
        var result = baked;
        baked = null;
        return result;
    }

    public static void Build(Transform parent)
    {
        var go = new GameObject("SFX");
        go.transform.SetParent(parent, false);
        sources = new AudioSource[Voices];
        for (int i = 0; i < Voices; i++)
        {
            sources[i] = go.AddComponent<AudioSource>();
            sources[i].playOnAwake = false;
            sources[i].spatialBlend = 0f;
        }
        loop = go.AddComponent<AudioSource>();
        loop.playOnAwake = false;
        loop.loop = true;
        loop.volume = AmbientLevel;
        wind = go.AddComponent<AudioSource>();
        wind.playOnAwake = false;
        wind.loop = true;
        wind.volume = 0f;
        windLevel = 0f;
        // Музыка боя — файл Assets/Resources/Music/Gameplay. Играет только во время попытки.
        music = go.AddComponent<AudioSource>();
        music.playOnAwake = false;
        music.loop = true;
        music.volume = 0f;
        music.clip = Resources.Load<AudioClip>("Music/Gameplay");
        musicTarget = 0f;
        AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, 0.8f);
        Slow = 1f;

        // Свои файлы подхватываются при каждом запуске, поэтому замена звука не требует перезапуска редактора.
        Publish();

        loop.clip = Resources.Load<AudioClip>("Music/Menu") ?? ambient;
        loop.Play();
        wind.clip = swoosh;
        wind.Play();
    }

    static float White() { return (float)rng.NextDouble() * 2f - 1f; }

    static void Publish()
    {
        swoosh = Make("Swoosh");
        squelch = Make("Squelch");
        fizz = Make("Fizz");
        crack = Make("Crack");
        thud = Make("Thud");
        magma = Make("Magma");
        ding = Make("Ding");
        bells = Make("Bells");
        note = Make("Note");
        combo = Make("Combo");
        start = Make("Start");
        gameOver = Make("GameOver");
        bomb = Make("Bomb");
        miss = Make("Miss");
        toss = Make("Toss");
        split = Make("Split");
        frenzy = Make("Frenzy");
        transmute = Make("Transmute");
        ambient = Make("Ambient");
        acidTakes = Takes("Acid");
        mercuryTakes = Takes("Mercury");
        magmaTakes = Takes("Magma");
    }

    // Дубли лежат в Resources/Sounds под именами с номером: Acid1, Acid2, Acid3… Сколько файлов — столько и дублей.
    static AudioClip[] Takes(string name)
    {
        var list = new List<AudioClip>();
        for (int i = 1; ; i++)
        {
            var clip = Resources.Load<AudioClip>(CustomFolder + "/" + name + i);
            if (clip == null) break;
            list.Add(clip);
        }
        return list.ToArray();
    }

    // Случайный дубль, но не тот же, что звучал только что.
    static AudioClip Take(AudioClip[] takes)
    {
        AudioClip clip = takes[Random.Range(0, takes.Length)];
        if (clip == lastTake && takes.Length > 1) clip = takes[(System.Array.IndexOf(takes, clip) + 1) % takes.Length];
        lastTake = clip;
        return clip;
    }

    static void EnsureSynthesized()
    {
        if (clips != null) return;
        Synthesize();
        clips = new Dictionary<string, AudioClip>();
        foreach (var pair in baked)
        {
            var clip = AudioClip.Create(pair.Key, pair.Value.Length, 1, Rate, false);
            clip.SetData(pair.Value, 0);
            clips[pair.Key] = clip;
        }
        baked = null;
    }

    // Свой звук из Resources/Sounds важнее синтезированного. Если файл не найден, используется синтезированный клип.
    static AudioClip Make(string name)
    {
        var custom = Resources.Load<AudioClip>(CustomFolder + "/" + name);
        if (custom != null) return custom;
        EnsureSynthesized();
        return clips != null && clips.TryGetValue(name, out var clip) ? clip : null;
    }

    static void Synthesize()
    {
        baked = new Dictionary<string, float[]>();
        rng = new System.Random(7);
        float lp = 0f, ph = 0f;

        // Свист клинка: мягкий воздушный шум без низов, зацикленный. Он звучит не «выстрелами», а непрерывно,
        // пока клинок движется; громкость и высоту задаёт скорость взмаха.
        float low = 0f;
        Bake("Swoosh", WindLoop + WindSeam, t =>
        {
            lp += (White() - lp) * 0.3f;
            low += (lp - low) * 0.03f;
            return (lp - low) * (0.8f + 0.2f * Mathf.Sin(Tau * 1.5f * t)) * 1.6f;
        }, true);
        baked["Swoosh"] = Seamless(baked["Swoosh"], (int)(WindSeam * Rate));

        // Сочный «чпок»: падающий тон, мокрый шум и низкий толчок.
        ph = 0f; lp = 0f;
        Bake("Squelch", 0.24f, t =>
        {
            ph += Tau * Mathf.Lerp(640f, 150f, Mathf.Sqrt(t / 0.24f)) / Rate;
            lp += (White() - lp) * 0.35f;
            float wet = 1f + 0.45f * Mathf.Sin(Tau * 41f * t);
            return (Mathf.Sin(ph) * Mathf.Exp(-t * 24f) * 0.5f + lp * Mathf.Exp(-t * 34f) * 0.75f) * wet
                 + Mathf.Sin(Tau * 92f * t) * Mathf.Exp(-t * 20f) * 0.5f;
        });

        // Шипение кислоты: мягкий «пшш» без резких верхов, пузырящаяся громкость и несколько мелких пузырьков.
        // Это негромкий хвост под желейным «чпоком», а не самостоятельный удар.
        lp = 0f;
        float fizzLow = 0f;
        var popAt = new float[5];
        var popHz = new float[5];
        for (int i = 0; i < popAt.Length; i++)
        {
            popAt[i] = 0.05f + 0.07f * i + (float)rng.NextDouble() * 0.04f;
            popHz[i] = 700f + (float)rng.NextDouble() * 900f;
        }
        Bake("Fizz", 0.5f, t =>
        {
            lp += (White() - lp) * 0.3f;
            fizzLow += (lp - fizzLow) * 0.06f;
            float bubble = 0.6f + 0.4f * Mathf.Sin(Tau * 17f * t + Mathf.Sin(Tau * 5f * t) * 2f);
            float v = (lp - fizzLow) * Mathf.Min(1f, t * 60f) * Mathf.Exp(-t * 9f) * bubble * 1.5f;
            for (int i = 0; i < popAt.Length; i++)
            {
                float local = t - popAt[i];
                if (local > 0f && local < 0.04f) v += Mathf.Sin(Tau * popHz[i] * (1f + 6f * local) * local) * Mathf.Sin(Mathf.PI * local / 0.04f) * 0.16f;
            }
            return v;
        });

        // Звон расколотого бокала: негармоничные высокие призвуки и щелчок.
        Bake("Crack", 0.3f, t =>
            (Mathf.Sin(Tau * 2350f * t) * Mathf.Exp(-t * 28f) + Mathf.Sin(Tau * 3720f * t) * Mathf.Exp(-t * 38f) * 0.8f
           + Mathf.Sin(Tau * 5210f * t) * Mathf.Exp(-t * 50f) * 0.6f + Mathf.Sin(Tau * 6890f * t) * Mathf.Exp(-t * 65f) * 0.45f) * 0.22f
           + White() * Mathf.Exp(-t * 200f) * 0.7f);

        ph = 0f; lp = 0f;
        Bake("Thud", 0.2f, t =>
        {
            ph += Tau * Mathf.Lerp(160f, 60f, t / 0.2f) / Rate;
            lp += (White() - lp) * 0.2f;
            return Mathf.Sin(ph) * Mathf.Exp(-t * 20f) * 0.8f + lp * Mathf.Exp(-t * 30f) * 0.6f;
        });

        // Тяжёлый глухой удар, треск раскалённого камня и горячее шипение пара.
        ph = 0f; lp = 0f;
        float crackle = 0f;
        Bake("Magma", 0.7f, t =>
        {
            ph += Tau * (42f + 60f * Mathf.Exp(-t * 14f)) / Rate;
            float w = White();
            lp += (w - lp) * 0.6f;
            // Треск: редкие сухие щелчки, которые к концу звука стихают.
            if (t > 0.03f && rng.NextDouble() < 0.0022) crackle = 0.5f + 0.5f * (float)rng.NextDouble();
            crackle *= 0.9f;
            float v = Mathf.Sin(ph) * Mathf.Exp(-t * 8f) * 1.1f + (w - lp) * Mathf.Min(1f, t * 30f) * Mathf.Exp(-t * 5f) * 0.28f
                    + crackle * w * Mathf.Exp(-t * 3.5f) * 0.55f;
            return v / (1f + Mathf.Abs(v) * 0.4f);
        });

        // Металлический «дзынь»: негармоничные обертоны с разной скоростью затухания.
        Bake("Ding", 0.7f, t =>
            (Mathf.Sin(Tau * 1480f * t) * Mathf.Exp(-t * 7f) + Mathf.Sin(Tau * 4085f * t) * Mathf.Exp(-t * 10f) * 0.6f
           + Mathf.Sin(Tau * 7990f * t) * Mathf.Exp(-t * 16f) * 0.35f + Mathf.Sin(Tau * 13200f * t) * Mathf.Exp(-t * 26f) * 0.2f) * 0.3f
           + White() * Mathf.Exp(-t * 260f) * 0.5f);

        // Перезвон колоколов (мажорный аккорд) и россыпь монет.
        var coinAt = new float[7];
        var coinHz = new float[7];
        for (int i = 0; i < coinAt.Length; i++)
        {
            coinAt[i] = 0.12f + 0.1f * i + (float)rng.NextDouble() * 0.05f;
            coinHz[i] = 3200f + (float)rng.NextDouble() * 1800f;
        }
        Bake("Bells", 1.7f, t =>
        {
            float v = Bell(t, 1046.5f) + Bell(t - 0.06f, 1318.5f) + Bell(t - 0.12f, 1568f) + Bell(t - 0.18f, 2093f);
            for (int i = 0; i < coinAt.Length; i++)
            {
                float local = t - coinAt[i];
                if (local > 0f) v += Mathf.Sin(Tau * coinHz[i] * local) * Mathf.Exp(-local * 26f) * 0.35f;
            }
            return v * 0.26f;
        });

        // Нота «лесенки»: чистый стеклянный тон, высота задаётся при воспроизведении.
        Bake("Note", 0.45f, t =>
            (Mathf.Sin(Tau * 523.25f * t) + 0.35f * Mathf.Sin(Tau * 1046.5f * t) + 0.12f * Mathf.Sin(Tau * 1569.75f * t))
            * Mathf.Min(1f, t * 400f) * Mathf.Exp(-t * 9f) * 0.5f);

        Bake("Combo", 0.6f, t => Notes(t, 0.07f, 8f, 1046.5f, 1318.5f, 1568f, 2093f) * 0.5f);
        Bake("Start", 0.6f, t => Notes(t, 0.07f, 7f, 523.25f, 659.25f, 784f, 1046.5f) * 0.5f);
        Bake("GameOver", 1.2f, t => Notes(t, 0.22f, 4.5f, 392f, 311.1f, 261.6f, 196f) * 0.55f);

        // Детонация: саб-бас, грохот и звон контузии в ушах.
        ph = 0f; lp = 0f;
        Bake("Bomb", 1.5f, t =>
        {
            ph += Tau * (34f + 110f * Mathf.Exp(-t * 3f)) / Rate;
            lp += (White() - lp) * 0.15f;
            float v = Mathf.Sin(ph) * Mathf.Exp(-t * 3.5f) * 1.2f + lp * Mathf.Exp(-t * 6f) * 2.4f;
            return v / (1f + Mathf.Abs(v)) * 1.35f + Mathf.Sin(Tau * 3650f * t) * Mathf.Min(1f, t * 5f) * Mathf.Exp(-t * 2.4f) * 0.07f;
        });

        ph = 0f; lp = 0f;
        Bake("Miss", 0.4f, t =>
        {
            ph += Tau * Mathf.Lerp(280f, 110f, t / 0.4f) / Rate;
            lp += (White() - lp) * 0.12f;
            return (Mathf.Sin(ph) + 0.4f * Mathf.Sin(2f * ph)) * Mathf.Exp(-t * 8f) * 0.5f + lp * Mathf.Exp(-t * 14f) * 0.9f;
        });

        ph = 0f; lp = 0f;
        Bake("Toss", 0.14f, t =>
        {
            float k = Mathf.Sin(Mathf.PI * t / 0.14f);
            ph += Tau * Mathf.Lerp(190f, 420f, t / 0.14f) / Rate;
            lp += (White() - lp) * 0.05f;
            return (lp * 1.6f + Mathf.Sin(ph) * 0.3f) * k;
        });

        // Деление: упругий «блуп» с подъёмом тона и следом два быстрых маленьких «поп» — по одному на мини-слайма.
        ph = 0f;
        Bake("Split", 0.32f, t =>
        {
            float v = 0f;
            if (t < 0.17f)
            {
                ph += Tau * (170f + 620f * (t / 0.17f) * (t / 0.17f)) / Rate;
                v = (Mathf.Sin(ph) + 0.3f * Mathf.Sin(ph * 2f)) * Mathf.Sin(Mathf.PI * t / 0.17f) * 0.6f;
            }
            for (int i = 0; i < 2; i++)
            {
                float local = t - 0.17f - 0.07f * i;
                if (local > 0f && local < 0.055f)
                    v += Mathf.Sin(Tau * (i == 0 ? 880f : 1175f) * (1f + 4f * local) * local) * Mathf.Sin(Mathf.PI * local / 0.055f) * 0.5f;
            }
            return v;
        });

        // Резонанс котлов: нарастающий гул и звон на вершине.
        ph = 0f;
        Bake("Frenzy", 1.1f, t =>
        {
            ph += Tau * (200f * Mathf.Pow(4f, Mathf.Min(1f, t / 0.6f))) / Rate;
            float saw = Mathf.Sin(ph) + Mathf.Sin(2f * ph) / 2f + Mathf.Sin(3f * ph) / 3f + Mathf.Sin(4f * ph) / 4f;
            float rise = saw * Mathf.Min(1f, t * 8f) * Mathf.Clamp01((0.68f - t) * 12f) * (0.7f + 0.3f * Mathf.Sin(Tau * 18f * t)) * 0.3f;
            return rise + (Bell(t - 0.58f, 1568f) + Bell(t - 0.58f, 2093f) + Bell(t - 0.64f, 2637f)) * 0.3f;
        });

        // Трансмутация: волшебное превращение — звонкий тон взлетает вверх, а над ним мерцают колокольчики.
        ph = 0f;
        float ph2 = 0f;
        var glintAt = new float[7];
        var glintHz = new float[7];
        for (int i = 0; i < glintAt.Length; i++)
        {
            glintAt[i] = 0.28f + 0.1f * i + (float)rng.NextDouble() * 0.05f;
            glintHz[i] = 2200f + (float)rng.NextDouble() * 2200f;
        }
        Bake("Transmute", 1.1f, t =>
        {
            float hz = 330f * Mathf.Pow(4f, Mathf.Min(1f, t / 0.5f));
            ph += Tau * hz / Rate;
            ph2 += Tau * hz * 1.012f / Rate;
            float v = (Mathf.Sin(ph) + Mathf.Sin(ph2) + 0.35f * Mathf.Sin(2f * ph)) * Mathf.Min(1f, t * 30f) * Mathf.Exp(-t * 2.4f) * 0.26f;
            for (int i = 0; i < glintAt.Length; i++)
            {
                float local = t - glintAt[i];
                if (local > 0f) v += Mathf.Sin(Tau * glintHz[i] * local) * Mathf.Min(1f, local * 400f) * Mathf.Exp(-local * 16f) * 0.16f;
            }
            return v;
        });

        // Атмосфера меню и лаборатории: благородный кристальный гармонический эмбиент с мягкими переливами колокольчиков.
        // Раздражающее бульканье котла полностью удалено, заменено на умиротворяющую пентатоническую тему.
        const float len = 12f;
        Bake("Ambient", len + WindSeam, t => 0f, true);
        float[] bed = Seamless(baked["Ambient"], (int)(WindSeam * Rate));
        baked["Ambient"] = bed;

        // Теплый струнный/стеклянный аккорд лаборатории (G-dur: G3, D4, G4, B4, D5) с фазовым хорусом
        float[] padFreqs = { 196.00f, 293.66f, 392.00f, 493.88f, 587.25f };
        for (int s = 0; s < bed.Length; s++)
        {
            float t = s / (float)Rate;
            float swell = 0.5f - 0.5f * Mathf.Cos(Tau * t / 6f);
            float v = 0f;
            for (int i = 0; i < padFreqs.Length; i++)
            {
                float f = padFreqs[i];
                v += (Mathf.Sin(Tau * f * t) + Mathf.Sin(Tau * (f + 0.18f) * t)) * (0.004f / (1f + 0.35f * i));
            }
            bed[s] += v * swell;
        }

        // Мягкие кристальные колокольчики (G-major пентатоника: D5, B4, G5, E5, D5)
        float[] chimeTimes = { 1.0f, 3.2f, 5.8f, 8.0f, 10.2f };
        float[] chimePitches = { 587.33f, 493.88f, 783.99f, 659.25f, 587.33f };

        for (int i = 0; i < chimeTimes.Length; i++)
        {
            float startTime = chimeTimes[i];
            float pitch = chimePitches[i];
            int from = (int)(startTime * Rate);
            int count = (int)(1.6f * Rate);

            for (int s = 0; s < count && from + s < bed.Length; s++)
            {
                float local = s / (float)Rate;
                float attack = Mathf.Clamp01(local / 0.015f);
                float decay = Mathf.Exp(-local * 2.8f);
                float env = attack * decay;

                // Основной тон + тонкий хрустальный обертон
                float sample = Mathf.Sin(Tau * pitch * local) + 0.28f * Mathf.Sin(Tau * pitch * 2.01f * local);
                bed[from + s] += sample * env * 0.024f;
            }
        }
    }

    // Общая громкость игры: ползунок в меню и на паузе. Запоминается между запусками.
    public static float Volume
    {
        get { return AudioListener.volume; }
        set
        {
            AudioListener.volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(VolumeKey, AudioListener.volume);
        }
    }

    // Глушение всего звука (при потере фокуса вкладки или показе рекламы)
    public static void SetMute(bool mute)
    {
        AudioListener.pause = mute;
    }

    // Внутриигровая пауза: глушит фоновые шумы и эффекты, приглушает музыку
    public static void Pause(bool on)
    {
        isPaused = on;
        Music(on ? 0.4f : 1f);
        if (on)
        {
            if (loop != null) loop.volume = 0f;
            if (wind != null) wind.volume = 0f;
        }
    }

    // Насколько громко должна звучать музыка боя: 1 — в игре, меньше — на паузе и после поражения, 0 — в меню.
    public static void Music(float level)
    {
        if (music == null || music.clip == null) return;
        musicTarget = level;
        if (level > 0f && !music.isPlaying)
        {
            music.time = 0f;
            music.Play();
        }
    }

    // Музыка плавно нарастает и стихает; когда она играет, фон лаборатории уходит на задний план.
    public static void Tick(float dt)
    {
        if (music == null) return;
        music.volume = Mathf.MoveTowards(music.volume, MusicLevel * musicTarget, dt * 0.9f);
        if (music.isPlaying && musicTarget <= 0f && music.volume <= 0f) music.Stop();
        if (isPaused)
        {
            if (loop != null) loop.volume = 0f;
            if (wind != null) wind.volume = 0f;
        }
        else if (loop != null)
        {
            loop.volume = AmbientLevel * (1f - 0.75f * Mathf.Clamp01(music.volume / MusicLevel));
        }
    }

    // Свист следует за клинком: нарастает мгновенно, затихает чуть медленнее. Короткий взмах — короткий звук,
    // длинный — длинный, медленное движение беззвучно.
    public static void Wind(float level, float dt)
    {
        if (wind == null || isPaused) return;
        windLevel = Mathf.MoveTowards(windLevel, level, dt * (level > windLevel ? 16f : 7f));
        wind.volume = 0.3f * windLevel * (0.4f + 0.6f * windLevel);
        wind.pitch = Mathf.Lerp(0.7f, 1.35f, windLevel);
    }

    public static void Bomb() { Play(bomb, 1f, Random.Range(0.92f, 1.05f)); }
    public static void Miss() { Play(miss, 0.8f, 1f); }
    public static void GameOver() { Play(gameOver, 0.8f, 1f); }
    public static void Start() { Play(start, 0.6f, 1f); }
    public static void Split() { Play(split, 0.75f, Random.Range(0.92f, 1.1f)); }
    public static void Frenzy() { Play(frenzy, 0.75f, 1f); }
    public static void Record() { Play(bells, 0.7f, 1f); }

    // Разрублен золотой король: колокольчики — сам срез, превращение — бомбы становятся золотом.
    public static void Transmute()
    {
        Play(bells, 0.8f, 1f);
        Play(transmute, 0.7f, 1f);
    }

    public static void Toss()
    {
        // Залп из нескольких объектов звучит одним броском, а не стеной шума.
        if (tossFrame == Time.frameCount) return;
        tossFrame = Time.frameCount;
        Play(toss, 0.28f, Random.Range(0.85f, 1.15f));
    }

    // Клинок ударил по каменной скорлупе жеоды: глухой удар с жидким откликом и короткий треск камня.
    public static void ShellCrack(int chain)
    {
        Play(thud, 0.85f, Random.Range(0.95f, 1.1f));
        Play(crack, 0.3f, 0.8f);
        Step(chain);
    }

    // У каждого слайма свой тембр среза, а ступень пентатоники показывает длину серии.
    public static void Slice(Kind kind, int chain)
    {
        float jitter = Random.Range(-0.04f, 0.04f);
        switch (kind)
        {
            case Kind.Magma:     // тяжёлый горячий удар: своя запись, а без неё — синтезированный с «чпоком»
                if (magmaTakes.Length > 0) Play(Take(magmaTakes), 0.65f, 1f + jitter);
                else
                {
                    Play(magma, 0.85f, 1f + jitter);
                    Play(squelch, 0.35f, 0.7f + jitter);
                }
                break;
            case Kind.Geode:     // аметист: сухой кристаллический треск и звон осколков
                Play(crack, 0.8f, 1f + jitter);
                Play(squelch, 0.45f, 0.9f + jitter);
                break;
            case Kind.Mercury:   // жидкий металл: свои записи, а без них — звон
                if (mercuryTakes.Length > 0) Play(Take(mercuryTakes), 0.42f, 1f + jitter);
                else Play(ding, 0.7f, 1f + jitter);
                break;
            case Kind.Nugget:
                Play(ding, 0.5f, 1.5f + jitter);
                Play(squelch, 0.5f, 1.25f);
                break;
            case Kind.King:      // колокольчики и превращение звучат следом — см. Transmute
                Play(squelch, 0.6f, 1.2f);
                break;
            case Kind.Blue:      // хлопок деления звучит, когда появляются мини-слаймы — см. Split
                Play(squelch, 0.7f, 0.8f + jitter);
                break;
            case Kind.Mini:
                Play(squelch, 0.7f, 1.3f + jitter);
                break;
            default:             // обычный зелёный: свои записи, а без них — желейный «чпок» с тихим шипением
                if (acidTakes.Length > 0) Play(Take(acidTakes), 0.65f, 1f + jitter);
                else
                {
                    Play(squelch, 0.8f, 1f + jitter);
                    Play(fizz, 0.22f, 1f + jitter);
                }
                break;
        }
        Step(chain);
    }

    public static void Combo(int count)
    {
        Play(combo, 0.7f, Mathf.Pow(2f, Ladder[Mathf.Clamp(count - 3, 0, 4)] / 12f));
    }

    static void Step(int chain)
    {
        int i = Mathf.Clamp(chain - 1, 0, Ladder.Length - 1);
        Play(note, 0.5f, Mathf.Pow(2f, Ladder[i] / 12f));
    }

    static void Play(AudioClip clip, float volume, float pitch)
    {
        if (isPaused) return;
        if (sources == null || sources[0] == null) return;
        var src = sources[next];
        next = (next + 1) % Voices;
        src.clip = clip;
        src.volume = volume;
        src.pitch = pitch * Slow;
        src.Play();
    }

    // Колокольный тон: основной и два призвука.
    static float Bell(float t, float hz)
    {
        if (t <= 0f) return 0f;
        return (Mathf.Sin(Tau * hz * t) + 0.5f * Mathf.Sin(Tau * hz * 2f * t) * Mathf.Exp(-t * 3f)
              + 0.3f * Mathf.Sin(Tau * hz * 2.76f * t) * Mathf.Exp(-t * 6f)) * Mathf.Min(1f, t * 300f) * Mathf.Exp(-t * 3.2f);
    }

    // Арпеджио из затухающих синусов: каждая нота вступает через step секунд.
    static float Notes(float t, float step, float decay, params float[] freqs)
    {
        float v = 0f;
        for (int i = 0; i < freqs.Length; i++)
        {
            float local = t - i * step;
            if (local < 0f) break;
            float env = Mathf.Min(1f, local * 200f) * Mathf.Exp(-local * decay);
            v += (Mathf.Sin(Tau * freqs[i] * local) + 0.3f * Mathf.Sin(2f * Tau * freqs[i] * local)) * env;
        }
        return v * 0.6f;
    }

    // Делает из отрезка шума бесшовную петлю: хвост длиной seam плавно вливается в начало.
    static float[] Seamless(float[] data, int seam)
    {
        int n = data.Length - seam;
        var result = new float[n];
        for (int i = 0; i < n; i++)
        {
            float k = i < seam ? i / (float)seam : 1f;
            result[i] = data[i] * Mathf.Sqrt(k) + (i < seam ? data[n + i] * Mathf.Sqrt(1f - k) : 0f);
        }
        return result;
    }

    static void Bake(string name, float duration, System.Func<float, float> gen, bool seamless = false)
    {
        int n = (int)(Rate * duration);
        var data = new float[n];
        float peak = 0f;
        for (int i = 0; i < n; i++)
        {
            // Короткое затухание в хвосте убирает щелчок на обрыве; петле оно не нужно.
            float tail = seamless ? 1f : Mathf.Min(1f, (n - i) / 256f);
            data[i] = gen(i / (float)Rate) * tail;
            peak = Mathf.Max(peak, Mathf.Abs(data[i]));
        }
        // Слишком громкий клип ужимаем целиком, а не срезаем верхушки: срез дал бы хрип.
        if (peak > 0.95f)
        {
            float k = 0.95f / peak;
            for (int i = 0; i < n; i++) data[i] *= k;
        }
        baked[name] = data;
    }
}
