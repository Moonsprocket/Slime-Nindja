using System.Collections.Generic;
using UnityEngine;

public enum Kind { Acid, Magma, Geode, Mercury, Blue, Mini, King, Nugget, Bomb }

// Описание летающего объекта: очки, размер и готовые спрайты.
public class SlimeDef
{
    public Kind kind;
    public string name;
    public int points;
    public float size = 1f;      // радиус относительно базового
    public float spin = 1f;      // множитель скорости вращения
    public bool penalty = true;  // упущенный стоит жизни
    public Color body, deep, core, glow;
    public Sprite whole, half, shell;
    public Sprite blink;         // тот же слайм с закрытыми глазами; есть только у слаймов с мордочкой

    public bool IsBomb { get { return kind == Kind.Bomb; } }
}

// Графика игры. Каждая картинка — файл PNG в Assets/Resources/Art: его можно открыть, перерисовать или заменить своим.
// Формулы ниже — запасной художник: если файла нет, картинка рисуется кодом. Исходные файлы создаются из этих же
// формул через меню редактора «Алхимия → Картинки».
public static class Art
{
    public const string Folder = "Art";

    const int N = 160;              // сторона текстуры объекта радиусом 1
    const float FaceTilt = 0.36f;   // сплюснутость среза у половинки
    const float TallExtent = 1.45f; // поле текстуры шире корпуса: влезают корона, шипы и фитиль

    public const float BombFuseTip = 1.3f;
    public const float RimUnits = 36f; // длина борта котла в «заклёпочных» единицах

    public static SlimeDef Acid, Magma, Geode, Mercury, Blue, Mini, King, Nugget, Bomb;
    public static Sprite White, Soft, Dot, Star, Ring, Heart, Shard, Crystal, Coin, Bead, Drip, Flame;
    public static Sprite Streak, Rays, Shock;       // росчерк клинка, лучи вспышки и ударная волна
    public static Sprite Wizard, WizardShock, Cauldron, Sickle, Burst; // герои комикса-предыстории
    public static Sprite Panel, Frame, Vignette, Wall, Rim;
    public static Sprite Pill, PillRing, PillGlow;  // полоска комбо: заливка, неоновый контур и ореол
    public static Sprite[] Mascot;                  // слайм на конце полоски: довольный, дерзкий, свирепый
    public static Sprite Cat, CatBlink;             // кошка алхимика на главном экране
    public static Sprite[] Potions;                 // склянки с зельями там же
    // Слои лаборатории нарисованы художником, формулы у них нет: полки по бокам (половинами) и борт котла.
    public static Sprite ShelvesLeft, ShelvesRight, Brim;
    public static Sprite Plate, Ribbon, Badge, Crown;   // панель меню в золотой рамке, лента под логотипом, табличка серии, корона
    public static Sprite[] Ranks;                   // значки званий
    public static Sprite[] Splats;
    public static Material Mat, SoftMat, DotMat, StarMat;

    static readonly Dictionary<string, Texture2D> drawn = new Dictionary<string, Texture2D>(); // нарисованное кодом
    static readonly HashSet<string> fromFile = new HashSet<string>();                          // взятое из файлов
    static System.Action<string, Draft> export;

    static readonly Color Clear = new Color(0f, 0f, 0f, 0f);
    static readonly Color EyeDark = new Color(0.05f, 0.05f, 0.12f);
    static readonly Vector3[] Bubbles =
    {
        new Vector3(-0.5f, -0.48f, 0.1f), new Vector3(0.62f, -0.44f, 0.07f), new Vector3(0.3f, 0.62f, 0.08f),
        new Vector3(-0.12f, 0.68f, 0.05f), new Vector3(0.7f, 0.3f, 0.05f), new Vector3(-0.76f, 0.2f, 0.045f),
        new Vector3(0.14f, -0.72f, 0.06f)
    };
    static readonly Vector3[] OneCore = { new Vector3(0f, -0.24f, 0.3f) };
    // Два ядра внизу синего слайма подсказывают, что он разделится надвое.
    static readonly Vector3[] TwoCores = { new Vector3(-0.3f, -0.5f, 0.17f), new Vector3(0.3f, -0.5f, 0.17f) };
    static readonly Vector3[] NoCores = new Vector3[0];

    // Мордочка: весёлая у желе, сердитая у магмы, невозмутимая у ртути. Blink — та же, но глаза закрыты.
    const int NoFace = 0, Happy = 1, Grumpy = 2, Cool = 3, Fierce = 4, Blink = 8;

    // Силуэты. У каждого вида слайма свой, но все остаются каплями желе: шар (половинки и самородок), пудинг
    // с узкой макушкой, капля с хохолком, двойная макушка, бутон, гранёный самоцвет, камень с языками пламени.
    const int Ball = 0, Pudding = 1, Drop = 2, Twin = 3, Bud = 4, Gem = 5, Tongues = 6;

    public static void Build()
    {
        fromFile.Clear();
        BuildShapes();
        BuildSlimes();
        BuildStory();
        BuildScene();
        BuildMaterials();
    }

    // Прогоняет все формулы и отдаёт заготовки наружу: так редактор выгружает исходные картинки в PNG.
    public static void Export(System.Action<string, Draft> sink)
    {
        export = sink;
        try
        {
            BuildShapes();
            BuildSlimes();
            BuildStory();
            BuildScene();
        }
        finally
        {
            export = null;
        }
    }

    // ---------- Слаймы ----------

    static void BuildSlimes()
    {
        Acid = new SlimeDef { kind = Kind.Acid, name = "Acid", points = 10,
            body = C(0.5f, 1f, 0.28f), deep = C(0.08f, 0.62f, 0.14f), core = C(0.92f, 1f, 0.45f), glow = C(0.55f, 1f, 0.2f) };
        Magma = new SlimeDef { kind = Kind.Magma, name = "Magma", points = 20, size = 1.12f, spin = 0.5f,
            body = C(1f, 0.5f, 0.1f), deep = C(0.75f, 0.14f, 0.03f), core = C(1f, 0.92f, 0.4f), glow = C(1f, 0.5f, 0.1f) };
        Geode = new SlimeDef { kind = Kind.Geode, name = "Geode", points = 30, size = 1.2f, spin = 0.4f,
            body = C(0.8f, 0.48f, 1f), deep = C(0.42f, 0.13f, 0.74f), core = C(1f, 0.84f, 1f), glow = C(0.8f, 0.4f, 1f) };
        Mercury = new SlimeDef { kind = Kind.Mercury, name = "Mercury", points = 40, size = 0.85f, spin = 1.4f,
            body = C(0.85f, 0.9f, 0.97f), deep = C(0.35f, 0.4f, 0.52f), core = Color.white, glow = C(0.65f, 0.9f, 1f) };
        Blue = new SlimeDef { kind = Kind.Blue, name = "Blue", points = 10, size = 1.4f, spin = 0.5f,
            body = C(0.38f, 0.74f, 1f), deep = C(0.08f, 0.3f, 0.86f), core = C(0.82f, 0.96f, 1f), glow = C(0.3f, 0.75f, 1f) };
        Mini = new SlimeDef { kind = Kind.Mini, name = "Mini", points = 15, size = 0.72f,
            body = Blue.body, deep = Blue.deep, core = Blue.core, glow = Blue.glow };
        King = new SlimeDef { kind = Kind.King, name = "King", points = 100, size = 0.75f, spin = 0.25f, penalty = false,
            body = C(1f, 0.86f, 0.28f), deep = C(0.86f, 0.5f, 0.05f), core = C(1f, 1f, 0.75f), glow = C(1f, 0.85f, 0.3f) };
        Nugget = new SlimeDef { kind = Kind.Nugget, name = "Nugget", points = 25, size = 0.7f, penalty = false,
            body = King.body, deep = King.deep, core = King.core, glow = King.glow };
        Bomb = new SlimeDef { kind = Kind.Bomb, name = "Bomb", size = 0.92f, spin = 0.6f, penalty = false,
            body = C(0.16f, 0.1f, 0.22f), deep = C(0.08f, 0.05f, 0.12f), core = C(1f, 0.2f, 0.5f), glow = C(1f, 0.15f, 0.25f) };

        Acid.whole = Whole("Slimes/Acid", 1f, (x, y) => Jelly(Acid, x, y, NoCores, Happy, Drop));
        Acid.blink = Whole("Slimes/Acid_blink", 1f, (x, y) => Jelly(Acid, x, y, NoCores, Happy | Blink, Drop));
        Acid.half = Half("Slimes/Acid_half", (x, y) => Jelly(Acid, x, y, NoCores, NoFace), (x, y, e) => JellyFace(Acid, x, y, e, 0.34f));

        Blue.whole = Whole("Slimes/Blue", 1f, (x, y) => Jelly(Blue, x, y, TwoCores, Happy, Twin));
        Blue.blink = Whole("Slimes/Blue_blink", 1f, (x, y) => Jelly(Blue, x, y, TwoCores, Happy | Blink, Twin));
        Mini.whole = Whole("Slimes/Mini", 1f, (x, y) => Jelly(Mini, x, y, NoCores, Happy, Bud));
        Mini.blink = Whole("Slimes/Mini_blink", 1f, (x, y) => Jelly(Mini, x, y, NoCores, Happy | Blink, Bud));
        Mini.half = Half("Slimes/Mini_half", (x, y) => Jelly(Mini, x, y, NoCores, NoFace), (x, y, e) => JellyFace(Mini, x, y, e, 0.34f));
        Blue.half = Mini.half;

        Geode.whole = Whole("Slimes/Geode", 1f, (x, y) => Jelly(Geode, x, y, NoCores, Happy, Gem));
        Geode.blink = Whole("Slimes/Geode_blink", 1f, (x, y) => Jelly(Geode, x, y, NoCores, Happy | Blink, Gem));
        Geode.shell = Whole("Slimes/Geode_shell", 1f, ShellPixel);
        Geode.half = Half("Slimes/Geode_half", (x, y) => Jelly(Geode, x, y, NoCores, NoFace), (x, y, e) => JellyFace(Geode, x, y, e, 0.4f));

        Magma.whole = Whole("Slimes/Magma", 1f, (x, y) => MagmaPixel(x, y, Grumpy));
        Magma.blink = Whole("Slimes/Magma_blink", 1f, (x, y) => MagmaPixel(x, y, Grumpy | Blink));
        Magma.half = Half("Slimes/Magma_half", (x, y) => MagmaPixel(x, y, NoFace), MagmaFace);

        Mercury.whole = Whole("Slimes/Mercury", 1f, (x, y) => MercuryPixel(x, y, Cool));
        Mercury.blink = Whole("Slimes/Mercury_blink", 1f, (x, y) => MercuryPixel(x, y, Cool | Blink));
        Bead = Whole("Slimes/Bead", 1f, (x, y) => MercuryPixel(x, y, NoFace));

        King.whole = Whole("Slimes/King", TallExtent, (x, y) => KingPixel(x, y, Happy));
        King.blink = Whole("Slimes/King_blink", TallExtent, (x, y) => KingPixel(x, y, Happy | Blink));
        King.half = Half("Slimes/King_half", (x, y) => Jelly(King, x, y, NoCores, NoFace), (x, y, e) => JellyFace(King, x, y, e, 0.4f));
        Nugget.whole = Whole("Slimes/Nugget", 1f, (x, y) => Jelly(Nugget, x, y, OneCore, NoFace));
        Nugget.half = King.half;

        Bomb.whole = Whole("Slimes/Bomb", TallExtent, BombPixel);
        if (export != null) return;

        // Слайму подложили свою картинку, а моргающую и разрезанную — нет: обходимся ею одной.
        // Слайм не моргает, а половинки вырезаются из самой картинки.
        foreach (var d in new[] { Acid, Magma, Geode, Mercury, Mini, King, Blue })
        {
            string name = "Slimes/" + d.name;
            if (!fromFile.Contains(name)) continue;
            if (!fromFile.Contains(name + "_blink")) d.blink = d.whole;
            if (d != Mercury && !fromFile.Contains("Slimes/" + (d == Blue ? Mini : d).name + "_half")) d.half = TopHalf(d.whole);
        }
        Nugget.half = King.half;
    }

