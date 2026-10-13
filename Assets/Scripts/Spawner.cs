using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Выбрасывает волны слаймов и бомб. Сам спавнер знает только, КАК лететь каждому рисунку;
// все числа — когда, сколько и как часто — лежат в ассете Balance и правятся в инспекторе.
//
// Попытка строится так: несколько секунд разминки одиночками, затем каждые несколько секунд появляется
// что-то новое (очередь, пара навстречу, бомба, строй для первого комбо, новый вид слайма), а размер волн
// и темп плавно растут по кривым до предела рефлексов.
public class Spawner : MonoBehaviour
{
    const string HintKey = "alchemia.tip.";
    const int HintShows = 3;            // каждая подсказка показывается только в первых нескольких попытках
    const float Spacing = 2.75f;        // шаг между слаймами, летящими строем
    const float BombRoom = 0.9f;        // на столько шире шаг по обе стороны от бомбы, летящей в строю
    const float EscortGap = 0.8f;       // просвет между королём и бомбой эскорта
    const float NearBomb = 1.1f;        // просвет, при котором слайм считается прижатым к бомбе
    const int BombTries = 10, FlightSamples = 16, MaxWave = 7;

    // Бросок, который уже рассчитан, но ещё не выпущен.
    struct Shot
    {
        public SlimeDef def;
        public Vector2 pos, vel;
        public float gravity, delay;

        public Vector2 At(float time)
        {
            float t = time - delay;
            return new Vector2(pos.x + vel.x * t, pos.y + vel.y * t - 0.5f * gravity * t * t);
        }
    }

    public float Elapsed { get; private set; }

    Balance balance;
    float timer, kingCooldown;
    bool running;
    int nextIntro, phraseLeft;
    bool magma, blue, geode, mercury, bombs, kingMet, escortMet;
    bool kingComing;    // король уже вызван и вот-вот вылетит
    Pattern lastPattern;

    public void Begin()
    {
        StopAllCoroutines();
        balance = Balance.Current;
        Elapsed = 0f;
        timer = 0.7f;
        kingCooldown = 0f;
        nextIntro = phraseLeft = 0;
        magma = blue = geode = mercury = bombs = kingMet = escortMet = kingComing = false;
        lastPattern = Pattern.Scatter;
        running = true;
    }

    public void Stop()
    {
        running = false;
        StopAllCoroutines();
    }

    // Пауза после взрыва: обрываем текущую волну и даём игроку передышку.
    public void Hold(float seconds)
    {
        StopAllCoroutines();
        phraseLeft = 0;
        kingComing = false;
        timer = Mathf.Max(timer, seconds);
    }

