using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

// Интерфейс целиком строится кодом: счёт, жизни, стартовый экран с правилами, экран поражения, всплывающие надписи.
public class Hud : MonoBehaviour
{
    // Палитра: светлый пергамент для обычных надписей, мятный «эфир» (цвет клинка) для счёта, подсказок
    // и наград. Панели меню — готовая картинка: фиолетовое поле в золотой рамке, под цвет логотипа.
    public static readonly Color Cream = new Color(0.96f, 0.95f, 0.9f);
    public static readonly Color Mint = new Color(0.62f, 0.98f, 0.88f);
    // У каждого блока меню свой акцент, чтобы экран не сливался в один цвет: звание — цвет самого звания,
    // заголовок его панели сиреневый, комбо и серия — розовые.
    public static readonly Color Lilac = new Color(0.8f, 0.72f, 1f);
    public static readonly Color Rose = new Color(1f, 0.66f, 0.76f);
    public static readonly Color Lime = new Color(0.88f, 1f, 0.5f);
    public static readonly Color Gold = new Color(1f, 0.85f, 0.3f);
    public static readonly Color Slime = new Color(0.3f, 0.86f, 0.3f);
    public static readonly Color Plum = new Color(0.13f, 0.06f, 0.22f, 0.9f);   // контур букв: тёмный, но не чёрный
    public static readonly Color Alarm = new Color(1f, 0.22f, 0.3f);
    static readonly Color HeartOn = Color.white;                            // сердце нарисовано цветным, подкрашивать не нужно
    static readonly Color HeartOff = new Color(0.3f, 0.28f, 0.4f, 0.55f);   // потерянная жизнь: тёмный силуэт
    static readonly Color Locked = new Color(0.36f, 0.32f, 0.5f, 0.75f);   // ещё не открытое: тёмный силуэт

    Font font, titleFont;
    RectTransform root, popups;
    Story story;
    ComboBar combo;
    CanvasGroup menu, game, over, pause, shop;
    float menuTarget, gameTarget, overTarget, pauseTarget, shopTarget;
    public bool IsShopOpen { get { return isShopOpen; } }
    bool isShopOpen;
    Text menuEssenceText, shopEssenceText;
    RectTransform shopBackBtnRect;
    RectTransform[] bladeCardRects;
    Text[] bladeCardBtnTexts;
    Image[] bladeCardSwatches;
    Text scoreText, deltaText, bestText, hintText, overScore, overBest, overStats, overRank, overEssence, menuPrompt, overPrompt, overEscPrompt, comboText, comboBonus;
    Text rankName, rankNext, rankBest, comboBest, comboNext, streakGoal, streakSteps, bestLine;
    StreakBar streak;
    RectTransform ghost, rankFill;
    Image[] hearts, mascots, rankSteps;
    Image comboGlow, comboRays, comboBand, titleGlow, rankIcon, rankGoal, rankHead, cat;
    VolumeBar menuVolume, pauseVolume, dragged;
    Color comboTint = Mint;
    float rankLeft, rankWidth, catBlinkAt;
    int openTiers, shownRank;
    bool pointerWasDown, overCounted;

    // Ползунок громкости. Он сам читает мышь, поэтому системе событий интерфейса игра не нужна.
    class VolumeBar
    {
        public RectTransform track, fill, knob;
        public Text percent;
        public float width;
    }
    float[] heartPunch;
    Image flash, aura, ghostImage;
    float scorePunch, hintLeft, hintSpan, overShownAt, deltaLeft, comboLeft, comboSpan;
    int shownScore, deltaSum, overTargetScore;

    public static Hud Instance { get; private set; }
    RectTransform pauseBtnRect, resumeBtnRect, menuBtnRect, bladeBtnRect, reviveBtnRect;
    Text bladeBtnText;

    public static Hud Create()
    {
        return new GameObject("HUD").AddComponent<Hud>();
    }

