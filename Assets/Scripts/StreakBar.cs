using UnityEngine;
using UnityEngine.UI;

// Полоса серии: сколько слаймов разрублено подряд без промаха и далеко ли до следующей вехи. Стоит в левом
// нижнем углу, на борту котла, и не заслоняет слаймов: слайм-талисман, полоса с зельем и табличка с числом. У каждого уровня серии свой цвет,
// мордочка талисмана и искры; на вехе полоса вспыхивает и заполняется заново, а после промаха гаснет и вздрагивает.
public class StreakBar : MonoBehaviour
{
    const float Width = 214f, Height = 24f;
    const int Sparks = 40;

    struct Tier
    {
        public string name;
        public Color color;
        public int face;        // какая мордочка у талисмана
        public float sparks;    // искр в секунду
    }

    // Уровней на один больше, чем вех в балансе: первый идёт до первой вехи, последний — после последней.
    static readonly Tier[] Tiers =
    {
        new Tier { name = "Серия", color = new Color(0.45f, 1f, 0.4f), face = 0, sparks = 0f },
        new Tier { name = "Хорошая серия", color = new Color(0.3f, 0.8f, 1f), face = 0, sparks = 3f },
        new Tier { name = "Высокая серия", color = new Color(1f, 0.82f, 0.22f), face = 1, sparks = 6f },
        new Tier { name = "Эпическая серия", color = new Color(1f, 0.4f, 0.3f), face = 2, sparks = 10f },
        new Tier { name = "Легендарная серия", color = new Color(0.86f, 0.45f, 1f), face = 2, sparks = 16f },
    };

    static readonly Color Idle = new Color(0.6f, 0.6f, 0.72f);     // серии ещё нет
    static readonly Color Broken = new Color(1f, 0.22f, 0.3f);     // серия оборвалась

    // Цвета уровней видны и в меню: там ими окрашены вехи серии.
    public static Color TierColor(int index) { return Tiers[Mathf.Clamp(index, 0, Tiers.Length - 1)].color; }

    CanvasGroup group;
    RectTransform shell, fill;
    Image glow, level, ring, head, mascot, mascotCrown, badge, badgeGlow, badgeCrown, flash;
    Text number, caption;
    TextGradient numberPaint;
    readonly Image[] sparks = new Image[Sparks];
    readonly Vector2[] sparkVel = new Vector2[Sparks];
    readonly float[] sparkAge = new float[Sparks], sparkLife = new float[Sparks], sparkSize = new float[Sparks];
    readonly Color[] sparkColor = new Color[Sparks];
    Vector2 home;
    int nextSpark, shown = -1, shownTier;
    float amount, punch, tierPunch, breakLeft, flashLeft, owed;