    static Sprite TopHalf(Sprite whole)
    {
        var tex = whole.texture;
        int half = tex.height / 2;
        return Sprite.Create(tex, new Rect(0, half, tex.width, tex.height - half), new Vector2(0.5f, 0f), whole.pixelsPerUnit, 0, SpriteMeshType.FullRect);
    }

    public static SlimeDef Def(Kind kind)
    {
        switch (kind)
        {
            case Kind.Magma: return Magma;
            case Kind.Geode: return Geode;
            case Kind.Mercury: return Mercury;
            case Kind.Blue: return Blue;
            case Kind.Mini: return Mini;
            case Kind.King: return King;
            case Kind.Nugget: return Nugget;
            case Kind.Bomb: return Bomb;
            default: return Acid;
        }
    }

    // Полупрозрачное желе: ядро и пузырьки внутри, глянцевый блик, свет собирается у нижнего края.
    static Color Jelly(SlimeDef d, float x, float y, Vector3[] cores, int face, int shape = Ball)
    {
        Warp(shape, ref x, ref y);
        float r2 = x * x + y * y;
        if (r2 >= 1f) return new Color(d.deep.r, d.deep.g, d.deep.b, 0f);
        float r = Mathf.Sqrt(r2), z = Mathf.Sqrt(1f - r2);
        float light = Mathf.Max(0f, -0.45f * x + 0.6f * y + 0.66f * z);

        Color c = Scale(Color.Lerp(d.body, d.deep, Smooth(0.3f, 1f, r)), 0.62f + 0.58f * light);
        c = Over(c, d.glow, Smooth(0.5f, 0.95f, r) * Mathf.Clamp01(0.25f - 0.8f * y + 0.3f * x) * 0.6f);

        foreach (var b in Bubbles)
        {
            float dx = x - b.x, dy = y - b.y, q = (dx * dx + dy * dy) / (b.z * b.z);
            if (q < 1f) c = Over(c, Color.white, 0.1f + 0.4f * Smooth(0.5f, 1f, q) + (dx < 0f && dy > 0f && q < 0.5f ? 0.25f : 0f));
        }

        foreach (var k in cores)
        {
            float dx = (x - k.x) / k.z, dy = (y - k.y) / k.z, q2 = dx * dx + dy * dy;
            if (q2 >= 1f) continue;
            float cz = Mathf.Sqrt(1f - q2);
            float cl = Mathf.Max(0f, -0.45f * dx + 0.6f * dy + 0.66f * cz);
            float spec = Mathf.Pow(Mathf.Max(0f, -0.42f * dx + 0.55f * dy + 0.72f * cz), 18f) * 0.8f;
            Color core = Scale(d.core, 0.6f + 0.55f * cl);
            core = new Color(core.r + spec, core.g + spec, core.b + spec, 1f);
            c = Over(c, core, Smooth(1f, 0.8f, q2) * 0.94f);
        }

        c = Face(c, x, y, face, EyeDark);

        // Большой блик идёт дугой вдоль верхнего левого края.
        float hx = (x + 0.36f) * 0.82f + (y - 0.52f) * 0.57f, hy = -(x + 0.36f) * 0.57f + (y - 0.52f) * 0.82f;
        float h = 1f - hx * hx / 0.09f - hy * hy / 0.017f;
        if (h > 0f) c = Over(c, Color.white, Smooth(0f, 0.5f, h) * 0.85f);
        float sx = x + 0.68f, sy = y - 0.1f;
        if (sx * sx + sy * sy < 0.0036f) c = Over(c, Color.white, 0.8f);

        c = Over(c, Scale(d.deep, 0.55f), Smooth(0.88f, 1f, r) * 0.7f);
        c.a = Mathf.Lerp(0.86f, 0.98f, Smooth(0.4f, 1f, r));
        return c;
    }

    // Край силуэта в полярных координатах: расстояние от центра до края под углом a. Оно всегда меньше единицы —
    // фигура целиком помещается в свою картинку, а круглая зона попадания остаётся чуть шире тела.
    static float Edge(int shape, float a)
    {
        float s = Mathf.Sin(a), up = Mathf.Max(0f, s), down = Mathf.Max(0f, -s);
        float t = Mathf.DeltaAngle(90f, a * Mathf.Rad2Deg) * Mathf.Deg2Rad;   // отклонение от макушки
        switch (shape)
        {
            case Drop:      // капля: широкий низ, узкий верх и острый хохолок, завалившийся набок
                return 0.93f - 0.13f * up * Mathf.Sqrt(up) - 0.05f * down * down + 0.19f * Bump(t + 0.12f, 0.16f);
            case Twin:      // две макушки с ложбинкой между ними: намёк, что слайм разделится надвое
                return 0.86f - 0.02f * down * down + (s > 0f ? 0.13f * Bump(Mathf.Abs(t) - 0.6f, 0.28f) - 0.1f * Bump(t, 0.2f) : 0f);
            case Bud:       // почти шарик с маленьким острым носиком
                return 0.92f - 0.04f * down * down + 0.07f * Bump(t, 0.16f);
            case Gem:       // скруглённый шестигранник остриём вверх
            {
                float seg = Mathf.PI / 3f;
                float flat = Mathf.Cos(seg * 0.5f) / Mathf.Cos(Mathf.Repeat(a - Mathf.PI * 0.5f + seg * 0.5f, seg) - seg * 0.5f);
                return 0.98f * Mathf.Lerp(1f, flat, 0.65f);
            }
            case Tongues:   // неровный камень и три языка пламени на макушке
                return 0.8f + 0.02f * Mathf.Cos(a * 5f + 1f) + 0.015f * Mathf.Cos(a * 3f)
                     + 0.185f * Bump(t, 0.1f) + 0.13f * Bump(Mathf.Abs(t) - 0.36f, 0.085f);
            default:
                return 1f;
        }
    }

    static float Bump(float d, float width)
    {
        return Mathf.Exp(-d * d / (width * width));
    }

    // Переводит точку в координаты круглого слайма: край силуэта становится окружностью единичного радиуса.
    // У центра масштаб один на всю фигуру, поэтому мордочка не искажается; к краю он подстраивается под силуэт.
    static void Warp(int shape, ref float x, ref float y)
    {
        if (shape == Ball) return;
        if (shape == Pudding)
        {
            x /= Mathf.Lerp(1f, 0.8f, Smooth(-0.35f, 1f, y));
            if (y < 0f) y /= 0.93f;
            return;
        }
        float r = Mathf.Sqrt(x * x + y * y);
        if (r < 1e-4f) return;
        float edge = Edge(shape, Mathf.Atan2(y, x));
        float k = Mathf.Lerp(0.9f, edge, Smooth(0.35f, 1f, r / edge));
        x /= k;
        y /= k;
    }