    void Update()
    {
        if (!running) return;
        float dt = Time.deltaTime;
        Elapsed += dt;
        timer -= dt;
        kingCooldown -= dt;
        if (timer > 0f) return;

        if (nextIntro < balance.intros.Length && Elapsed >= balance.intros[nextIntro].at)
        {
            Introduce(balance.intros[nextIntro], nextIntro);
            nextIntro++;
            return;
        }

        bool frenzy = GameManager.Instance.FrenzyLeft > 0f;
        float over = Mathf.Max(0f, (Elapsed - balance.peak) / 60f);
        float airTime = Mathf.Max(1.15f, balance.airTime.Evaluate(Elapsed) - 0.2f * over);

        int count = Mathf.Min(MaxWave, Mathf.FloorToInt(balance.load.Evaluate(Elapsed) + over + Random.value));
        // Изредка волна выходит на слайм меньше: короткая передышка внутри общего подъёма.
        if (count > 1 && Random.value < 0.15f) count--;

        int bombCount = 0;
        if (bombs)
        {
            if (Random.value < balance.bombChance.Evaluate(Elapsed)) bombCount++;
            if (Random.value < balance.secondBomb.Evaluate(Elapsed)) bombCount++;
        }
        if (frenzy)
        {
            // Перегрузка реторты: волны на слайм больше, чаще и всегда рисунком,
            // а бомба проносится между слаймами или занимает место в шеренге.
            count = Mathf.Clamp(count + 1, 2, MaxWave);
            bombCount = bombs && Random.value < 0.55f ? 1 : 0;
        }

        Pattern pattern = Choose(count, frenzy);
        lastPattern = pattern;
        var wave = new List<Shot>();
        switch (pattern)
        {
            case Pattern.Stream: Stream(wave, count, airTime); break;
            case Pattern.Cross: Cross(wave, count, airTime); break;
            case Pattern.Column: Column(wave, count, airTime); break;
            case Pattern.Fan: Fan(wave, count, airTime); break;
            case Pattern.Arc: Arc(wave, count, airTime); break;
            case Pattern.Stairs: Stairs(wave, count, airTime); break;
            case Pattern.Figure: Figure(wave, count, airTime); break;
            case Pattern.Row:
                bool bombInside = frenzy && bombCount > 0 && Random.value < 0.6f;
                if (bombInside) bombCount = 0;
                Row(wave, count, bombInside, airTime, null);
                break;
            default: Scatter(wave, count, airTime); break;
        }
        float length = 0f;
        foreach (var shot in wave) length = Mathf.Max(length, shot.delay);
        AddBombs(wave, bombCount, airTime);
        StartCoroutine(Release(wave));

        // Золотой король — подмога, а не случайный гость: он вылетает, только когда в воздухе начинается замес.
        // Чем гуще слаймы и бомбы, тем вероятнее король. Он появляется, пока волна ещё взлетает: если разрезать
        // его сразу, замедление придётся на тот миг, когда слаймов и бомб в воздухе больше всего.
        if (kingMet && !kingComing && kingCooldown <= 0f)
        {
            int crowd = Pressure(wave), need = Mathf.RoundToInt(balance.kingCrowd.Evaluate(Elapsed));
            if (crowd >= need && Random.value < balance.kingChance * (1f + 0.5f * (crowd - need)))
                StartCoroutine(KingAfter(Mathf.Max(0f, length * 0.5f - 0.4f), escortMet && Random.value < balance.escortChance.Evaluate(Elapsed)));
        }

        // Ритм. Паузы неравные, чтобы волны не шли как по метроному; иногда две-три волны идут серией
        // почти подряд, а за серией следует длинная передышка.
        float rest = balance.interval.Evaluate(Elapsed) * Random.Range(balance.restSpread.x, balance.restSpread.y)
            + 0.07f * count - 0.12f * over;
        if (phraseLeft == 0 && !frenzy && Elapsed > 6f && Random.value < balance.phraseChance) phraseLeft = Random.Range(2, 4);
        if (phraseLeft > 0)
        {
            phraseLeft--;
            rest *= phraseLeft > 0 ? 0.55f : 1.6f;
        }
        if (frenzy) rest *= 0.85f;
        timer = length + Mathf.Max(0.35f, rest);
    }

    // Насколько тесно будет в воздухе: объекты этой волны и те, что уже летят и не успеют упасть.
    // Бомба считается за двоих: она опаснее слайма, и именно её король превращает в золото.
    static int Pressure(List<Shot> wave)
    {
        int crowd = 0;
        foreach (var shot in wave) crowd += shot.def.IsBomb ? 2 : 1;
        Rect view = GameManager.View;
        foreach (var other in Sliceable.Active)
        {
            if (!other.Alive || other.Def.kind == Kind.King || other.Def.kind == Kind.Nugget) continue;
            if (other.PosAfter(0.8f).y > view.yMin) crowd += other.Def.IsBomb ? 2 : 1;
        }
        return crowd;
    }

    IEnumerator KingAfter(float delay, bool escort)
    {
        kingComing = true;
        if (delay > 0f) yield return new WaitForSeconds(delay);
        LaunchKing(escort);
    }

