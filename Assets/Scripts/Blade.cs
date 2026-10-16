using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Эфирный клинок: читает мышь или палец, рисует след и режет всё, через что прошла траектория взмаха.
// Траектория — это не одна точка на кадр, а все положения указателя между кадрами.
[DefaultExecutionOrder(50)] // после слаймов: к моменту проверки они уже сдвинулись на этот кадр
public class Blade : MonoBehaviour
{
    const float TrailTime = 0.16f;    // сколько секунд живёт хвост следа
    const float MinCutSpeed = 8f;     // ед/с: медленное перетаскивание и удержание на месте не режут
    const float WindFrom = 14f, WindFull = 55f; // ед/с: с какой скорости взмах слышен и когда звучит в полную силу
    const float HitPadding = 0.12f;   // небольшой запас к радиусу, чтобы касания «на грани» засчитывались
    const int Smoothing = 3;          // сколько точек сглаженной кривой приходится на один отрезок следа
    const int MaxSamples = 64;        // больше промежуточных точек за кадр не бывает нужно

    static readonly Color Aether = new Color(0.4f, 0.9f, 1f);
    static readonly Color Golden = new Color(1f, 0.78f, 0.2f);

    struct Point
    {
        public Vector2 pos;
        public float time;
    }

    struct Hit
    {
        public Sliceable target;
        public float along;
        public Vector2 offset;
    }

    public static int Stroke { get; private set; } // номер текущего взмаха: комбо считается внутри одного
    public static bool Cutting { get; private set; }

    static readonly List<Hit> hits = new List<Hit>();
    static readonly List<Vector2> samples = new List<Vector2>(); // экранные положения мыши, пришедшие между кадрами

    readonly List<Point> points = new List<Point>();
    Vector3[] buffer = new Vector3[256];
    LineRenderer core, glow;
    SpriteRenderer tip;
    Vector2 last;
    bool wasDown, golden, mergedBefore;
    float speed, sparkTimer;

    void Awake()
    {
        glow = MakeLine("Glow", Fx.OrderBlade);
        core = MakeLine("Core", Fx.OrderBlade + 1);
        tip = Fx.NewSprite("Tip", Art.Soft, Color.clear, Fx.OrderBlade, transform).GetComponent<SpriteRenderer>();
        golden = true;
        SetGolden(false);
    }

    void OnEnable()
    {
        samples.Clear();
        // По умолчанию система ввода склеивает все движения мыши за кадр в одно событие — ради траектории
        // склейку нужно отключить, иначе промежуточные точки до нас не дойдут.
        mergedBefore = InputSystem.settings.disableRedundantEventsMerging;
        InputSystem.settings.disableRedundantEventsMerging = true;
        InputSystem.onEvent += OnInputEvent;
    }

    void OnDisable()
    {
        InputSystem.onEvent -= OnInputEvent;
        InputSystem.settings.disableRedundantEventsMerging = mergedBefore;
    }

    // Мышь или тачскрин сообщают о себе чаще, чем рисуются кадры. Каждое промежуточное положение запоминается,
    // и взмах проверяется по настоящей ломаной, а не по хорде от кадра до кадра.
    static void OnInputEvent(InputEventPtr e, InputDevice device)
    {
        if (!(device is Mouse) && !(device is Pen) && !(device is Touchscreen)) return;
        if (InputState.currentUpdateType != InputUpdateType.Dynamic || samples.Count >= MaxSamples) return;
        if (!e.IsA<StateEvent>() && !e.IsA<DeltaStateEvent>()) return;
        Vector2 screen;
        if (device is Touchscreen touch)
        {
            if (touch.primaryTouch.position.ReadValueFromEvent(e, out screen)) samples.Add(screen);
        }
        else if (device is Pointer pointer)
        {
            if (pointer.position.ReadValueFromEvent(e, out screen)) samples.Add(screen);
        }
    }