    void Awake()
    {
        Instance = this;
        font = LoadFont("Philosopher-Regular", "Trebuchet MS", "Verdana", "Arial");
        titleFont = LoadFont("Philosopher-Bold", "Palatino Linotype", "Georgia", "Times New Roman");

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        // Expand: холст не меньше 1280x720 в любой ориентации, поэтому вёрстка не обрезается на узких экранах.
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        root = (RectTransform)transform;

        Vector2 topLeft = new Vector2(0f, 1f), topRight = new Vector2(1f, 1f), top = new Vector2(0.5f, 1f);
        Vector2 center = new Vector2(0.5f, 0.5f), bottom = new Vector2(0.5f, 0f);

        aura = Picture(root, Art.Vignette, Color.clear, Vector2.zero, center, Vector2.zero, Vector2.zero);
        Stretch(aura.rectTransform);
        aura.preserveAspect = false;
        flash = Picture(root, Art.White, Color.clear, Vector2.zero, center, Vector2.zero, Vector2.zero);
        Stretch(flash.rectTransform);
        flash.preserveAspect = false;

        // --- Бой ---
        game = Group("Game");
        Picture(game.transform, Art.Acid.whole, Color.white, topLeft, topLeft, new Vector2(26f, -20f), new Vector2(62f, 62f));
        scoreText = Label(game.transform, font, "0", 62, Cream, TextAnchor.UpperLeft, topLeft, new Vector2(100f, -12f));
        deltaText = Label(game.transform, font, "", 28, Mint, TextAnchor.UpperLeft, topLeft, new Vector2(104f, -84f));
        bestText = Label(game.transform, font, "", 22, new Color(Cream.r, Cream.g, Cream.b, 0.8f), TextAnchor.UpperLeft, topLeft, new Vector2(30f, -122f));
        Label(game.transform, font, "правая кнопка мыши — пауза", 16, new Color(1f, 1f, 1f, 0.4f), TextAnchor.LowerRight, new Vector2(1f, 0f), new Vector2(-22f, 14f));
        pauseBtnRect = BuildPauseButton(game.transform, topRight, new Vector2(-46f, -54f));
        hearts = new Image[GameManager.MaxLives];
        heartPunch = new float[GameManager.MaxLives];
        for (int i = 0; i < hearts.Length; i++)
        {
            // Центральная опорная точка, чтобы сердце «подпрыгивало» на месте при потере жизни.
            hearts[i] = Picture(game.transform, Art.Heart, HeartOn, topRight, center,
                new Vector2(-110f - (hearts.Length - 1 - i) * 62f, -54f), new Vector2(58f, 58f));
        }
        streak = StreakBar.Create(game.transform, font, titleFont);

        combo = ComboBar.Create(root, font, titleFont);

        // Надпись комбо: мятное зарево за буквами, чтобы её было видно поверх любой сцены.
        comboGlow = Picture(root, Art.Soft, Color.clear, center, center, new Vector2(0f, 72f), new Vector2(1100f, 300f));
        comboGlow.preserveAspect = false;
        // За буквами — веер лучей и тёмная полоса-росчерк: надпись читается на любой сцене и выглядит ударом.
        comboRays = Picture(root, Art.Rays, Color.clear, center, center, new Vector2(0f, 80f), new Vector2(620f, 620f));
        comboBand = Picture(root, Art.Streak, Color.clear, center, center, new Vector2(0f, 82f), new Vector2(1040f, 170f));
        comboBand.preserveAspect = false;
        comboText = Label(root, titleFont, "", 96, Color.white, TextAnchor.MiddleCenter, center, new Vector2(0f, 86f));
        Paint(comboText, Color.white, Mint);
        comboBonus = Label(root, font, "", 46, Cream, TextAnchor.MiddleCenter, center, new Vector2(0f, 6f));
        hintText = Label(root, font, "", 42, Mint, TextAnchor.MiddleCenter, top, new Vector2(0f, -184f));

        // Всплывающие надписи живут на вложенном холсте: их анимация не заставляет перестраивать весь интерфейс.
        popups = (RectTransform)Group("Popups").transform;
        popups.gameObject.AddComponent<Canvas>();

        // --- Меню ---
        menu = Group("Menu");
        // Декор: кошка алхимика сидит на борту котла под панелью звания. Склянки и полки — на самом фоне.
        cat = Picture(menu.transform, Art.Cat, new Color(1f, 1f, 1f, 0.92f), Vector2.zero, new Vector2(0.5f, 0f), new Vector2(236f, 44f), new Vector2(88f, 119f));

        // Название написано шрифтом: зелёное зарево, буквы цвета зелья и лента с подзаголовком.
        titleGlow = Picture(menu.transform, Art.Soft, new Color(0.3f, 1f, 0.4f, 0.2f), top, center, new Vector2(0f, -82f), new Vector2(980f, 250f));
        titleGlow.preserveAspect = false;
        Paint(Label(menu.transform, titleFont, "АЛХИМИЯ", 116, Color.white, TextAnchor.MiddleCenter, top, new Vector2(0f, -76f)), Lime, Slime);
        Picture(menu.transform, Art.Ribbon, Color.white, top, center, new Vector2(0f, -168f), new Vector2(500f, 86f));
        Label(menu.transform, titleFont, "С Л А Й М О В Ы Й   П Р О Р Ы В", 23, Cream, TextAnchor.MiddleCenter, top, new Vector2(0f, -163f));
        bestLine = Label(menu.transform, font, "", 18, new Color(Cream.r, Cream.g, Cream.b, 0.85f), TextAnchor.MiddleCenter, top, new Vector2(0f, -212f));

        // Правила — три плашки в ряд; главное слово в каждой выделено цветом своего значка.
        // У картинок разные поля, поэтому масштаб значков свой у каждой.
        Chip(-374f, Art.Acid.whole, 1f, "Режь " + Tint("слаймы", Lime) + " взмахом");
        Chip(0f, Art.Bomb.whole, 1.3f, Tint("Бомбы", Lilac) + " не трогай");
        Chip(374f, Art.Heart, 1.05f, "Не упускай: жизней " + Tint("три", Rose));

        // По бокам от стартового слайма — то, ради чего хочется сыграть ещё: звание и путь к следующему,
        // лучшее комбо и уровни, которые ещё не открыты.
        BuildRankPanel(-424f);
        BuildComboPanel(424f);

        bladeBtnRect = BuildActionButton(menu.transform, bottom, new Vector2(0f, 150f), new Vector2(440f, 48f), "КУЗНИЦА КЛИНКОВ", Mint);
        Picture(bladeBtnRect, Art.Sickle, Mint, new Vector2(0f, 0.5f), center, new Vector2(26f, 0f), new Vector2(28f, 28f));
        bladeBtnText = bladeBtnRect.GetComponentInChildren<Text>();
        bladeBtnText.fontSize = 20;

        var menuEssencePlate = Picture(menu.transform, Art.Plate, new Color(0.18f, 0.12f, 0.28f, 0.95f), topRight, center, new Vector2(-130f, -46f), new Vector2(210f, 44f));
        menuEssencePlate.type = Image.Type.Sliced;
        Picture(menuEssencePlate.rectTransform, Art.Crystal, Color.white, new Vector2(0f, 0.5f), center, new Vector2(24f, 0f), new Vector2(24f, 24f));
        menuEssenceText = Label(menuEssencePlate.rectTransform, titleFont, "0", 22, Mint, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(14f, 0f));

        menuPrompt = Label(menu.transform, titleFont, "Разрежь слайма, чтобы начать", 36, Mint, TextAnchor.MiddleCenter, bottom, new Vector2(0f, 104f));
        Label(menu.transform, font, "зажми кнопку мыши и проведи через него", 20, new Color(1f, 1f, 1f, 0.6f), TextAnchor.MiddleCenter, bottom, new Vector2(0f, 70f));
        Label(menu.transform, font, "Пробел — предыстория", 18, new Color(1f, 1f, 1f, 0.5f), TextAnchor.LowerLeft, Vector2.zero, new Vector2(26f, 18f));
        menuVolume = BuildVolume(menu.transform, new Vector2(1f, 0f), new Vector2(-136f, 26f), 150f);
        // Призрачный взмах поверх стартового слайма показывает жест без слов.
        ghostImage = Picture(menu.transform, Art.Soft, Color.clear, new Vector2(0.5f, GameManager.StartHeight), center, Vector2.zero, new Vector2(200f, 18f));
        ghostImage.preserveAspect = false;
        ghost = ghostImage.rectTransform;
        ghost.localRotation = Quaternion.Euler(0f, 0f, 18f);

        // --- Поражение ---
        over = Group("GameOver");
        var dim = Picture(over.transform, Art.White, new Color(0f, 0f, 0f, 0.55f), Vector2.zero, center, Vector2.zero, Vector2.zero);
        Stretch(dim.rectTransform);
        dim.preserveAspect = false;
        var shade = Picture(over.transform, Art.Vignette, new Color(0f, 0f, 0f, 0.8f), Vector2.zero, center, Vector2.zero, Vector2.zero);
        Stretch(shade.rectTransform);
        shade.preserveAspect = false;
        Paint(Label(over.transform, titleFont, "ИГРА ОКОНЧЕНА", 62, Color.white, TextAnchor.MiddleCenter, center, new Vector2(0f, 212f)), Cream, new Color(0.8f, 0.72f, 0.95f));
        overScore = Label(over.transform, titleFont, "0", 130, Color.white, TextAnchor.MiddleCenter, center, new Vector2(0f, 76f));
        Paint(overScore, Color.white, Mint);
        overBest = Label(over.transform, font, "", 34, Cream, TextAnchor.MiddleCenter, center, new Vector2(0f, -36f));
        overStats = Label(over.transform, font, "", 24, new Color(1f, 1f, 1f, 0.75f), TextAnchor.MiddleCenter, center, new Vector2(0f, -88f));
        overRank = Label(over.transform, font, "", 26, Cream, TextAnchor.MiddleCenter, center, new Vector2(0f, -126f));
        overEssence = Label(over.transform, font, "", 22, Lilac, TextAnchor.MiddleCenter, center, new Vector2(0f, -162f));
        reviveBtnRect = BuildActionButton(over.transform, center, new Vector2(0f, -206f), new Vector2(360f, 48f), "ВТОРОЙ ШАНС (+1 ЖИЗНЬ)", Lime);
        overPrompt = Label(over.transform, titleFont, "НАЖМИ — И СНОВА В БОЙ", 38, Mint, TextAnchor.MiddleCenter, center, new Vector2(0f, -260f));
        overEscPrompt = Label(over.transform, font, "Esc — в меню", 18, new Color(1f, 1f, 1f, 0.45f), TextAnchor.MiddleCenter, center, new Vector2(0f, -300f));

        // --- Пауза ---
        pause = Group("Pause");
        var veil = Picture(pause.transform, Art.White, new Color(0.02f, 0.01f, 0.06f, 0.66f), Vector2.zero, center, Vector2.zero, Vector2.zero);
        Stretch(veil.rectTransform);
        veil.preserveAspect = false;
        Paint(Label(pause.transform, titleFont, "ПАУЗА", 88, Color.white, TextAnchor.MiddleCenter, center, new Vector2(0f, 130f)), Color.white, Lilac);
        pauseVolume = BuildVolume(pause.transform, center, new Vector2(0f, 20f), 280f);
        Label(pause.transform, font, "колёсико мыши или ползунок", 17, new Color(1f, 1f, 1f, 0.5f), TextAnchor.MiddleCenter, center, new Vector2(0f, -14f));
        resumeBtnRect = BuildActionButton(pause.transform, center, new Vector2(0f, -80f), new Vector2(260f, 52f), "ПРОДОЛЖИТЬ", Mint);
        menuBtnRect = BuildActionButton(pause.transform, center, new Vector2(0f, -145f), new Vector2(220f, 44f), "В МЕНЮ", Cream);
        Label(pause.transform, font, "ПКМ — продолжить   •   Esc — в меню", 16, new Color(1f, 1f, 1f, 0.45f), TextAnchor.MiddleCenter, center, new Vector2(0f, -195f));

        BuildShopOverlay();

        // Комикс создаётся последним и поэтому лежит поверх всего интерфейса.
        story = Story.Create(root, font, titleFont);

        menu.alpha = game.alpha = over.alpha = pause.alpha = 0f;
        if (shop != null) shop.alpha = 0f;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        ClearPopupPool();
    }