    // Рисунок выбирается по таблице из Balance: подходят те, что уже открыты и годятся для такого числа слаймов.
    // Только что сыгранный рисунок выпадает реже — волны не должны повторяться подряд.
    Pattern Choose(int count, bool frenzy)
    {
        float total = 0f;
        foreach (var rule in balance.patterns) total += Weight(rule, count, frenzy);
        float roll = Random.value * total;
        foreach (var rule in balance.patterns)
        {
            float weight = Weight(rule, count, frenzy);
            if (weight <= 0f) continue;
            if ((roll -= weight) <= 0f) return rule.pattern;
        }
        return Pattern.Scatter;
    }

    float Weight(PatternRule rule, int count, bool frenzy)
    {
        if (Elapsed < rule.from || count < rule.minCount || count > rule.maxCount) return 0f;
        // В перегрузке слаймы летят только строем: россыпь и очереди превратили бы её в хаос.
        if (frenzy && (rule.pattern == Pattern.Scatter || rule.pattern == Pattern.Stream || rule.pattern == Pattern.Column)) return 0f;
        return rule.pattern == lastPattern ? rule.weight * 0.3f : rule.weight;
    }

    void Introduce(Intro intro, int index)
    {
        if (!string.IsNullOrEmpty(intro.hint))
        {
            int shown = PlayerPrefs.GetInt(HintKey + index, 0);
            if (shown < HintShows)
            {
                PlayerPrefs.SetInt(HintKey + index, shown + 1);
                GameManager.Instance.Hud.Hint(intro.hint, 2.6f);
            }
        }

        float airTime = Mathf.Max(balance.airTime.Evaluate(Elapsed), 2.4f), pause = 1.5f;
        var wave = new List<Shot>();
        switch (intro.kind)
        {
            case Kind.King:
                // Знакомство с королём — сразу в деле: он вылетает вместе с веером слаймов и бомбой, и видно,
                // зачем он нужен. Разрежешь его — время замедлится, а бомба станет золотом.
                kingMet = true;
                escortMet |= intro.escort;
                Fan(wave, Mathf.Max(4, Mathf.RoundToInt(balance.load.Evaluate(Elapsed)) + 1), airTime);
                AddBombs(wave, bombs && !intro.escort ? 1 : 0, airTime);
                StartCoroutine(KingAfter(0.15f, intro.escort));
                pause = 2.4f;
                break;
            case Kind.Bomb:
                // Первая бомба всегда вылетает рядом с одним слаймом: видно, что резать, а что нет.
                bombs = true;
                Rect view = GameManager.View;
                float x = view.center.x + Random.Range(-0.2f, 0.2f) * view.width;
                wave.Add(Plan(Art.Acid, x - 3f * GameManager.Unit, x - 5f * GameManager.Unit, 0.72f, airTime));
                wave.Add(Plan(Art.Bomb, x + 3f * GameManager.Unit, x + 5f * GameManager.Unit, 0.72f, airTime));
                break;
            default:
                if (intro.kind == Kind.Magma) magma = true;
                else if (intro.kind == Kind.Blue) blue = true;
                else if (intro.kind == Kind.Geode) geode = true;
                else if (intro.kind == Kind.Mercury) mercury = true;
                // Шеренга одинаковых слаймов — готовая цель для одного взмаха: так игрок получает первое комбо.
                if (intro.row > 1) Row(wave, intro.row, false, airTime, Art.Def(intro.kind));
                else wave.Add(PlanRandom(Art.Def(intro.kind), airTime, 0f));
                break;
        }
        StartCoroutine(Release(wave));
        timer = pause;
    }

    // ---------- Рисунки волн ----------

    // Россыпь: слаймы вылетают из случайных мест разом.
    void Scatter(List<Shot> wave, int slimes, float airTime)
    {
        for (int i = 0; i < slimes; i++) wave.Add(PlanRandom(Pick(), airTime, 0f));
    }

