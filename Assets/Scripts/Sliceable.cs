using System.Collections.Generic;
using UnityEngine;

// Летающий объект: слайм любого вида или чумная спора (бомба).
// Движение считается вручную, без Rigidbody: так траектория и попадание полностью предсказуемы.
public class Sliceable : MonoBehaviour
{
    public const float BaseRadius = 1.15f;
    const float BombReach = 0.82f;   // бомба «прощает» касание шипов: задевает только взмах по корпусу
    const float HoverScale = 1.9f;   // стартовый слайм в меню крупнее обычного: в него легко попасть первым же взмахом
    const float SplitGuard = 0.16f;  // после раскола/деления объект мгновение неуязвим, иначе тот же взмах убьёт его сразу

    public static readonly List<Sliceable> Active = new List<Sliceable>();

    public SlimeDef Def { get; private set; }
    public float Radius { get; private set; }
    public bool Alive { get; private set; }
    public bool Shelled { get; private set; }
    public Vector2 Pos { get { return transform.position; } }
    public Vector2 Prev { get; private set; }   // где объект был кадр назад: клинок проверяет попадание по его пути
    public bool CanCut { get { return Alive && guard <= 0f; } }
    public float Reach { get { return Def.IsBomb ? Radius * BombReach : Radius; } }

    Vector2 vel;
    float gravity, spin, angle, age, guard, wobble, phase, trailTimer, blinkTimer, blinkLeft;
    bool hover;
    Transform body, glow;
    SpriteRenderer bodyRenderer, glowRenderer;

    public static Sliceable Spawn(SlimeDef def, Vector2 pos, Vector2 vel, float gravity, bool hover = false, float guard = 0f)
    {
        var go = new GameObject(def.name);
        go.transform.SetParent(Fx.Root, false);
        go.transform.position = pos;

        var s = go.AddComponent<Sliceable>();
        s.Def = def;
        s.vel = vel;
        s.gravity = gravity;
        s.hover = hover;
        s.guard = guard;
        s.Shelled = def.kind == Kind.Geode;
        s.angle = def.kind == Kind.King || hover ? 0f : Random.Range(0f, 360f);
        s.spin = hover ? 0f : Random.Range(90f, 220f) * def.spin * (Random.value < 0.5f ? -1f : 1f);
        s.phase = Random.Range(0f, 6.28f);
        s.wobble = 0.26f;
        s.blinkTimer = Random.Range(0.6f, 2.5f);
        s.Prev = pos;
        s.Alive = true;
        s.Radius = RadiusOf(def) * (hover ? HoverScale : 1f);

        int order = def.IsBomb ? Fx.OrderBomb : Fx.OrderSlime;
        var glowGo = Fx.NewSprite("Glow", Art.Soft, WithAlpha(def.glow, def.IsBomb ? 0.6f : 0.3f), order - 1, go.transform);
        s.glow = glowGo.transform;
        s.glow.localScale = Vector3.one * 2.5f;
        s.glowRenderer = glowGo.GetComponent<SpriteRenderer>();

        var bodyGo = Fx.NewSprite("Body", s.Shelled ? def.shell : def.whole, Color.white, order, go.transform);
        s.body = bodyGo.transform;
        s.bodyRenderer = bodyGo.GetComponent<SpriteRenderer>();

        s.Pose(0f);
        Active.Add(s);
        return s;
    }

    // Где объект окажется через t секунд: бросок летит по известной параболе.
    public Vector2 PosAfter(float t)
    {
        Vector2 p = Pos;
        return hover ? p : new Vector2(p.x + vel.x * t, p.y + vel.y * t - 0.5f * gravity * t * t);
    }

    public static float RadiusOf(SlimeDef def)
    {
        return BaseRadius * def.size * GameManager.Unit;
    }

