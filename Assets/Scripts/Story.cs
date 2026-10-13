using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Комикс-предыстория: четыре кадра, которые за десять секунд объясняют, кто мы и зачем рубим слаймов.
// Показывается при первом запуске; из меню открывается пробелом. Любой кадр пролистывается нажатием.
//
// Кадр устроен как страница комикса: сверху картинка в рамке (сцена на полу лаборатории, всё лишнее обрезано
// по краю), под ней отдельная полоса с подписью, в углу номер кадра.
public class Story : MonoBehaviour
{
    const float PanelWidth = 574f, PanelHeight = 266f, CaptionHeight = 62f, Gap = 18f;
    const float Floor = -64f;       // уровень пола в координатах сцены: на нём стоят герои
    const float Beat = 2.8f;        // через столько секунд сам открывается следующий кадр
    const float InputLock = 0.25f;  // нажатие, которым комикс открыли, не должно сразу его листать

    static readonly Color Patina = new Color(0.36f, 0.8f, 0.72f);   // рамки кадров: позеленевшая медь
    static readonly Color Cream = new Color(0.96f, 0.95f, 0.9f);
    static readonly Color Mint = new Color(0.62f, 0.98f, 0.88f);
    static readonly Color Ink = new Color(0.07f, 0.05f, 0.12f, 1f);

    static readonly string[] Captions =
    {
        "Королевская лаборатория. Верховный алхимик варит эликсир века…",
        "…но автоклав не выдержал! Зелье ожило и рвануло наружу.",
        "Каждый слайм, упавший обратно в котёл, раскаляет его. Три таких — и замок взлетит на воздух!",
        "Спасение одно — Эфирный серп. Руби слаймов на лету, но не задень чумные споры!",
    };

    // Оттенок стены в каждом кадре задаёт настроение: покой, вспышка, тревога, надежда.
    static readonly Color[] Moods =
    {
        new Color(0.82f, 0.88f, 1f), new Color(1f, 0.9f, 0.76f), new Color(1f, 0.66f, 0.66f), new Color(0.78f, 0.95f, 1f)
    };

    // Участник кадра: покачивается, вращается или пульсирует на своём месте.
    struct Actor
    {
        public RectTransform rt;
        public Vector2 home;
        public float bob, spin, pulse, speed, phase, tilt;
    }

    public bool Active { get; private set; }

    readonly List<Actor> actors = new List<Actor>();
    Font font, titleFont;
    CanvasGroup group;
    CanvasGroup[] panels;
    float[] shownAt;
    Text prompt;
    int shown;
    float openedAt, nextAt;