    // Очередь: по одному через короткие промежутки, волной вдоль котла — слева направо или наоборот.
    void Stream(List<Shot> wave, int slimes, float airTime)
    {
        Rect view = GameManager.View;
        float u = GameManager.Unit;
        float gap = Random.Range(0.26f, 0.4f);
        float left = view.xMin + 4f * u, right = view.xMax - 4f * u;
        bool reverse = Random.value < 0.5f;
        float apex = Random.Range(0.62f, 0.8f);
        for (int i = 0; i < slimes; i++)
        {
            float k = slimes > 1 ? i / (slimes - 1f) : 0.5f;
            float x = Mathf.Lerp(left, right, reverse ? 1f - k : k) + Random.Range(-1f, 1f) * u;
            wave.Add(Plan(Pick(), x, x + Random.Range(-2f, 2f) * u, apex, airTime, false, i * gap));
        }
    }

    // Ножницы: пары летят навстречу друг другу и сходятся в вершине — миг, когда обе режутся одним взмахом.
    void Cross(List<Shot> wave, int slimes, float airTime)
    {
        Rect view = GameManager.View;
        float u = GameManager.Unit;
        float center = view.center.x + Random.Range(-0.18f, 0.18f) * view.width;
        float apex = Random.Range(0.62f, 0.76f);
        for (int pair = 0; pair * 2 < slimes; pair++)
        {
            float reach = Mathf.Min(Random.Range(5f, 8f) * u, view.width * 0.3f);
            float delay = pair * 0.4f;
            wave.Add(Plan(Pick(true), center - reach, center + reach, apex, airTime, true, delay));
            if (pair * 2 + 1 < slimes) wave.Add(Plan(Pick(true), center + reach, center - reach, apex, airTime, true, delay));
        }
    }

    // Строй: ровная шеренга по параллельным дугам — идеальная цель для одного длинного взмаха.
    // В перегрузке одно из мест может занять бомба; вокруг неё строй раздвинут. only — все слаймы одного вида.
    void Row(List<Shot> wave, int slimes, bool bombInside, float airTime, SlimeDef only)
    {
        int slots = slimes + (bombInside ? 1 : 0);
        int bombSlot = bombInside ? Random.Range(1, slots - 1) : -1;
        float room = bombInside ? BombRoom * GameManager.Unit : 0f;
        float step, from, to;
        Line(slots, 2f * room, out step, out from, out to);
        float width = step * (slots - 1) + 2f * room;
        float apex = Random.Range(0.6f, 0.8f);
        for (int i = 0; i < slots; i++)
        {
            float offset = i * step - width * 0.5f;
            if (bombInside && i >= bombSlot) offset += i > bombSlot ? 2f * room : room;
            SlimeDef def = i == bombSlot ? Art.Bomb : only ?? Pick(true);
            wave.Add(Plan(def, from + offset, to + offset, apex, airTime, true));
        }
    }

    // Змейка: слаймы вылетают друг за другом из одной точки и летят по одной дуге.
    void Column(List<Shot> wave, int slimes, float airTime)
    {
        Rect view = GameManager.View;
        float u = GameManager.Unit;
        float from = Random.Range(view.xMin + 4f * u, view.xMax - 4f * u);
        float to = Mathf.Clamp(from + Random.Range(-0.35f, 0.35f) * view.width, view.xMin + 3f * u, view.xMax - 3f * u);
        float apex = Random.Range(0.66f, 0.82f);
        for (int i = 0; i < slimes; i++) wave.Add(Plan(Pick(true), from, to, apex, airTime, true, i * 0.2f));
    }

    // Веер: слаймы бьют из одной точки и расходятся в стороны.
    void Fan(List<Shot> wave, int slimes, float airTime)
    {
        Rect view = GameManager.View;
        float u = GameManager.Unit;
        float origin = view.center.x + Random.Range(-0.25f, 0.25f) * view.width;
        float spread = Mathf.Min(Random.Range(4.5f, 7f) * u, view.width * 0.8f / slimes);
        float apex = Random.Range(0.62f, 0.8f);
        for (int i = 0; i < slimes; i++)
        {
            float k = i - (slimes - 1) * 0.5f;
            wave.Add(Plan(Pick(), origin + k * 0.6f * u, origin + k * spread, apex + Random.Range(-0.04f, 0.04f), airTime));
        }
    }

