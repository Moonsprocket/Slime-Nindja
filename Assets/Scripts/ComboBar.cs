using UnityEngine;
using UnityEngine.UI;

// Полоска комбо: неоновая «пилюля» со слаймом-талисманом на конце. Появляется вместе с перегрузкой реторты
// и показывает, сколько её осталось. Чем крупнее комбо, тем выше уровень полоски: меняются цвет, размер,
// мордочка талисмана и частицы вокруг — от пары капель до кристаллов и радужного сияния.
public class ComboBar : MonoBehaviour
{
    const float Width = 440f, Height = 34f;
    const int Particles = 110;

    // Уровень полоски: цвет, имя и то, что летит вокруг.
    struct Tier
    {
        public string name;
        public Color color;
        public int face;            // какая мордочка у талисмана
        public float scale;         // размер всей полоски
        public float drops, stars, shards, embers;  // частиц в секунду
    }

    static readonly Tier[] Tiers =
    {
        new Tier { name = "Кипение", color = new Color(0.3f, 0.75f, 1f), face = 0, scale = 1f, drops = 12f },
        new Tier { name = "Бурление", color = new Color(0.35f, 1f, 0.5f), face = 1, scale = 1.05f, drops = 12f, stars = 9f },
        new Tier { name = "Резонанс", color = new Color(1f, 0.82f, 0.22f), face = 2, scale = 1.1f, drops = 8f, stars = 13f, shards = 9f },
        new Tier { name = "Пламя", color = new Color(1f, 0.45f, 0.15f), face = 3, scale = 1.16f, stars = 13f, shards = 11f, embers = 28f },
        new Tier { name = "Великое делание", color = new Color(0.95f, 0.4f, 1f), face = 4, scale = 1.22f, drops = 8f, stars = 22f, shards = 20f, embers = 20f },
    };

    enum Bit { Drop, Star, Shard, Ember }

    // Ореол нарисован с запасом вокруг «пилюли»: рамка ореола во столько же раз больше ложа полоски.
    static readonly Vector2 GlowSize = new Vector2(Width + 12f + (Height + 12f) * 0.55f, (Height + 12f) * 1.55f);

    // Уровни видны и в меню: там они показаны как цель, к которой стоит стремиться.
    public static int TierCount { get { return Tiers.Length; } }
    public static string TierName(int index) { return Tiers[index].name; }
    public static Color TierColor(int index) { return Tiers[index].color; }
    public static Sprite TierFace(int index) { return Art.Mascot[Tiers[index].face]; }

    // Цвет комбо такого размера: обычное комбо мятное, а начиная с порога перегрузки — цвет своего уровня.
    public static Color ComboColor(int combo)
    {
        int tier = combo - Balance.Current.frenzyCombo;
        return tier < 0 ? new Color(0.62f, 0.98f, 0.88f) : Tiers[Mathf.Min(tier, Tiers.Length - 1)].color;
    }

    // Цвет текущего уровня — им же окрашивается кайма экрана.
    public Color Heat { get; private set; }

    CanvasGroup group;
    RectTransform bar, fill;
    Image haze, glow, level, ring, head, mascot, flash;
    Text counter, title;
    TextGradient counterPaint;
    readonly Image[] bits = new Image[Particles];
    readonly Vector2[] bitVel = new Vector2[Particles];
    readonly float[] bitAge = new float[Particles], bitLife = new float[Particles], bitSize = new float[Particles], bitSpin = new float[Particles];
    readonly Bit[] bitKind = new Bit[Particles];
    readonly Color[] bitColor = new Color[Particles];
    readonly float[] owed = new float[4];
    int nextBit, shownTier, shownCombo;
    float punch, counterPunch, flashLeft;
    bool wasOn;