    // Большие блестящие глаза, румянец и рот. Глаза крупнее и ниже центра — так мордочка выглядит детской.
    static Color Face(Color c, float x, float y, int face, Color eye)
    {
        int mood = face & ~Blink;
        if (mood == NoFace) return c;

        if (mood == Happy)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float bx = x - side * 0.54f, by = y + 0.1f;
                float q = bx * bx / 0.02f + by * by / 0.011f;
                if (q < 1f) c = Over(c, C(1f, 0.42f, 0.58f), (1f - q) * 0.6f);
            }
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float ex = x - side * 0.3f, ey = y - 0.12f;
            if ((face & Blink) != 0)
            {
                // Закрытый глаз — довольная дужка.
                float arc = Mathf.Abs(Mathf.Sqrt(ex * ex + (ey + 0.06f) * (ey + 0.06f)) - 0.12f);
                if (ey > -0.02f) c = Over(c, eye, Smooth(0.034f, 0.02f, arc));
                continue;
            }
            float q = ex * ex / 0.0169f + ey * ey / 0.0289f;
            if (q < 1f)
            {
                c = Over(c, eye, Smooth(1f, 0.8f, q));
                float gx = ex + 0.04f, gy = ey - 0.065f;
                if (gx * gx + gy * gy < 0.003f) c = Over(c, Color.white, 0.97f);
                gx = ex - 0.045f;
                gy = ey + 0.07f;
                if (gx * gx + gy * gy < 0.0009f) c = Over(c, Color.white, 0.8f);
            }
            if (mood == Grumpy || mood == Fierce)
            {
                // Насупленные брови: внутренний конец ниже внешнего.
                float t = Mathf.Clamp01((side * x - 0.14f) / 0.32f);
                float brow = Mathf.Abs(y - (0.3f + 0.13f * t));
                if (side * x > 0.12f && side * x < 0.48f) c = Over(c, C(0.12f, 0.05f, 0.04f), Smooth(0.045f, 0.028f, brow));
            }
        }

        float my = y + 0.1f;
        if (mood == Happy)
        {
            // Открытая улыбка с язычком.
            if (my < 0f && x * x / 0.0144f + my * my / 0.0121f < 1f)
            {
                c = Over(c, C(0.32f, 0.05f, 0.12f), 0.95f);
                float ty = my + 0.115f;
                if (x * x + ty * ty < 0.0064f) c = Over(c, C(1f, 0.5f, 0.62f), 0.95f);
            }
        }
        else if (mood == Fierce)
        {
            // Широкая зубастая ухмылка.
            if (my < 0.02f && x * x / 0.04f + my * my / 0.02f < 1f)
            {
                c = Over(c, C(0.32f, 0.05f, 0.12f), 0.95f);
                if (my > -0.04f) c = Over(c, Color.white, 0.95f);
            }
        }
        else if (mood == Grumpy)
        {
            // Поджатые губы дугой вниз.
            float arc = Mathf.Abs(Mathf.Sqrt(x * x + (my + 0.13f) * (my + 0.13f)) - 0.11f);
            if (my > -0.07f && Mathf.Abs(x) < 0.1f) c = Over(c, C(0.12f, 0.05f, 0.04f), Smooth(0.03f, 0.018f, arc));
        }
        else
        {
            // Лёгкая ухмылка.
            float arc = Mathf.Abs(Mathf.Sqrt(x * x + (my - 0.05f) * (my - 0.05f)) - 0.1f);
            if (my < -0.01f) c = Over(c, eye, Smooth(0.028f, 0.016f, arc));
        }
        return c;
    }

    // Срез желе: кожица по краю, светлая мякоть и глянцевое ядро в центре.
    static Color JellyFace(SlimeDef d, float x, float y, float e, float coreR)
    {
        Color c = Color.Lerp(Color.Lerp(d.body, Color.white, 0.45f), d.body, Smooth(0.15f, 0.85f, e));
        if (e > 0.9f) c = Scale(d.deep, 0.85f);
        else if (e > 0.84f) c = Color.Lerp(d.body, d.deep, 0.5f);
        if (e < coreR)
        {
            float q = e / coreR;
            c = Color.Lerp(Color.Lerp(d.core, Color.white, 0.5f), Scale(d.core, 0.8f), Smooth(0.3f, 1f, q));
            if (q > 0.86f) c = Scale(d.deep, 0.9f);
            float gx = x + coreR * 0.3f, gy = y - coreR * 0.35f;
            if (gx * gx + gy * gy < coreR * coreR * 0.05f) c = Color.white;
        }
        // Влажный отблеск на срезе.
        float w = 1f - (x + 0.45f) * (x + 0.45f) / 0.05f - (y - 0.3f) * (y - 0.3f) / 0.12f;
        if (w > 0f && e < 0.82f) c = Over(c, Color.white, w * 0.45f);
        c.a = 1f;
        return c;
    }

    // Базальтовые плиты с раскалёнными жилами между ними.
    static Color MagmaPixel(float x, float y, int face)
    {
        // У целого слайма над камнем пляшут языки пламени; половинка остаётся круглой.
        float raw = Mathf.Sqrt(x * x + y * y);
        if (face != NoFace) Warp(Tongues, ref x, ref y);
        float r2 = x * x + y * y;
        if (r2 >= 1f) return new Color(1f, 0.4f, 0.05f, 0f);
        float r = Mathf.Sqrt(r2), z = Mathf.Sqrt(1f - r2);
        float light = Mathf.Max(0f, -0.45f * x + 0.6f * y + 0.66f * z);
        float id, edge;
        Voronoi(x * 2.5f + 7f, y * 2.5f + 3f, out id, out edge);

        Color rock = Scale(Color.Lerp(C(0.36f, 0.16f, 0.11f), C(0.2f, 0.09f, 0.08f), id), 0.65f + 0.7f * light);
        Color lava = Color.Lerp(C(1f, 0.38f, 0.05f), C(1f, 0.92f, 0.4f), Smooth(0.1f, 0f, edge));
        Color c = Over(rock, lava, Smooth(0.2f, 0.06f, edge));
        c = Face(c, x, y, face, C(1f, 0.95f, 0.5f));
        // Всё, что выступает за камень, — огонь: от оранжевого у основания до жёлтого на кончиках.
        if (face != NoFace) c = Over(c, Color.Lerp(C(1f, 0.5f, 0.08f), C(1f, 0.92f, 0.45f), Smooth(0.84f, 0.97f, raw)), Smooth(0.81f, 0.88f, raw));
        float spec = Mathf.Pow(Mathf.Max(0f, -0.42f * x + 0.55f * y + 0.72f * z), 20f) * 0.35f;
        c = new Color(c.r + spec, c.g + spec, c.b + spec, 1f);
        c = Over(c, C(1f, 0.55f, 0.1f), Smooth(0.84f, 1f, r) * 0.85f);
        c.a = 1f;
        return c;
    }

    static Color MagmaFace(float x, float y, float e)
    {
        if (e > 0.88f) return C(0.24f, 0.1f, 0.08f);
        float n = Noise(x * 6f + 3f, y * 6f);
        Color c = Color.Lerp(C(1f, 0.95f, 0.55f), C(1f, 0.35f, 0.04f), Smooth(0f, 0.9f, e) * 0.8f + n * 0.25f);
        if (e > 0.8f) c = Color.Lerp(c, C(0.5f, 0.12f, 0.03f), 0.6f);
        c.a = 1f;
        return c;
    }

    // Гранёная каменная скорлупа жеоды с проросшими аметистами.
    static Color ShellPixel(float x, float y)
    {
        float r = Mathf.Sqrt(x * x + y * y);
        float seg = Mathf.PI * 2f / 10f;
        float lim = Mathf.Cos(seg * 0.5f) / Mathf.Cos(Mathf.Repeat(Mathf.Atan2(y, x), seg) - seg * 0.5f);
        if (r >= lim) return new Color(0.6f, 0.55f, 0.7f, 0f);
        float z = Mathf.Sqrt(Mathf.Max(0f, 1f - r * r));
        float light = Mathf.Max(0f, -0.45f * x + 0.6f * y + 0.66f * z);
        float id, edge;
        Voronoi(x * 3f + 1.3f, y * 3f + 4.2f, out id, out edge);

        Color c;
        if (id > 0.74f)
        {
            c = Scale(Color.Lerp(C(0.62f, 0.25f, 0.95f), C(1f, 0.72f, 1f), Hash(id * 91f, 3f)), 0.75f + 0.6f * light);
            c = Over(c, Color.white, Smooth(0.25f, 0.45f, edge) * 0.6f);
        }
        else
        {
            c = Scale(Color.Lerp(C(0.56f, 0.52f, 0.66f), C(0.32f, 0.3f, 0.43f), id * 1.35f), 0.6f + 0.65f * light);
            c = Scale(c, 0.9f + 0.2f * Noise(x * 22f, y * 22f));
        }
        c = Over(c, C(0.1f, 0.08f, 0.16f), Smooth(0.08f, 0.02f, edge));
        c = Over(c, C(0.78f, 0.72f, 0.9f), Smooth(lim - 0.09f, lim, r) * 0.75f);
        c.a = 1f;
        return c;
    }

    // Жидкая ртуть: хромовый шар, в котором отражаются «небо», тёмный горизонт и «пол».
    static Color MercuryPixel(float x, float y, int face)
    {
        float r2 = x * x + y * y;
        if (r2 >= 1f) return new Color(0.7f, 0.9f, 1f, 0f);
        float r = Mathf.Sqrt(r2), z = Mathf.Sqrt(1f - r2);
        float ry = 2f * z * y + 0.1f * Mathf.Sin(x * 5f), rz = 2f * z * z - 1f;

        Color c;
        if (ry > 0.06f) c = Color.Lerp(C(0.5f, 0.64f, 0.84f), Color.white, Smooth(0.06f, 0.85f, ry));
        else if (ry > -0.14f) c = Color.Lerp(C(0.08f, 0.1f, 0.2f), C(0.5f, 0.64f, 0.84f), Smooth(-0.02f, 0.06f, ry));
        else c = Color.Lerp(C(0.08f, 0.1f, 0.2f), C(0.72f, 0.78f, 0.9f), Smooth(-0.14f, -0.95f, ry));
        c = Over(c, C(0.22f, 0.28f, 0.42f), Smooth(0.1f, -0.9f, rz) * 0.55f);

        c = Face(c, x, y, face, EyeDark);
        float spec = Mathf.Pow(Mathf.Max(0f, -0.42f * x + 0.55f * y + 0.72f * z), 30f);
        c = new Color(c.r + spec, c.g + spec, c.b + spec, 1f);
        c = Over(c, C(0.72f, 0.95f, 1f), Smooth(0.9f, 1f, r) * 0.75f);
        c.a = 1f;
        return c;
    }

    static Color KingPixel(float x, float y, int face)
    {
        Color c = Jelly(King, x, y, NoCores, face, Pudding);
        Color gold;
        return CrownPixel(x, y - 0.8f, out gold) ? gold : c;
    }

    // Корона: обруч и три зубца с шариками. cy — высота над основанием короны.
    static bool CrownPixel(float x, float cy, out Color gold)
    {
        gold = new Color(1f, 0.8f, 0.18f, 0f);
        if (cy <= 0f || Mathf.Abs(x) > 0.54f) return false;

        float top = 0.18f;
        for (int i = -1; i <= 1; i++) top = Mathf.Max(top, 0.18f + 0.36f - Mathf.Abs(x - i * 0.4f) * 2.1f);
        if (cy > top) return false;

        gold = Scale(C(1f, 0.8f, 0.18f), 0.8f + 0.4f * Smooth(0.5f, -0.5f, x));
        if (cy < 0.05f || (cy > 0.14f && cy < 0.18f)) gold = Scale(gold, 0.68f);
        if (top - cy < 0.035f || Mathf.Abs(x) > 0.5f) gold = C(0.62f, 0.36f, 0.04f);
        float gx = x, gy = cy - 0.1f;
        if (gx * gx + gy * gy < 0.003f) gold = C(1f, 0.25f, 0.3f);
        for (int i = -1; i <= 1; i++)
        {
            float bx = x - i * 0.4f, by = cy - 0.5f;
            if (bx * bx + by * by < 0.004f) gold = C(1f, 0.95f, 0.6f);
        }
        gold.a = 1f;
        return true;
    }

    // Чумная спора: шипастая чёрно-пурпурная сфера со злыми глазами и фитилём.
    static Color BombPixel(float x, float y)
    {
        const float body = 0.8f;
        float r = Mathf.Sqrt(x * x + y * y);
        float seg = Mathf.PI * 2f / 8f;
        float da = Mathf.Abs(Mathf.Repeat(Mathf.Atan2(y, x), seg) - seg * 0.5f);
        float spike = 0.36f * Mathf.Max(0f, 1f - da / 0.2f);

        if (r < body)
        {
            float nx = x / body, ny = y / body, z = Mathf.Sqrt(Mathf.Max(0f, 1f - nx * nx - ny * ny));
            float light = Mathf.Max(0f, -0.45f * nx + 0.6f * ny + 0.66f * z);
            Color c = Scale(C(0.2f, 0.12f, 0.28f), 0.45f + 0.9f * light);
            float id, edge;
            Voronoi(x * 3.2f + 2f, y * 3.2f + 9f, out id, out edge);
            c = Over(c, C(1f, 0.15f, 0.5f), Smooth(0.06f, 0f, edge) * 0.75f);
            for (int side = -1; side <= 1; side += 2)
            {
                float ex = x - side * 0.27f, ey = y - 0.06f + side * ex * 0.7f;
                float q = ex * ex / 0.02f + ey * ey / 0.0042f;
                if (q < 1f) c = Color.Lerp(C(1f, 0.95f, 0.6f), C(1f, 0.15f, 0.1f), Smooth(0f, 0.8f, q));
            }
            float spec = Mathf.Pow(Mathf.Max(0f, -0.42f * nx + 0.55f * ny + 0.72f * z), 16f) * 0.5f;
            c = new Color(c.r + spec * 0.8f, c.g + spec * 0.6f, c.b + spec, 1f);
            // Светлый кант, чтобы тёмная спора не терялась на тёмной стене.
            c = Over(c, C(0.6f, 0.38f, 0.78f), Smooth(0.84f, 1f, r / body) * 0.85f);
            c.a = 1f;
            return c;
        }
        if (r < body + spike)
        {
            Color c = Color.Lerp(C(0.34f, 0.18f, 0.46f), C(0.85f, 0.62f, 0.95f), (r - body) / 0.36f);
            c.a = 1f;
            return c;
        }
        if (Mathf.Abs(x) < 0.075f && y > 0.7f && y < BombFuseTip) return C(0.78f, 0.64f, 0.42f);
        return new Color(0.5f, 0.3f, 0.65f, 0f);
    }

    // ---------- Простые формы ----------

    static void BuildShapes()
    {
        White = MakeSprite("Effects/White", Tex(4, 4, -1, 1, -1, 1, (x, y) => Color.white), new Vector2(0.5f, 0.5f), 4f);

        Soft = MakeSprite("Effects/Soft", Tex(64, 64, -1, 1, -1, 1, (x, y) =>
        {
            float a = Mathf.Clamp01(1f - Mathf.Sqrt(x * x + y * y));
            return new Color(1, 1, 1, a * a);
        }), new Vector2(0.5f, 0.5f), 32f);

        Dot = MakeSprite("Effects/Dot", Tex(32, 32, -1, 1, -1, 1, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float k = 0.8f + 0.2f * Mathf.Clamp01(1f - ((x + 0.3f) * (x + 0.3f) + (y - 0.3f) * (y - 0.3f)) * 2f);
            return new Color(k, k, k, Smooth(1f, 0.8f, r));
        }), new Vector2(0.5f, 0.5f), 16f);

        Star = MakeSprite("Effects/Star", Tex(64, 64, -1, 1, -1, 1, (x, y) =>
        {
            float s = Mathf.Sqrt(Mathf.Abs(x)) + Mathf.Sqrt(Mathf.Abs(y));
            float core = Mathf.Clamp01(1f - Mathf.Sqrt(x * x + y * y) * 3f);
            return new Color(1, 1, 1, Mathf.Clamp01(Smooth(1f, 0.5f, s) + core));
        }), new Vector2(0.5f, 0.5f), 32f);

        Ring = MakeSprite("Effects/Ring", Tex(128, 128, -1, 1, -1, 1, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            return new Color(1, 1, 1, Smooth(0.1f, 0f, Mathf.Abs(r - 0.88f)));
        }), new Vector2(0.5f, 0.5f), 64f);

        Heart = MakeSprite("Interface/Heart", Tex(160, 160, -1.1f, 1.1f, -1.1f, 1.1f, HeartPixel), new Vector2(0.5f, 0.5f), 160f / 2.2f);

        // Острый осколок: неправильный треугольник с гранью посередине.
        Shard = MakeSprite("Effects/Shard", Tex(48, 48, -1, 1, -1, 1, (x, y) =>
        {
            bool inside = Side(x, y, -0.8f, -0.6f, 0.9f, -0.35f) >= 0f && Side(x, y, 0.9f, -0.35f, 0.05f, 0.95f) >= 0f
                       && Side(x, y, 0.05f, 0.95f, -0.8f, -0.6f) >= 0f;
            float k = Side(x, y, 0.05f, 0.95f, 0.1f, -0.5f) > 0f ? 1f : 0.7f;
            return new Color(k, k, k, inside ? 1f : 0f);
        }), new Vector2(0.5f, 0.5f), 24f);

        Crystal = MakeSprite("Effects/Crystal", Tex(48, 96, -0.5f, 0.5f, -1, 1, (x, y) =>
        {
            float half = 0.46f * Mathf.Min(1f, (1f - Mathf.Abs(y)) / 0.42f);
            if (Mathf.Abs(x) >= half) return new Color(1f, 0.8f, 1f, 0f);
            float k = x < -0.14f ? 0.62f : x < 0.14f ? 1f : 0.8f;
            if (Mathf.Abs(y) > 0.58f) k *= y > 0f ? 1.12f : 0.8f;
            Color c = Scale(C(0.74f, 0.4f, 1f), k);
            if (x > -0.12f && x < -0.02f && y > -0.3f && y < 0.45f) c = Over(c, Color.white, 0.6f);
            c.a = 1f;
            return c;
        }), new Vector2(0.5f, 0.5f), 48f);

        Coin = MakeSprite("Effects/Coin", Tex(48, 48, -1, 1, -1, 1, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            if (r >= 1f) return new Color(1f, 0.8f, 0.2f, 0f);
            Color c = r > 0.76f ? C(0.86f, 0.55f, 0.08f) : Scale(C(1f, 0.84f, 0.25f), 0.85f + 0.3f * Smooth(0.6f, -0.6f, x - y));
            float s = Mathf.Sqrt(Mathf.Abs(x)) + Mathf.Sqrt(Mathf.Abs(y));
            if (s < 0.72f) c = C(1f, 0.97f, 0.7f);
            c.a = 1f;
            return c;
        }), new Vector2(0.5f, 0.5f), 24f);

        // Потёк: тонкая струйка с каплей на конце, растёт вниз от верхней точки.
        Drip = MakeSprite("Effects/Drip", Tex(16, 64, -1, 1, 0, 1, (x, y) =>
        {
            float half = Mathf.Lerp(0.42f, 0.95f, Smooth(0.3f, 0.08f, y)) * Smooth(0f, 0.06f, y);
            return new Color(1, 1, 1, Smooth(half, half - 0.3f, Mathf.Abs(x)));
        }), new Vector2(0.5f, 1f), 64f);

        // Язык пламени: округлое основание и острая верхушка, плотный в середине.
        Flame = MakeSprite("Effects/Flame", Tex(32, 64, -1, 1, 0, 1, (x, y) =>
        {
            float half = 0.95f * Mathf.Pow(1f - y, 0.8f) * Mathf.Sqrt(Mathf.Clamp01(y * 5f));
            return new Color(1, 1, 1, half > 0.001f ? Mathf.Sqrt(Mathf.Clamp01(1f - Mathf.Abs(x) / half)) : 0f);
        }), new Vector2(0.5f, 0f), 64f);

        // Росчерк клинка: полоса с острыми концами, плотная по оси и тающая к краям.
        Streak = MakeSprite("Effects/Streak", Tex(128, 32, -1, 1, -1, 1, (x, y) =>
        {
            float half = Mathf.Pow(Mathf.Max(0f, 1f - x * x), 1.5f);
            return new Color(1, 1, 1, half > 0.001f ? Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(y) / half), 1.5f) : 0f);
        }), new Vector2(0.5f, 0.5f), 64f);

        // Лучи вспышки: неровный веер света, тающий к краям.
        Rays = MakeSprite("Effects/Rays", Tex(256, 256, -1, 1, -1, 1, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y), a = Mathf.Atan2(y, x);
            float ray = Smooth(0.72f, 0.99f, 0.5f + 0.5f * Mathf.Cos(a * 14f + 1.3f * Mathf.Sin(a * 3f)));
            return new Color(1, 1, 1, ray * Smooth(1f, 0.15f, r) * Smooth(0.04f, 0.18f, r) * 0.9f);
        }), new Vector2(0.5f, 0.5f), 128f);

        // Ударная волна: кольцо с чёткой кромкой снаружи и мягким следом внутри.
        Shock = MakeSprite("Effects/Shock", Tex(128, 128, -1, 1, -1, 1, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            return new Color(1, 1, 1, Mathf.Pow(Smooth(0.74f, 0.95f, r), 1.6f) * Smooth(1f, 0.95f, r));
        }), new Vector2(0.5f, 0.5f), 64f);

        Splats = new Sprite[4];
        for (int i = 0; i < Splats.Length; i++) Splats[i] = MakeSplat("Effects/Splat" + (i + 1), 1337 + i * 17);

        Panel = MakeSprite("Interface/Panel", Tex(64, 64, -32, 32, -32, 32, (x, y) => new Color(1, 1, 1, Mathf.Clamp01(0.5f - RoundBox(x, y, 31f, 14f)))),
            new Vector2(0.5f, 0.5f), 64f, 20f);
        Frame = MakeSprite("Interface/Frame", Tex(64, 64, -32, 32, -32, 32, (x, y) => new Color(1, 1, 1, Mathf.Clamp01(1.6f - Mathf.Abs(RoundBox(x, y, 31f, 14f) + 2f)))),
            new Vector2(0.5f, 0.5f), 64f, 20f);

        // Полоска комбо. Все три картинки растягиваются: закруглённые концы остаются, середина тянется.
        Pill = MakeSprite("Interface/Pill", Tex(64, 64, -32, 32, -32, 32, (x, y) =>
            new Color(1, 1, 1, Mathf.Clamp01(0.5f - RoundBox(x, y, 31f, 31f)))), new Vector2(0.5f, 0.5f), 64f, 31f);
        PillRing = MakeSprite("Interface/PillRing", Tex(64, 64, -32, 32, -32, 32, (x, y) =>
            new Color(1, 1, 1, Mathf.Clamp01(1.7f - Mathf.Abs(RoundBox(x, y, 31f, 31f) + 2.6f)))), new Vector2(0.5f, 0.5f), 64f, 31f);
        // Ореол гаснет до нуля раньше края картинки: иначе вокруг полоски был бы виден обрезанный прямоугольник.
        PillGlow = MakeSprite("Interface/PillGlow", Tex(96, 96, -48, 48, -48, 48, (x, y) =>
        {
            float d = Mathf.Max(0f, RoundBox(x, y, 31f, 31f));
            return new Color(1, 1, 1, 0.85f * Mathf.Exp(-d / 5.5f) * Smooth(16f, 6f, d));
        }), new Vector2(0.5f, 0.5f), 96f, 47f);

        // Слайм-талисман полоски комбо. Он серо-белый: игра красит его в цвет уровня.
        var plain = new SlimeDef { body = C(0.97f, 0.97f, 1f), deep = C(0.66f, 0.66f, 0.76f), core = Color.white, glow = Color.white };
        Mascot = new[]
        {
            Whole("Interface/Mascot1", 1f, (x, y) => Jelly(plain, x, y, NoCores, Happy, Drop)),
            Whole("Interface/Mascot2", 1f, (x, y) => Jelly(plain, x, y, NoCores, Cool, Drop)),
            Whole("Interface/Mascot3", 1f, (x, y) => Jelly(plain, x, y, NoCores, Fierce, Drop)),
        };

        Vignette = MakeSprite("Interface/Vignette", Tex(128, 128, -1, 1, -1, 1, (x, y) =>
            new Color(1, 1, 1, Smooth(0.62f, 1.38f, Mathf.Sqrt(x * x + y * y)))), new Vector2(0.5f, 0.5f), 128f);

        // Панель меню: фиолетовое поле в золотой рамке с ромбами по углам. Растягивается, углы остаются как есть.
        Plate = MakeSprite("Interface/Plate", Tex(96, 96, -48, 48, -48, 48, PlatePixel), new Vector2(0.5f, 0.5f), 100f, 34f);
        Ribbon = MakeSprite("Interface/Ribbon", Tex(560, 96, -280, 280, -48, 48, RibbonPixel), new Vector2(0.5f, 0.5f), 100f);
        // Табличка с числом серии: светлая кайма и серое поле. Игра красит её в цвет уровня серии.
        Badge = MakeSprite("Interface/Badge", Tex(120, 88, -60, 60, -44, 44, BadgePixel), new Vector2(0.5f, 0.5f), 100f);
        Crown = MakeSprite("Interface/Crown", Tex(128, 96, -1f, 1f, -0.75f, 0.75f, CrownIcon), new Vector2(0.5f, 0f), 64f);

        // Значки званий: медаль в цвет звания и символ ремесла — от капли ученика до короны легенды.
        // Цвета берутся из баланса, а сами значки после выгрузки — обычные картинки.
        var ranks = Balance.Current.ranks;
        Ranks = new Sprite[ranks.Length];
        for (int i = 0; i < Ranks.Length; i++)
        {
            int rank = i;
            Color tint = ranks[i].color;
            Ranks[i] = MakeSprite("Interface/Rank" + (i + 1), Tex(128, 128, -1, 1, -1, 1, (x, y) => RankPixel(x, y, rank, tint)),
                new Vector2(0.5f, 0.5f), 64f);
        }
    }

    // Панель меню. Координаты — в единицах холста: квадрат 96x96, который растягивается серединой.
    static Color PlatePixel(float x, float y)
    {
        float d = RoundBox(x, y, 46f, 12f);
        Color gold = Color.Lerp(C(0.55f, 0.36f, 0.14f), C(1f, 0.88f, 0.56f), Mathf.Clamp01(0.55f - (x - y) / 120f));
        if (d > 0.5f) return new Color(gold.r, gold.g, gold.b, 0f);
        Color field = Color.Lerp(C(0.1f, 0.05f, 0.19f), C(0.2f, 0.11f, 0.34f), Smooth(-46f, 46f, y));
        float fieldA = 0.9f * Mathf.Clamp01(-d - 2.5f);

        // Рамка: толстая линия снаружи и тонкая внутри; в углах — ромбы, рядом с ними по бусине.
        float line = Mathf.Clamp01(2.1f - Mathf.Abs(d + 3.2f));
        float thin = 0.7f * Mathf.Clamp01(1f - Mathf.Abs(d + 8.5f));
        float cx = Mathf.Abs(x) - 37f, cy = Mathf.Abs(y) - 37f;
        float diamond = Mathf.Abs(cx) + Mathf.Abs(cy);
        float bead = Mathf.Min(Mathf.Sqrt(cx * cx + (cy + 15f) * (cy + 15f)), Mathf.Sqrt((cx + 15f) * (cx + 15f) + cy * cy));
        float goldA = Mathf.Max(Mathf.Max(line, thin), Mathf.Max(Mathf.Clamp01(9f - diamond), Mathf.Clamp01(2.8f - bead)));
        if (diamond < 9f) gold = diamond > 6.2f ? Scale(gold, 0.55f) : diamond < 2.8f ? C(1f, 0.98f, 0.82f) : gold;

        Color c = Color.Lerp(field, gold, goldA);
        c.a = goldA + fieldA * (1f - goldA);
        return c;
    }

    // Лента под логотипом: фиолетовое полотно с золотой каймой, за ним — раздвоенные хвосты.
    static Color RibbonPixel(float x, float y)
    {
        float ax = Mathf.Abs(x), ty = y + 10f;
        Color gold = Color.Lerp(C(0.6f, 0.4f, 0.15f), C(1f, 0.88f, 0.55f), Smooth(-30f, 34f, y));
        Color c = new Color(0.4f, 0.2f, 0.6f, 0f);
        if (ax > 212f && Mathf.Abs(ty) < 24f && ax < 276f - 20f * (1f - Mathf.Abs(ty) / 24f))
        {
            c = Color.Lerp(C(0.17f, 0.07f, 0.3f), C(0.3f, 0.14f, 0.48f), Smooth(-24f, 24f, ty));
            if (Mathf.Abs(ty) > 20.5f) c = Scale(gold, 0.75f);
            if (ax < 238f && y < -22f) c = C(0.1f, 0.04f, 0.2f);   // сгиб в тени полотна
        }
        float d = Box(x, y - 6f, 238f, 28f, 7f);
        if (d >= 0.5f) return c;
        Color band = Color.Lerp(C(0.3f, 0.13f, 0.5f), C(0.52f, 0.28f, 0.76f), Smooth(-22f, 34f, y));
        band = Over(band, Color.white, Smooth(18f, 30f, y) * 0.14f);
        float line = Mathf.Clamp01(1.8f - Mathf.Abs(d + 2.2f)), thin = 0.75f * Mathf.Clamp01(1f - Mathf.Abs(d + 6.5f));
        band = Color.Lerp(band, gold, Mathf.Max(line, thin));
        float bandA = Mathf.Clamp01(0.5f - d);
        Color result = Color.Lerp(c, band, c.a > 0f ? bandA : 1f);
        result.a = bandA + c.a * (1f - bandA);
        return result;
    }

    // Корона для интерфейса: обруч с камнями и три зубца с шариками, в тёмном контуре — ровная и читаемая в любом размере.
    static Color CrownIcon(float x, float y)
    {
        Color c = CrownBody(x, y, 0f);
        if (c.a > 0f) return c;
        return CrownBody(x, y, 0.06f).a > 0f ? C(0.36f, 0.17f, 0.04f) : new Color(0.36f, 0.17f, 0.04f, 0f);
    }

    // g — на сколько раздуть фигуру (для контура).
    static Color CrownBody(float x, float y, float g)
    {
        float ax = Mathf.Abs(x);
        float top = Mathf.Max(-0.3f, Mathf.Max(0.5f - ax * 1.7f, 0.36f - Mathf.Abs(ax - 0.8f) * 1.7f));
        bool ball = Oval(x, y - 0.56f, 0.09f + g, 0.09f + g) || Oval(ax - 0.8f, y - 0.42f, 0.09f + g, 0.09f + g);
        bool body = ax < 0.8f + g && y > -0.6f - g && y < top + g;
        if (!ball && !body) return new Color(1f, 0.8f, 0.2f, 0f);
        if (g > 0f) return Color.white;

        Color gold = Scale(C(1f, 0.82f, 0.2f), 0.82f + 0.36f * Smooth(0.7f, -0.7f, x - y * 0.4f));
        if (ball) return C(1f, 0.95f, 0.62f);
        if (y < -0.3f)
        {
            // Обруч: темнее зубцов, со светлой кромкой и тремя камнями.
            gold = Scale(gold, y > -0.36f ? 1.15f : 0.8f);
            if (Oval(x, y + 0.46f, 0.085f, 0.085f)) gold = C(1f, 0.3f, 0.36f);
            else if (Oval(ax - 0.45f, y + 0.46f, 0.07f, 0.07f)) gold = C(0.4f, 0.85f, 1f);
        }
        return gold;
    }

    // Табличка серии: щиток со срезанными углами.
    static Color BadgePixel(float x, float y)
    {
        float d = Mathf.Max(Box(x, y, 56f, 40f, 10f), (Mathf.Abs(x) + Mathf.Abs(y) - 80f) * 0.7071f);
        if (d > 0.5f) return new Color(1f, 1f, 1f, 0f);
        Color c = Color.Lerp(C(0.2f, 0.17f, 0.3f), C(0.34f, 0.3f, 0.47f), Smooth(-40f, 40f, y));
        float rim = Mathf.Clamp01(2.4f - Mathf.Abs(d + 3.4f)), thin = 0.5f * Mathf.Clamp01(1f - Mathf.Abs(d + 8f));
        c = Color.Lerp(c, Color.white, Mathf.Max(rim, thin));
        c.a = 0.96f * Mathf.Clamp01(0.5f - d);
        return c;
    }

    // Медаль звания. Край тем наряднее, чем выше звание: гладкий, с фестонами, с зубцами, с лучами.
    static Color RankPixel(float x, float y, int rank, Color tint)
    {
        float r = Mathf.Sqrt(x * x + y * y), a = Mathf.Atan2(y, x);
        int points = rank < 2 ? 0 : rank < 4 ? 8 : rank < 6 ? 12 : 16;
        float depth = rank < 2 ? 0f : rank < 4 ? 0.06f : rank < 6 ? 0.1f : 0.15f;
        float outer = 0.83f + depth * (points > 0 ? Mathf.Pow(0.5f + 0.5f * Mathf.Cos(a * points), rank < 4 ? 1f : 2.2f) : 0f);
        if (r >= outer) return new Color(tint.r, tint.g, tint.b, 0f);

        float light = Mathf.Clamp01(0.55f - 0.3f * x + 0.4f * y);
        Color c;
        if (r > 0.66f)
        {
            // Ободок: металл в цвет звания, тёмный кант снаружи и светлая фаска изнутри.
            c = Color.Lerp(Scale(tint, 0.5f), Color.Lerp(tint, Color.white, 0.45f), light);
            c = Over(c, Scale(tint, 0.3f), Smooth(outer - 0.07f, outer, r) * 0.85f);
            c = Over(c, Color.white, Smooth(0.72f, 0.66f, r) * 0.4f);
        }
        else
        {
            c = Color.Lerp(C(0.19f, 0.11f, 0.32f), C(0.09f, 0.05f, 0.17f), Smooth(-0.6f, 0.6f, x - y));
            c = Over(c, tint, Smooth(0.46f, 0.66f, r) * 0.28f);
            float shade;
            if (RankSign(rank, x / 0.5f, y / 0.5f, out shade)) c = Scale(Color.Lerp(tint, Color.white, 0.5f), shade * (0.82f + 0.3f * light));
        }
        c.a = 1f;
        return c;
    }

    // Символ на медали в поле [-1..1]: капля, ступка с пестиком, колба, знак философского камня, звезда,
    // колпак волшебника, корона. shade — яркость точки символа.
    static bool RankSign(int rank, float x, float y, out float shade)
    {
        shade = 1f;
        float ax = Mathf.Abs(x);
        switch (rank)
        {
            case 0:
            {
                float half = y < -0.3f ? Mathf.Sqrt(Mathf.Max(0f, 0.36f - (y + 0.3f) * (y + 0.3f))) : 0.6f * Mathf.Pow(Mathf.Clamp01(1f - (y + 0.3f) / 1.25f), 1.15f);
                if (y < -0.9f || y > 0.95f || ax >= half) return false;
                if (Oval(x + 0.22f, y + 0.2f, 0.12f, 0.2f)) shade = 1.3f;
                return true;
            }
            case 1:
            {
                // Пестик лежит наискось и выглядывает из ступки.
                float px = (x - 0.28f) * 0.766f + (y - 0.52f) * 0.643f, py = -(x - 0.28f) * 0.643f + (y - 0.52f) * 0.766f;
                bool pestle = y > 0.24f && (Mathf.Abs(py) < 0.11f && px > -0.5f && px < 0.48f || Oval(px - 0.48f, py, 0.17f, 0.17f));
                bool bowl = y < 0.1f && y > -0.64f && Oval(x, y - 0.1f, 0.78f, 0.76f);
                bool lip = ax < 0.9f && y >= 0.08f && y < 0.24f;
                bool foot = ax < 0.44f && y <= -0.6f && y > -0.84f;
                if (!pestle && !bowl && !lip && !foot) return false;
                shade = pestle ? 0.78f : lip ? 1.15f : foot ? 0.85f : 1f;
                return true;
            }
            case 2:
            {
                bool body = Oval(x, y + 0.3f, 0.64f, 0.64f);
                bool neck = ax < 0.2f && y > 0f && y < 0.74f;
                bool lip = ax < 0.31f && y >= 0.7f && y < 0.9f;
                if (!body && !neck && !lip) return false;
                // Стекло тусклое, зелье и пузырьки над ним яркие.
                bool liquid = body && y < -0.2f && Oval(x, y + 0.3f, 0.5f, 0.5f);
                bool bubble = Oval(x - 0.14f, y + 0.02f, 0.09f, 0.09f) || Oval(x + 0.12f, y - 0.22f, 0.07f, 0.07f);
                shade = liquid || bubble ? 1.2f : lip ? 0.8f : 0.58f;
                return true;
            }
            case 3:
            {
                // Круг в треугольнике — знак философского камня.
                float e = Mathf.Min(Mathf.Min(Side(x, y, -0.94f, -0.68f, 0.94f, -0.68f), Side(x, y, 0.94f, -0.68f, 0f, 0.95f)), Side(x, y, 0f, 0.95f, -0.94f, -0.68f)) / 1.88f;
                float ring = Mathf.Sqrt(x * x + (y + 0.14f) * (y + 0.14f));
                return e > 0f && e < 0.15f || Mathf.Abs(ring - 0.33f) < 0.07f || ring < 0.11f;
            }
            case 4:
            {
                // Пятиконечная звезда: край — отрезок между лучом и впадиной.
                const float seg = Mathf.PI * 0.4f, beta = Mathf.PI * 0.2f, inner = 0.42f;
                float turn = Mathf.Repeat(Mathf.Atan2(x, y) + seg * 0.5f, seg) - seg * 0.5f, phi = Mathf.Abs(turn);
                float lim = inner * Mathf.Sin(beta) / (Mathf.Sin(phi) + inner * Mathf.Sin(beta - phi));
                if (Mathf.Sqrt(x * x + y * y) >= lim) return false;
                shade = turn > 0f ? 0.84f : 1.1f;   // у каждого луча светлая и тёмная грань
                return true;
            }
            case 5:
            {
                float h = (y + 0.5f) / 1.45f, lean = 0.26f * h * h;
                bool cone = h > 0f && h < 1f && Mathf.Abs(x - lean) < 0.56f * (1f - h);
                bool brim = Oval(x, y + 0.56f, 0.98f, 0.19f);
                if (!cone && !brim) return false;
                shade = brim ? 0.85f : h < 0.2f ? 0.6f : 0.9f + 0.25f * Smooth(0.3f, -0.3f, x - lean);
                return true;
            }
            default:
            {
                float cy = y + 0.62f, top = 0.34f;
                for (int i = -1; i <= 1; i++) top = Mathf.Max(top, 1.12f - Mathf.Abs(x - i * 0.62f) * 2.3f);
                bool ball = false;
                for (int i = -1; i <= 1; i++) ball |= Oval(x - i * 0.62f, cy - 1.14f, 0.12f, 0.12f);
                if (!ball && (cy < 0f || cy > top || ax > 0.84f)) return false;
                shade = ball ? 1.25f : cy < 0.3f ? (Mathf.Repeat(x + 0.155f, 0.31f) < 0.1f && cy > 0.1f && cy < 0.22f ? 1.3f : 0.75f) : 0.95f + 0.2f * Smooth(0.5f, -0.5f, x);
                return true;
            }
        }
    }

    // Клякса из слившихся капель (метаболы) с отлетевшими брызгами.
    static Sprite MakeSplat(string name, int seed)
    {
        var rng = new System.Random(seed);
        var blobs = new Vector3[16];
        blobs[0] = new Vector3(0f, 0f, 0.34f);
        for (int i = 1; i < blobs.Length; i++)
        {
            float a = (float)rng.NextDouble() * Mathf.PI * 2f;
            bool far = i > 9;
            float r = far ? 0.62f + (float)rng.NextDouble() * 0.26f : 0.26f + (float)rng.NextDouble() * 0.3f;
            float size = far ? 0.04f + (float)rng.NextDouble() * 0.045f : 0.09f + (float)rng.NextDouble() * 0.12f;
            blobs[i] = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, size);
        }
        return MakeSprite(name, Tex(128, 128, -1, 1, -1, 1, (x, y) =>
        {
            float f = 0f;
            foreach (var b in blobs)
            {
                float dx = x - b.x, dy = y - b.y;
                f += b.z * b.z / (dx * dx + dy * dy + 1e-4f);
            }
            float k = 0.85f + 0.15f * Smooth(1f, 2.5f, f);
            return new Color(k, k, k, Smooth(0.9f, 1.15f, f));
        }), new Vector2(0.5f, 0.5f), 64f);
    }

    // ---------- Комикс ----------

    static void BuildStory()
    {
        Wizard = MakeSprite("Story/Wizard", Tex(160, 208, -1f, 1f, -1.05f, 1.55f, (x, y) => WizardPixel(x, y, false)), new Vector2(0.5f, 0.5f), 80f);
        WizardShock = MakeSprite("Story/WizardShock", Tex(160, 208, -1f, 1f, -1.05f, 1.55f, (x, y) => WizardPixel(x, y, true)), new Vector2(0.5f, 0.5f), 80f);
        Cauldron = MakeSprite("Story/Cauldron", Tex(208, 176, -1.3f, 1.3f, -1.1f, 1.1f, CauldronPixel), new Vector2(0.5f, 0.5f), 80f);

        // Эфирный серп: светящийся полумесяц.
        Sickle = MakeSprite("Story/Sickle", Tex(128, 128, -1, 1, -1, 1, (x, y) =>
        {
            float outer = Mathf.Sqrt(x * x + y * y), inner = Mathf.Sqrt((x - 0.34f) * (x - 0.34f) + (y - 0.1f) * (y - 0.1f));
            float a = Smooth(0.95f, 0.9f, outer) * Smooth(0.74f, 0.79f, inner);
            Color c = Color.Lerp(C(0.35f, 0.85f, 1f), Color.white, Smooth(0.86f, 0.74f, inner));
            c.a = a;
            return c;
        }), new Vector2(0.5f, 0.5f), 64f);

        // Комиксный «бабах»: неровная звезда с жёлтой сердцевиной.
        Burst = MakeSprite("Story/Burst", Tex(192, 192, -1, 1, -1, 1, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float turn = Mathf.Repeat(Mathf.Atan2(y, x) / (Mathf.PI * 2f), 1f) * 12f;
            float spike = Mathf.Abs(Mathf.Repeat(turn, 1f) - 0.5f) * 2f;
            float lim = (0.5f + 0.48f * (1f - spike)) * (0.8f + 0.2f * Hash(Mathf.Floor(turn), 7f));
            if (r >= lim) return new Color(1f, 0.5f, 0.1f, 0f);
            Color c = r > lim - 0.07f ? C(0.75f, 0.12f, 0.05f) : Color.Lerp(C(1f, 0.97f, 0.7f), C(1f, 0.55f, 0.08f), Smooth(0.2f, 0.9f, r / lim));
            c.a = 1f;
            return c;
        }), new Vector2(0.5f, 0.5f), 96f);
    }

    // Сердце — простая иконка: квадрат, поставленный на угол, и два полукруга сверху. Освещено так же, как
    // слаймы и бомба (свет слева сверху, тёмный кант по краю), но без бликов и пузырей — мягкий перелив цвета.
    static Color HeartPixel(float x, float y)
    {
        const float d = 0.8f;                       // полудиагональ квадрата
        float ax = Mathf.Abs(x), py = y + 0.1f;     // сердце чуть опущено, чтобы стоять по центру картинки
        float square = (ax + Mathf.Abs(py) - d) * 0.7071f;
        float lobe = Mathf.Sqrt((ax - d * 0.5f) * (ax - d * 0.5f) + (py - d * 0.5f) * (py - d * 0.5f)) - d * 0.7071f;
        float edge = Mathf.Min(square, lobe);       // расстояние до края: меньше нуля — внутри
        if (edge >= 0f) return new Color(0.9f, 0.2f, 0.28f, 0f);
        float light = Mathf.Clamp01(0.5f - 0.32f * x + 0.38f * y);
        Color c = Color.Lerp(C(0.76f, 0.1f, 0.22f), C(1f, 0.38f, 0.46f), light);
        float gx = x + 0.42f, gy = y - 0.36f;
        c = Over(c, Color.white, Smooth(0.1f, 0f, gx * gx + gy * gy * 1.6f) * 0.2f);
        c = Over(c, C(0.4f, 0.03f, 0.12f), Smooth(-0.09f, 0f, edge) * 0.9f);
        c.a = 1f;
        return c;
    }

    // Верховный алхимик: мантия, борода и колпак со звездой. shock — руки вверх, глаза круглые.
    // Вокруг фигуры идёт тёмный контур, как в рисованном комиксе: та же фигура, но чуть раздутая.
    static Color WizardPixel(float x, float y, bool shock)
    {
        Color c = WizardBody(x, y, shock, 0f);
        if (c.a > 0f) return c;
        return WizardBody(x, y, shock, Outline).a > 0f ? Ink : new Color(Ink.r, Ink.g, Ink.b, 0f);
    }

    const float Outline = 0.045f;
    static readonly Color Ink = new Color(0.13f, 0.07f, 0.22f, 1f);

    // g — на сколько раздуть все части фигуры (для контура).
    static Color WizardBody(float x, float y, bool shock, float g)
    {
        Color c = new Color(0.3f, 0.2f, 0.5f, 0f);
        Color robe = C(0.42f, 0.26f, 0.7f), robeDark = C(0.28f, 0.16f, 0.5f), skin = C(1f, 0.82f, 0.68f), gold = C(1f, 0.8f, 0.25f);

        // Рукава и ладони: опущены вдоль мантии или вскинуты вверх.
        for (int side = -1; side <= 1; side += 2)
        {
            float sx = x - side * (shock ? 0.6f : 0.52f), sy = y - (shock ? 0.2f : -0.2f);
            if (Oval(sx, sy, 0.2f + g, 0.265f + g)) c = Scale(robeDark, 0.9f + 0.25f * Smooth(0.2f, -0.2f, sx));
            float hx = x - side * (shock ? 0.74f : 0.6f), hy = y - (shock ? 0.5f : -0.44f);
            if (Oval(hx, hy, 0.105f + g, 0.105f + g)) c = skin;
        }

        // Мантия колоколом с золотой каймой и поясом.
        if (y > -1f - g && y < 0.2f + g)
        {
            float half = Mathf.Lerp(0.78f, 0.34f, (y + 1f) / 1.2f) + g;
            if (Mathf.Abs(x) < half)
            {
                c = Scale(robe, 0.78f + 0.4f * Smooth(half, -half, x));
                if (y < -0.9f || Mathf.Abs(y + 0.3f) < 0.045f) c = Scale(gold, 0.85f + 0.3f * Smooth(half, -half, x));
            }
        }

        // Голова, борода клином, нос.
        float dx = x, dy = y - 0.46f;
        if (Oval(dx, dy, 0.292f + g, 0.292f + g)) c = Scale(skin, 0.92f + 0.12f * Smooth(0.25f, -0.25f, dx));
        if (y < 0.4f + g && y > -0.3f - g && Mathf.Abs(x) < 0.3f * Mathf.Sqrt(Mathf.Max(0f, (y + 0.3f + g) / 0.7f)) + g)
            c = Scale(Color.white, 0.84f + 0.16f * Smooth(0.2f, -0.2f, x));
        if (dx * dx + (dy + 0.06f) * (dy + 0.06f) < 0.0045f) c = C(1f, 0.66f, 0.56f);

        for (int side = -1; side <= 1; side += 2)
        {
            float ex = x - side * 0.13f, ey = y - 0.52f;
            float q = ex * ex + ey * ey;
            if (shock)
            {
                if (q < 0.0064f) c = Color.white;
                if (q < 0.0016f) c = EyeDark;
            }
            else if (q < 0.0025f) c = EyeDark;
        }
        if (shock && Oval(x, y - 0.27f, 0.07f, 0.095f)) c = C(0.3f, 0.06f, 0.1f);

        // Колпак с загнутым кончиком, полями и звездой.
        float hat = (y - 0.68f) / 0.84f;
        if (hat > -g && hat < 1f + g)
        {
            float cx = 0.2f * hat * hat;
            if (Mathf.Abs(x - cx) < 0.4f * (1f - hat) + g)
            {
                c = Scale(robe, 0.8f + 0.4f * Smooth(0.3f, -0.3f, x - cx));
                if (hat < 0.13f) c = gold;
                float px = x - 0.06f, py = y - 1.02f;
                if (Mathf.Sqrt(Mathf.Abs(px)) + Mathf.Sqrt(Mathf.Abs(py)) < 0.36f) c = C(1f, 0.92f, 0.5f);
            }
        }
        if (Oval(x, y - 0.68f, 0.548f + g, 0.078f + g)) c = robeDark;

        return c;
    }

    static bool Oval(float x, float y, float a, float b)
    {
        return x * x / (a * a) + y * y / (b * b) < 1f;
    }

    // Медный автоклав: пузатый котёл на ножках, обод с заклёпками, светящееся зелье. Тоже с контуром.
    static Color CauldronPixel(float x, float y)
    {
        Color c = CauldronBody(x, y, 0f);
        if (c.a > 0f) return c;
        return CauldronBody(x, y, Outline).a > 0f ? Ink : new Color(Ink.r, Ink.g, Ink.b, 0f);
    }

    static Color CauldronBody(float x, float y, float g)
    {
        Color c = new Color(0.6f, 0.3f, 0.12f, 0f);
        Color copper = C(0.8f, 0.46f, 0.2f), dark = C(0.36f, 0.17f, 0.08f);

        for (int side = -1; side <= 1; side += 2)
        {
            float lx = x - side * 0.6f;
            if (y < -0.78f + g && y > -1.02f - g && Mathf.Abs(lx) < 0.1f + (y + 1.02f) * 0.2f + g) c = dark;
        }

        if (Oval(x, y + 0.2f, 1.05f + g, 0.78f + g) && y < 0.42f + g)
        {
            float bx = x / 1.05f, by = (y + 0.2f) / 0.78f;
            float z = Mathf.Sqrt(Mathf.Max(0f, 1f - bx * bx - by * by));
            float light = Mathf.Max(0f, -0.45f * bx + 0.5f * by + 0.7f * z);
            c = Scale(Color.Lerp(dark, copper, 0.35f + 0.65f * light), 0.75f + 0.55f * light);
            // Обод с заклёпками.
            if (Mathf.Abs(y + 0.14f) < 0.08f)
            {
                c = Scale(c, 0.72f);
                float px = Mathf.Repeat(x + 0.14f, 0.28f) - 0.14f, py = y + 0.14f;
                if (px * px + py * py < 0.0028f) c = C(1f, 0.74f, 0.42f);
            }
            float spec = Mathf.Pow(Mathf.Max(0f, -0.42f * bx + 0.55f * by + 0.72f * z), 14f) * 0.5f;
            c = new Color(c.r + spec, c.g + spec * 0.8f, c.b + spec * 0.6f, 1f);
        }

        // Горловина и зелье в ней.
        if (Oval(x, y - 0.42f, 0.98f + g, 0.17f + g))
        {
            float mx = x / 0.98f, my = (y - 0.42f) / 0.17f;
            float m2 = mx * mx + my * my;
            c = m2 > 0.72f ? Scale(copper, 1.15f) : Color.Lerp(C(0.75f, 1f, 0.45f), C(0.2f, 0.8f, 0.25f), Smooth(0f, 0.72f, m2));
            c.a = 1f;
        }
        return c;
    }

    // ---------- Сцена ----------

    static void BuildScene()
    {
        const float aspect = 16f / 9f;
        Wall = MakeSprite("Scene/Wall", Tex(640, 360, -aspect, aspect, -1, 1, WallPixel, true), new Vector2(0.5f, 0.5f), 360f);
        Rim = MakeSprite("Scene/Rim", Tex(1024, 44, 0, RimUnits, 0, 1, RimPixel), new Vector2(0.5f, 0f), 44f);

        // Декор главного экрана: тихий фон, который не спорит с меню.
        // Слои художника сделаны под экран 16:9 и ложатся поверх стены. Полки делятся пополам: каждая половина
        // держится своего края экрана при любом формате окна. Борт котла рисуется поверх слаймов.
        ShelvesLeft = Part("Scene/Shelves", 0f, 0.5f, new Vector2(0f, 0.5f));
        ShelvesRight = Part("Scene/Shelves", 0.5f, 1f, new Vector2(1f, 0.5f));
        Brim = FromFile("Scene/Brim", new Vector2(0.5f, 0f), 1f);
        Cat = MakeSprite("Scene/Cat", Tex(160, 216, -1f, 1f, -1f, 1.7f, (x, y) => CatPixel(x, y, false)), new Vector2(0.5f, 0f), 80f);
        CatBlink = MakeSprite("Scene/CatBlink", Tex(160, 216, -1f, 1f, -1f, 1.7f, (x, y) => CatPixel(x, y, true)), new Vector2(0.5f, 0f), 80f);
        Color[] liquids = { C(0.45f, 0.95f, 0.5f), C(0.72f, 0.5f, 1f), C(1f, 0.55f, 0.68f) };
        Potions = new Sprite[liquids.Length];
        for (int i = 0; i < Potions.Length; i++)
        {
            int shape = i;
            Color liquid = liquids[i];
            Potions[i] = MakeSprite("Scene/Potion" + (i + 1), Tex(128, 160, -1f, 1f, -1f, 1.5f, (x, y) => PotionPixel(x, y, shape, liquid)),
                new Vector2(0.5f, 0f), 64f);
        }
    }

    // Чёрная кошка в ведьминой шляпе сидит анфас: тёмный силуэт со светлым кантом, чтобы не теряться на тёмном фоне,
    // и зелёные глаза.
    static Color CatPixel(float x, float y, bool blink)
    {
        Color c = CatBody(x, y, blink, 0f);
        if (c.a > 0f) return c;
        return CatBody(x, y, blink, 0.035f).a > 0f ? C(0.46f, 0.4f, 0.64f) : new Color(0.46f, 0.4f, 0.64f, 0f);
    }

    static Color CatBody(float x, float y, bool blink, float g)
    {
        Color c = new Color(0.1f, 0.08f, 0.16f, 0f);
        Color fur = C(0.12f, 0.1f, 0.19f);

        // Хвост: дуга, обнимающая бок.
        float tx = x - 0.5f, ty = y + 0.66f;
        float ring = Mathf.Abs(Mathf.Sqrt(tx * tx + ty * ty) - 0.34f);
        if (ring < 0.075f + g && (tx > -0.1f || ty < 0f)) c = fur;

        // Туловище грушей и голова.
        if (Oval(x, y + 0.62f, 0.6f + g, 0.38f + g) || Oval(x, y + 0.3f, 0.46f + g, 0.62f + g)) c = Scale(fur, 0.9f + 0.5f * Smooth(0.5f, -0.5f, x));
        if (Oval(x, y - 0.42f, 0.42f + g, 0.38f + g)) c = Scale(fur, 0.95f + 0.5f * Smooth(0.4f, -0.4f, x));

        // Уши треугольниками.
        float ax = Mathf.Abs(x);
        float ear = Mathf.Min(Mathf.Min(Side(ax, y, 0.1f, 0.68f, 0.44f, 0.56f), Side(ax, y, 0.44f, 0.56f, 0.36f, 1.08f)), Side(ax, y, 0.36f, 1.08f, 0.1f, 0.68f));
        if (ear > -g * 0.5f) c = fur;
        float inner = Mathf.Min(Mathf.Min(Side(ax, y, 0.2f, 0.72f, 0.38f, 0.64f), Side(ax, y, 0.38f, 0.64f, 0.34f, 0.94f)), Side(ax, y, 0.34f, 0.94f, 0.2f, 0.72f));
        if (g == 0f && inner > 0f) c = C(0.42f, 0.24f, 0.4f);

        // Шляпа: колпак с загнутым кончиком, лента и поля. Кончики ушей выглядывают по бокам колпака.
        float hat = (y - 0.84f) / 0.8f, lean = 0.24f * hat * hat;
        if (hat > -g && hat < 1f + g && Mathf.Abs(x - lean) < 0.3f * (1f - hat) + g)
        {
            c = Scale(C(0.42f, 0.24f, 0.68f), 0.8f + 0.45f * Smooth(0.3f, -0.3f, x - lean));
            if (hat < 0.2f) c = C(0.5f, 0.92f, 0.56f);
        }
        if (Oval(x, y - 0.84f, 0.64f + g, 0.09f + g)) c = Scale(C(0.32f, 0.17f, 0.54f), 0.9f + 0.4f * Smooth(0.5f, -0.5f, x));

        if (g > 0f) return c;

        // Глаза: зелёные со щёлкой зрачка; когда кошка моргает — две дужки.
        float ex = ax - 0.17f, ey = y - 0.45f;
        if (blink)
        {
            if (Mathf.Abs(ey + ex * ex * 3f) < 0.02f && Mathf.Abs(ex) < 0.1f) c = C(0.5f, 0.85f, 0.45f);
        }
        else if (Oval(ex, ey, 0.095f, 0.115f))
        {
            c = Color.Lerp(C(0.75f, 1f, 0.55f), C(0.3f, 0.8f, 0.35f), Smooth(0f, 0.11f, Mathf.Abs(ey)));
            if (Mathf.Abs(ex) < 0.022f) c = C(0.05f, 0.05f, 0.1f);
        }
        // Нос.
        if (Mathf.Abs(x) < 0.05f - (0.36f - y) * 0.9f && y < 0.36f && y > 0.3f) c = C(0.95f, 0.55f, 0.65f);
        return c;
    }

    // Склянка с зельем: стекло с тёмным контуром, цветная жидкость, пробка и блик. shape: 0 — круглая колба,
    // 1 — высокая бутыль, 2 — коническая колба.
    static Color PotionPixel(float x, float y, int shape, Color liquid)
    {
        Color c = PotionBody(x, y, shape, liquid, 0f);
        if (c.a > 0f) return c;
        return PotionBody(x, y, shape, liquid, Outline).a > 0f ? Ink : new Color(Ink.r, Ink.g, Ink.b, 0f);
    }

    static Color PotionBody(float x, float y, int shape, Color liquid, float g)
    {
        Color c = new Color(0.7f, 0.85f, 1f, 0f);
        float ax = Mathf.Abs(x);
        bool body;
        float level;    // до какой высоты налито
        if (shape == 0)
        {
            body = Oval(x, y + 0.3f, 0.68f + g, 0.68f + g);
            level = -0.1f;
        }
        else if (shape == 1)
        {
            body = ax < 0.46f + g && y > -0.96f - g && y < 0.3f + g || Oval(x, y - 0.3f, 0.46f + g, 0.2f + g);
            level = 0.05f;
        }
        else
        {
            body = y > -0.96f - g && y < 0.3f + g && ax < Mathf.Lerp(0.82f, 0.2f, (y + 0.96f) / 1.26f) + g;
            level = -0.35f;
        }
        bool neck = ax < 0.2f + g && y > 0.1f && y < 1f + g;
        bool lip = ax < 0.28f + g && y > 0.94f - g && y < 1.06f + g;
        bool cork = ax < 0.17f + g && y > 1.02f && y < 1.3f + g;

        if (body || neck || lip)
        {
            // Стекло: бледное и полупрозрачное.
            c = new Color(0.78f, 0.9f, 1f, 0.34f);
            if (body && y < level)
            {
                // Жидкость светлее у поверхности и темнее на дне.
                c = Color.Lerp(Scale(liquid, 0.6f), liquid, Smooth(-1f, level, y));
                if (y > level - 0.07f) c = Color.Lerp(liquid, Color.white, 0.45f);
                c.a = 0.95f;
            }
            // Блик на левом боку.
            if (body && Mathf.Abs(x + 0.3f + (y + 0.3f) * 0.12f) < 0.05f && y > -0.72f && y < 0.12f) c = Over(c, Color.white, 0.55f);
        }
        if (cork) c = Scale(C(0.62f, 0.42f, 0.26f), 0.85f + 0.3f * Smooth(0.15f, -0.15f, x));
        return c;
    }

    static readonly Vector3[] Flasks =
    {
        new Vector3(-1.58f, 0.1f, 0f), new Vector3(-1.32f, 0.075f, 1f), new Vector3(-1.1f, 0.09f, 2f),
        new Vector3(1.08f, 0.085f, 3f), new Vector3(1.3f, 0.1f, 4f), new Vector3(1.56f, 0.07f, 0f)
    };
    static readonly Color[] FlaskColors =
    {
        C(0.4f, 1f, 0.3f), C(1f, 0.45f, 0.15f), C(0.7f, 0.35f, 1f), C(0.3f, 0.75f, 1f), C(1f, 0.85f, 0.3f)
    };

    // Каменная стена лаборатории: кладка, свет факелов, зелёное зарево котла, полки с колбами.
    static Color WallPixel(float x, float y)
    {
        const float bw = 0.44f, bh = 0.18f, shelf = 0.02f;
        float row = Mathf.Floor(y / bh);
        float ox = x + (Mathf.Repeat(row, 2f) < 1f ? 0f : bw * 0.5f) + 20f;
        float col = Mathf.Floor(ox / bw);
        float fx = ox / bw - col, fy = y / bh - row;

        float tone = 0.72f + 0.5f * Hash(col, row);
        float grain = 0.82f + 0.2f * Noise(x * 46f, y * 46f) + 0.22f * Noise(x * 8f + 5f, y * 8f);
        Color c = Scale(C(0.105f + 0.03f * Hash(col + 3.3f, row + 7.7f), 0.11f, 0.165f), tone * grain);
        float gap = Mathf.Min(Mathf.Min(fx, 1f - fx) * bw, Mathf.Min(fy, 1f - fy) * bh);
        c = Scale(c, Mathf.Lerp(0.3f, 1f, Smooth(0.004f, 0.016f, gap)));
        c = Scale(c, 1f + 0.22f * Smooth(0.78f, 0.97f, fy) - 0.18f * Smooth(0.24f, 0.04f, fy));

        float lit = 0f;
        for (int side = -1; side <= 1; side += 2)
        {
            float dx = x - side * 1.28f, dy = y - 0.52f;
            lit += 1f / (1f + (dx * dx + dy * dy) * 7f);
        }
        c += C(1f, 0.55f, 0.2f) * (lit * 0.2f * tone);
        c += C(0.2f, 0.9f, 0.35f) * (Smooth(-0.1f, -1f, y) * 0.11f);

        // Полки с колбами по бокам — в центре стена чистая, чтобы не спорить с игровыми объектами.
        float ax = Mathf.Abs(x);
        if (ax > 0.94f && ax < 1.72f)
        {
            if (y > shelf - 0.035f && y < shelf) c = Scale(C(0.3f, 0.19f, 0.11f), 0.7f + 0.5f * Noise(x * 30f, 2f));
            else if (y < shelf - 0.035f && y > shelf - 0.09f) c = Scale(c, 0.55f);
        }
        foreach (var f in Flasks)
        {
            if (ax < 0.9f || y < shelf || y > shelf + 0.45f) break;
            Color fc = FlaskColors[(int)f.z];
            float dx = x - f.x, dy = y - (shelf + f.y);
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            c += fc * (0.025f / (1f + d * d * 60f));
            bool neck = Mathf.Abs(dx) < f.y * 0.3f && dy > 0f && dy < f.y * 1.9f;
            if (d < f.y || neck)
            {
                // Колбы намеренно тусклые: это фон, они не должны спорить с летящими слаймами.
                Color glass = Scale(fc, dy < f.y * 0.25f && d < f.y ? 0.26f : 0.07f);
                if (d > f.y * 0.84f && !neck) glass = Scale(fc, 0.17f);
                float gx = dx + f.y * 0.4f, gy = dy - f.y * 0.35f;
                if (gx * gx + gy * gy < f.y * f.y * 0.04f) glass = Scale(Color.white, 0.22f);
                c = Over(c, glass, 0.85f);
            }
        }

        c = Scale(c, 1f - 0.32f * (x * x / 3.16f + y * y));
        c.a = 1f;
        return c;
    }

    // Медный борт автоклава вдоль нижнего края: слаймы вылетают из-за него и падают обратно.
    static Color RimPixel(float x, float y)
    {
        Color c = Color.Lerp(C(0.3f, 0.14f, 0.07f), C(0.8f, 0.46f, 0.2f), Smooth(0f, 0.95f, y));
        c = Over(c, C(1f, 0.82f, 0.55f), Smooth(0.1f, 0f, Mathf.Abs(y - 0.84f)) * 0.7f);
        c = Scale(c, 1f - 0.4f * Smooth(0.05f, 0f, Mathf.Abs(y - 0.62f)));
        c = Over(c, C(0.2f, 0.55f, 0.45f), Smooth(0.45f, 0.9f, Noise(x * 0.9f, y * 1.6f)) * Smooth(0.75f, 0.2f, y) * 0.45f);

        float px = Mathf.Repeat(x, 1f) - 0.5f, py = (y - 0.32f) * 1.55f;
        float rr = px * px + py * py;
        if (rr < 0.014f) c = Scale(C(0.95f, 0.6f, 0.3f), 0.7f + 0.6f * Mathf.Clamp01(0.5f - (px + py) * 4f));
        else if (rr < 0.022f) c = Scale(c, 0.55f);

        c += C(0.3f, 1f, 0.4f) * (Smooth(0.8f, 1f, y) * 0.3f);
        c.a = Smooth(1f, 0.93f, y);
        return c;
    }

    static void BuildMaterials()
    {
        // Unlit-шейдер не зависит от 2D-света в сцене. Материал в Resources гарантирует, что шейдер попадёт в билд;
        // если его всё же нет, берём стандартный материал спрайтов.
        var asset = Resources.Load<Material>("SpriteUnlit");
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (asset != null)
        {
            Mat = new Material(asset);
        }
        else if (shader != null)
        {
            Mat = new Material(shader);
        }
        else
        {
            var probe = new GameObject("MaterialProbe");
            Mat = new Material(probe.AddComponent<SpriteRenderer>().sharedMaterial);
            Object.Destroy(probe);
        }
        SoftMat = new Material(Mat) { mainTexture = Soft.texture };
        DotMat = new Material(Mat) { mainTexture = Dot.texture };
        StarMat = new Material(Mat) { mainTexture = Star.texture };
    }

    // ---------- Вспомогательное ----------

    static Color C(float r, float g, float b) { return new Color(r, g, b, 1f); }

    static Color Scale(Color c, float k) { return new Color(c.r * k, c.g * k, c.b * k, c.a); }

    // Накладывает цвет поверх, не трогая прозрачность основы.
    static Color Over(Color c, Color top, float k)
    {
        k = Mathf.Clamp01(k);
        return new Color(c.r + (top.r - c.r) * k, c.g + (top.g - c.g) * k, c.b + (top.b - c.b) * k, c.a);
    }

    // Плавная ступенька; при a > b работает как убывающая.
    static float Smooth(float a, float b, float v)
    {
        float t = Mathf.Clamp01((v - a) / (b - a));
        return t * t * (3f - 2f * t);
    }

    static float Side(float x, float y, float ax, float ay, float bx, float by)
    {
        return (bx - ax) * (y - ay) - (by - ay) * (x - ax);
    }

    static float RoundBox(float x, float y, float half, float radius)
    {
        return Box(x, y, half, half, radius);
    }

    // Расстояние до края скруглённого прямоугольника: меньше нуля — внутри.
    static float Box(float x, float y, float halfW, float halfH, float radius)
    {
        float qx = Mathf.Abs(x) - (halfW - radius), qy = Mathf.Abs(y) - (halfH - radius);
        float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
        return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
    }

    static float Hash(float x, float y)
    {
        return Mathf.Repeat(Mathf.Sin(x * 127.1f + y * 311.7f) * 43758.5453f, 1f);
    }

    static float Noise(float x, float y)
    {
        float ix = Mathf.Floor(x), iy = Mathf.Floor(y);
        float fx = x - ix, fy = y - iy;
        fx = fx * fx * (3f - 2f * fx);
        fy = fy * fy * (3f - 2f * fy);
        return Mathf.Lerp(Mathf.Lerp(Hash(ix, iy), Hash(ix + 1f, iy), fx), Mathf.Lerp(Hash(ix, iy + 1f), Hash(ix + 1f, iy + 1f), fx), fy);
    }

    // Ячеистый узор: id — случайное число ячейки, edge — расстояние до её границы.
    static void Voronoi(float x, float y, out float id, out float edge)
    {
        float cx = Mathf.Floor(x), cy = Mathf.Floor(y);
        float d1 = 9f, d2 = 9f;
        id = 0f;
        for (int j = -1; j <= 1; j++)
        {
            for (int i = -1; i <= 1; i++)
            {
                float gx = cx + i, gy = cy + j;
                float px = gx + 0.15f + 0.7f * Hash(gx, gy), py = gy + 0.15f + 0.7f * Hash(gy + 31.7f, gx + 11.3f);
                float dd = (x - px) * (x - px) + (y - py) * (y - py);
                if (dd < d1)
                {
                    d2 = d1;
                    d1 = dd;
                    id = Hash(gx + 5.1f, gy + 9.7f);
                }
                else if (dd < d2) d2 = dd;
            }
        }
        edge = Mathf.Sqrt(d2) - Mathf.Sqrt(d1);
    }

    static Sprite Whole(string name, float extent, System.Func<float, float, Color> f)
    {
        int n = Mathf.RoundToInt(N * extent);
        return MakeSprite(name, Tex(n, n, -extent, extent, -extent, extent, f), new Vector2(0.5f, 0.5f), N / 2f);
    }

    // Половинка: купол и наклонённый к зрителю срез. Опорная точка — центр среза.
    static Sprite Half(string name, System.Func<float, float, Color> dome, System.Func<float, float, float, Color> face)
    {
        int h = Mathf.RoundToInt(N * 0.7f);
        float height = h / (N / 2f);
        float below = height - 1f;
        return MakeSprite(name, Tex(N, h, -1, 1, -below, 1, (x, y) =>
        {
            float fy = y / FaceTilt;
            float e = Mathf.Sqrt(x * x + fy * fy);
            if (e < 1f) return face(x, fy, e);
            return y > 0f ? dome(x, y) : Clear;
        }), new Vector2(0.5f, below / height), N / 2f);
    }

    // Картинка берётся из файла Assets/Resources/Art/<name>.png, а если его нет — рисуется формулой.
    // Размер в игре задан заготовкой и не зависит от разрешения файла: свою картинку можно сделать любой чёткости.
    static Sprite MakeSprite(string name, Draft draft, Vector2 pivot, float pixelsPerUnit, float border = 0f)
    {
        if (export != null)
        {
            export(name, draft);
            return null;
        }
        Texture2D tex = Resources.Load<Texture2D>(Folder + "/" + name);
        if (tex != null) fromFile.Add(name);
        else if (!drawn.TryGetValue(name, out tex) || tex == null) drawn[name] = tex = draft.Bake();
        float k = tex.width / (float)draft.w;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, pixelsPerUnit * k, 0, SpriteMeshType.FullRect,
            new Vector4(border, border, border, border) * k);
    }

    // Картинка без формулы: её нарисовал художник. Если файла нет, её нет совсем, и игра обходится запасным
    // вариантом (стена из формулы вместо лаборатории, надпись вместо логотипа). unitsHigh — высота в мировых единицах.
    static Sprite FromFile(string name, Vector2 pivot, float unitsHigh)
    {
        if (export != null) return null;
        Texture2D tex = Resources.Load<Texture2D>(Folder + "/" + name);
        if (tex == null) return null;
        fromFile.Add(name);
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, tex.height / unitsHigh, 0, SpriteMeshType.FullRect);
    }

    // Часть картинки художника: полоса по ширине от from до to (в долях ширины). Высота спрайта — одна мировая единица.
    static Sprite Part(string name, float from, float to, Vector2 pivot)
    {
        if (export != null) return null;
        Texture2D tex = Resources.Load<Texture2D>(Folder + "/" + name);
        if (tex == null) return null;
        fromFile.Add(name);
        return Sprite.Create(tex, new Rect(tex.width * from, 0, tex.width * (to - from), tex.height), pivot, tex.height, 0, SpriteMeshType.FullRect);
    }

    static Draft Tex(int w, int h, float x0, float x1, float y0, float y1, System.Func<float, float, Color> f, bool plain = false)
    {
        return new Draft { w = w, h = h, x0 = x0, x1 = x1, y0 = y0, y1 = y1, f = f, plain = plain };
    }
}