    // Плашка правила: значок и одна короткая строка. Значок стоит с запасом от рамки.
    void Chip(float x, Sprite icon, float iconScale, string text)
    {
        Vector2 top = new Vector2(0.5f, 1f), center = new Vector2(0.5f, 0.5f);
        const float y = -256f, w = 350f, h = 70f;
        Plate(x, y, w, h);
        Picture(menu.transform, icon, Color.white, top, center, new Vector2(x - w * 0.5f + 56f, y), new Vector2(44f, 44f) * iconScale);
        var label = Label(menu.transform, font, text, 22, Cream, TextAnchor.MiddleLeft, top, new Vector2(x - w * 0.5f + 96f, y));
        Fit(label, w - 120f, 32f, 22);
    }

    // Размеры двух боковых панелей: с запасом, чтобы строки не упирались в рамку и не ужимались.
    const float PanelY = -430f, PanelWidth = 408f, PanelHeight = 252f, PanelEdge = 178f;

    // Панель «Звание»: значок и название в цвет звания, путь к следующему — стеклянная трубка с зельем, в конце
    // которой ждёт значок следующего звания. Ниже — лестница всех званий: полученные цветные, будущие — силуэты.
    void BuildRankPanel(float x)
    {
        Vector2 top = new Vector2(0.5f, 1f), left = new Vector2(0f, 0.5f), center = new Vector2(0.5f, 0.5f);
        const float y = PanelY, edge = PanelEdge, barY = y + 8f;
        Plate(x, y, PanelWidth, PanelHeight);
        Label(menu.transform, font, "ЗВАНИЕ", 16, new Color(Lilac.r, Lilac.g, Lilac.b, 0.9f), TextAnchor.MiddleLeft, top, new Vector2(x - edge, y + 102f));
        rankIcon = Picture(menu.transform, RankIcon(0), Color.white, top, center, new Vector2(x - edge + 36f, y + 58f), new Vector2(72f, 72f));
        rankName = Label(menu.transform, titleFont, "", 34, Color.white, TextAnchor.MiddleLeft, top, new Vector2(x - edge + 84f, y + 58f));
        Fit(rankName, edge * 2f - 84f, 44f, 34);

        rankWidth = edge * 2f - 48f;
        var bed = Picture(menu.transform, Art.Pill, new Color(0f, 0f, 0f, 0.6f), top, left, new Vector2(x - edge - 4f, barY), new Vector2(rankWidth + 8f, 24f));
        bed.type = Image.Type.Sliced;
        var fill = Picture(menu.transform, Art.Pill, Color.white, top, left, new Vector2(x - edge, barY), new Vector2(rankWidth, 16f));
        fill.type = Image.Type.Sliced;
        rankFill = fill.rectTransform;
        var gloss = Picture(rankFill, Art.Pill, new Color(1f, 1f, 1f, 0.4f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        gloss.type = Image.Type.Sliced;
        gloss.rectTransform.anchorMin = new Vector2(0.03f, 0.55f);
        gloss.rectTransform.anchorMax = new Vector2(0.97f, 0.88f);
        var ring = Picture(menu.transform, Art.PillRing, new Color(1f, 1f, 1f, 0.75f), top, left, new Vector2(x - edge - 4f, barY), new Vector2(rankWidth + 8f, 24f));
        ring.type = Image.Type.Sliced;
        rankLeft = x - edge;
        rankHead = Picture(menu.transform, Art.Star, Color.white, top, center, new Vector2(rankLeft, barY), new Vector2(28f, 28f));
        rankGoal = Picture(menu.transform, RankIcon(0), Color.white, top, center, new Vector2(x + edge - 16f, barY), new Vector2(40f, 40f));

        rankNext = Label(menu.transform, font, "", 20, Cream, TextAnchor.MiddleLeft, top, new Vector2(x - edge, y - 26f));
        Fit(rankNext, edge * 2f, 28f, 20);

        rankSteps = new Image[Balance.Current.ranks.Length];
        float step = edge * 2f / rankSteps.Length;
        for (int i = 0; i < rankSteps.Length; i++)
            rankSteps[i] = Picture(menu.transform, RankIcon(i), Color.white, top, center, new Vector2(x - edge + step * (i + 0.5f), y - 66f), new Vector2(36f, 36f));

        var slicedBed = Picture(menu.transform, Art.Pill, new Color(0f, 0f, 0f, 0.65f), top, center, new Vector2(x, y - 104f), new Vector2(edge * 2f + 8f, 32f));
        slicedBed.type = Image.Type.Sliced;
        Picture(slicedBed.rectTransform, Art.Sickle, Mint, new Vector2(0f, 0.5f), center, new Vector2(20f, 0f), new Vector2(22f, 22f));
        rankBest = Label(slicedBed.rectTransform, titleFont, "", 18, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(10f, 0f));
    }

    // Значков столько же, сколько званий в балансе; если званий добавили, лишние берут последний значок.
    static Sprite RankIcon(int rank)
    {
        return Art.Ranks[Mathf.Min(rank, Art.Ranks.Length - 1)];
    }

    // Панель «Комбо и серия»: лучшее комбо игрока и слаймы-ступени — каждые пять слаймов в одном взмахе открывают
    // следующую. Открытые окрашены в свой цвет, закрытые стоят тёмными силуэтами. Ниже — серия без промаха и её вехи.
    void BuildComboPanel(float x)
    {
        Vector2 top = new Vector2(0.5f, 1f), center = new Vector2(0.5f, 0.5f);
        const float y = PanelY, edge = PanelEdge;
        Plate(x, y, PanelWidth, PanelHeight);
        Label(menu.transform, font, "КОМБО И СЕРИЯ", 16, new Color(Rose.r, Rose.g, Rose.b, 0.9f), TextAnchor.MiddleLeft, top, new Vector2(x - edge, y + 102f));
        comboBest = Label(menu.transform, titleFont, "", 34, Color.white, TextAnchor.MiddleLeft, top, new Vector2(x - edge, y + 68f));
        Paint(comboBest, Color.white, Rose);

        int[] goals = Balance.Current.comboGoals;
        mascots = new Image[goals.Length];
        float step = edge * 2f / mascots.Length;
        for (int i = 0; i < mascots.Length; i++)
        {
            float at = x - edge + step * (i + 0.5f);
            mascots[i] = Picture(menu.transform, ComboBar.TierFace(Mathf.Min(i, ComboBar.TierCount - 1)), Color.white, top, center, new Vector2(at, y + 20f), new Vector2(48f, 48f));
            Label(menu.transform, font, "×" + goals[i], 16, new Color(Cream.r, Cream.g, Cream.b, 0.85f), TextAnchor.MiddleCenter, top, new Vector2(at, y - 14f));
        }
        comboNext = Label(menu.transform, font, "", 20, Cream, TextAnchor.MiddleLeft, top, new Vector2(x - edge, y - 44f));
        Fit(comboNext, edge * 2f, 28f, 20);
        streakGoal = Label(menu.transform, font, "", 19, new Color(Cream.r, Cream.g, Cream.b, 0.9f), TextAnchor.MiddleLeft, top, new Vector2(x - edge, y - 74f));
        Fit(streakGoal, edge * 2f, 26f, 19);
        streakSteps = Label(menu.transform, font, "", 18, new Color(Cream.r, Cream.g, Cream.b, 0.75f), TextAnchor.MiddleLeft, top, new Vector2(x - edge, y - 103f));
    }

    void BuildShopOverlay()
    {
        Vector2 top = new Vector2(0.5f, 1f), center = new Vector2(0.5f, 0.5f), bottom = new Vector2(0.5f, 0f), topRight = new Vector2(1f, 1f);
        shop = Group("Shop");
        shop.alpha = 0f;
        shop.blocksRaycasts = false;
        shop.interactable = false;

        var dim = Picture(shop.transform, Art.White, new Color(0.04f, 0.02f, 0.08f, 0.94f), Vector2.zero, center, Vector2.zero, Vector2.zero);
        Stretch(dim.rectTransform);
        dim.preserveAspect = false;

        var shade = Picture(shop.transform, Art.Vignette, new Color(0f, 0f, 0f, 0.82f), Vector2.zero, center, Vector2.zero, Vector2.zero);
        Stretch(shade.rectTransform);
        shade.preserveAspect = false;

        var glow = Picture(shop.transform, Art.Soft, new Color(0.2f, 0.9f, 0.6f, 0.15f), top, center, new Vector2(0f, -48f), new Vector2(900f, 140f));
        glow.preserveAspect = false;

        var title = Label(shop.transform, titleFont, "КУЗНИЦА КЛИНКОВ", 48, Color.white, TextAnchor.MiddleCenter, top, new Vector2(0f, -44f));
        Paint(title, Lime, Mint);
        Label(shop.transform, font, "Все клинки несут чисто косметический эффект и сохраняют соревновательный баланс", 18, Cream, TextAnchor.MiddleCenter, top, new Vector2(0f, -80f));

        var shopBadge = Picture(shop.transform, Art.Plate, new Color(0.18f, 0.12f, 0.28f, 0.95f), topRight, center, new Vector2(-130f, -46f), new Vector2(210f, 44f));
        shopBadge.type = Image.Type.Sliced;
        Picture(shopBadge.rectTransform, Art.Crystal, Color.white, new Vector2(0f, 0.5f), center, new Vector2(24f, 0f), new Vector2(24f, 24f));
        shopEssenceText = Label(shopBadge.rectTransform, titleFont, "0", 22, Mint, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(14f, 0f));

        var blades = GameManager.Blades;
        bladeCardRects = new RectTransform[blades.Length];
        bladeCardBtnTexts = new Text[blades.Length];
        bladeCardSwatches = new Image[blades.Length];

        for (int i = 0; i < blades.Length; i++)
        {
            var blade = blades[i];
            int col = i % 4;
            int row = i / 4;
            float cx = -420f + col * 280f;
            float cy = 84f - row * 134f;

            var cardGo = new GameObject("Card_" + blade.id, typeof(RectTransform));
            var cardRt = (RectTransform)cardGo.transform;
            cardRt.SetParent(shop.transform, false);
            cardRt.anchorMin = cardRt.anchorMax = center;
            cardRt.pivot = center;
            cardRt.anchoredPosition = new Vector2(cx, cy);
            cardRt.sizeDelta = new Vector2(264f, 122f);
            bladeCardRects[i] = cardRt;

            var cardBg = Picture(cardRt, Art.Plate, new Color(0.13f, 0.08f, 0.22f, 0.96f), center, center, Vector2.zero, new Vector2(264f, 122f));
            cardBg.type = Image.Type.Sliced;

            bladeCardSwatches[i] = Picture(cardRt, Art.Streak, blade.color, top, center, new Vector2(0f, -14f), new Vector2(190f, 14f));
            bladeCardSwatches[i].preserveAspect = false;

            Label(cardRt, titleFont, blade.name, 19, Color.white, TextAnchor.MiddleCenter, top, new Vector2(0f, -34f));

            Color rarityColor = blade.rarity == "Легендарный" ? Gold : blade.rarity == "Эпический" ? Lilac : blade.rarity == "Редкий" ? Mint : Cream;
            Label(cardRt, font, blade.rarity.ToUpper(), 13, rarityColor, TextAnchor.MiddleCenter, top, new Vector2(0f, -54f));

            var btn = BuildActionButton(cardRt, bottom, new Vector2(0f, 20f), new Vector2(220f, 30f), "", Mint);
            bladeCardBtnTexts[i] = btn.GetComponentInChildren<Text>();
            bladeCardBtnTexts[i].fontSize = 17;
        }

        shopBackBtnRect = BuildActionButton(shop.transform, bottom, new Vector2(0f, 40f), new Vector2(280f, 44f), "← НАЗАД В МЕНЮ", Cream);
    }

    // Ползунок громкости: подпись, дорожка с заливкой, бегунок и проценты. pos — середина дорожки.
    VolumeBar BuildVolume(Transform parent, Vector2 anchor, Vector2 pos, float width)
    {
        Vector2 left = new Vector2(0f, 0.5f), center = new Vector2(0.5f, 0.5f);
        var bar = new VolumeBar { width = width };
        Label(parent, font, "Громкость", 18, new Color(Cream.r, Cream.g, Cream.b, 0.8f), TextAnchor.MiddleRight, anchor, pos + new Vector2(-width * 0.5f - 16f, 0f));
        var track = Picture(parent, Art.Pill, new Color(0f, 0f, 0f, 0.6f), anchor, center, pos, new Vector2(width + 6f, 16f));
        track.type = Image.Type.Sliced;
        bar.track = track.rectTransform;
        var fill = Picture(parent, Art.Pill, Mint, anchor, left, pos + new Vector2(-width * 0.5f, 0f), new Vector2(width, 10f));
        fill.type = Image.Type.Sliced;
        bar.fill = fill.rectTransform;
        bar.knob = Picture(parent, Art.Pill, Color.white, anchor, center, pos, new Vector2(22f, 22f)).rectTransform;
        bar.percent = Label(parent, font, "", 18, Cream, TextAnchor.MiddleLeft, anchor, pos + new Vector2(width * 0.5f + 16f, 0f));
        return bar;
    }

    // Мышь на ползунке двигает громкость, колёсико меняет её шагами по пять процентов.
    void UpdateVolume(VolumeBar bar, bool active, bool began, bool down, Vector2 screen)
    {
        float volume = Sfx.Volume;
        if (active)
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(bar.track, screen, null, out local);
            bool inside = Mathf.Abs(local.y) < 20f && Mathf.Abs(local.x) < bar.width * 0.5f + 18f;
            if (began && inside) dragged = bar;
            if (dragged == bar && down) volume = Mathf.Clamp01(local.x / bar.width + 0.5f);
            float wheel = Blade.Wheel();
            if (wheel != 0f) volume = Mathf.Clamp01(Mathf.Round((volume + wheel * 0.05f) * 20f) / 20f);
            if (!Mathf.Approximately(volume, Sfx.Volume)) Sfx.Volume = volume;
        }
        if (!down && dragged == bar) dragged = null;
        bar.fill.sizeDelta = new Vector2(Mathf.Max(10f, bar.width * volume), 10f);
        bar.knob.anchoredPosition = bar.track.anchoredPosition + new Vector2((volume - 0.5f) * bar.width, 0f);
        bar.percent.text = Mathf.RoundToInt(volume * 100f) + "%";
    }

    // Панель меню: фиолетовое поле в золотой рамке с ромбами по углам (картинка Art/Interface/Plate).
    void Plate(float x, float y, float w, float h)
    {
        var plate = Picture(menu.transform, Art.Plate, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(w, h));
        plate.type = Image.Type.Sliced;
    }

    // Выделяет слово цветом внутри строки.
    static string Tint(string text, Color color)
    {
        return "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + text + "</color>";
    }

    // Строка живёт в своём прямоугольнике и ужимается, если шрифт на этой машине шире: за рамку текст не вылезет.
    static void Fit(Text label, float width, float height, int size)
    {
        label.rectTransform.sizeDelta = new Vector2(width, height);
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 11;
        label.resizeTextMaxSize = size;
    }

    // ---------- Экраны ----------

    public void ShowStory()
    {
        menuTarget = gameTarget = overTarget = 0f;
        over.alpha = 0f;
        hintLeft = comboLeft = 0f;
        story.Show();
    }

    public void ShowMenu(int best, int bestCombo, int bestStreak, int totalSliced)
    {
        story.Hide();
        menuTarget = 1f;
        gameTarget = 0f;
        overTarget = 0f;
        over.alpha = 0f;
        hintLeft = comboLeft = 0f;

        // Звание: значок, название и полоса пути окрашены в его цвет; в конце полосы — значок следующего звания.
        var balance = Balance.Current;
        int rank = balance.RankOf(best);
        Color tint = balance.ranks[rank].color;
        float way = 1f;
        shownRank = rank;
        rankName.text = balance.ranks[rank].name;
        Paint(rankName, Color.white, tint);
        rankIcon.sprite = RankIcon(rank);
        if (rank + 1 < balance.ranks.Length)
        {
            int from = balance.ranks[rank].score, to = balance.ranks[rank + 1].score;
            way = Mathf.Clamp01((best - from) / (float)Mathf.Max(1, to - from));
            rankNext.text = "До звания " + Tint("«" + balance.ranks[rank + 1].name + "»", balance.ranks[rank + 1].color) + ": " + Tint(Number(to - best), Color.white);
            rankGoal.sprite = RankIcon(rank + 1);
        }
        else
        {
            rankNext.text = Tint("Высшее звание получено!", tint);
            rankGoal.sprite = RankIcon(rank);
        }
        float filled = Mathf.Max(16f, rankWidth * way);
        rankFill.sizeDelta = new Vector2(filled, 16f);
        rankFill.GetComponent<Image>().color = tint;
        rankHead.rectTransform.anchoredPosition = new Vector2(rankLeft + filled - 4f, rankHead.rectTransform.anchoredPosition.y);
        for (int i = 0; i < rankSteps.Length; i++) rankSteps[i].color = i <= rank ? Color.white : Locked;
        rankBest.text = "ВСЕГО РАЗРУБЛЕНО: " + Tint(Number(totalSliced), Gold);
        bestLine.text = best > 0 ? "Лучший результат: " + Tint(Number(best), Color.white) : "";

        // Комбо: ступени идут через каждые пять слаймов в одном взмахе.
        int[] goals = balance.comboGoals;
        openTiers = balance.ComboStep(bestCombo);
        comboBest.text = bestCombo > 0 ? "Комбо: ×" + bestCombo : "Комбо впереди";
        for (int i = 0; i < mascots.Length; i++)
            mascots[i].color = i < openTiers ? ComboBar.TierColor(Mathf.Min(i, ComboBar.TierCount - 1)) : Locked;
        comboNext.text = openTiers < goals.Length
            ? "Цель: комбо " + Tint("×" + goals[openTiers], Rose) + " одним взмахом"
            : Tint("Все ступени комбо взяты!", Rose);

        // Серия: лучшая, ближайшая цель и вехи — взятые окрашены в цвет своего уровня полосы серии.
        streakGoal.text = "Серия без промаха: " + Tint("×" + bestStreak, Color.white) + "    цель " + Tint("×" + balance.NextStreakGoal(bestStreak), Rose);
        string steps = "Вехи серии:";
        for (int i = 0; i < balance.streakGoals.Length; i++)
            steps += "   " + Tint("×" + balance.streakGoals[i], bestStreak >= balance.streakGoals[i] ? StreakBar.TierColor(i + 1) : Locked);
        streakSteps.text = steps;
        RefreshShopUI();
    }

    public void ShowPause(bool on)
    {
        pauseTarget = on ? 1f : 0f;
    }

    public void ShowGame(int best)
    {
        menuTarget = 0f;
        gameTarget = 1f;
        overTarget = 0f;
        isShopOpen = false;
        shopTarget = 0f;
        if (shop != null) shop.alpha = 0f;
        // Рестарт должен ощущаться мгновенным, поэтому старые экраны не затухают, а исчезают сразу.
        menu.alpha = 0f;
        over.alpha = 0f;
        bestText.text = best > 0 ? "Рекорд: " + Number(best) : "";
        hintLeft = comboLeft = deltaLeft = 0f;
        deltaSum = 0;
        streak.Restart();
    }

    public void ShowGameOver(int score, int best, bool newBest, int sliced, int bestCombo, int bestStreak, bool newRank)
    {
        // Звание по рекорду и сколько осталось до следующего: причина нажать «ещё раз».
        var balance = Balance.Current;
        int rank = balance.RankOf(best);
        string title = Tint(balance.ranks[rank].name, balance.ranks[rank].color);
        if (newRank) overRank.text = "НОВОЕ ЗВАНИЕ: «" + title + "»!";
        else if (rank + 1 < balance.ranks.Length)
            overRank.text = "Звание: " + title + "     до «" + Tint(balance.ranks[rank + 1].name, balance.ranks[rank + 1].color) + "» — "
                + Number(balance.ranks[rank + 1].score - best);
        else overRank.text = "Звание: " + title;

        int earned = GameManager.Instance != null ? GameManager.Instance.LastEarnedEssence : GameManager.CalculateEarnedEssence(score, bestCombo, sliced);
        if (overEssence != null)
        {
            int total = GameManager.Instance != null ? GameManager.Instance.Essence : 0;
            overEssence.text = "+ " + Number(earned) + " ЭССЕНЦИИ (за счёт и комбо)  •  Всего: " + Number(total);
        }

        bool canRevive = GameManager.Instance != null && GameManager.Instance.CanRevive();
        if (reviveBtnRect != null)
        {
            reviveBtnRect.gameObject.SetActive(canRevive);
        }
        if (overPrompt != null)
        {
            overPrompt.rectTransform.anchoredPosition = new Vector2(0f, canRevive ? -260f : -206f);
        }
        if (overEscPrompt != null)
        {
            overEscPrompt.rectTransform.anchoredPosition = new Vector2(0f, canRevive ? -300f : -250f);
        }

        overTarget = 1f;
        gameTarget = 0.35f;
        overShownAt = Time.unscaledTime;
        overCounted = false;
        overTargetScore = score;
        overBest.text = newBest ? "НОВЫЙ РЕКОРД!" : "Лучший: " + Number(best);
        overBest.color = newBest ? Mint : Cream;
        overStats.text = "Разрезано: " + sliced + (bestCombo > 0 ? "     •     Комбо: ×" + bestCombo : "") + "     •     Серия: ×" + bestStreak;
        hintLeft = 0f;
    }

    public void UpdateBladeButton()
    {
        if (bladeBtnText == null || GameManager.Instance == null) return;
        var gm = GameManager.Instance;
        int idx = gm.SelectedBladeIndex;
        var blade = GameManager.Blades[Mathf.Clamp(idx, 0, GameManager.Blades.Length - 1)];
        bladeBtnText.text = "КУЗНИЦА КЛИНКОВ  •  " + blade.name.ToUpper();
        bladeBtnText.color = blade.color;
    }

    public void RefreshShopUI()
    {
        UpdateBladeButton();
        if (GameManager.Instance == null) return;
        var gm = GameManager.Instance;
        if (menuEssenceText != null) menuEssenceText.text = Number(gm.Essence);
        if (shopEssenceText != null) shopEssenceText.text = "ЭССЕНЦИЯ: " + Number(gm.Essence);

        if (bladeCardBtnTexts != null)
        {
            for (int i = 0; i < bladeCardBtnTexts.Length; i++)
            {
                if (bladeCardBtnTexts[i] == null) continue;
                var blade = GameManager.Blades[i];
                bool unlocked = gm.IsBladeUnlocked(i);
                bool isSelected = gm.SelectedBladeIndex == i;

                if (bladeCardSwatches != null && bladeCardSwatches[i] != null)
                    bladeCardSwatches[i].color = blade.color;

                if (isSelected)
                {
                    bladeCardBtnTexts[i].text = "[ ВЫБРАН ]";
                    bladeCardBtnTexts[i].color = Gold;
                }
                else if (unlocked)
                {
                    bladeCardBtnTexts[i].text = "ВЫБРАТЬ";
                    bladeCardBtnTexts[i].color = Mint;
                }
                else if (gm.Essence >= blade.cost)
                {
                    bladeCardBtnTexts[i].text = "КУПИТЬ: " + Number(blade.cost);
                    bladeCardBtnTexts[i].color = Lime;
                }
                else
                {
                    bladeCardBtnTexts[i].text = "КУПИТЬ: " + Number(blade.cost);
                    bladeCardBtnTexts[i].color = Locked;
                }
            }
        }
    }

    public void OpenShop(bool open = true)
    {
        isShopOpen = open;
        RefreshShopUI();
        Sfx.Toss();
    }

    public void CloseShop()
    {
        OpenShop(false);
    }

    void OnBladeCardClicked(int index)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (gm.SelectedBladeIndex == index)
        {
            Sfx.Toss();
            return;
        }
        if (gm.IsBladeUnlocked(index))
        {
            gm.SelectBlade(index);
            Hint("Выбран клинок «" + GameManager.Blades[index].name + "»!", 1.8f);
        }
        else if (gm.Essence >= GameManager.Blades[index].cost)
        {
            gm.BuyBlade(index);
            Hint("Куплен клинок «" + GameManager.Blades[index].name + "»!", 2.2f);
        }
        else
        {
            Sfx.Miss();
            int needed = GameManager.Blades[index].cost - gm.Essence;
            Hint("Не хватает " + Number(needed) + " эссенции!", 1.8f);
        }
        RefreshShopUI();
    }