    void Update()
    {
        // Объект, убранный в этом кадре, ещё доживает его до конца — он уже ни на что не влияет.
        if (!Alive) return;
        float dt = Time.deltaTime;
        float u = GameManager.Unit;
        Prev = Pos;
        age += dt;
        guard -= dt;
        wobble *= Mathf.Exp(-4f * dt);
        // Слаймы с мордочкой не крутятся волчком, а покачиваются: лицо всегда читается. Камень и бомбы вращаются.
        bool face = Def.blink != null && !Shelled;
        if (face && !hover) angle = Mathf.LerpAngle(angle, Mathf.Sin(age * 3.2f + phase) * 20f, 1f - Mathf.Exp(-9f * dt));
        else angle += spin * dt;
        Blink(face);

        if (hover)
        {
            // Стартовый слайм висит на месте, покачивается и «дышит», приглашая его разрезать.
            Rect view = GameManager.View;
            Radius = RadiusOf(Def) * HoverScale;
            angle = Mathf.Sin(age * 1.6f) * 8f;
            transform.position = new Vector3(view.center.x, view.yMin + view.height * GameManager.StartHeight + Mathf.Sin(age * 2.2f) * 0.3f * u, 0f);
            Pose(0.06f * Mathf.Sin(age * 4.4f));
            // Вокруг стартового слайма мерцают искры: взгляд сам находит, что резать.
            trailTimer -= dt;
            if (trailTimer <= 0f)
            {
                trailTimer = 0.09f;
                Fx.BladeSpark(Pos + Random.insideUnitCircle.normalized * (Radius * Random.Range(1.1f, 1.7f)), Color.Lerp(Def.glow, Color.white, Random.value));
            }
            return;
        }

        vel.y -= gravity * dt;
        transform.position += (Vector3)(vel * dt);

        // Желе вытягивается вдоль скорости и упруго дрожит после броска или удара.
        float stretch = Mathf.Clamp01(vel.magnitude / (32f * u)) * 0.14f + 0.055f * Mathf.Sin(age * 8f + phase) + wobble * Mathf.Sin(age * 26f);
        Pose(Def.IsBomb || Shelled ? stretch * 0.25f : stretch);

        if (Def.IsBomb)
        {
            // Спора тревожно пульсирует красным ореолом, фитиль искрит.
            float pulse = 0.5f + 0.5f * Mathf.Sin(age * 14f);
            glow.localScale = Vector3.one * (2.5f + 0.6f * pulse);
            glowRenderer.color = WithAlpha(Def.glow, 0.4f + 0.35f * pulse);
        }

        trailTimer -= dt;
        if (trailTimer <= 0f)
        {
            trailTimer = 0.035f;
            if (Def.IsBomb) Fx.FuseSpark(body.TransformPoint(0f, Art.BombFuseTip, 0f));
            else Fx.Trail(Def.kind, Pos, Radius);
        }

        Rect v = GameManager.View;
        Vector2 p = Pos;
        if (vel.y < 0f && p.y < v.yMin - Radius * 1.3f)
        {
            bool penalty = Def.penalty;
            Remove();
            if (penalty) GameManager.Instance.OnSlimeMissed(p.x, Def);
        }
        else if ((vel.x > 0f && p.x > v.xMax + Radius * 4f) || (vel.x < 0f && p.x < v.xMin - Radius * 4f))
        {
            Remove();
        }
    }