    void Update()
    {
        float u = GameManager.Unit;
        float dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        float now = Time.unscaledTime;
        SetGolden(GameManager.Instance.Empowered);

        Vector2 screen;
        bool byTouch;
        bool down = ReadPointer(out screen, out byTouch);
        // На паузе, в магазине или при нажатии на элементы интерфейса клинок убран: ни следа, ни срезов.
        if (GameManager.Instance.Paused || (Hud.Instance != null && (Hud.Instance.IsPointerOverUI(screen) || Hud.Instance.IsShopOpen))) down = false;
        Vector2 world = GameManager.ScreenToWorld(screen);

        if (down && wasDown)
        {
            // Путь за кадр: все промежуточные точки и текущее положение.
            float dist = 0f;
            Vector2 from = last;
            for (int i = 0; i <= samples.Count; i++)
            {
                Vector2 to = i < samples.Count ? GameManager.ScreenToWorld(samples[i]) : world;
                dist += (to - from).magnitude;
                from = to;
            }
            // Скорость сглажена: один «пустой» кадр посреди взмаха не должен его прерывать.
            // Для тач-устройств порог скорости сбалансирован мягче, а зона попадания шире под палец
            float minSpeed = (byTouch ? 5.5f : 7.2f) * u;
            speed = Mathf.Max(dist / dt, speed * Mathf.Exp(-14f * dt));
            Cutting = speed > minSpeed;
            float hitPadding = (byTouch ? 0.22f : 0.14f) * u;

            float done = 0f;
            from = last;
            for (int i = 0; i <= samples.Count; i++)
            {
                Vector2 to = i < samples.Count ? GameManager.ScreenToWorld(samples[i]) : world;
                Vector2 step = to - from;
                float length = step.magnitude;
                if (length < 0.02f * u && i < samples.Count) continue; // дрожание на месте в путь не идёт
                if (length > 0.02f * u) points.Add(new Point { pos = to, time = now });
                if (Cutting && length > 0.0001f) Cut(from, to, step / length, hitPadding, done / dist, (done + length) / dist);
                done += length;
                from = to;
            }

            if (Cutting)
            {
                sparkTimer -= dist;
                if (sparkTimer <= 0f)
                {
                    sparkTimer = 0.9f * u;
                    string bladeId = (GameManager.Instance != null) ? GameManager.Instance.ActiveBladeId : "azure";
                    Color bladeCol = golden ? Golden : ((GameManager.Instance != null) ? GameManager.Instance.ActiveBladeColor : Aether);
                    Fx.BladeSpark(world, bladeId, bladeCol);
                }
            }
        }
        else if (down)
        {
            Stroke++;
            speed = 0f;
            Cutting = false;
            points.Clear();
            points.Add(new Point { pos = world, time = now });
        }
        else
        {
            if (wasDown) GameManager.Instance.OnStrokeEnd();
            speed = 0f;
            Cutting = false;
        }
        samples.Clear();
        last = world;
        wasDown = down;

        // Свист клинка следует за рукой: чем быстрее взмах, тем он громче и выше; рука остановилась — стих.
        Sfx.Wind(Cutting ? Mathf.InverseLerp(WindFrom * u, WindFull * u, speed) : 0f, dt);

        while (points.Count > 0 && now - points[0].time > TrailTime) points.RemoveAt(0);
        Draw(u, world, down);
    }

    // Попадание — по всей траектории, поэтому быстрый взмах не «перепрыгивает» объект.
    // t0..t1 — доля кадра, за которую клинок прошёл этот отрезок: цель за то же время тоже сдвинулась,
    // и проверка идёт в её системе отсчёта. Задетые объекты режутся в том порядке, в каком клинок до них дошёл.
    static void Cut(Vector2 a, Vector2 b, Vector2 dir, float padding, float t0, float t1)
    {
        hits.Clear();
        foreach (var s in Sliceable.Active)
        {
            if (!s.CanCut) continue;
            Vector2 ra = a - Vector2.Lerp(s.Prev, s.Pos, t0);
            Vector2 rab = b - Vector2.Lerp(s.Prev, s.Pos, t1) - ra;
            float along = -Vector2.Dot(ra, rab) / Mathf.Max(rab.sqrMagnitude, 1e-8f);
            float t = Mathf.Clamp01(along);
            float reach = s.Def.IsBomb ? s.Reach : s.Reach + padding;
            if ((ra + rab * t).sqrMagnitude > reach * reach) continue;
            // Линия разреза — там, где клинок пройдёт мимо центра, продолжи он движение по прямой.
            hits.Add(new Hit { target = s, along = t, offset = ra + rab * along });
        }
        if (hits.Count == 0) return;
        hits.Sort((x, y) => x.along.CompareTo(y.along));
        for (int i = 0; i < hits.Count; i++) hits[i].target.Slice(dir, hits[i].offset);
        hits.Clear();
    }