    // Радуга или чаша: шеренга, у которой вершины дуг лежат на кривой — середина выше краёв или наоборот.
    void Arc(List<Shot> wave, int slimes, float airTime)
    {
        float step, from, to;
        Line(slimes, 0f, out step, out from, out to);
        float width = step * (slimes - 1);
        bool bowl = Random.value < 0.4f;
        float low = Random.Range(0.52f, 0.58f), bulge = Random.Range(0.2f, 0.26f);
        for (int i = 0; i < slimes; i++)
        {
            float k = slimes > 1 ? 2f * i / (slimes - 1f) - 1f : 0f;
            float apex = low + bulge * (bowl ? k * k : 1f - k * k);
            float offset = i * step - width * 0.5f;
            wave.Add(Plan(Pick(true), from + offset, to + offset, apex, airTime, true));
        }
    }

    // Лесенка: та же шеренга, но слаймы взлетают по очереди и выстраиваются в косую линию.
    void Stairs(List<Shot> wave, int slimes, float airTime)
    {
        float step, from, to;
        Line(slimes, 0f, out step, out from, out to);
        float width = step * (slimes - 1);
        bool reverse = Random.value < 0.5f;
        float apex = Random.Range(0.64f, 0.8f);
        for (int i = 0; i < slimes; i++)
        {
            float offset = (reverse ? slimes - 1 - i : i) * step - width * 0.5f;
            wave.Add(Plan(Pick(true), from + offset, to + offset, apex, airTime, true, i * 0.16f));
        }
    }

    // Фигура: треугольник, ромб или кольцо из одинаковых слаймов. У всех одна скорость, поэтому фигура
    // летит не рассыпаясь; иногда это стайка мини-слаймов.
    void Figure(List<Shot> wave, int slimes, float airTime)
    {
        Rect view = GameManager.View;
        float u = GameManager.Unit;
        SlimeDef def = blue && Random.value < 0.35f ? Art.Mini : Pick(true);
        int ring = slimes > 5 ? slimes - 1 : slimes;   // у больших фигур один слайм в центре
        float step = Spacing * u * def.size;
        float radius = step / (2f * Mathf.Sin(Mathf.PI / ring));
        float turn = Random.Range(0f, Mathf.PI * 2f);

        float margin = radius + 2.5f * u;
        float from = Random.Range(view.xMin + margin, view.xMax - margin);
        float to = Mathf.Clamp(from + Random.Range(-0.25f, 0.25f) * view.width, view.xMin + margin, view.xMax - margin);
        Shot center = Plan(def, from, to, Random.Range(0.66f, 0.8f), airTime, true);
        for (int i = 0; i < slimes; i++)
        {
            Shot shot = center;
            // Вся фигура стартует из-под нижнего края: верхний слайм появляется первым, остальные следом.
            float angle = turn + i * Mathf.PI * 2f / ring;
            Vector2 offset = i < ring ? new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius : Vector2.zero;
            shot.pos += offset - Vector2.up * radius;
            wave.Add(shot);
        }
    }

    // Место для шеренги из slots объектов: шаг между соседями и середина строя на взлёте и на спуске.
    static void Line(int slots, float extra, out float step, out float from, out float to)
    {
        Rect view = GameManager.View;
        float u = GameManager.Unit;
        step = Mathf.Min(Spacing * u, (view.width * 0.8f - extra) / slots);
        float margin = 2f * u + (step * (slots - 1) + extra) * 0.5f;
        from = Random.Range(view.xMin + margin, view.xMax - margin);
        to = Mathf.Clamp(from + Random.Range(-0.3f, 0.3f) * view.width, view.xMin + margin, view.xMax - margin);
    }