    public static StreakBar Create(Transform parent, Font font, Font display)
    {
        var go = new GameObject("StreakBar", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
        rt.anchoredPosition = new Vector2(12f, 8f);
        rt.sizeDelta = new Vector2(392f, 96f);
        var streak = go.AddComponent<StreakBar>();
        streak.Build(rt, font, display);
        return streak;
    }

    void Build(RectTransform root, Font font, Font display)
    {
        shell = root;
        home = root.anchoredPosition;
        group = gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        Vector2 right = new Vector2(1f, 0.5f), center = new Vector2(0.5f, 0.5f), left = new Vector2(0f, 0.5f);
        var barGo = new GameObject("Bar", typeof(RectTransform));
        var bar = (RectTransform)barGo.transform;
        bar.SetParent(root, false);
        bar.anchorMin = bar.anchorMax = right;
        bar.anchoredPosition = new Vector2(-116f - Width * 0.5f, 4f);
        bar.sizeDelta = new Vector2(Width, Height);

        // Слои снизу вверх: ореол, искры, тёмное ложе, зелье с бликом, светлый контур, огонёк на конце, талисман, табличка.
        glow = Hud.Picture(bar, Art.PillGlow, Color.clear, center, center, Vector2.zero,
            new Vector2(Width + 10f + (Height + 10f) * 0.55f, (Height + 10f) * 1.55f));
        glow.type = Image.Type.Sliced;
        for (int i = 0; i < Sparks; i++)
        {
            sparks[i] = Hud.Picture(bar, Art.Star, Color.clear, left, center, Vector2.zero, Vector2.one);
            sparks[i].preserveAspect = false;
        }
        var bed = Hud.Picture(bar, Art.Pill, new Color(0.05f, 0.04f, 0.1f, 0.88f), center, center, Vector2.zero, new Vector2(Width + 10f, Height + 10f));
        bed.type = Image.Type.Sliced;
        level = Hud.Picture(bar, Art.Pill, Color.white, left, left, Vector2.zero, new Vector2(Width, Height));
        level.type = Image.Type.Sliced;
        fill = level.rectTransform;
        var gloss = Hud.Picture(fill, Art.Pill, new Color(1f, 1f, 1f, 0.36f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        gloss.type = Image.Type.Sliced;
        gloss.rectTransform.anchorMin = new Vector2(0.04f, 0.56f);
        gloss.rectTransform.anchorMax = new Vector2(0.96f, 0.86f);
        ring = Hud.Picture(bar, Art.PillRing, Color.white, center, center, Vector2.zero, new Vector2(Width + 10f, Height + 10f));
        ring.type = Image.Type.Sliced;
        head = Hud.Picture(bar, Art.Soft, Color.clear, left, center, Vector2.zero, new Vector2(Height * 2.4f, Height * 2.4f));
        mascot = Hud.Picture(bar, Art.Mascot[0], Color.white, left, center, new Vector2(-14f, 2f), new Vector2(66f, 66f));
        // Корона стоит на макушке ровно и не сплющивается вместе с талисманом: она лежит рядом, а не внутри него.
        mascotCrown = Hud.Picture(bar, Art.Crown, Color.clear, left, new Vector2(0.5f, 0f), new Vector2(-14f, 24f), new Vector2(40f, 30f));
        caption = Hud.Label(bar, font, "", 16, Color.white, TextAnchor.MiddleCenter, center, new Vector2(8f, -31f));

        badgeGlow = Hud.Picture(root, Art.Soft, Color.clear, right, center, new Vector2(-61f, 4f), new Vector2(214f, 168f));
        badgeGlow.preserveAspect = false;
        badge = Hud.Picture(root, Art.Badge, Color.white, right, center, new Vector2(-61f, 4f), new Vector2(122f, 90f));
        badgeCrown = Hud.Picture(root, Art.Crown, Color.clear, right, new Vector2(0.5f, 0f), new Vector2(-61f, 40f), new Vector2(48f, 36f));
        Hud.Label(root, font, "СЕРИЯ", 15, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleCenter, right, new Vector2(-61f, 29f));
        number = Hud.Label(root, display, "0", 40, Color.white, TextAnchor.MiddleCenter, right, new Vector2(-61f, -5f));
        numberPaint = number.GetComponent<TextGradient>();
        flash = Hud.Picture(root, Art.Soft, Color.clear, center, center, new Vector2(0f, 4f), new Vector2(560f, 190f));
        flash.preserveAspect = false;
    }

    // Новая попытка начинается с чистой полосы — без «обрыва» прошлой серии.
    public void Restart()
    {
        shown = -1;
        amount = punch = tierPunch = breakLeft = flashLeft = 0f;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime, time = Time.unscaledTime;
        var gm = GameManager.Instance;
        var balance = Balance.Current;
        int streak = gm.State == GameState.Playing || gm.State == GameState.GameOver ? gm.Streak : 0;

        if (streak != shown)
        {
            int tier = Mathf.Min(balance.StreakTier(streak), Tiers.Length - 1);
            if (shown >= 0 && streak > shown)
            {
                punch = 1f;
                // Веха взята: вспышка, россыпь искр, полоса начинает новый круг.
                if (tier > shownTier || balance.IsStreakGoal(streak))
                {
                    tierPunch = flashLeft = 1f;
                    for (int i = 0; i < 26; i++) Emit(Random.Range(0f, Width), Tiers[tier].color, true);
                }
            }
            else if (shown >= 3) breakLeft = 1f;
            shown = streak;
            shownTier = tier;
            int from, to;
            balance.StreakSpan(streak, out from, out to);
            number.text = streak.ToString();
            caption.text = Tiers[tier].name + "   →   ×" + to;
            mascot.sprite = Art.Mascot[Tiers[tier].face];
        }

        punch = Mathf.MoveTowards(punch, 0f, dt * 6f);
        tierPunch = Mathf.MoveTowards(tierPunch, 0f, dt * 2.5f);
        breakLeft = Mathf.MoveTowards(breakLeft, 0f, dt * 2.2f);
        flashLeft = Mathf.MoveTowards(flashLeft, 0f, dt * 2.4f);

        // Пока серии нет, полоса стоит приглушённой; на высшем уровне цвет переливается.
        bool legend = shownTier == Tiers.Length - 1;
        Color color = shown > 0 ? Tiers[shownTier].color : Idle;
        if (legend) color = Color.HSVToRGB(0.68f + 0.22f * Mathf.Sin(time * 1.8f), 0.6f, 1f);
        float breath = 0.5f + 0.5f * Mathf.Sin(time * (2.5f + shownTier));
        group.alpha = Mathf.MoveTowards(group.alpha, shown > 0 || breakLeft > 0f ? 1f : 0.62f, dt * 3f);

        int start, goal;
        balance.StreakSpan(Mathf.Max(shown, 0), out start, out goal);
        float target = shown > 0 ? Mathf.Clamp01((shown - start) / (float)(goal - start)) : 0f;
        amount = Mathf.MoveTowards(amount, target, dt * (target < amount ? 4f : 1.6f));
        float width = Mathf.Max(Height, Width * amount);
        fill.sizeDelta = new Vector2(width, Height);
        level.color = new Color(color.r, color.g, color.b, amount > 0.004f ? 1f : 0f);
        ring.color = Color.Lerp(Color.Lerp(color, Color.white, 0.55f), Broken, breakLeft);
        glow.color = new Color(color.r, color.g, color.b, shown > 0 ? 0.28f + 0.1f * shownTier + 0.12f * breath + 0.3f * punch : 0f);
        head.color = new Color(1f, 1f, 1f, amount > 0.004f ? 0.35f + 0.3f * breath + 0.3f * punch : 0f);
        head.rectTransform.anchoredPosition = new Vector2(width - Height * 0.3f, 0f);

        // Талисман подпрыгивает тем живее, чем выше уровень; на легендарной серии на нём корона.
        float hop = shown > 0 ? Mathf.Abs(Mathf.Sin(time * (3f + shownTier))) : 0f;
        mascot.color = Color.Lerp(color, Color.white, 0.12f);
        mascot.rectTransform.anchoredPosition = new Vector2(-14f, 2f + (1.5f + shownTier) * hop);
        mascot.rectTransform.localScale = new Vector3(1f + 0.05f * (1f - hop), 1f - 0.05f * (1f - hop), 1f) * (1f + 0.2f * punch + 0.3f * tierPunch);
        mascotCrown.color = badgeCrown.color = legend ? Color.white : Color.clear;
        mascotCrown.rectTransform.anchoredPosition = new Vector2(-14f, 24f + (1.5f + shownTier) * hop);

        Color plate = Color.Lerp(Color.Lerp(color, Color.white, 0.25f), Broken, breakLeft);
        badge.color = plate;
        badge.transform.localScale = Vector3.one * (1f + 0.08f * punch + 0.22f * tierPunch);
        badgeGlow.color = new Color(color.r, color.g, color.b, shown > 0 ? 0.16f + 0.06f * shownTier + 0.3f * tierPunch : 0f);
        numberPaint.top = Color.white;
        numberPaint.bottom = Color.Lerp(plate, Color.white, 0.45f);
        number.SetVerticesDirty();
        number.transform.localScale = Vector3.one * (1f + 0.3f * punch + 0.5f * tierPunch);
        caption.color = new Color(Mathf.Lerp(color.r, 1f, 0.6f), Mathf.Lerp(color.g, 1f, 0.6f), Mathf.Lerp(color.b, 1f, 0.6f), shown > 0 ? 0.95f : 0.6f);
        flash.color = new Color(1f, 1f, 1f, 0.7f * flashLeft * flashLeft);
        shell.anchoredPosition = home + (breakLeft > 0f ? Random.insideUnitCircle * (7f * breakLeft * breakLeft) : Vector2.zero);

        if (shown > 0 && gm.State == GameState.Playing && !gm.Paused)
        {
            owed += Tiers[shownTier].sparks * dt * Random.Range(0.3f, 1.7f);
            while (owed >= 1f)
            {
                owed -= 1f;
                Emit(Random.value < 0.6f ? width + Random.Range(-14f, 4f) : Random.Range(8f, width), color, false);
            }
        }
        Animate(dt);
    }

    // Искра срывается с полосы и всплывает; burst — россыпь во все стороны на взятой вехе.
    void Emit(float x, Color color, bool burst)
    {
        int i = nextSpark;
        nextSpark = (nextSpark + 1) % Sparks;
        sparks[i].sprite = Random.value < 0.6f ? Art.Star : Art.Dot;
        sparks[i].rectTransform.anchoredPosition = new Vector2(x, Random.Range(-0.3f, 0.5f) * Height);
        sparkVel[i] = burst ? new Vector2(Random.Range(-90f, 90f), Random.Range(-30f, 150f)) : new Vector2(Random.Range(-20f, 30f), Random.Range(20f, 70f));
        sparkSize[i] = sparks[i].sprite == Art.Star ? Random.Range(14f, 30f) : Random.Range(6f, 12f);
        sparkLife[i] = Random.Range(0.5f, 1f);
        sparkAge[i] = 0f;
        sparkColor[i] = Color.Lerp(color, Color.white, Random.Range(0.2f, 0.8f));
    }

    void Animate(float dt)
    {
        for (int i = 0; i < Sparks; i++)
        {
            if (sparkLife[i] <= 0f) continue;
            sparkAge[i] += dt;
            float k = sparkAge[i] / sparkLife[i];
            if (k >= 1f)
            {
                sparkLife[i] = 0f;
                sparks[i].color = Color.clear;
                continue;
            }
            var rt = sparks[i].rectTransform;
            sparkVel[i] *= Mathf.Exp(-1.4f * dt);
            rt.anchoredPosition += sparkVel[i] * dt;
            float size = sparkSize[i] * (0.6f + 0.4f * Mathf.Abs(Mathf.Sin(sparkAge[i] * 13f)));
            rt.sizeDelta = new Vector2(size, size);
            Color c = sparkColor[i];
            c.a = Mathf.Min(1f, k * 8f) * (1f - k * k);
            sparks[i].color = c;
        }
    }
}
