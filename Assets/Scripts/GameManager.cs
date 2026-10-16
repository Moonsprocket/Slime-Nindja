using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;

public enum GameState { Story, Menu, Playing, GameOver }

// Правила игры и состояние попытки. Всё остальное (клинок, интерфейс, эффекты, звук) создаётся отсюда,
// поэтому сцене достаточно камеры и этого объекта.
[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public const int MaxLives = 3;
    public const float RestartLock = 0.7f; // защита от случайного рестарта взмахом, которым игрок проиграл

    const string BestKey = "alchemia.best";
    const string StoryKey = "alchemia.story"; // предыстория сама показывается только при первом запуске
    const string ComboKey = "alchemia.combo", SlicedKey = "alchemia.sliced", StreakKey = "alchemia.streak";
    const string EssenceKey = "alchemia.essence";
    const string SelectedBladeKey = "alchemia.blade_selected";
    const string UnlockedBladesKey = "alchemia.blades_unlocked";
    const float InterstitialCooldown = 70f;

    static readonly Color Gold = new Color(1f, 0.85f, 0.3f);
    static readonly Color Mint = new Color(0.62f, 0.98f, 0.88f);  // цвет надписей-наград, тот же, что в интерфейсе

    public struct BladeInfo
    {
        public string id;
        public string name;
        public string rarity;
        public int cost;
        public Color color;

        public BladeInfo(string id, string name, string rarity, int cost, Color color)
        {
            this.id = id;
            this.name = name;
            this.rarity = rarity;
            this.cost = cost;
            this.color = color;
        }
    }

    public static readonly BladeInfo[] Blades = new BladeInfo[]
    {
        new BladeInfo("azure", "Эфирный скальпель", "Базовый", 0, new Color(0.4f, 0.9f, 1f)),
        new BladeInfo("flame", "Пламенный росчерк", "Обычный", 300, new Color(1f, 0.45f, 0.15f)),
        new BladeInfo("emerald", "Изумрудный алхимик", "Обычный", 500, new Color(0.2f, 1f, 0.45f)),
        new BladeInfo("bone", "Костяной серп", "Обычный", 750, new Color(0.94f, 0.93f, 0.9f)),
        new BladeInfo("frost", "Ледяная игла", "Редкий", 1000, new Color(0.55f, 0.91f, 1f)),
        new BladeInfo("thunder", "Громовой коготь", "Редкий", 1500, new Color(1f, 0.94f, 0.3f)),
        new BladeInfo("amethyst", "Аметистовый шип", "Редкий", 2000, new Color(0.7f, 0.35f, 1f)),
        new BladeInfo("shadow", "Теневой клинок", "Эпический", 2500, new Color(0.35f, 0.15f, 0.55f)),
        new BladeInfo("mercury", "Ртутный стилет", "Эпический", 3000, new Color(0.85f, 0.9f, 0.95f)),
        new BladeInfo("sunbeam", "Солнечный луч", "Эпический", 3500, new Color(1f, 0.84f, 0f)),
        new BladeInfo("bloodmoon", "Кровавая луна", "Легендарный", 4500, new Color(1f, 0.15f, 0.25f)),
        new BladeInfo("magnum_opus", "Клинок Философа", "Легендарный", 5000, new Color(1f, 0.4f, 0.8f))
    };

    public static GameManager Instance { get; private set; }
    public static Rect View { get; private set; }        // видимая область в мировых координатах
    public static float Unit { get; private set; } = 1f; // масштаб мира: 1 при высоте экрана 20 единиц
    public const float StartHeight = 0.36f;               // на какой высоте экрана висит стартовый слайм в меню

    public GameState State { get; private set; }
    public int Score { get; private set; }
    public int Lives { get; private set; }
    public int Best { get; private set; }
    public int Sliced { get; private set; }
    public int BestCombo { get; private set; }
    public int BestComboEver { get; private set; }  // лучшее комбо за все попытки
    public int TotalSliced { get; private set; }    // сколько слаймов разрублено за всё время
    public int Streak { get; private set; }         // серия: слаймы подряд без промаха и без взрыва
    public int BestStreak { get; private set; }     // лучшая серия этой попытки
    public int BestStreakEver { get; private set; } // и за все попытки
    public int Essence { get; private set; }        // мета-валюта для кузницы клинков
    public int LastEarnedEssence { get; private set; } // начислено за последний забег
    public int SelectedBladeIndex { get; private set; }
    public bool RevivedThisRun { get; private set; }
    public float FrenzyLeft { get; private set; }   // Alembic Frenzy: очки ×2, но котёл извергается на пределе
    public float FrenzySpan { get; private set; }
    public int FrenzyTier { get; private set; }     // уровень полоски комбо: чем крупнее комбо, тем он выше
    public int FrenzyCombo { get; private set; }    // лучшее комбо этой перегрузки
    public float SlowLeft { get; private set; }     // трансмутация: мир замедлен, рука игрока — нет
    public Hud Hud { get; private set; }
    public bool Paused { get; private set; }        // пауза по правой кнопке мыши

    public Color ActiveBladeColor
    {
        get
        {
            if (SelectedBladeIndex >= 0 && SelectedBladeIndex < Blades.Length)
                return Blades[SelectedBladeIndex].color;
            return Blades[0].color;
        }
    }

    public string ActiveBladeId
    {
        get
        {
            if (SelectedBladeIndex >= 0 && SelectedBladeIndex < Blades.Length)
                return Blades[SelectedBladeIndex].id;
            return Blades[0].id;
        }
    }

    public int Mult { get { return FrenzyLeft > 0f ? 2 : 1; } }
    public bool Empowered { get { return FrenzyLeft > 0f || SlowLeft > 0f; } }

    Camera cam;
    Transform camRig;
    Vector3 camRigBase;
    Spawner spawner;
    Balance balance;    // числа правил и сложности: ассет Assets/Resources/Balance
    ChromaticAberration chroma;

    int chain, chainStroke;
    float chainTimer, stateSince, graceLeft, fireworkTimer, frenzyRest;
    Vector2 chainPos;
    readonly List<Vector2> chainPoints = new List<Vector2>();   // где прошли срезы текущей серии
    int knownRank;      // высшее звание, о котором игрок уже знает: о новом сообщаем прямо в бою
    bool newBest, fanfare, saved;
    float shakeAmp, shakeDuration, shakeLeft;
    float flow = 1f, dipScale = 1f, dipLeft, hitstopLeft, shock;
    float lastInterstitialTime = -100f;
    string unlockedBlades = "azure";

    void Awake()
    {
        Instance = this;
        balance = Balance.Current;
        Time.timeScale = 1f;
        Application.targetFrameRate = Mathf.Max(60, Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value));
        Sliceable.Active.Clear();
        Sliceable.ClearPool();
        Fx.ClearPool();

        cam = Camera.main;
        cam.backgroundColor = Color.black;
        // Если камерой управляет Cinemachine, трясти нужно виртуальную камеру: позицию основной он перезаписывает.
        var vcam = FindAnyObjectByType<CinemachineCamera>();
        camRig = vcam != null && cam.GetComponent<CinemachineBrain>() != null ? vcam.transform : cam.transform;
        camRigBase = camRig.position;
        UpdateView();

        Sfx.Prepare();
        Art.Build();
        Sfx.Build(transform);
        Fx.Build();
        Backdrop.Create();
        BuildPostFx();

        Hud = Hud.Create();
        new GameObject("Blade").AddComponent<Blade>();
        spawner = GetComponent<Spawner>();
        if (spawner == null) spawner = gameObject.AddComponent<Spawner>();

        Best = PlayerPrefs.GetInt(BestKey, 0);
        BestComboEver = PlayerPrefs.GetInt(ComboKey, 0);
        BestStreakEver = PlayerPrefs.GetInt(StreakKey, 0);
        TotalSliced = PlayerPrefs.GetInt(SlicedKey, 0);
        Essence = PlayerPrefs.GetInt(EssenceKey, 0);
        SelectedBladeIndex = PlayerPrefs.GetInt(SelectedBladeKey, 0);
        unlockedBlades = PlayerPrefs.GetString(UnlockedBladesKey, "azure");
    }

    void Start()
    {
        if (PlayerPrefs.GetInt(StoryKey, 0) == 0) ToStory();
        else ToMenu();

        if (YandexSDKManager.Instance != null)
        {
            YandexSDKManager.Instance.GameReady();
            YandexSDKManager.Instance.LoadCloud(OnCloudLoaded);
        }
    }

    void OnCloudLoaded(YandexSDKManager.CloudData cloud)
    {
        if (cloud == null) return;
        bool changed = false;

        // Рекорды и статистика объединяются строго по максимуму
        if (cloud.bestScore > Best) { Best = cloud.bestScore; PlayerPrefs.SetInt(BestKey, Best); changed = true; }
        if (cloud.bestCombo > BestComboEver) { BestComboEver = cloud.bestCombo; PlayerPrefs.SetInt(ComboKey, BestComboEver); changed = true; }
        if (cloud.bestStreak > BestStreakEver) { BestStreakEver = cloud.bestStreak; PlayerPrefs.SetInt(StreakKey, BestStreakEver); changed = true; }
        if (cloud.totalSliced > TotalSliced) { TotalSliced = cloud.totalSliced; PlayerPrefs.SetInt(SlicedKey, TotalSliced); changed = true; }

        // Эссенция: берем максимум (если на другом устройстве заработано больше)
        if (cloud.essence > Essence) { Essence = cloud.essence; PlayerPrefs.SetInt(EssenceKey, Essence); changed = true; }

        // Разблокированные клинки объединяются через HashSet (Set Union) — локальные покупки не теряются
        var currentSet = new HashSet<string>(unlockedBlades.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries));
        currentSet.Add(Blades[0].id); // базовый клинок всегда открыт
        if (!string.IsNullOrEmpty(cloud.unlockedBlades))
        {
            var parts = cloud.unlockedBlades.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                if (!string.IsNullOrEmpty(p) && currentSet.Add(p))
                {
                    changed = true;
                }
            }
        }
        unlockedBlades = string.Join(",", currentSet);
        PlayerPrefs.SetString(UnlockedBladesKey, unlockedBlades);

        // Синхронизация выбранного клинка из облака при первом входе
        if (!string.IsNullOrEmpty(cloud.selectedBlade) && SelectedBladeIndex == 0)
        {
            for (int i = 0; i < Blades.Length; i++)
            {
                if (Blades[i].id == cloud.selectedBlade && IsBladeUnlocked(i))
                {
                    SelectedBladeIndex = i;
                    PlayerPrefs.SetInt(SelectedBladeKey, SelectedBladeIndex);
                    changed = true;
                    break;
                }
            }
        }

        if (changed)
        {
            PlayerPrefs.Save();
            if (State == GameState.Menu && Hud != null)
            {
                Hud.ShowMenu(Best, BestComboEver, BestStreakEver, TotalSliced);
                Hud.RefreshShopUI();
            }
        }
    }

    public bool IsBladeUnlocked(int index)
    {
        if (index == 0) return true;
        if (index < 0 || index >= Blades.Length) return false;
        return unlockedBlades.Contains(Blades[index].id);
    }

    public bool SelectBlade(int index)
    {
        if (index < 0 || index >= Blades.Length) return false;
        if (!IsBladeUnlocked(index)) return false;

        SelectedBladeIndex = index;
        PlayerPrefs.SetInt(SelectedBladeKey, SelectedBladeIndex);
        PlayerPrefs.Save();
        SaveCloudData();
        Sfx.Toss();

        var blade = FindAnyObjectByType<Blade>();
        if (blade != null) blade.RefreshBladeColor();
        if (Hud != null) Hud.RefreshShopUI();
        return true;
    }

    public bool BuyBlade(int index)
    {
        if (index < 0 || index >= Blades.Length) return false;
        if (IsBladeUnlocked(index)) return SelectBlade(index);

        int cost = Blades[index].cost;
        if (Essence < cost) return false;

        Essence -= cost;
        var set = new HashSet<string>(unlockedBlades.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries));
        set.Add(Blades[index].id);
        unlockedBlades = string.Join(",", set);
        SelectedBladeIndex = index;

        PlayerPrefs.SetInt(EssenceKey, Essence);
        PlayerPrefs.SetString(UnlockedBladesKey, unlockedBlades);
        PlayerPrefs.SetInt(SelectedBladeKey, SelectedBladeIndex);
        PlayerPrefs.Save();
        SaveCloudData();

        Sfx.Start();
        var blade = FindAnyObjectByType<Blade>();
        if (blade != null) blade.RefreshBladeColor();
        if (Hud != null) Hud.RefreshShopUI();
        return true;
    }

    public void CycleBladeSkin()
    {
        int nextIndex = (SelectedBladeIndex + 1) % Blades.Length;
        if (IsBladeUnlocked(nextIndex))
        {
            SelectBlade(nextIndex);
        }
        else if (Essence >= Blades[nextIndex].cost)
        {
            BuyBlade(nextIndex);
        }
        else
        {
            Sfx.Toss();
        }
    }

    // Калиброванная степенная формула заработка софт-валюты «Эссенция» по RESEARCH_REPORT.md (раздел 4.7)
    public static int CalculateEarnedEssence(int score, int bestCombo, int sliced)
    {
        if (score <= 0) return 0;
        float baseReward = 0.35f * Mathf.Pow(score, 0.65f);
        float comboReward = 1.2f * Mathf.Max(0, bestCombo);
        float slicedReward = 0.05f * Mathf.Max(0, sliced);
        return Mathf.Max(1, Mathf.RoundToInt(baseReward + comboReward + slicedReward));
    }

    public void SaveCloudData()
    {
        if (YandexSDKManager.Instance != null)
        {
            YandexSDKManager.Instance.SaveCloud(new YandexSDKManager.CloudData
            {
                bestScore = Best,
                bestCombo = BestComboEver,
                bestStreak = BestStreakEver,
                totalSliced = TotalSliced,
                essence = Essence,
                unlockedBlades = unlockedBlades,
                selectedBlade = Blades[Mathf.Clamp(SelectedBladeIndex, 0, Blades.Length - 1)].id
            });
        }
    }

    public bool CanRevive()
    {
        return !RevivedThisRun && State == GameState.GameOver;
    }

    public void RequestReviveAd()
    {
        if (!CanRevive()) return;
        if (YandexSDKManager.Instance != null)
        {
            YandexSDKManager.Instance.ShowRewarded(onRewarded: ReviveRun);
        }
        else
        {
            ReviveRun();
        }
    }

    public void ReviveRun()
    {
        if (!CanRevive()) return;
        RevivedThisRun = true;
        Lives = 1;
        State = GameState.Playing;
        ClearField();
        spawner.Begin();
        spawner.Hold(1.2f);
        Hud.ShowGame(Best);
        Hud.SetLives(Lives, false);
        Hud.SetScore(Score, false);
        Hud.Shout("ВТОРОЙ ШАНС!", "+1 ЖИЗНЬ", 2.0f, Hud.Mint);
        Sfx.Music(1f);
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        UpdateView();
        Sfx.Tick(dt);

        // Пауза: мир замирает целиком, ждём только её снятия или выхода в меню.
        if (State == GameState.Playing && Blade.PausePressed()) SetPause(!Paused);
        if (Paused)
        {
            if (Blade.EscapePressed())
            {
                SaveProgress();
                ToMenu();
            }
            else Time.timeScale = 0f;
            return;
        }

        if (State == GameState.GameOver)
        {
            Vector2 screen;
            bool overUI = Blade.Pointer(out screen) && Hud != null && Hud.IsPointerOverUI(screen);
            if (Time.unscaledTime - stateSince >= RestartLock && Blade.PressBegan() && !overUI) StartGame();
            else if (Blade.EscapePressed()) ToMenu();
            else if (newBest) Celebrate(dt);
        }
        else if (State == GameState.Playing && Blade.EscapePressed())
        {
            SaveProgress();
            ToMenu();
        }
        else if (State == GameState.Menu && Blade.SpacePressed())
        {
            ToStory();
        }

        if (chain > 0 && hitstopLeft <= 0f)
        {
            chainTimer -= dt;
            if (chainTimer <= 0f) EndChain();
        }

        graceLeft -= Time.deltaTime;
        if (SlowLeft > 0f) SlowLeft -= dt;
        if (FrenzyLeft > 0f) FrenzyLeft = Mathf.Max(0f, FrenzyLeft - Time.deltaTime);
        else frenzyRest -= Time.deltaTime;

        UpdateTime(dt);
        UpdateShake(dt);
    }

    // ---------- Ход попытки ----------

    public void SetPause(bool on)
    {
        Paused = on;
        Hud.ShowPause(on);
        Sfx.Pause(on);
        if (!on)
        {
            Time.timeScale = flow;
            PlayerPrefs.Save();   // громкость могла измениться
        }
        else
        {
            Time.timeScale = 0f;
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        HandleAppFocus(hasFocus);
    }

    void OnApplicationPause(bool pauseStatus)
    {
        HandleAppFocus(!pauseStatus);
    }

    void HandleAppFocus(bool active)
    {
        if (!active)
        {
            if (State == GameState.Playing && !Paused)
            {
                SetPause(true);
            }
            Sfx.SetMute(true);
        }
        else
        {
            Sfx.SetMute(false);
        }
    }

    void ToStory()
    {
        ResetRun();
        spawner.Stop();
        State = GameState.Story;
        Sfx.Music(0f);
        Hud.ShowStory();
    }

    public void EndStory()
    {
        PlayerPrefs.SetInt(StoryKey, 1);
        PlayerPrefs.Save();
        ToMenu();
    }

    public void ExitToMenu()
    {
        SaveProgress();
        ToMenu();
    }

    void ToMenu()
    {
        if (YandexSDKManager.Instance != null) YandexSDKManager.Instance.GameplayStop();
        ResetRun();
        spawner.Stop();
        State = GameState.Menu;
        Sfx.Music(0f);
        Hud.ShowMenu(Best, BestComboEver, BestStreakEver, TotalSliced);
        // Стартовый слайм: первый же взмах и учит механике, и запускает игру.
        Sliceable.Spawn(Art.Acid, View.center, Vector2.zero, 0f, true);
    }

    void StartGame()
    {
        if (YandexSDKManager.Instance != null) YandexSDKManager.Instance.GameplayStart();
        ResetRun();
        Lives = MaxLives;
        RevivedThisRun = false;
        knownRank = balance.RankOf(Best);
        saved = false;
        State = GameState.Playing;
        Hud.ShowGame(Best);
        Hud.SetScore(0, false);
        Hud.SetLives(Lives, false);
        spawner.Begin();
        Sfx.Start();
        Sfx.Music(1f);
    }

    void ResetRun()
    {
        ClearField();
        if (Paused)
        {
            Paused = false;
            Hud.ShowPause(false);
            Sfx.Pause(false);
        }
        Time.timeScale = flow = 1f;
        dipLeft = hitstopLeft = 0f;
        FrenzyLeft = SlowLeft = graceLeft = frenzyRest = 0f;
        chain = 0;
        Score = Sliced = BestCombo = Streak = BestStreak = 0;
        newBest = false;
    }

    void GameOver()
    {
        if (YandexSDKManager.Instance != null) YandexSDKManager.Instance.GameplayStop();
        EndChain();
        FrenzyLeft = SlowLeft = 0f;
        State = GameState.GameOver;
        stateSince = Time.unscaledTime;
        spawner.Stop();
        ClearField();

        int rankBefore = balance.RankOf(Best);
        newBest = Score > Best;
        if (newBest)
        {
            Best = Score;
            PlayerPrefs.SetInt(BestKey, Best);
        }
        int earnedEssence = CalculateEarnedEssence(Score, BestCombo, Sliced);
        LastEarnedEssence = earnedEssence;
        Essence += earnedEssence;
        PlayerPrefs.SetInt(EssenceKey, Essence);

        SaveProgress();
        fireworkTimer = 0.6f;
        fanfare = true;
        Hud.ShowGameOver(Score, Best, newBest, Sliced, BestCombo, BestStreak, balance.RankOf(Best) > rankBefore);
        Sfx.GameOver();
        Sfx.Music(0.3f);
        TimeDip(0.3f, 0.5f);

        if (Time.unscaledTime - lastInterstitialTime >= InterstitialCooldown && YandexSDKManager.Instance != null)
        {
            lastInterstitialTime = Time.unscaledTime;
            YandexSDKManager.Instance.ShowInterstitial();
        }
    }

    // Лучшее комбо и общий счёт разрубленных слаймов копятся между попытками — это долгая цель игрока.
    void SaveProgress()
    {
        if (saved) return;
        saved = true;
        BestComboEver = Mathf.Max(BestComboEver, BestCombo);
        BestStreakEver = Mathf.Max(BestStreakEver, BestStreak);
        PlayerPrefs.SetInt(StreakKey, BestStreakEver);
        TotalSliced += Sliced;
        PlayerPrefs.SetInt(ComboKey, BestComboEver);
        PlayerPrefs.SetInt(SlicedKey, TotalSliced);
        PlayerPrefs.SetInt(EssenceKey, Essence);
        PlayerPrefs.Save();

        if (YandexSDKManager.Instance != null)
        {
            YandexSDKManager.Instance.SaveCloud(new YandexSDKManager.CloudData
            {
                bestScore = Best,
                bestCombo = BestComboEver,
                bestStreak = BestStreakEver,
                totalSliced = TotalSliced,
                essence = Essence,
                unlockedBlades = unlockedBlades,
                selectedBlade = Blades[Mathf.Clamp(SelectedBladeIndex, 0, Blades.Length - 1)].id
            });

            if (Best > 0)
            {
                YandexSDKManager.Instance.SubmitLeaderboardScore("leaderboard", Best);
            }
        }
    }

    // Золотой салют на экране поражения, если побит рекорд.
    void Celebrate(float dt)
    {
        fireworkTimer -= dt;
        if (fireworkTimer > 0f) return;
        if (fanfare) Sfx.Record();
        fanfare = false;
        fireworkTimer = Random.Range(1.2f, 2f);
        Vector2 at = new Vector2(Random.Range(View.xMin + View.width * 0.15f, View.xMax - View.width * 0.15f),
            Random.Range(View.center.y, View.yMax - View.height * 0.12f));
        Fx.Firework(at, Color.Lerp(Gold, new Color(1f, 0.6f, 0.2f), Random.value));
    }

    // ---------- События клинка ----------

    public void OnSlimeSliced(SlimeDef def, Vector2 pos, float cut = 0.5f)
    {
        if (State == GameState.Menu)
        {
            // Первый разрез запускает игру — и отмечен вспышкой: волна света, звёзды и клич по центру экрана.
            Sfx.Slice(def.kind, 1);
            StartGame();
            Fx.StartBurst(pos, def.glow);
            Hud.Flash(Color.Lerp(def.glow, Color.white, 0.4f), 0.5f);
            Hud.Shout("ЗА ДЕЛО!", "", 1.1f, Mint);
            Hitstop(0.08f);
            Shake(0.3f, 0.35f);
            TimeDip(0.35f, 0.25f);
            return;
        }
        if (State != GameState.Playing) return;

        bool bullseye = Mathf.Abs(cut) <= 0.12f;

        // Комбо — это слаймы, разрезанные одним движением: новый взмах начинает новую серию.
        if (chain > 0 && Blade.Stroke != chainStroke) EndChain();
        if (chain == 0) chainPoints.Clear();
        chainPoints.Add(pos);
        chain++;
        chainStroke = Blade.Stroke;
        chainTimer = balance.comboWindow;
        chainPos = pos;
        Sliced++;
        GrowStreak();
        if (bullseye) GrowStreak(); // бонусный шаг серии за идеальный срез

        // Счётчик серии растёт прямо под клинком: видно, как взмах превращается в комбо.
        if (chain >= 2)
        {
            Hud.Popup(pos + Vector2.up * (1.5f * Unit), "×" + chain, ComboBar.ComboColor(chain), 36 + 8 * Mathf.Min(chain, 6), 0.6f);
            Fx.ChainHit(pos, chain, def.glow);
        }

        int gain = bullseye ? def.points * 2 * Mult : def.points * Mult;
        AddScore(gain);

        if (bullseye)
        {
            Essence++;
            PlayerPrefs.SetInt(EssenceKey, Essence);
            Hud.Popup(pos + Vector2.up * (2.2f * Unit), "В ЯБЛОЧКО! +1◆", new Color(1f, 0.88f, 0.25f), 38, 0.85f);
            Fx.BladeSpark(pos, new Color(1f, 0.9f, 0.3f));
            Fx.BladeSpark(pos, Color.white);
            Sfx.Slice(Kind.Nugget, chain);
        }

        // Очки всплывают в цвет слайма; у золотых короля и самородка — мятным, как остальные награды.
        bool golden = def.kind == Kind.King || def.kind == Kind.Nugget;
        Hud.Popup(pos, "+" + gain, golden ? Mint : Color.Lerp(def.glow, Color.white, 0.35f), def.points >= 100 ? 64 : def.points >= 30 ? 42 : 34, 0.7f);
        Sfx.Slice(def.kind, chain);

        switch (def.kind)
        {
            case Kind.King:
                Hitstop(0.09f);
                Transmutation();
                break;
            case Kind.Magma:
            case Kind.Geode:
            case Kind.Blue:
                Hitstop(0.05f);
                Shake(0.13f, 0.16f);
                break;
            default:
                Hitstop(0.03f);
                Shake(0.07f + 0.02f * Mathf.Min(chain, 6), 0.1f);
                break;
        }
    }

    public void OnShellCracked(Vector2 pos)
    {
        Sfx.ShellCrack(chain + 1);
        Hitstop(0.05f);
        Shake(0.12f, 0.14f);
        // Раскол скорлупы не обрывает серию: добить жеоду можно тем же движением.
        if (chain > 0 && Blade.Stroke == chainStroke) chainTimer = balance.comboWindow;
    }

    public void OnStrokeEnd()
    {
        if (chain > 0) EndChain();
    }

    public void OnSlimeMissed(float worldX, SlimeDef def)
    {
        if (State != GameState.Playing) return;
        Fx.Splash(new Vector2(worldX, View.yMin + 1.3f * Unit), def.glow);
        if (graceLeft > 0f) return;
        graceLeft = balance.missGrace;
        Sfx.Miss();
        Hud.MissMark(worldX);
        Hud.Flash(new Color(1f, 0.1f, 0.1f), 0.22f);
        Shake(0.2f, 0.25f);
        // Одна ошибка — одно сердце: остальные слаймы лопаются без штрафа, и котёл берёт паузу.
        // Иначе плотный залп, упущенный целиком, отнимал бы все три жизни за пару секунд.
        BreakStreak();
        EndChain();
        ClearField();
        spawner.Hold(1.4f);
        LoseLife();
    }

    public void OnBombHit(Vector2 pos)
    {
        Fx.Explode(pos);
        Sfx.Bomb();
        Hud.Flash(Color.white, 0.85f);
        Shake(0.75f, 0.6f);
        shock = 1f;
        if (State != GameState.Playing) return;

        // Взрыв сжигает серию и режимы и сметает всё с поля без штрафа, иначе одна ошибка стоила бы нескольких жизней.
        BreakStreak();
        chain = 0;
        FrenzyLeft = SlowLeft = 0f;
        TimeDip(0.22f, 0.45f);
        ClearField();
        spawner.Hold(1.5f);
        LoseLife();
    }

    void EndChain()
    {
        int n = chain;
        chain = 0;
        if (n < balance.comboMin || State != GameState.Playing) return;

        // Нелинейная прогрессия награды за комбо по ST2-02 (RESEARCH_REPORT 4.6): 15 * n^1.4 * Mult
        int bonus = Mathf.RoundToInt(15f * Mathf.Pow(n, 1.4f) * Mult);
        AddScore(bonus);

        // Кинематографический Micro Slow-Mo при высоком комбо (ST2-03)
        if (n >= 3)
        {
            TimeDip(0.35f, 0.22f);
        }
        // Комбо крупнее всех прежних может открыть новую ступень из главного меню — об этом стоит сказать сразу.
        int known = Mathf.Max(BestComboEver, BestCombo);
        if (balance.ComboStep(n) > balance.ComboStep(known)) Hud.Hint("Новая ступень комбо: ×" + balance.comboGoals[balance.ComboStep(n) - 1] + "!", 2.4f);
        BestCombo = Mathf.Max(BestCombo, n);
        // У каждого размера комбо свой цвет: им окрашены надпись, вспышка, молния и взрыв.
        Color tint = ComboBar.ComboColor(n);
        Hud.ClearPopups();   // счётчики взмаха своё отработали: надпись комбо выходит на чистую сцену
        Hud.Combo(n, bonus, tint);
        Hud.Flash(tint, Mathf.Min(0.3f, 0.1f + 0.03f * n));
        Fx.ComboLink(chainPoints, tint);
        Fx.ComboBurst(chainPos, n, tint);
        Sfx.Combo(n);
        Shake(0.2f + 0.05f * n, 0.3f);
        TimeDip(0.25f, 0.16f);
        // Крупное комбо отдаётся в «объектив»: короткий цветной сдвиг по краям кадра.
        if (n >= 5) shock = Mathf.Max(shock, Mathf.Min(0.6f, 0.25f + 0.07f * (n - 5)));
        if (n >= balance.frenzyCombo) StartFrenzy(n);
    }

    // Серия — слаймы, разрубленные подряд без промаха и без взрыва. Она растёт всю попытку, на вехах даёт бонус,
    // а главная веха (×100) — цель, ради которой стоит играть аккуратно.
    void GrowStreak()
    {
        Streak++;
        BestStreak = Mathf.Max(BestStreak, Streak);
        if (!balance.IsStreakGoal(Streak)) return;
        // Веха: бонус, объявление и вспышка в цвет нового уровня полосы серии.
        int bonus = balance.streakBonus * Streak * Mult;
        int tier = balance.StreakTier(Streak);
        Color tint = StreakBar.TierColor(tier);
        AddScore(bonus);
        Hud.Hint("Серия ×" + Streak + "!   +" + bonus, 2.2f);
        Hud.Flash(tint, 0.28f);
        Fx.ComboBurst(chainPos, 3 + tier, tint);
        Sfx.Combo(7);
    }

    // Полоса серии сама замечает обрыв и показывает его.
    void BreakStreak()
    {
        Streak = 0;
    }

    // Alembic Frenzy: награда за большое комбо и одновременно испытание жадности.
    void StartFrenzy(int combo)
    {
        // Уровень растёт на ступень за каждый слайм сверх порога. Комбо крупнее прежнего прямо во время
        // перегрузки поднимает уровень полоски, но саму перегрузку не продлевает.
        int tier = combo - balance.frenzyCombo + 1;
        if (FrenzyLeft > 0f)
        {
            if (combo > FrenzyCombo) FrenzyCombo = combo;
            if (tier > FrenzyTier)
            {
                FrenzyTier = tier;
                Sfx.Frenzy();
            }
            return;
        }
        if (frenzyRest > 0f) return;
        FrenzyTier = tier;
        FrenzyCombo = combo;
        FrenzyLeft = FrenzySpan = balance.frenzySeconds + Mathf.Min(2, combo - balance.frenzyCombo);
        frenzyRest = balance.frenzyRest;
        Sfx.Frenzy();
        Hud.Flash(Mint, 0.3f);
        Shake(0.3f, 0.4f);
    }

    // Награда за короля: время замедляется, а все бомбы на экране превращаются в золото.
    void Transmutation()
    {
        SlowLeft = balance.slowSeconds;
        flow = Mathf.Min(flow, balance.slowScale);
        Sfx.Transmute();
        Hud.Flash(Gold, 0.65f);
        Hud.Hint("ТРАНСМУТАЦИЯ!", 1.6f);
        Shake(0.4f, 0.45f);
        for (int i = Sliceable.Active.Count - 1; i >= 0; i--) Sliceable.Active[i].Transmute();
    }

    void AddScore(int amount)
    {
        Score += amount;
        Hud.SetScore(Score, true);
        // Новое звание объявляется сразу, как только счёт до него дорос: награда приходит посреди боя.
        int rank = balance.RankOf(Score);
        if (rank > knownRank)
        {
            knownRank = rank;
            Hud.Hint("Новое звание: «" + balance.ranks[rank].name + "»!", 2.6f);
            Hud.Flash(Mint, 0.3f);
            Sfx.Record();
        }
    }

    void LoseLife()
    {
        Lives--;
        Hud.SetLives(Lives, true);
        if (Lives <= 0) GameOver();
    }

    void ClearField()
    {
        for (int i = Sliceable.Active.Count - 1; i >= 0; i--) Sliceable.Active[i].Vanish();
    }

    // ---------- Время и камера ----------

    void UpdateTime(float dt)
    {
        // Три слоя: фоновый темп (обычный или замедленный), короткие провалы после сильных событий и стоп-кадр удара.
        float target = SlowLeft > 0f ? balance.slowScale : 1f;
        if (dipLeft > 0f)
        {
            dipLeft -= dt;
            target = Mathf.Min(target, dipScale);
        }
        flow = Mathf.MoveTowards(flow, target, dt * 5f);

        if (hitstopLeft > 0f)
        {
            hitstopLeft -= dt;
            Time.timeScale = 0.02f;
        }
        else
        {
            Time.timeScale = flow;
        }
        Sfx.Slow = Mathf.Lerp(0.72f, 1f, Mathf.InverseLerp(balance.slowScale, 1f, flow));

        shock = Mathf.MoveTowards(shock, 0f, dt * 1.6f);
        if (chroma != null) chroma.intensity.value = shock;
    }

    void UpdateShake(float dt)
    {
        if (shakeLeft <= 0f) return;
        shakeLeft -= dt;
        float k = Mathf.Max(0f, shakeLeft / shakeDuration);
        camRig.position = shakeLeft > 0f ? camRigBase + (Vector3)(Random.insideUnitCircle * (shakeAmp * k * k)) : camRigBase;
    }

    void Shake(float amplitude, float duration)
    {
        if (shakeLeft > 0f && amplitude * Unit < shakeAmp * (shakeLeft / shakeDuration)) return;
        shakeAmp = amplitude * Unit;
        shakeDuration = shakeLeft = duration;
    }

    void TimeDip(float scale, float duration)
    {
        dipScale = dipLeft > 0f ? Mathf.Min(dipScale, scale) : scale;
        dipLeft = Mathf.Max(dipLeft, duration);
        flow = Mathf.Min(flow, scale);
    }

    // Стоп-кадр в момент удара: мир замирает на долю секунды, а клинок продолжает движение.
    void Hitstop(float duration)
    {
        hitstopLeft = Mathf.Max(hitstopLeft, duration);
    }

    // Свечение неона (bloom), лёгкая виньетка и хроматическая «контузия» после взрыва.
    void BuildPostFx()
    {
        var data = cam.GetUniversalAdditionalCameraData();
        if (data == null) return;
        data.renderPostProcessing = true;

        // Постоянные эффекты лежат в профиле-ассете: по нему сборка понимает, какие шейдеры постобработки
        // нужны игре, и не вырезает их из билда. Если ассета нет, тот же профиль собирается кодом.
        var look = Resources.Load<VolumeProfile>("PostFx");
        if (look == null)
        {
            look = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = look.Add<Bloom>(true);
            bloom.threshold.Override(0.78f);
            bloom.intensity.Override(0.8f);
            bloom.scatter.Override(0.62f);
            var vignette = look.Add<Vignette>(true);
            vignette.intensity.Override(0.26f);
            vignette.smoothness.Override(0.5f);
        }
        AddVolume("PostFx", look, 10f);

        // «Контузия» меняется каждый кадр, поэтому живёт в отдельном профиле поверх основного — ассет не трогаем.
        var pulse = ScriptableObject.CreateInstance<VolumeProfile>();
        chroma = pulse.Add<ChromaticAberration>(true);
        chroma.intensity.Override(0f);
        AddVolume("PostFxShock", pulse, 20f);
    }

    void AddVolume(string name, VolumeProfile profile, float priority)
    {
        var volume = new GameObject(name).AddComponent<Volume>();
        volume.transform.SetParent(transform, false);
        volume.isGlobal = true;
        volume.priority = priority;
        volume.sharedProfile = profile;
    }

    void UpdateView()
    {
        float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
        View = new Rect(camRigBase.x - halfW, camRigBase.y - halfH, halfW * 2f, halfH * 2f);
        // На узких (портретных) экранах масштаб считается от ширины, чтобы слаймы не занимали пол-экрана.
        Unit = Mathf.Min(View.height, View.width * 1.3f) / 20f;
    }

    public static Vector2 ScreenToWorld(Vector2 screen)
    {
        // Считаем от неподвижной области, а не от камеры: тряска не должна сбивать клинок.
        return new Vector2(
            View.xMin + screen.x / Mathf.Max(1, Screen.width) * View.width,
            View.yMin + screen.y / Mathf.Max(1, Screen.height) * View.height);
    }
}