    // Бомбы подбираются под уже составленную волну: из нескольких случайных бросков берётся тот,
    // что меньше всего летит вплотную к слаймам. Чистый срез есть всегда, но со временем он уже.
    void AddBombs(List<Shot> wave, int count, float airTime)
    {
        if (count <= 0) return;
        float span = 0f;
        foreach (var shot in wave) span = Mathf.Max(span, shot.delay);
        float allowed = balance.crowding.Evaluate(Elapsed);
        for (int i = 0; i < count; i++)
        {
            float delay = Random.value * span + Random.Range(0f, 0.3f);
            Shot best = default(Shot);
            float bestCrowd = float.MaxValue;
            for (int attempt = 0; attempt < BombTries && bestCrowd > allowed; attempt++)
            {
                Shot shot = PlanRandom(Art.Bomb, airTime, delay);
                float crowd = Crowd(shot, wave);
                if (crowd >= bestCrowd) continue;
                best = shot;
                bestCrowd = crowd;
            }
            wave.Add(best);
        }
    }

    // Доля видимого полёта бомбы, которую она проведёт вплотную к слаймам — и этой волны, и уже летящим.
    static float Crowd(Shot bomb, List<Shot> wave)
    {
        Rect view = GameManager.View;
        float reach = Sliceable.RadiusOf(bomb.def) + NearBomb * GameManager.Unit;
        float flight = 2f * bomb.vel.y / bomb.gravity;
        int seen = 0, close = 0;
        for (int i = 1; i < FlightSamples; i++)
        {
            float time = bomb.delay + flight * i / FlightSamples;
            Vector2 at = bomb.At(time);
            if (at.y < view.yMin) continue;
            seen++;

            bool near = false;
            for (int k = 0; k < wave.Count && !near; k++)
            {
                Shot other = wave[k];
                if (other.def.IsBomb || time < other.delay) continue;
                near = IsNear(at, other.At(time), reach + Sliceable.RadiusOf(other.def), view);
            }
            for (int k = 0; k < Sliceable.Active.Count && !near; k++)
            {
                Sliceable other = Sliceable.Active[k];
                if (other.Def.IsBomb || !other.Alive) continue;
                near = IsNear(at, other.PosAfter(time), reach + other.Radius, view);
            }
            if (near) close++;
        }
        return seen > 0 ? close / (float)seen : 0f;
    }

    static bool IsNear(Vector2 bomb, Vector2 slime, float distance, Rect view)
    {
        return slime.y > view.yMin && (bomb - slime).sqrMagnitude < distance * distance;
    }

    IEnumerator Release(List<Shot> wave)
    {
        wave.Sort((a, b) => a.delay.CompareTo(b.delay));
        float clock = 0f;
        for (int i = 0; i < wave.Count; i++)
        {
            if (wave[i].delay > clock)
            {
                yield return new WaitForSeconds(wave[i].delay - clock);
                clock = wave[i].delay;
            }
            Sliceable.Spawn(wave[i].def, wave[i].pos, wave[i].vel, wave[i].gravity);
            Sfx.Toss();
        }
    }

    // ---------- Броски ----------

    // Вид слайма для броска. plain — без синего: в строю делящийся слайм сломал бы рисунок.
    SlimeDef Pick(bool plain = false)
    {
        // Чем дальше, тем меньше простых слаймов и больше редких.
        float acid = balance.plainWeight.Evaluate(Elapsed);
        float wMagma = magma ? 3f : 0f;
        float wBlue = blue && !plain ? 2.2f : 0f;
        float wGeode = geode ? 2.2f : 0f;
        float wMercury = mercury ? 2f : 0f;
        float roll = Random.value * (acid + wMagma + wBlue + wGeode + wMercury);
        if ((roll -= acid) < 0f) return Art.Acid;
        if ((roll -= wMagma) < 0f) return Art.Magma;
        if ((roll -= wBlue) < 0f) return Art.Blue;
        if ((roll -= wGeode) < 0f) return Art.Geode;
        return Art.Mercury;
    }

    static Shot PlanRandom(SlimeDef def, float airTime, float delay)
    {
        Rect view = GameManager.View;
        float margin = Sliceable.RadiusOf(def) + 0.6f * GameManager.Unit;
        float x0 = Random.Range(view.xMin + margin, view.xMax - margin);
        float x1 = x0 + Random.Range(-0.4f, 0.4f) * view.width;
        return Plan(def, x0, x1, Random.Range(0.55f, 0.86f), airTime, false, delay);
    }