    void Draw(float u, Vector2 pointer, bool down)
    {
        float heat = Mathf.Clamp01(speed / (40f * u));
        Color c = golden ? Golden : Aether;
        tip.transform.position = pointer;
        tip.transform.localScale = Vector3.one * ((0.5f + 0.5f * heat) * u);
        tip.color = new Color(c.r, c.g, c.b, down ? 0.25f + 0.5f * heat : 0f);

        int n = points.Count;
        if (n < 2)
        {
            core.positionCount = 0;
            glow.positionCount = 0;
            return;
        }

        // Сплайн Катмулла — Рома превращает ломаную в плавный росчерк.
        int total = (n - 1) * Smoothing + 1;
        if (buffer.Length < total) buffer = new Vector3[total * 2];
        int k = 0;
        for (int i = 0; i < n - 1; i++)
        {
            Vector2 p0 = points[Mathf.Max(i - 1, 0)].pos, p1 = points[i].pos;
            Vector2 p2 = points[i + 1].pos, p3 = points[Mathf.Min(i + 2, n - 1)].pos;
            for (int s = 0; s < Smoothing; s++) buffer[k++] = CatmullRom(p0, p1, p2, p3, s / (float)Smoothing);
        }
        buffer[k++] = points[n - 1].pos;

        core.widthMultiplier = (golden ? 0.26f : 0.2f) * u;
        glow.widthMultiplier = (golden ? 0.85f : 0.62f) * u;
        core.positionCount = k;
        core.SetPositions(buffer);
        glow.positionCount = k;
        glow.SetPositions(buffer);
    }

    static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float t2 = t * t, t3 = t2 * t;
        return 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3);
    }

    // В режимах усиления (Frenzy, трансмутация) клинок озаряется золотым пламенем.
    void SetGolden(bool value)
    {
        if (golden == value) return;
        golden = value;
        RefreshBladeColor();
    }

    public void RefreshBladeColor()
    {
        Color baseColor = (GameManager.Instance != null) ? GameManager.Instance.ActiveBladeColor : Aether;
        Paint(glow, golden ? Golden : baseColor, 0.4f);
        Paint(core, golden ? new Color(1f, 0.97f, 0.8f) : Color.white, 1f);
    }

    static void Paint(LineRenderer line, Color color, float alpha)
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(alpha, 0.6f), new GradientAlphaKey(alpha, 1f) });
        line.colorGradient = gradient;
    }

    LineRenderer MakeLine(string name, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var line = go.AddComponent<LineRenderer>();
        line.sharedMaterial = Art.Mat;
        line.useWorldSpace = true;
        line.positionCount = 0;
        line.numCapVertices = 6;
        line.numCornerVertices = 4;
        line.sortingOrder = order;
        line.widthCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        return line;
    }

    // ---------- Ввод ----------

    static bool ReadPointer(out Vector2 screen, out bool byTouch)
    {
        var touch = Touchscreen.current;
        byTouch = touch != null && touch.primaryTouch.press.isPressed;
        if (byTouch)
        {
            screen = touch.primaryTouch.position.ReadValue();
            return true;
        }
        var mouse = Mouse.current;
        if (mouse != null)
        {
            screen = mouse.position.ReadValue();
            return mouse.leftButton.isPressed;
        }
        screen = default(Vector2);
        return false;
    }

    // Где сейчас указатель и нажат ли он: нужно интерфейсу для ползунка громкости.
    public static bool Pointer(out Vector2 screen)
    {
        bool byTouch;
        return ReadPointer(out screen, out byTouch);
    }

    public static bool PressBegan()
    {
        var touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame) return true;
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;
        var keys = Keyboard.current;
        return keys != null && (keys.spaceKey.wasPressedThisFrame || keys.enterKey.wasPressedThisFrame);
    }

    // Правая кнопка мыши ставит игру на паузу и снимает с неё.
    public static bool PausePressed()
    {
        var mouse = Mouse.current;
        return mouse != null && mouse.rightButton.wasPressedThisFrame;
    }

    // Колёсико мыши: шаги вверх положительные.
    public static float Wheel()
    {
        var mouse = Mouse.current;
        return mouse != null ? Mathf.Clamp(mouse.scroll.ReadValue().y, -1f, 1f) : 0f;
    }

    public static bool SpacePressed()
    {
        var keys = Keyboard.current;
        return keys != null && keys.spaceKey.wasPressedThisFrame;
    }

    public static bool EscapePressed()
    {
        var keys = Keyboard.current;
        return keys != null && keys.escapeKey.wasPressedThisFrame;
    }
}