    // ---------- Счёт и жизни ----------

    public void SetScore(int score, bool punch)
    {
        if (punch)
        {
            // Под счётом копится прибавка за последние мгновения: «+30», «+44»…
            deltaSum = deltaLeft > 0f ? deltaSum + score - shownScore : score - shownScore;
            deltaLeft = 0.8f;
            deltaText.text = "+" + Number(deltaSum);
            scorePunch = 1f;
        }
        shownScore = score;
        scoreText.text = Number(score);
    }

    public void SetLives(int lives, bool punch)
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            bool on = i < lives;
            if (punch && !on && hearts[i].color == HeartOn) heartPunch[i] = 1f;
            hearts[i].color = on ? HeartOn : HeartOff;
        }
    }

    // ---------- Отклик ----------

    public void Flash(Color color, float alpha)
    {
        color.a = Mathf.Max(alpha, flash.color.a);
        flash.color = color;
    }

    public void Hint(string text, float seconds)
    {
        hintText.text = text;
        hintLeft = hintSpan = seconds;
    }

    public void Combo(int count, int bonus, Color tint)
    {
        string word = count >= 10 ? "УЛЬТРАКОМБО" : count >= 7 ? "МЕГАКОМБО" : count >= 5 ? "СУПЕРКОМБО" : "КОМБО";
        Shout(word + " ×" + count + "!", "+" + Number(bonus), 0.9f + 0.12f * Mathf.Min(count, 6), tint);
    }

    // Крупная надпись по центру экрана: комбо, начало попытки. Буквы и зарево окрашены в tint.
    public void Shout(string text, string below, float seconds, Color tint)
    {
        comboTint = tint;
        Paint(comboText, Color.white, tint);
        comboText.text = text;
        comboText.fontSize = text.Length > 13 ? 70 : text.Length > 10 ? 82 : 96;   // длинное слово — буквы чуть мельче
        comboBonus.text = below;
        comboLeft = comboSpan = seconds;
    }

    static readonly Queue<HudPopup> popupPool = new Queue<HudPopup>();

    public static void RecyclePopup(HudPopup popup)
    {
        if (popup != null) popupPool.Enqueue(popup);
    }

    public static void ClearPopupPool()
    {
        while (popupPool.Count > 0)
        {
            var p = popupPool.Dequeue();
            if (p != null && p.gameObject != null)
            {
                if (Application.isPlaying) Destroy(p.gameObject);
                else DestroyImmediate(p.gameObject);
            }
        }
        popupPool.Clear();
    }

    // Убирает все всплывающие надписи разом: перед крупной надписью, чтобы буквы не наслаивались друг на друга.
    public void ClearPopups()
    {
        for (int i = popups.childCount - 1; i >= 0; i--)
        {
            var child = popups.GetChild(i);
            var hp = child.GetComponent<HudPopup>();
            if (hp != null)
            {
                child.gameObject.SetActive(false);
                RecyclePopup(hp);
            }
            else
            {
                Destroy(child.gameObject);
            }
        }
    }

    public void Popup(Vector2 world, string text, Color color, int size, float life = 0.9f)
    {
        Rect view = GameManager.View;
        Vector2 canvas = root.rect.size;
        // Надпись не должна уезжать за край экрана.
        float nx = Mathf.Clamp((world.x - view.xMin) / view.width, 0.06f, 0.94f);
        float ny = Mathf.Clamp((world.y - view.yMin) / view.height, 0.08f, 0.9f);
        Vector2 pos = new Vector2(nx * canvas.x, ny * canvas.y);

        HudPopup popup = null;
        while (popupPool.Count > 0)
        {
            var candidate = popupPool.Dequeue();
            if (candidate != null && candidate.gameObject != null)
            {
                popup = candidate;
                break;
            }
        }

        if (popup == null)
        {
            var label = Label(popups, font, text, size, color, TextAnchor.MiddleCenter, Vector2.zero, pos);
            popup = label.gameObject.AddComponent<HudPopup>();
        }
        else
        {
            popup.gameObject.SetActive(true);
        }

        popup.Setup(font, text, size, color, life, pos);
    }

    public void MissMark(float worldX)
    {
        Rect view = GameManager.View;
        Popup(new Vector2(worldX, view.yMin + view.height * 0.12f), "×", Alarm, 120, 0.8f);
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        float time = Time.unscaledTime;
        var gm = GameManager.Instance;
        float pulse = 0.5f + 0.5f * Mathf.Sin(time * 5f);

        menu.alpha = Mathf.MoveTowards(menu.alpha, menuTarget, dt * 6f);
        game.alpha = Mathf.MoveTowards(game.alpha, gameTarget, dt * 6f);
        over.alpha = Mathf.MoveTowards(over.alpha, overTarget, dt * 5f);
        pause.alpha = Mathf.MoveTowards(pause.alpha, pauseTarget, dt * 8f);
        if (shop != null)
        {
            shopTarget = isShopOpen ? 1f : 0f;
            shop.alpha = Mathf.MoveTowards(shop.alpha, shopTarget, dt * 7f);
            shop.blocksRaycasts = shop.alpha > 0.5f;
            shop.interactable = shop.alpha > 0.5f;
        }

        // Ползунки громкости: в меню и на паузе.
        Vector2 pointer;
        bool pointerDown = Blade.Pointer(out pointer);
        bool pointerBegan = pointerDown && !pointerWasDown;
        pointerWasDown = pointerDown;
        UpdateVolume(menuVolume, gm.State == GameState.Menu && !isShopOpen, pointerBegan, pointerDown, pointer);
        UpdateVolume(pauseVolume, gm.Paused, pointerBegan, pointerDown, pointer);

        if (isShopOpen)
        {
            if (Blade.EscapePressed())
            {
                CloseShop();
            }
            else if (pointerBegan)
            {
                if (shopBackBtnRect != null && IsInside(shopBackBtnRect, pointer))
                {
                    CloseShop();
                }
                else if (bladeCardRects != null)
                {
                    for (int i = 0; i < bladeCardRects.Length; i++)
                    {
                        if (bladeCardRects[i] != null && IsInside(bladeCardRects[i], pointer))
                        {
                            OnBladeCardClicked(i);
                            break;
                        }
                    }
                }
            }
        }
        else if (pointerBegan)
        {
            if (gm.State == GameState.Playing && !gm.Paused)
            {
                if (pauseBtnRect != null && IsInside(pauseBtnRect, pointer))
                {
                    gm.SetPause(true);
                }
            }
            else if (gm.Paused)
            {
                if (resumeBtnRect != null && IsInside(resumeBtnRect, pointer))
                {
                    gm.SetPause(false);
                }
                else if (menuBtnRect != null && IsInside(menuBtnRect, pointer))
                {
                    gm.ExitToMenu();
                }
            }
            else if (gm.State == GameState.Menu)
            {
                if (bladeBtnRect != null && IsInside(bladeBtnRect, pointer))
                {
                    OpenShop(true);
                }
            }
            else if (gm.State == GameState.GameOver)
            {
                if (reviveBtnRect != null && reviveBtnRect.gameObject.activeSelf && IsInside(reviveBtnRect, pointer))
                {
                    gm.RequestReviveAd();
                }
            }
        }

        // Счёт: Punch Scale 1.0 → 1.2 → 1.0.
        scorePunch = Mathf.MoveTowards(scorePunch, 0f, dt * 6f);
        scoreText.transform.localScale = Vector3.one * (1f + 0.22f * scorePunch);
        deltaLeft -= dt;
        deltaText.color = new Color(Mint.r, Mint.g, Mint.b, Mathf.Clamp01(deltaLeft * 3f));

        for (int i = 0; i < hearts.Length; i++)
        {
            // Потерянное сердце подпрыгивает, вспыхивает белым и гаснет в серый контур.
            heartPunch[i] = Mathf.MoveTowards(heartPunch[i], 0f, dt * 3f);
            float p = heartPunch[i];
            hearts[i].transform.localScale = Vector3.one * (1f + 0.8f * p);
            hearts[i].rectTransform.anchoredPosition = new Vector2(hearts[i].rectTransform.anchoredPosition.x, -54f + 26f * Mathf.Sin(p * Mathf.PI));
            if (p > 0f) hearts[i].color = Color.Lerp(HeartOff, Color.white, p);
            else if (gm.State == GameState.Playing && gm.Lives == 1 && i == 0)
                hearts[i].transform.localScale = Vector3.one * (1f + 0.14f * pulse); // последнее сердце бьётся
        }

        Color f = flash.color;
        f.a = Mathf.MoveTowards(f.a, 0f, dt * 2.2f);
        flash.color = f;

        // Подсказка выезжает сверху и уходит обратно.
        hintLeft -= dt;
        float hintIn = Mathf.Clamp01((hintSpan - hintLeft) * 5f), hintOut = Mathf.Clamp01(hintLeft * 3f);
        hintText.color = new Color(Mint.r, Mint.g, Mint.b, hintLeft > 0f ? hintOut : 0f);
        hintText.rectTransform.anchoredPosition = new Vector2(0f, -184f + 60f * (1f - hintIn) * (1f - hintIn));

        // «КОМБО ×N!» вспыхивает по центру: удар масштабом с золотым заревом, затем надпись держится и гаснет.
        comboLeft -= dt;
        float ck = comboSpan > 0f ? Mathf.Clamp01(1f - comboLeft / comboSpan) : 1f;
        float comboAlpha = comboLeft > 0f ? Mathf.Clamp01((1f - ck) * 3f) : 0f;
        float comboHit = Mathf.Max(0f, 1f - ck * 6f);
        comboText.color = new Color(1f, 1f, 1f, comboAlpha);
        comboBonus.color = new Color(Cream.r, Cream.g, Cream.b, comboAlpha);
        comboText.transform.localScale = Vector3.one * (1f + 0.9f * comboHit + 0.08f * ck + 0.03f * Mathf.Sin(time * 16f));
        comboText.transform.localRotation = Quaternion.Euler(0f, 0f, -3f);
        float comboIn = Mathf.Clamp01(ck * 9f);
        comboRays.color = new Color(comboTint.r, comboTint.g, comboTint.b, comboAlpha * 0.4f);
        comboRays.transform.localRotation = Quaternion.Euler(0f, 0f, time * 45f);
        comboRays.transform.localScale = Vector3.one * (0.6f + 0.5f * comboIn + 0.15f * ck);
        comboBand.color = new Color(comboTint.r * 0.2f, comboTint.g * 0.15f, comboTint.b * 0.28f, comboAlpha * 0.82f);
        comboBand.transform.localScale = new Vector3(0.25f + 0.75f * comboIn, 1f - 0.25f * ck, 1f);
        comboBand.transform.localRotation = Quaternion.Euler(0f, 0f, -3f);
        comboGlow.color = new Color(comboTint.r, comboTint.g, comboTint.b, comboAlpha * (0.4f + 0.4f * comboHit));
        comboGlow.transform.localScale = Vector3.one * (1f + 0.5f * comboHit);

        bool frenzyOn = gm.FrenzyLeft > 0f && gm.State == GameState.Playing;

        // Кайма экрана: цвет уровня комбо в перегрузке, золото в трансмутации, тревожный красный на последнем сердце.
        Color target = Color.clear;
        if (gm.State == GameState.Playing)
        {
            if (gm.SlowLeft > 0f) target = new Color(1f, 0.9f, 0.55f, 0.26f);
            else if (frenzyOn) target = new Color(combo.Heat.r, combo.Heat.g, combo.Heat.b, 0.06f + 0.03f * pulse);
            else if (gm.Lives == 1) target = new Color(1f, 0.1f, 0.15f, 0.07f + 0.08f * pulse);
        }
        Color a = aura.color;
        if (target.a > 0f) a = new Color(target.r, target.g, target.b, a.a);
        a.a = Mathf.MoveTowards(a.a, target.a, dt * 1.5f);
        aura.color = a;

        // Меню живёт тихо: зарево за названием мерцает, кошка дышит и изредка моргает,
        // значок текущего звания в лестнице пульсирует, открытые слаймы комбо покачиваются.
        titleGlow.color = new Color(0.3f, 1f, 0.4f, 0.16f + 0.06f * pulse);
        if (time > catBlinkAt + 0.16f) catBlinkAt = time + Random.Range(2.5f, 6f);
        cat.sprite = time > catBlinkAt ? Art.CatBlink : Art.Cat;
        cat.transform.localScale = new Vector3(1f, 1f + 0.012f * Mathf.Sin(time * 1.7f), 1f);
        rankIcon.transform.localRotation = Quaternion.Euler(0f, 0f, 4f * Mathf.Sin(time * 1.4f));
        rankGoal.transform.localScale = Vector3.one * (0.92f + 0.1f * pulse);
        rankHead.transform.localScale = Vector3.one * (0.85f + 0.25f * pulse);
        rankHead.transform.localRotation = Quaternion.Euler(0f, 0f, -time * 40f);
        for (int i = 0; i < rankSteps.Length; i++)
            rankSteps[i].transform.localScale = Vector3.one * (i == shownRank ? 1.18f + 0.1f * pulse : 1f);
        for (int i = 0; i < mascots.Length; i++)
        {
            float hop = i < openTiers ? Mathf.Abs(Mathf.Sin(time * 3f + i * 0.9f)) : 0f;
            mascots[i].transform.localScale = new Vector3(1f + 0.05f * (1f - hop), 1f - 0.05f * (1f - hop), 1f) * (i < openTiers ? 1f : 0.9f);
        }
        menuPrompt.transform.localScale = Vector3.one * (1f + 0.05f * pulse);
        menuPrompt.color = new Color(Mint.r, Mint.g, Mint.b, 0.75f + 0.25f * pulse);
        float swipe = Mathf.Repeat(time, 1.9f) / 0.55f;
        ghost.anchoredPosition = new Vector2(Mathf.Lerp(-200f, 200f, swipe), Mathf.Lerp(-66f, 66f, swipe));
        ghostImage.color = new Color(1f, 1f, 1f, swipe < 1f ? 0.75f * Mathf.Sin(swipe * Mathf.PI) : 0f);

        // Финальный счёт «набегает» за полсекунды; приглашение к рестарту появляется, когда рестарт уже принимается.
        float since = time - overShownAt;
        if (overTarget > 0f && !overCounted)
        {
            float counted = Mathf.Clamp01(since / 0.55f);
            overScore.text = Number(Mathf.RoundToInt(overTargetScore * counted));
            overCounted = counted >= 1f;
        }
        overPrompt.transform.localScale = Vector3.one * (1f + 0.05f * pulse);
        overPrompt.color = new Color(Mint.r, Mint.g, Mint.b, since >= GameManager.RestartLock ? 0.7f + 0.3f * pulse : 0f);
    }

    // ---------- Кирпичики ----------

    static string Number(int value)
    {
        return value.ToString("N0", CultureInfo.InvariantCulture);
    }

    // Шрифт лежит в проекте (Assets/Resources/Fonts). Если файла нет, берётся похожий системный, а затем встроенный.
    static Font LoadFont(string file, params string[] fallbacks)
    {
        var own = Resources.Load<Font>("Fonts/" + file);
        if (own != null) return own;
        var installed = new HashSet<string>(Font.GetOSInstalledFontNames());
        foreach (var name in fallbacks)
        {
            if (!installed.Contains(name)) continue;
            var os = Font.CreateDynamicFontFromOSFont(name, 64);
            if (os != null) return os;
        }
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    // Красит буквы градиентом сверху вниз. Цвет самого текста остаётся белым: им управляют вспышки и прозрачность.
    public static void Paint(Text text, Color top, Color bottom)
    {
        var gradient = text.GetComponent<TextGradient>();
        gradient.top = top;
        gradient.bottom = bottom;
        text.color = new Color(1f, 1f, 1f, text.color.a);
    }

    CanvasGroup Group(string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(root, false);
        Stretch(rt);
        var group = go.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        return group;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public static Image Picture(Transform parent, Sprite sprite, Color color, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go = new GameObject("Image", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        image.preserveAspect = true;
        return image;
    }

    public static Text Label(Transform parent, Font face, string content, int size, Color color, TextAnchor align, Vector2 anchor, Vector2 pos)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor;
        // Опорная точка совпадает с выравниванием: текст растёт от заданной позиции в нужную сторону.
        rt.pivot = new Vector2(((int)align % 3) * 0.5f, 1f - ((int)align / 3) * 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = Vector2.zero;
        var text = go.AddComponent<Text>();
        text.font = face;
        text.fontSize = size;
        text.fontStyle = FontStyle.Normal;  // жирность заложена в самом шрифте
        text.color = color;
        text.alignment = align;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.text = content;
        // Градиент стоит раньше контура, иначе он перекрасил бы и сам контур.
        go.AddComponent<TextGradient>();
        var outline = go.AddComponent<Outline>();
        outline.effectColor = Plum;
        float thick = Mathf.Clamp(size * 0.04f, 1.5f, 4f);
        outline.effectDistance = new Vector2(thick, -thick);
        return text;
    }

    RectTransform BuildPauseButton(Transform parent, Vector2 anchor, Vector2 pos)
    {
        var go = new GameObject("PauseBtn", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(50f, 50f);

        var bg = Picture(rt, Art.Plate, new Color(0.18f, 0.12f, 0.28f, 0.95f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(50f, 50f));
        bg.type = Image.Type.Sliced;

        Picture(rt, Art.Pill, Cream, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-6f, 0f), new Vector2(5f, 20f));
        Picture(rt, Art.Pill, Cream, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(6f, 0f), new Vector2(5f, 20f));

        return rt;
    }

    RectTransform BuildActionButton(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, string text, Color textColor)
    {
        var go = new GameObject("Btn_" + text, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var bg = Picture(rt, Art.Plate, new Color(0.18f, 0.12f, 0.28f, 0.95f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
        bg.type = Image.Type.Sliced;

        Label(rt, titleFont, text, 24, textColor, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero);
        return rt;
    }

    public bool IsPointerOverUI(Vector2 screenPoint)
    {
        if (isShopOpen) return true;
        if (game != null && game.alpha > 0.5f && pauseBtnRect != null && IsInside(pauseBtnRect, screenPoint))
            return true;
        if (GameManager.Instance != null && GameManager.Instance.Paused)
        {
            if (resumeBtnRect != null && IsInside(resumeBtnRect, screenPoint)) return true;
            if (menuBtnRect != null && IsInside(menuBtnRect, screenPoint)) return true;
            if (pauseVolume != null && pauseVolume.track != null)
            {
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(pauseVolume.track, screenPoint, null, out local);
                if (Mathf.Abs(local.y) < 25f && Mathf.Abs(local.x) < pauseVolume.width * 0.5f + 25f) return true;
            }
        }
        if (GameManager.Instance != null && GameManager.Instance.State == GameState.Menu)
        {
            if (bladeBtnRect != null && IsInside(bladeBtnRect, screenPoint)) return true;
            if (menuVolume != null && menuVolume.track != null)
            {
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(menuVolume.track, screenPoint, null, out local);
                if (Mathf.Abs(local.y) < 25f && Mathf.Abs(local.x) < menuVolume.width * 0.5f + 25f) return true;
            }
        }
        if (GameManager.Instance != null && GameManager.Instance.State == GameState.GameOver)
        {
            if (reviveBtnRect != null && reviveBtnRect.gameObject.activeSelf && IsInside(reviveBtnRect, screenPoint)) return true;
        }
        return false;
    }

    public static bool IsInside(RectTransform rt, Vector2 screenPoint)
    {
        if (rt == null || !rt.gameObject.activeInHierarchy) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(rt, screenPoint, null);
    }
}

// Всплывающая надпись: выскакивает, поднимается и гаснет.
public class HudPopup : MonoBehaviour
{
    public float life = 0.9f;

    RectTransform rt;
    Text text;
    Vector2 origin;
    Color color;
    float age;

    public void Setup(Font face, string content, int size, Color col, float lifeSpan, Vector2 pos)
    {
        if (rt == null) rt = (RectTransform)transform;
        if (text == null) text = GetComponent<Text>();
        text.font = face;
        text.fontSize = size;
        text.text = content;
        text.color = col;
        color = col;
        origin = pos;
        rt.anchoredPosition = pos;
        rt.localScale = Vector3.one;
        life = lifeSpan;
        age = 0f;
    }

    void Start()
    {
        if (rt == null) rt = (RectTransform)transform;
        if (text == null) text = GetComponent<Text>();
        if (origin == Vector2.zero) origin = rt.anchoredPosition;
        if (color == default(Color)) color = text.color;
    }

    void Update()
    {
        age += Time.unscaledDeltaTime;
        float k = Mathf.Clamp01(age / life);
        rt.anchoredPosition = origin + Vector2.up * (70f * (1f - (1f - k) * (1f - k)));
        rt.localScale = Vector3.one * (1f + 0.6f * Mathf.Max(0f, 1f - k * 6f));
        text.color = new Color(color.r, color.g, color.b, Mathf.Clamp01((1f - k) * 2.5f));
        if (k >= 1f)
        {
            gameObject.SetActive(false);
            Hud.RecyclePopup(this);
        }
    }
}