    // Бросок снизу по параболе: вершина и точка падения всегда внутри экрана при любом его формате.
    // exact — строй летит как единое целое, без поправок на характер отдельного слайма.
    static Shot Plan(SlimeDef def, float x0, float x1, float apexHeight, float airTime, bool exact = false, float delay = 0f)
    {
        Rect view = GameManager.View;
        float radius = Sliceable.RadiusOf(def);
        float margin = radius + 0.4f * GameManager.Unit;

        if (!exact)
        {
            switch (def.kind)
            {
                case Kind.Magma:    // тяжёлый: высокая крутая дуга
                    apexHeight = Mathf.Max(apexHeight, Random.Range(0.8f, 0.9f));
                    x1 = Mathf.Lerp(x0, x1, 0.4f);
                    airTime *= 1.1f;
                    break;
                case Kind.Mercury:  // стремительный
                    airTime *= 0.74f;
                    break;
                case Kind.Geode:    // массивный: чуть дольше в воздухе, чтобы хватило времени на два удара
                    apexHeight = Mathf.Max(apexHeight, 0.68f);
                    airTime *= 1.1f;
                    break;
                case Kind.Blue:     // крупный и неторопливый, с запасом высоты для мини-слаймов
                    apexHeight = Mathf.Clamp(apexHeight, 0.58f, 0.74f);
                    airTime *= 1.05f;
                    break;
            }
        }

        x0 = Mathf.Clamp(x0, view.xMin + margin, view.xMax - margin);
        x1 = Mathf.Clamp(x1, view.xMin + margin, view.xMax - margin);
        float y0 = view.yMin - radius * 1.2f;
        float apex = Mathf.Min(view.yMin + view.height * apexHeight, view.yMax - radius);

        // Гравитация подобрана так, чтобы бросок на «эталонную» высоту длился ровно airTime.
        float gravity = 8f * (0.75f * view.height) / (airTime * airTime);
        float vy = Mathf.Sqrt(2f * gravity * (apex - y0));
        float vx = (x1 - x0) * gravity / (2f * vy);
        return new Shot { def = def, pos = new Vector2(x0, y0), vel = new Vector2(vx, vy), gravity = gravity, delay = delay };
    }

    // Король проносится пологой дугой через весь экран. Эскорт — две бомбы спереди и сзади:
    // безопасен только точный взмах поперёк траектории.
    void LaunchKing(bool escort)
    {
        Rect view = GameManager.View;
        float u = GameManager.Unit;
        float radius = Sliceable.RadiusOf(Art.King);
        float dir = Random.value < 0.5f ? 1f : -1f;
        float flight = Mathf.Lerp(2.7f, 2.2f, Mathf.Clamp01(Elapsed / balance.peak));
        // Под конвоем король летит медленнее: взмах должен требовать точности, а не везения.
        if (escort) flight *= 1.2f;

        float pad = escort ? 5f * u : radius * 2f;
        float x0 = dir > 0f ? view.xMin - pad : view.xMax + pad;
        float y0 = view.yMin + view.height * Random.Range(0.3f, 0.42f);
        float apex = view.yMin + view.height * Random.Range(0.7f, 0.8f);
        float half = flight * 0.5f;
        float vy = 2f * (apex - y0) / half;
        float gravity = vy / half;
        Vector2 vel = new Vector2(dir * (view.width + pad * 2f) / flight, vy);

        Sliceable.Spawn(Art.King, new Vector2(x0, y0), vel, gravity);
        if (escort)
        {
            float gap = radius + Sliceable.RadiusOf(Art.Bomb) + EscortGap * u;
            Sliceable.Spawn(Art.Bomb, new Vector2(x0 - gap, y0), vel, gravity);
            Sliceable.Spawn(Art.Bomb, new Vector2(x0 + gap, y0), vel, gravity);
        }
        kingCooldown = balance.kingCooldown;
        kingComing = false;
        Sfx.Toss();
    }
}