    public static ComboBar Create(RectTransform root, Font font, Font display)
    {
        var go = new GameObject("ComboBar", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(root, false);
        Hud.Stretch(rt);
        var combo = go.AddComponent<ComboBar>();
        combo.Build(rt, font, display);
        return combo;
    }

    void Build(RectTransform root, Font font, Font display)
    {
        group = gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        group.alpha = 0f;
        Heat = Tiers[0].color;

        Vector2 top = new Vector2(0.5f, 1f), center = new Vector2(0.5f, 0.5f), left = new Vector2(0f, 0.5f);
        var barGo = new GameObject("Bar", typeof(RectTransform));
        bar = (RectTransform)barGo.transform;
        bar.SetParent(root, false);
        bar.anchorMin = bar.anchorMax = top;
        bar.anchoredPosition = new Vector2(24f, -94f);
        bar.sizeDelta = new Vector2(Width, Height);

        // Слои снизу вверх: ореол, частицы, тёмное ложе, уровень с бликом, неоновый контур, искра на конце, талисман.
        // Дымка — широкое мягкое свечение за полоской; ореол — узкая неоновая кайма вплотную к ней.
        haze = Hud.Picture(bar, Art.Soft, Color.clear, center, center, Vector2.zero, new Vector2(Width * 1.5f, Height * 5f));
        haze.preserveAspect = false;
        glow = Hud.Picture(bar, Art.PillGlow, Color.white, center, center, Vector2.zero, GlowSize);
        glow.type = Image.Type.Sliced;
        for (int i = 0; i < Particles; i++)
        {
            bits[i] = Hud.Picture(bar, Art.Dot, Color.clear, left, center, Vector2.zero, Vector2.one);
            bits[i].preserveAspect = false;
        }
        var bed = Hud.Picture(bar, Art.Pill, new Color(0.05f, 0.04f, 0.1f, 0.88f), center, center, Vector2.zero, new Vector2(Width + 12f, Height + 12f));
        bed.type = Image.Type.Sliced;
        level = Hud.Picture(bar, Art.Pill, Color.white, left, left, Vector2.zero, new Vector2(Width, Height));
        level.type = Image.Type.Sliced;
        fill = level.rectTransform;
        var gloss = Hud.Picture(fill, Art.Pill, new Color(1f, 1f, 1f, 0.38f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        gloss.type = Image.Type.Sliced;
        gloss.rectTransform.anchorMin = new Vector2(0.03f, 0.56f);
        gloss.rectTransform.anchorMax = new Vector2(0.97f, 0.86f);
        ring = Hud.Picture(bar, Art.PillRing, Color.white, center, center, Vector2.zero, new Vector2(Width + 12f, Height + 12f));
        ring.type = Image.Type.Sliced;
        head = Hud.Picture(bar, Art.Soft, Color.white, left, center, Vector2.zero, new Vector2(Height * 2.6f, Height * 2.6f));
        mascot = Hud.Picture(bar, Art.Mascot[0], Color.white, left, center, new Vector2(-14f, 2f), new Vector2(84f, 84f));
        flash = Hud.Picture(bar, Art.Soft, Color.clear, center, center, Vector2.zero, new Vector2(Width * 1.5f, Height * 6f));
        flash.preserveAspect = false;

        counter = Hud.Label(bar, display, "", 44, Color.white, TextAnchor.MiddleLeft, left, new Vector2(36f, 38f));
        counterPaint = counter.GetComponent<TextGradient>();
        title = Hud.Label(bar, font, "", 21, Color.white, TextAnchor.MiddleCenter, center, new Vector2(0f, -38f));
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        float time = Time.unscaledTime;
        var gm = GameManager.Instance;
        bool on = gm.FrenzyLeft > 0f && gm.State == GameState.Playing;
        group.alpha = Mathf.MoveTowards(group.alpha, on ? 1f : 0f, dt * 6f);
        if (on && !wasOn)
        {
            punch = 1f;
            shownTier = 0;
            shownCombo = 0;
        }
        wasOn = on;
        punch = Mathf.MoveTowards(punch, 0f, dt * 3.5f);
        counterPunch = Mathf.MoveTowards(counterPunch, 0f, dt * 4f);
        flashLeft = Mathf.MoveTowards(flashLeft, 0f, dt * 2.6f);

        if (on)
        {
            int index = Mathf.Clamp(gm.FrenzyTier, 1, Tiers.Length) - 1;
            Tier tier = Tiers[index];
            // Новый уровень — вспышка и подпрыгнувшая полоска; новое число комбо — подпрыгнувший счётчик.
            if (gm.FrenzyTier != shownTier)
            {
                if (shownTier != 0) flashLeft = punch = 1f;
                shownTier = gm.FrenzyTier;
                mascot.sprite = Art.Mascot[tier.face];
                title.text = tier.name.ToUpper() + "  ×" + gm.Mult;
            }
            if (gm.FrenzyCombo != shownCombo)
            {
                shownCombo = gm.FrenzyCombo;
                counter.text = "×" + shownCombo;
                counterPunch = 1f;
            }

            // На высшем уровне цвет переливается от бирюзового через синий и фиолетовый к розовому.
            Color color = tier.color;
            if (index == Tiers.Length - 1) color = Color.HSVToRGB(0.68f + 0.22f * Mathf.Sin(time * 1.8f), 0.65f, 1f);
            float amount = Mathf.Clamp01(gm.FrenzyLeft / Mathf.Max(0.01f, gm.FrenzySpan));
            float alarm = amount < 0.25f ? 0.5f + 0.5f * Mathf.Sin(time * 22f) : 0f;   // на последних секундах полоска мигает
            float breath = 0.5f + 0.5f * Mathf.Sin(time * (3f + index));
            Heat = color;

            float width = Mathf.Max(Height, Width * amount);
            fill.sizeDelta = new Vector2(width, Height);
            level.color = Color.Lerp(Color.Lerp(color, Color.white, 0.15f), Color.white, 0.5f * alarm);
            ring.color = Color.Lerp(color, Color.white, 0.6f);
            glow.color = new Color(color.r, color.g, color.b, 0.7f + 0.3f * breath);
            glow.rectTransform.sizeDelta = GlowSize + Vector2.one * (3f * index * breath);
            haze.color = new Color(color.r, color.g, color.b, 0.14f + 0.05f * index + 0.06f * breath);
            haze.rectTransform.sizeDelta = new Vector2(Width * (1.4f + 0.08f * index), Height * (4.5f + 0.6f * index));
            head.color = new Color(1f, 1f, 1f, 0.5f + 0.4f * breath);
            head.rectTransform.anchoredPosition = new Vector2(width - Height * 0.3f, 0f);
            bar.localScale = Vector3.one * (tier.scale * (1f + 0.18f * punch + 0.03f * alarm));

            // Талисман дышит и подпрыгивает; чем выше уровень, тем он беспокойнее.
            float hop = Mathf.Abs(Mathf.Sin(time * (4f + 1.5f * index)));
            mascot.color = Color.Lerp(color, Color.white, 0.12f);
            mascot.rectTransform.anchoredPosition = new Vector2(-14f, 2f + (3f + 2f * index) * hop);
            mascot.rectTransform.localScale = new Vector3(1f + 0.06f * (1f - hop), 1f - 0.06f * (1f - hop), 1f) * (1f + 0.25f * counterPunch);
            mascot.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 6f * Mathf.Sin(time * (3f + index)));

            counterPaint.top = Color.white;
            counterPaint.bottom = Color.Lerp(color, Color.white, 0.6f);
            counter.SetVerticesDirty();
            counter.transform.localScale = Vector3.one * (1f + 0.35f * counterPunch);
            counter.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
            title.color = Color.Lerp(color, Color.white, 0.75f);
            flash.color = new Color(1f, 1f, 1f, 0.8f * flashLeft * flashLeft);

            Emit(Bit.Drop, tier.drops, dt, width, color);
            Emit(Bit.Star, tier.stars, dt, width, color);
            Emit(Bit.Shard, tier.shards, dt, width, color);
            Emit(Bit.Ember, tier.embers, dt, width, color);
        }

        if (group.alpha > 0f) Animate(dt);
    }

    // Частицы вылетают из уровня полоски в случайные моменты и в случайных местах: одинаковых рядов не получается.
    void Emit(Bit kind, float perSecond, float dt, float width, Color color)
    {
        owed[(int)kind] += perSecond * dt * Random.Range(0.3f, 1.7f);
        while (owed[(int)kind] >= 1f)
        {
            owed[(int)kind] -= 1f;
            int i = nextBit;
            nextBit = (nextBit + 1) % Particles;
            // Половина частиц срывается с «головы» уровня, остальные — со всей его длины.
            float x = Random.value < 0.5f ? width + Random.Range(-18f, 6f) : Random.Range(10f, width);
            bits[i].rectTransform.anchoredPosition = new Vector2(x, Random.Range(-0.3f, 0.5f) * Height);
            bitKind[i] = kind;
            bitAge[i] = 0f;
            bitColor[i] = Color.Lerp(color, Color.white, Random.Range(0.1f, 0.7f));
            switch (kind)
            {
                case Bit.Drop:     // капли взлетают дугой и падают
                    bits[i].sprite = Art.Dot;
                    bitVel[i] = new Vector2(Random.Range(-40f, 70f), Random.Range(90f, 190f));
                    bitSize[i] = Random.Range(9f, 19f);
                    bitLife[i] = Random.Range(0.5f, 0.9f);
                    bitSpin[i] = 0f;
                    break;
                case Bit.Star:     // искры медленно всплывают и мерцают
                    bits[i].sprite = Art.Star;
                    bitVel[i] = new Vector2(Random.Range(-25f, 45f), Random.Range(20f, 70f));
                    bitSize[i] = Random.Range(18f, 40f);
                    bitLife[i] = Random.Range(0.6f, 1.2f);
                    bitSpin[i] = Random.Range(-120f, 120f);
                    break;
                case Bit.Shard:    // кристаллы разлетаются веером, вращаясь
                    bits[i].sprite = Art.Shard;
                    bitVel[i] = new Vector2(Random.Range(-60f, 150f), Random.Range(60f, 170f));
                    bitSize[i] = Random.Range(22f, 46f);
                    bitLife[i] = Random.Range(0.7f, 1.2f);
                    bitSpin[i] = Random.Range(-360f, 360f);
                    bitColor[i] = Color.HSVToRGB(Random.value, 0.45f, 1f) * 0.5f + bitColor[i] * 0.5f;
                    break;
                default:            // угольки уносит вверх и назад, как от костра на ветру
                    bits[i].sprite = Art.Soft;
                    bitVel[i] = new Vector2(Random.Range(-90f, 30f), Random.Range(50f, 150f));
                    bitSize[i] = Random.Range(12f, 30f);
                    bitLife[i] = Random.Range(0.35f, 0.8f);
                    bitSpin[i] = 0f;
                    bitColor[i] = Color.Lerp(color, new Color(1f, 0.95f, 0.6f), Random.value);
                    break;
            }
        }
    }

    void Animate(float dt)
    {
        for (int i = 0; i < Particles; i++)
        {
            if (bitLife[i] <= 0f) continue;
            bitAge[i] += dt;
            float k = bitAge[i] / bitLife[i];
            if (k >= 1f)
            {
                bitLife[i] = 0f;
                bits[i].color = Color.clear;
                continue;
            }
            var rt = bits[i].rectTransform;
            if (bitKind[i] == Bit.Drop || bitKind[i] == Bit.Shard) bitVel[i].y -= 420f * dt;   // тяжёлые частицы падают
            else bitVel[i] *= Mathf.Exp(-1.2f * dt);
            rt.anchoredPosition += bitVel[i] * dt;
            rt.localRotation = Quaternion.Euler(0f, 0f, bitSpin[i] * bitAge[i]);
            float size = bitSize[i] * (bitKind[i] == Bit.Star ? 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(bitAge[i] * 14f)) : 1f - 0.5f * k);
            rt.sizeDelta = new Vector2(size, size);
            Color c = bitColor[i];
            c.a = Mathf.Min(1f, k * 8f) * (1f - k * k);
            bits[i].color = c;
        }
    }
}

// Вертикальный градиент на тексте: верх букв одного цвета, низ — другого. Умножается на цвет самого текста,
// поэтому прозрачность и вспышки работают как обычно.
public class TextGradient : BaseMeshEffect
{
    public Color top = Color.white, bottom = Color.white;

    public override void ModifyMesh(VertexHelper helper)
    {
        if (!IsActive() || helper.currentVertCount == 0 || top == bottom) return;
        var vertex = new UIVertex();
        float min = float.MaxValue, max = float.MinValue;
        for (int i = 0; i < helper.currentVertCount; i++)
        {
            helper.PopulateUIVertex(ref vertex, i);
            min = Mathf.Min(min, vertex.position.y);
            max = Mathf.Max(max, vertex.position.y);
        }
        for (int i = 0; i < helper.currentVertCount; i++)
        {
            helper.PopulateUIVertex(ref vertex, i);
            Color tint = Color.Lerp(bottom, top, Mathf.InverseLerp(min, max, vertex.position.y));
            vertex.color = (Color)vertex.color * tint;
            helper.SetUIVertex(vertex, i);
        }
    }
}