    public static Story Create(RectTransform root, Font font, Font titleFont)
    {
        var go = new GameObject("Story", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(root, false);
        Hud.Stretch(rt);
        var story = go.AddComponent<Story>();
        story.font = font;
        story.titleFont = titleFont;
        story.Build(rt);
        return story;
    }

    void Build(RectTransform root)
    {
        group = gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        group.alpha = 0f;

        Vector2 center = new Vector2(0.5f, 0.5f);
        var dim = Hud.Picture(root, Art.White, new Color(0.03f, 0.02f, 0.07f, 0.93f), Vector2.zero, center, Vector2.zero, Vector2.zero);
        Hud.Stretch(dim.rectTransform);
        dim.preserveAspect = false;

        Hud.Paint(Hud.Label(root, titleFont, "КАК ВСЁ НАЧАЛОСЬ", 40, Color.white, TextAnchor.MiddleCenter, center, new Vector2(0f, 320f)), Color.white, Mint);

        panels = new CanvasGroup[Captions.Length];
        shownAt = new float[Captions.Length];
        for (int i = 0; i < panels.Length; i++)
        {
            float x = (i % 2 == 0 ? -1f : 1f) * (PanelWidth + Gap) * 0.5f;
            float y = 144f - (i / 2) * (PanelHeight + Gap);
            RectTransform stage;
            panels[i] = Panel(root, i, new Vector2(x, y), out stage);
            switch (i)
            {
                case 0: Brewing(stage); break;
                case 1: Explosion(stage); break;
                case 2: Danger(stage); break;
                default: Hero(stage); break;
            }
        }

        prompt = Hud.Label(root, titleFont, "", 28, Mint, TextAnchor.MiddleCenter, center, new Vector2(0f, -316f));
        Hud.Label(root, font, "Esc — пропустить", 17, new Color(1f, 1f, 1f, 0.45f), TextAnchor.MiddleRight, center, new Vector2(590f, -316f));
    }

    public void Show()
    {
        Active = true;
        shown = 0;
        openedAt = Time.unscaledTime;
        nextAt = openedAt + 0.35f;
        foreach (var panel in panels) panel.alpha = 0f;
    }

    public void Hide()
    {
        Active = false;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        float time = Time.unscaledTime;
        group.alpha = Mathf.MoveTowards(group.alpha, Active ? 1f : 0f, dt * (Active ? 4f : 7f));
        if (group.alpha <= 0f) return;

        if (Active)
        {
            bool all = shown >= panels.Length;
            if (Blade.EscapePressed())
            {
                GameManager.Instance.EndStory();
                return;
            }
            if (time - openedAt > InputLock && Blade.PressBegan())
            {
                if (all)
                {
                    GameManager.Instance.EndStory();
                    return;
                }
                nextAt = time;
            }
            if (!all && time >= nextAt)
            {
                shownAt[shown] = time;
                shown++;
                nextAt = time + Beat;
                // Второй кадр — взрыв, он и звучит хлопком.
                if (shown == 2) Sfx.Split();
                else Sfx.Toss();
            }

            float pulse = 0.5f + 0.5f * Mathf.Sin(time * 5f);
            prompt.text = shown >= panels.Length ? "Нажми — и за дело!" : "Нажми — дальше";
            prompt.color = new Color(Mint.r, Mint.g, Mint.b, 0.7f + 0.3f * pulse);
            prompt.transform.localScale = Vector3.one * (1f + 0.04f * pulse);
        }

        // Кадр выскакивает с лёгким перелётом, как наклейка.
        for (int i = 0; i < panels.Length; i++)
        {
            bool on = i < shown;
            float k = on ? Mathf.Clamp01((time - shownAt[i]) / 0.28f) : 0f;
            panels[i].alpha = k;
            float back = k - 1f;
            panels[i].transform.localScale = Vector3.one * (0.82f + 0.18f * (1f + 2.6f * back * back * back + 1.6f * back * back));
        }

        foreach (var a in actors)
        {
            float wave = Mathf.Sin(time * a.speed + a.phase);
            a.rt.anchoredPosition = a.home + Vector2.up * (a.bob * wave);
            a.rt.localRotation = Quaternion.Euler(0f, 0f, a.tilt + (a.spin != 0f ? a.spin * time : 0f) + a.bob * 0.25f * Mathf.Cos(time * a.speed + a.phase));
            if (a.pulse != 0f) a.rt.localScale = Vector3.one * (1f + a.pulse * wave);
        }
    }

    // ---------- Кадры ----------
    // Координаты — от центра сцены. Сцена 560x190: по вертикали от -95 до 95, пол на высоте Floor.

    // 1. Мирная лаборатория: алхимик у булькающего автоклава.
    void Brewing(RectTransform stage)
    {
        Put(stage, Art.Soft, 120f, 30f, 260f, new Color(0.35f, 1f, 0.4f, 0.4f), pulse: 0.08f, speed: 2.2f);
        Standing(stage, Art.Cauldron, 120f, 126f);
        Standing(stage, Art.Wizard, -140f, 150f, bob: 2f, speed: 2f);
        Color brew = new Color(0.6f, 1f, 0.45f, 0.9f);
        Put(stage, Art.Dot, 88f, 62f, 14f, brew, bob: 6f, speed: 3.1f);
        Put(stage, Art.Dot, 124f, 78f, 20f, brew, bob: 6f, speed: 2.3f, phase: 1.7f);
        Put(stage, Art.Dot, 156f, 60f, 11f, brew, bob: 5f, speed: 3.8f, phase: 3.1f);
        Put(stage, Art.Star, -62f, 34f, 24f, new Color(1f, 0.95f, 0.6f, 0.9f), pulse: 0.3f, speed: 4f);
        Put(stage, Art.Star, 26f, 66f, 16f, new Color(1f, 0.95f, 0.6f, 0.7f), pulse: 0.3f, speed: 5f, phase: 2f);
    }

    // 2. Взрыв: слаймы разлетаются из лопнувшего котла.
    void Explosion(RectTransform stage)
    {
        Put(stage, Art.Burst, 96f, 2f, 250f, Color.white, pulse: 0.05f, speed: 9f, spin: 12f);
        Put(stage, Art.Cauldron, 96f, -16f, 132f, Color.white, tilt: -14f, bob: 1.5f, speed: 22f);
        Put(stage, Art.Acid.whole, -8f, 4f, 52f, Color.white, tilt: 18f, bob: 4f, speed: 4f);
        Put(stage, Art.Mini.whole, 44f, 66f, 34f, Color.white, tilt: -22f, bob: 4f, speed: 5f, phase: 3f);
        Put(stage, Art.Geode.whole, 134f, 68f, 44f, Color.white, tilt: 10f, bob: 4f, speed: 3.8f, phase: 4f);
        Put(stage, Art.Blue.whole, 214f, 36f, 62f, Color.white, tilt: -16f, bob: 4f, speed: 3.4f, phase: 1f);
        Put(stage, Art.Magma.whole, 226f, -34f, 46f, Color.white, tilt: 24f, bob: 4f, speed: 4.4f, phase: 2f);
        Standing(stage, Art.WizardShock, -196f, 146f, bob: 2f, speed: 18f);
        var boom = Hud.Label(stage, titleFont, "БА-БАХ!", 34, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(-84f, 64f));
        Hud.Paint(boom, Color.white, new Color(1f, 0.45f, 0.3f));
        Track(boom.rectTransform, tilt: -8f, pulse: 0.06f, speed: 10f);
    }

    // 3. Угроза: слаймы падают обратно, котёл раскаляется, запас прочности — три сердца.
    void Danger(RectTransform stage)
    {
        Put(stage, Art.Soft, -20f, -20f, 330f, new Color(1f, 0.22f, 0.08f, 0.55f), pulse: 0.1f, speed: 6f);
        for (int i = 0; i < 5; i++)
        {
            float x = -20f + (i - 2) * 32f;
            Put(stage, Art.Flame, x, Floor + 22f - Mathf.Abs(i - 2) * 4f, 50f, new Color(1f, 0.55f + 0.08f * (i % 2), 0.12f, 0.9f), pulse: 0.16f, speed: 11f + i, phase: i * 1.3f);
        }
        Standing(stage, Art.Cauldron, -20f, 126f, new Color(1f, 0.74f, 0.68f));
        Streak(stage, -30f, 86f);
        Put(stage, Art.Acid.whole, -30f, 60f, 40f, Color.white, bob: 3f, speed: 7f);
        Streak(stage, 26f, 88f);
        Put(stage, Art.Mini.whole, 26f, 66f, 28f, Color.white, bob: 3f, speed: 7f, phase: 1.5f);
        Standing(stage, Art.WizardShock, -206f, 142f, bob: 2f, speed: 14f);

        // Справа — табличка «прочность котла»: два сердца целы, третье погасло.
        Vector2 center = new Vector2(0.5f, 0.5f);
        var plate = Hud.Picture(stage, Art.Panel, new Color(0.05f, 0.04f, 0.1f, 0.8f), center, center, new Vector2(190f, 14f), new Vector2(172f, 94f));
        plate.type = Image.Type.Sliced;
        for (int i = 0; i < 3; i++)
            Put(stage, Art.Heart, 142f + i * 48f, 28f, 44f, i < 2 ? Color.white : new Color(0.3f, 0.28f, 0.4f, 0.6f), pulse: i == 1 ? 0.1f : 0f, speed: 9f);
        Hud.Label(stage, font, "прочность котла", 15, Cream, TextAnchor.MiddleCenter, center, new Vector2(190f, -14f));
    }

    // 4. Герой: росчерк серпа, разрубленный слайм и спора, которую трогать нельзя.
    void Hero(RectTransform stage)
    {
        Color aether = new Color(0.4f, 0.9f, 1f);
        Standing(stage, Art.Wizard, -200f, 150f, bob: 2f, speed: 2.4f);
        Put(stage, Art.Soft, -116f, 36f, 130f, new Color(aether.r, aether.g, aether.b, 0.5f), pulse: 0.1f, speed: 5f);
        Put(stage, Art.Sickle, -116f, 36f, 78f, Color.white, tilt: -20f, bob: 2f, speed: 2.4f);

        var glow = Put(stage, Art.Soft, 30f, 26f, 1f, new Color(aether.r, aether.g, aether.b, 0.6f), tilt: 14f);
        glow.preserveAspect = false;
        glow.rectTransform.sizeDelta = new Vector2(290f, 28f);
        var slash = Put(stage, Art.White, 30f, 26f, 1f, Color.white, tilt: 14f);
        slash.preserveAspect = false;
        slash.rectTransform.sizeDelta = new Vector2(220f, 4f);

        Put(stage, Art.Acid.half, 20f, 46f, 58f, Color.white, tilt: 22f, bob: 3f, speed: 4f);
        Put(stage, Art.Acid.half, 44f, 2f, 58f, Color.white, tilt: 196f, bob: 3f, speed: 4f, phase: 3.1f);
        Color juice = new Color(0.55f, 1f, 0.3f, 0.95f);
        Put(stage, Art.Dot, -14f, 12f, 10f, juice, bob: 3f, speed: 6f);
        Put(stage, Art.Dot, 78f, 58f, 13f, juice, bob: 3f, speed: 5f, phase: 2f);
        Put(stage, Art.Dot, 88f, 12f, 8f, juice, bob: 3f, speed: 7f, phase: 4f);
        Hud.Label(stage, titleFont, "+10", 26, Mint, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(104f, 66f));

        Put(stage, Art.Bomb.whole, 208f, 6f, 92f, Color.white, bob: 3f, speed: 3f, tilt: 8f);
        Put(stage, Art.Ring, 208f, 6f, 112f, new Color(1f, 0.2f, 0.25f, 0.95f), pulse: 0.04f, speed: 7f);
        var bar = Put(stage, Art.White, 208f, 6f, 1f, new Color(1f, 0.2f, 0.25f, 0.95f), tilt: 45f);
        bar.preserveAspect = false;
        bar.rectTransform.sizeDelta = new Vector2(96f, 7f);
    }

    // ---------- Кирпичики ----------

    // Рамка кадра: картинка на фоне стены с полом, под ней полоса подписи, кант и номер в углу.
    CanvasGroup Panel(RectTransform root, int index, Vector2 pos, out RectTransform stage)
    {
        Vector2 center = new Vector2(0.5f, 0.5f), bottom = new Vector2(0.5f, 0f), top = new Vector2(0.5f, 1f);
        var go = new GameObject("Panel", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(root, false);
        rt.anchorMin = rt.anchorMax = center;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        var panel = go.AddComponent<CanvasGroup>();

        var bed = Hud.Picture(rt, Art.Panel, Ink, center, center, Vector2.zero, rt.sizeDelta);
        bed.type = Image.Type.Sliced;

        // Сцена обрезается по рамке: то, что вылезло за край картинки, не видно.
        float stageHeight = PanelHeight - CaptionHeight - 14f;
        var stageGo = new GameObject("Stage", typeof(RectTransform), typeof(RectMask2D));
        stage = (RectTransform)stageGo.transform;
        stage.SetParent(rt, false);
        stage.anchorMin = stage.anchorMax = top;
        stage.pivot = center;
        stage.anchoredPosition = new Vector2(0f, -7f - stageHeight * 0.5f);
        stage.sizeDelta = new Vector2(PanelWidth - 14f, stageHeight);
        var wall = Hud.Picture(stage, Art.Wall, Moods[index], center, center, new Vector2(0f, 10f), new Vector2(PanelWidth * 1.4f, PanelWidth * 1.4f * 9f / 16f));
        wall.preserveAspect = false;
        // Пол: тёмная плита со светлой кромкой — героям есть на чём стоять.
        float floorTop = stageHeight * 0.5f + Floor;
        var floor = Hud.Picture(stage, Art.White, new Color(0.09f, 0.07f, 0.14f, 0.92f), bottom, bottom, Vector2.zero, new Vector2(PanelWidth, floorTop));
        floor.preserveAspect = false;
        var edge = Hud.Picture(stage, Art.White, new Color(1f, 1f, 1f, 0.14f), bottom, bottom, new Vector2(0f, floorTop - 2f), new Vector2(PanelWidth, 2f));
        edge.preserveAspect = false;

        // Подпись на своей полосе под картинкой.
        var line = Hud.Picture(rt, Art.White, new Color(Patina.r, Patina.g, Patina.b, 0.5f), bottom, center, new Vector2(0f, CaptionHeight + 6f), new Vector2(PanelWidth - 14f, 2f));
        line.preserveAspect = false;
        var text = Hud.Label(rt, font, Captions[index], 19, Cream, TextAnchor.MiddleCenter, bottom, new Vector2(0f, CaptionHeight * 0.5f + 5f));
        text.rectTransform.sizeDelta = new Vector2(PanelWidth - 56f, CaptionHeight - 10f);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 12;
        text.resizeTextMaxSize = 19;

        var frame = Hud.Picture(rt, Art.Frame, Patina, center, center, Vector2.zero, rt.sizeDelta);
        frame.type = Image.Type.Sliced;

        // Номер кадра на мятном кружке.
        Vector2 corner = new Vector2(0f, 1f);
        Hud.Picture(rt, Art.Pill, Mint, corner, center, new Vector2(8f, -8f), new Vector2(36f, 36f));
        var number = Hud.Label(rt, titleFont, (index + 1).ToString(), 22, Ink, TextAnchor.MiddleCenter, corner, new Vector2(8f, -8f));
        number.GetComponent<Outline>().enabled = false;
        return panel;
    }

    // Ставит картинку на сцену кадра и, если заданы движения, оживляет её.
    Image Put(RectTransform stage, Sprite sprite, float x, float y, float size, Color color,
        float tilt = 0f, float bob = 0f, float pulse = 0f, float speed = 0f, float spin = 0f, float phase = 0f)
    {
        Vector2 center = new Vector2(0.5f, 0.5f);
        var image = Hud.Picture(stage, sprite, color, center, center, new Vector2(x, y), new Vector2(size, size));
        Track(image.rectTransform, tilt, bob, pulse, speed, spin, phase);
        return image;
    }

    // Ставит героя ногами на пол: height — его рост на экране.
    Image Standing(RectTransform stage, Sprite sprite, float x, float height, Color? tint = null, float bob = 0f, float speed = 0f)
    {
        var image = Put(stage, sprite, x, Floor + height * 0.5f - 3f, 1f, tint ?? Color.white, bob: bob, speed: speed);
        image.rectTransform.sizeDelta = new Vector2(height * sprite.rect.width / sprite.rect.height, height);
        return image;
    }

    void Track(RectTransform rt, float tilt = 0f, float bob = 0f, float pulse = 0f, float speed = 0f, float spin = 0f, float phase = 0f)
    {
        rt.localRotation = Quaternion.Euler(0f, 0f, tilt);
        if (bob == 0f && pulse == 0f && spin == 0f) return;
        actors.Add(new Actor { rt = rt, home = rt.anchoredPosition, tilt = tilt, bob = bob, pulse = pulse, speed = speed, spin = spin, phase = phase });
    }

    // Штрихи скорости над падающим слаймом.
    void Streak(RectTransform stage, float x, float y)
    {
        for (int i = -1; i <= 1; i++)
        {
            var line = Put(stage, Art.White, x + i * 12f, y + (i == 0 ? 6f : 0f), 1f, new Color(1f, 1f, 1f, 0.5f));
            line.preserveAspect = false;
            line.rectTransform.sizeDelta = new Vector2(3f, i == 0 ? 20f : 14f);
        }
    }
}