// Заготовка картинки: размер, поле координат [x0..x1] x [y0..y1] и формула цвета.
// В текстуру превращается, только когда готового файла нет, и при выгрузке исходных картинок.
public class Draft
{
    public int w, h;
    public float x0, x1, y0, y1;
    public System.Func<float, float, Color> f;
    public bool plain;  // плавным фонам сглаживание не нужно — им хватает одной пробы на пиксель

    // scale увеличивает разрешение: формулы от него не зависят, поэтому файлы выгружаются чётче, чем нужно игре.
    public Texture2D Bake(int scale = 1, bool readable = false)
    {
        var tex = new Texture2D(w * scale, h * scale, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        tex.SetPixels(Render(scale));
        tex.Apply(false, !readable);
        return tex;
    }

    // Рисует пиксели со сглаживанием 2x2. Строки считаются параллельно на всех ядрах:
    // функции цвета — чистая математика без общего состояния.
    Color[] Render(int scale)
    {
        int width = w * scale, height = h * scale;
        var px = new Color[width * height];
        int n = plain ? 1 : 2;
        System.Threading.Tasks.Parallel.For(0, height, j =>
        {
            for (int i = 0; i < width; i++)
            {
                float r = 0, g = 0, b = 0, a = 0, pr = 0, pg = 0, pb = 0;
                for (int s = 0; s < n * n; s++)
                {
                    float u = Mathf.Lerp(x0, x1, (i + (s % n + 0.5f) / n) / width);
                    float v = Mathf.Lerp(y0, y1, (j + (s / n + 0.5f) / n) / height);
                    Color c = f(u, v);
                    r += c.r; g += c.g; b += c.b; a += c.a;
                    pr += c.r * c.a; pg += c.g * c.a; pb += c.b * c.a;
                }
                // Цвет прозрачных пикселей берём от соседей, иначе по краю появится тёмная кайма.
                float k = 1f / (n * n);
                px[j * width + i] = a > 0f ? new Color(pr / a, pg / a, pb / a, a * k) : new Color(r * k, g * k, b * k, 0f);
            }
        });
        return px;
    }
}