    // Корень повёрнут по скорости и растянут вдоль неё, а спрайт внутри довёрнут до своего угла вращения.
    void Pose(float stretch)
    {
        float heading = hover || vel.sqrMagnitude < 0.01f ? 90f : Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, heading);
        transform.localScale = new Vector3(Radius * (1f + stretch), Radius * (1f - stretch), 1f);
        body.localRotation = Quaternion.Euler(0, 0, angle - heading);
    }

    // Время от времени слайм моргает: глаза на мгновение сменяются довольными дужками.
    void Blink(bool face)
    {
        if (!face) return;
        float dt = Time.unscaledDeltaTime;
        blinkTimer -= dt;
        if (blinkTimer <= 0f)
        {
            blinkTimer = Random.Range(1.6f, 4f);
            blinkLeft = 0.14f;
        }
        blinkLeft -= dt;
        bodyRenderer.sprite = blinkLeft > 0f ? Def.blink : Def.whole;
    }

    // offset — где линия взмаха прошла относительно центра: разрез и половинки ложатся по ней.
    public void Slice(Vector2 dir, Vector2 offset)
    {
        if (!CanCut) return;
        Vector2 pos = Pos;
        float u = GameManager.Unit;
        // Насколько разрез смещён от центра поперёк клинка, в долях радиуса.
        float cut = Mathf.Clamp(Vector2.Dot(offset, new Vector2(-dir.y, dir.x)) / Radius, -0.7f, 0.7f);

        if (Def.IsBomb)
        {
            Remove();
            GameManager.Instance.OnBombHit(pos);
            return;
        }

        if (Shelled)
        {
            // Первый удар только раскалывает скорлупу и подбрасывает жеоду — желе добивается вторым.
            Shelled = false;
            guard = SplitGuard;
            wobble = 0.3f;
            bodyRenderer.sprite = Def.whole;
            Fx.ShellBreak(pos, dir, vel, Radius, cut);
            vel = new Vector2(vel.x * 0.5f + dir.x * 1.5f * u, Mathf.Max(vel.y * 0.4f, 0f) + 7f * u);
            spin *= 2f;
            GameManager.Instance.OnShellCracked(pos);
            return;
        }

        Remove();
        if (Def.kind == Kind.Blue)
        {
            Fx.Split(Def, pos, dir, vel, Radius, cut);
            SpawnMinis(pos, dir);
        }
        else
        {
            Fx.Slice(Def, pos, dir, vel, Radius, spin, cut);
        }
        GameManager.Instance.OnSlimeSliced(Def, pos);
    }

    // Митоз: два мини-слайма разлетаются в стороны от разреза по крутым дугам.
    void SpawnMinis(Vector2 pos, Vector2 dir)
    {
        float u = GameManager.Unit;
        Rect v = GameManager.View;
        float r = RadiusOf(Art.Mini);
        Vector2 n = new Vector2(-dir.y, dir.x);
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 from = pos + n * (side * Radius * 0.45f);
            float apex = Mathf.Min(from.y + Random.Range(3f, 5.5f) * u, v.yMax - r);
            float vy = Mathf.Sqrt(2f * gravity * Mathf.Max(0.5f * u, apex - from.y));
            // Горизонтальную скорость подбираем так, чтобы мини-слайм упал внутри экрана.
            float fall = (vy + Mathf.Sqrt(vy * vy + 2f * gravity * Mathf.Max(0f, from.y - v.yMin))) / gravity;
            float sign = Mathf.Abs(n.x) > 0.3f ? Mathf.Sign(n.x) * side : side;
            float landing = Mathf.Clamp(from.x + sign * Random.Range(3.5f, 6.5f) * u * fall * 0.6f, v.xMin + r * 1.5f, v.xMax - r * 1.5f);
            Spawn(Art.Mini, from, new Vector2((landing - from.x) / fall, vy), gravity, false, SplitGuard);
        }
        Sfx.Split();
    }

    // Трансмутация: бомба становится золотым самородком с той же траекторией.
    public void Transmute()
    {
        if (!Alive || !Def.IsBomb) return;
        Fx.Transmute(Pos, Radius);
        Spawn(Art.Nugget, Pos, vel, gravity);
        Remove();
    }

    // Убрать без очков и штрафа (очистка поля после взрыва или при рестарте).
    public void Vanish()
    {
        if (!Alive) return;
        Fx.Puff(Pos, Radius, Def.glow);
        Remove();
    }

    void Remove()
    {
        Alive = false;
        Active.Remove(this);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        Active.Remove(this);
    }

    static Color WithAlpha(Color c, float a)
    {
        c.a = a;
        return c;
    }
}
