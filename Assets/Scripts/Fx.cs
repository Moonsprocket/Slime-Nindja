using System.Collections.Generic;
using UnityEngine;

// Визуальный отклик в мире: половинки, брызги, кляксы, осколки, взрывы, искры.
public static class Fx
{
    public const int OrderWall = -100, OrderAmbient = -90, OrderSplat = -50, OrderHalf = -3, OrderGlow = -2,
        OrderSlime = 0, OrderBomb = 5, OrderRim = 8, OrderParticles = 10, OrderSlash = 15, OrderBlade = 20;

    static readonly Color Gold = new Color(1f, 0.85f, 0.3f);
    static readonly Color Steam = new Color(1f, 1f, 1f, 0.5f);

    public static Transform Root { get; private set; }

    static ParticleSystem drops, sparks, embers, smoke, stars;

    public static void Build()
    {
        Root = new GameObject("FX").transform;
        var shrink = AnimationCurve.Linear(0f, 1f, 1f, 0.2f);
        drops = MakeParticles("Drops", Art.DotMat, 2.4f, shrink);
        sparks = MakeParticles("Sparks", Art.SoftMat, 0f, shrink);
        embers = MakeParticles("Embers", Art.SoftMat, 1.6f, shrink);
        smoke = MakeParticles("Smoke", Art.SoftMat, -0.12f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.5f));
        stars = MakeParticles("Stars", Art.StarMat, 0.5f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
    }

    // ---------- Срезы ----------

    // cut — смещение линии разреза от центра поперёк клинка, в долях радиуса: росчерк и половинки ложатся по ней.
    public static void Slice(SlimeDef def, Vector2 pos, Vector2 dir, Vector2 vel, float radius, float spin, float cut)
    {
        float u = GameManager.Unit;
        Vector2 n = new Vector2(-dir.y, dir.x);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // Срез — это удар: цветной росчерк клинка, вспышка света, кольцо и струя сока вдоль линии разреза.
        SlashMark(pos + n * (cut * radius), angle, radius, def.glow);
        Flash(pos, radius * 2.8f, Color.Lerp(def.glow, Color.white, 0.4f), 0.16f);
        Pulse(pos, radius * 3.2f, def.glow, 0.26f);
        Spray(def, pos, dir, radius);

        switch (def.kind)
        {
            case Kind.Magma:
                Halves(def, pos, n, dir, angle, vel, radius, spin, 0.8f, cut);
                for (int i = 0; i < 22; i++)
                {
                    Color c = Color.Lerp(new Color(1f, 0.4f, 0.05f), new Color(1f, 0.95f, 0.5f), Random.value);
                    Emit(embers, pos, Fan(n, dir) * (Random.Range(5f, 16f) * u) + vel * 0.3f, Random.Range(0.18f, 0.42f) * u, Random.Range(0.4f, 0.9f), c);
                }
                for (int i = 0; i < 7; i++)
                    Emit(smoke, pos + Random.insideUnitCircle * (radius * 0.6f), Random.insideUnitCircle * (1.5f * u) + Vector2.up * (2f * u),
                        Random.Range(1.2f, 2.2f) * u, Random.Range(0.6f, 1.1f), Steam);
                Splat(pos, radius, def.glow, false);
                break;

            case Kind.Mercury:
                // Ртуть не режется пополам — она разлетается зеркальными каплями.
                for (int i = 0; i < 11; i++)
                {
                    var bead = NewPiece("Bead", Art.Bead, Color.white, pos + Random.insideUnitCircle * (radius * 0.5f), radius * Random.Range(0.16f, 0.4f));
                    bead.vel = Fan(n, dir) * (Random.Range(4f, 11f) * u) + vel * 0.5f;
                    bead.gravity = 24f * u;
                    bead.life = Random.Range(0.6f, 1.1f);
                    bead.shrink = true;
                }
                for (int i = 0; i < 4; i++) Bolt(pos, pos + Random.insideUnitCircle.normalized * (radius * Random.Range(1.6f, 3f)));
                for (int i = 0; i < 12; i++)
                    Emit(sparks, pos, Random.insideUnitCircle.normalized * (Random.Range(6f, 18f) * u), Random.Range(0.15f, 0.3f) * u,
                        Random.Range(0.12f, 0.3f), Color.Lerp(def.glow, Color.white, Random.value));
                Splat(pos, radius * 0.8f, def.body, false);
                break;

            case Kind.King:
            case Kind.Nugget:
                Halves(def, pos, n, dir, angle, vel, radius, spin, 1f, cut);
                Droplets(def, pos, n, dir, vel, radius, 14);
                Coins(pos, vel, def.kind == Kind.King ? 16 : 5);
                Sparkle(pos, Gold, def.kind == Kind.King ? 34 : 10, def.kind == Kind.King ? 16f : 9f);
                if (def.kind == Kind.King) Pulse(pos, radius * 9f, Gold, 0.5f);
                Splat(pos, radius, def.glow, false);
                break;

            default:
                Halves(def, pos, n, dir, angle, vel, radius, spin, 1f, cut);
                Droplets(def, pos, n, dir, vel, radius, 26);
                Splat(pos, radius, def.glow, def.kind == Kind.Acid);
                if (def.kind == Kind.Geode) GeodeCore(pos, n, dir, vel, radius);
                break;
        }
    }

    // Скорлупа жеоды раскалывается на острые осколки, желе остаётся целым.
    public static void ShellBreak(Vector2 pos, Vector2 dir, Vector2 vel, float radius, float cut)
    {
        float u = GameManager.Unit;
        Vector2 n = new Vector2(-dir.y, dir.x);
        SlashMark(pos + n * (cut * radius), Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, radius, new Color(0.8f, 0.75f, 1f));
        Flash(pos, radius * 2.4f, Color.white, 0.14f);
        Pulse(pos, radius * 2.2f, Color.white, 0.18f);
        for (int i = 0; i < 12; i++)
        {
            float g = Random.Range(0.42f, 0.7f);
            Color c = i % 4 == 0 ? Art.Geode.glow : new Color(g, g * 0.95f, g * 1.15f);
            var shard = NewPiece("Shard", Art.Shard, c, pos + Random.insideUnitCircle * (radius * 0.7f), radius * Random.Range(0.2f, 0.42f));
            shard.vel = (Random.insideUnitCircle.normalized + n * Random.Range(-0.6f, 0.6f) + dir * 0.5f) * (Random.Range(4f, 11f) * u) + vel * 0.4f;
            shard.spin = Random.Range(-720f, 720f);
            shard.gravity = 26f * u;
        }
        for (int i = 0; i < 8; i++)
        {
            float g = Random.Range(0.5f, 0.7f);
            Emit(smoke, pos + Random.insideUnitCircle * radius, Random.insideUnitCircle * (2.5f * u), Random.Range(0.8f, 1.6f) * u,
                Random.Range(0.3f, 0.6f), new Color(g, g, g * 1.1f, 0.45f));
        }
    }

    // Митоз: слайм с хлопком лопается, вместо половинок вылетают два маленьких.
    public static void Split(SlimeDef def, Vector2 pos, Vector2 dir, Vector2 vel, float radius, float cut)
    {
        Vector2 n = new Vector2(-dir.y, dir.x);
        SlashMark(pos + n * (cut * radius), Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, radius, def.glow);
        Flash(pos, radius * 3f, Color.Lerp(def.glow, Color.white, 0.4f), 0.18f);
        Pulse(pos, radius * 3.2f, def.glow, 0.3f);
        Pulse(pos, radius * 2f, Color.white, 0.18f);
        Droplets(def, pos, n, dir, vel, radius, 22);
        Splat(pos, radius, def.glow, false);
    }

    static void Halves(SlimeDef def, Vector2 pos, Vector2 n, Vector2 dir, float angle, Vector2 vel, float radius, float spin, float push, float cut)
    {
        float u = GameManager.Unit;
        for (int side = -1; side <= 1; side += 2)
        {
            // Половинки расходятся строго перпендикулярно клинку. Если взмах прошёл не по центру,
            // с его стороны отлетает кусок поменьше, а с другой остаётся побольше.
            float share = side * cut > 0f ? 1f - Mathf.Abs(cut) * 0.6f : 1f + Mathf.Abs(cut) * 0.2f;
            var piece = NewPiece("Half", def.half, Color.white, pos + n * (radius * (cut * 0.6f + side * 0.14f * share)), radius * share, OrderHalf);
            piece.transform.rotation = Quaternion.Euler(0, 0, side > 0 ? angle : angle + 180f);
            piece.vel = vel * 0.5f + n * (side * Random.Range(3f, 5f) * push * u / share) + dir * (Random.Range(1f, 3f) * u);
            piece.spin = spin * 0.4f + side * Random.Range(110f, 300f);
            piece.gravity = 26f * u;
        }
    }

    static void Droplets(SlimeDef def, Vector2 pos, Vector2 n, Vector2 dir, Vector2 vel, float radius, int count)
    {
        float u = GameManager.Unit;
        for (int i = 0; i < count; i++)
        {
            Emit(drops, pos + Random.insideUnitCircle * (radius * 0.4f), Fan(n, dir) * (Random.Range(3.5f, 11f) * u) + vel * 0.3f,
                Random.Range(0.16f, 0.42f) * u, Random.Range(0.4f, 0.85f), Color.Lerp(def.body, def.glow, Random.value));
        }
    }

    // Из жеоды выпадает сверкающий аметист и горсть мелких кристаллов.
    static void GeodeCore(Vector2 pos, Vector2 n, Vector2 dir, Vector2 vel, float radius)
    {
        float u = GameManager.Unit;
        var gem = NewPiece("Amethyst", Art.Crystal, Color.white, pos, radius * 0.55f, OrderParticles);
        gem.vel = new Vector2(vel.x * 0.3f + Random.Range(-1.5f, 1.5f) * u, 7f * u);
        gem.spin = Random.Range(-260f, 260f);
        gem.gravity = 24f * u;
        gem.sparkle = Art.Geode.core;
        for (int i = 0; i < 6; i++)
        {
            var bit = NewPiece("Crystal", Art.Crystal, Color.white, pos, radius * Random.Range(0.14f, 0.24f), OrderParticles);
            bit.vel = Fan(n, dir) * (Random.Range(4f, 9f) * u) + vel * 0.3f;
            bit.spin = Random.Range(-600f, 600f);
            bit.gravity = 24f * u;
        }
        Sparkle(pos, Art.Geode.core, 8, 8f);
    }

    static void Coins(Vector2 pos, Vector2 vel, int count)
    {
        float u = GameManager.Unit;
        for (int i = 0; i < count; i++)
        {
            var coin = NewPiece("Coin", Art.Coin, Color.white, pos, Random.Range(0.26f, 0.4f) * u, OrderParticles);
            Vector2 d = Random.insideUnitCircle;
            coin.vel = new Vector2(d.x * 9f * u, (5f + Mathf.Abs(d.y) * 11f) * u) + vel * 0.2f;
            coin.gravity = 24f * u;
            coin.flip = Random.Range(9f, 16f);
        }
    }

    // ---------- События ----------

    public static void Explode(Vector2 pos)
    {
        float u = GameManager.Unit;
        for (int i = 0; i < 46; i++)
        {
            Color c = Color.Lerp(new Color(1f, 0.3f, 0.5f), new Color(1f, 0.95f, 0.7f), Random.value);
            Emit(sparks, pos, Random.insideUnitCircle.normalized * (Random.Range(4f, 24f) * u),
                Random.Range(0.25f, 0.7f) * u, Random.Range(0.3f, 0.75f), c);
        }
        for (int i = 0; i < 14; i++)
        {
            float g = Random.Range(0.1f, 0.26f);
            Emit(smoke, pos + Random.insideUnitCircle * (1.4f * u), Random.insideUnitCircle * (3f * u),
                Random.Range(2f, 3.8f) * u, Random.Range(0.7f, 1.3f), new Color(g * 1.3f, g, g * 1.5f, 0.8f));
        }
        Emit(sparks, pos, Vector2.zero, 11f * u, 0.28f, new Color(1f, 0.9f, 0.85f, 0.95f));
        Pulse(pos, 7f * u, new Color(1f, 0.4f, 0.6f), 0.35f);
        Pulse(pos, 12f * u, Color.white, 0.5f);

        var scorch = NewSprite("Scorch", Art.Splats[Random.Range(0, Art.Splats.Length)], new Color(0.05f, 0.02f, 0.08f, 0.75f), OrderSplat);
        scorch.transform.position = pos;
        scorch.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        scorch.transform.localScale = Vector3.one * (3.4f * u);
        scorch.AddComponent<Fade>().life = 4f;
    }

    // Бомба превращается в золото: вспышка искр на месте трансмутации.
    public static void Transmute(Vector2 pos, float radius)
    {
        Pulse(pos, radius * 3.5f, Gold, 0.35f);
        Sparkle(pos, Gold, 14, 9f);
        for (int i = 0; i < 6; i++)
            Emit(smoke, pos + Random.insideUnitCircle * radius, Random.insideUnitCircle * (2f * GameManager.Unit),
                radius * Random.Range(0.8f, 1.4f), Random.Range(0.3f, 0.5f), new Color(0.5f, 0.3f, 0.65f, 0.5f));
    }

    public static void Puff(Vector2 pos, float radius, Color color)
    {
        color.a = 0.7f;
        for (int i = 0; i < 8; i++)
        {
            Emit(sparks, pos + Random.insideUnitCircle * (radius * 0.5f), Random.insideUnitCircle * (4f * GameManager.Unit),
                radius * Random.Range(0.4f, 0.8f), Random.Range(0.25f, 0.45f), color);
        }
    }

    // Упущенный слайм плюхается обратно в котёл.
    public static void Splash(Vector2 pos, Color color)
    {
        float u = GameManager.Unit;
        for (int i = 0; i < 14; i++)
            Emit(drops, pos, new Vector2(Random.Range(-4f, 4f), Random.Range(7f, 15f)) * u, Random.Range(0.2f, 0.45f) * u, Random.Range(0.5f, 0.9f), color);
    }

    // Начало попытки: три волны света расходятся от разрезанного слайма, во все стороны летят лучи и звёзды.
    public static void StartBurst(Vector2 pos, Color color)
    {
        float u = GameManager.Unit;
        Pulse(pos, 7f * u, Color.white, 0.35f);
        Shock(pos, 11f * u, color, 0.55f);
        Shock(pos, 17f * u, Color.Lerp(color, Color.white, 0.5f), 0.8f);
        Burst(pos, 9f * u, Color.Lerp(color, Color.white, 0.6f), 0.6f, 40f);
        const int rays = 28;
        for (int i = 0; i < rays; i++)
        {
            float angle = i * Mathf.PI * 2f / rays;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Emit(sparks, pos, dir * (Random.Range(16f, 30f) * u), Random.Range(0.35f, 0.6f) * u, Random.Range(0.35f, 0.6f),
                Color.Lerp(color, Color.white, Random.value));
        }
        Sparkle(pos, Color.Lerp(color, Color.white, 0.3f), 46, 15f);
    }

    // Каждый следующий срез в серии бьёт сильнее предыдущего: кольцо шире, искр больше.
    public static void ChainHit(Vector2 pos, int chain, Color color)
    {
        float u = GameManager.Unit;
        int power = Mathf.Min(chain, 8);
        Shock(pos, (1.9f + 0.45f * power) * u, Color.Lerp(color, Color.white, 0.5f), 0.26f);
        Sparkle(pos, Color.Lerp(color, Color.white, 0.4f), 3 * power, 7f + power);
    }

    // Молния комбо: светящаяся ломаная через все слаймы, разрезанные одним взмахом, — серию видно целиком.
    public static void ComboLink(List<Vector2> points, Color color)
    {
        if (points.Count < 2) return;
        float u = GameManager.Unit;
        var go = new GameObject("ComboLink");
        go.transform.SetParent(Root, false);
        var link = go.AddComponent<Link>();
        link.points = points.ToArray();
        link.color = color;
        foreach (var p in points)
        {
            Pulse(p, 2.4f * u, color, 0.4f);
            Emit(stars, p, Vector2.zero, 1.4f * u, 0.45f, Color.white, Random.Range(0f, 360f));
        }
    }

    // Комбо: вспышка, веер лучей, ударная волна и росчерк крест-накрест на месте последнего среза; во все стороны
    // летят искры и звёзды. У каждого размера комбо свой цвет, и с каждой ступенью добавляется новый эффект:
    // второй веер лучей, кристаллы, огненный фонтан, радужный салют.
    public static void ComboBurst(Vector2 pos, int count, Color color)
    {
        float u = GameManager.Unit;
        Color bright = Color.Lerp(color, Color.white, 0.5f);
        Flash(pos, (5f + 1.2f * count) * u, bright, 0.3f);
        Burst(pos, (4.5f + 0.8f * count) * u, bright, 0.5f, 50f);
        Shock(pos, (4.5f + 0.9f * count) * u, color, 0.45f);
        Pulse(pos, (3.5f + 0.6f * count) * u, Color.white, 0.3f);

        // Росчерк крест-накрест: широкий цветной след и белое лезвие поверх.
        float tilt = Random.Range(22f, 38f), length = (9f + 1.6f * count) * u;
        for (int side = -1; side <= 1; side += 2)
        {
            Slash(pos, side * tilt, length * 1.05f, 2.6f * u, new Color(color.r, color.g, color.b, 0.7f), 0.36f);
            Slash(pos, side * tilt, length, 1.1f * u, Color.white, 0.28f);
        }

        Sparkle(pos, Color.Lerp(color, Color.white, 0.3f), 24 + 10 * count, 15f);
        int rays = 14 + 5 * count;
        for (int i = 0; i < rays; i++)
        {
            float angle = i * Mathf.PI * 2f / rays;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Emit(sparks, pos, dir * (Random.Range(16f, 32f) * u), Random.Range(0.3f, 0.6f) * u, Random.Range(0.3f, 0.6f),
                Color.Lerp(color, Color.white, Random.value));
            Emit(embers, pos, dir * (Random.Range(8f, 17f) * u), Random.Range(0.25f, 0.5f) * u, Random.Range(0.5f, 0.95f),
                Color.Lerp(color, Color.white, Random.value * 0.6f));
        }

        // ×5 и выше: быстрое кольцо звёзд.
        if (count >= 5) Sparkle(pos, Color.white, 26, 24f);
        // ×6 и выше: второй веер лучей навстречу первому и кристаллы во все стороны.
        if (count >= 6)
        {
            Burst(pos, (3.5f + 0.6f * count) * u, Color.white, 0.4f, -80f);
            for (int i = 0; i < 10 + 3 * (count - 6); i++)
            {
                var shard = NewPiece("Shard", Art.Shard, Color.Lerp(color, Color.white, Random.Range(0.2f, 0.7f)), pos, Random.Range(0.35f, 0.7f) * u);
                shard.vel = Random.insideUnitCircle.normalized * (Random.Range(9f, 20f) * u) + Vector2.up * (6f * u);
                shard.spin = Random.Range(-720f, 720f);
                shard.gravity = 24f * u;
            }
        }
        // ×7 и выше: огненный фонтан вверх.
        if (count >= 7)
        {
            for (int i = 0; i < 46; i++)
                Emit(embers, pos, new Vector2(Random.Range(-7f, 7f), Random.Range(10f, 26f)) * u, Random.Range(0.3f, 0.65f) * u,
                    Random.Range(0.6f, 1.2f), Color.Lerp(color, new Color(1f, 0.95f, 0.6f), Random.value));
        }
        // ×8 и выше: радужный салют и ещё две цветные волны.
        if (count >= 8)
        {
            for (int i = 0; i < 60; i++)
                Emit(sparks, pos, Random.insideUnitCircle.normalized * (Random.Range(10f, 34f) * u), Random.Range(0.3f, 0.7f) * u,
                    Random.Range(0.5f, 1f), Color.HSVToRGB(Random.value, 0.6f, 1f));
            Shock(pos, 14f * u, Color.HSVToRGB(0.55f, 0.6f, 1f), 0.7f);
            Shock(pos, 18f * u, Color.HSVToRGB(0.85f, 0.6f, 1f), 0.9f);
        }
    }

    public static void Sparkle(Vector2 pos, Color color, int count, float speed)
    {
        float u = GameManager.Unit;
        for (int i = 0; i < count; i++)
        {
            Emit(stars, pos, Random.insideUnitCircle * (speed * u), Random.Range(0.35f, 0.9f) * u, Random.Range(0.4f, 1f),
                Color.Lerp(color, Color.white, Random.value * 0.6f), Random.Range(0f, 360f));
        }
    }

    public static void Firework(Vector2 pos, Color color)
    {
        float u = GameManager.Unit;
        Pulse(pos, 5f * u, color, 0.4f);
        for (int i = 0; i < 40; i++)
        {
            Vector2 d = Random.insideUnitCircle.normalized;
            Emit(embers, pos, d * (Random.Range(6f, 14f) * u), Random.Range(0.35f, 0.7f) * u, Random.Range(0.7f, 1.3f),
                Color.Lerp(color, Color.white, Random.value * 0.5f));
        }
        Sparkle(pos, color, 18, 7f);
    }

    public static void FuseSpark(Vector2 pos)
    {
        float u = GameManager.Unit;
        Emit(sparks, pos, Random.insideUnitCircle * (2.5f * u), Random.Range(0.12f, 0.3f) * u,
            Random.Range(0.12f, 0.3f), Color.Lerp(new Color(1f, 0.5f, 0.1f), new Color(1f, 1f, 0.7f), Random.value));
    }

    // След за летящим объектом: искры у короля, угли у магмы, блики у ртути.
    public static void Trail(Kind kind, Vector2 pos, float radius)
    {
        float u = GameManager.Unit;
        Vector2 at = pos + Random.insideUnitCircle * (radius * 0.7f);
        switch (kind)
        {
            case Kind.King:
            case Kind.Nugget:
                Emit(stars, at, Random.insideUnitCircle * u, Random.Range(0.3f, 0.7f) * u, Random.Range(0.3f, 0.6f), Gold, Random.Range(0f, 360f));
                break;
            case Kind.Magma:
                Emit(embers, at, Random.insideUnitCircle * (1.5f * u), Random.Range(0.12f, 0.28f) * u, Random.Range(0.3f, 0.6f),
                    Color.Lerp(new Color(1f, 0.4f, 0.05f), new Color(1f, 0.9f, 0.4f), Random.value));
                break;
            case Kind.Mercury:
                Emit(sparks, at, Vector2.zero, Random.Range(0.2f, 0.4f) * u, 0.22f, new Color(0.7f, 0.92f, 1f, 0.6f));
                break;
        }
    }

    public static void Bubble(Vector2 pos, Color color)
    {
        float u = GameManager.Unit;
        color.a = 0.75f;
        Emit(smoke, pos, new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(0.6f, 1.6f)) * u, Random.Range(0.1f, 0.24f) * u, Random.Range(0.4f, 0.8f), color);
    }

    public static void BladeSpark(Vector2 pos, Color color)
    {
        float u = GameManager.Unit;
        Emit(stars, pos, Random.insideUnitCircle * (1.5f * u), Random.Range(0.2f, 0.5f) * u, Random.Range(0.15f, 0.35f), color, Random.Range(0f, 360f));
    }

    // Пылинки и угольки, медленно плывущие в воздухе лаборатории.
    public static void Mote(Vector2 pos, Color color, float size, float life)
    {
        Emit(smoke, pos, new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(0.2f, 0.7f)) * GameManager.Unit, size, life, color);
    }

    // ---------- Кирпичики ----------

    // Росчерк клинка: широкое цветное свечение и поверх — тонкая белая линия. Обе полосы быстро сужаются и гаснут.
    static void SlashMark(Vector2 pos, float angle, float radius, Color color)
    {
        float u = GameManager.Unit;
        color.a = 0.8f;
        var glow = NewSprite("SlashGlow", Art.Soft, color, OrderSlash);
        glow.transform.position = pos;
        glow.transform.rotation = Quaternion.Euler(0, 0, angle);
        glow.transform.localScale = new Vector3(radius * 3.4f, 0.6f * u, 1f);
        var glowFade = glow.AddComponent<Fade>();
        glowFade.life = 0.24f;
        glowFade.squash = true;

        Slash(pos, angle, radius * 6f, 0.5f * u, Color.white, 0.18f);
    }

    // Полоса с острыми концами вдоль взмаха: быстро сужается и гаснет.
    static void Slash(Vector2 pos, float angle, float length, float width, Color color, float life)
    {
        var slash = NewSprite("Slash", Art.Streak, color, OrderSlash + 1);
        slash.transform.position = pos;
        slash.transform.rotation = Quaternion.Euler(0, 0, angle);
        slash.transform.localScale = new Vector3(length * 0.5f, width * 2f, 1f);
        var fade = slash.AddComponent<Fade>();
        fade.life = life;
        fade.squash = true;
    }

    // Веер лучей: раскрывается, поворачиваясь, и гаснет.
    static void Burst(Vector2 pos, float radius, Color color, float life, float spin)
    {
        color.a = 0.5f;
        var go = NewSprite("Burst", Art.Rays, color, OrderParticles - 1);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        go.transform.localScale = Vector3.one * (radius * 0.55f);
        var fade = go.AddComponent<Fade>();
        fade.life = life;
        fade.grow = 2f;
        fade.spin = spin;
    }

    // Ударная волна: плотное кольцо, которое быстро расходится от точки удара.
    static void Shock(Vector2 pos, float radius, Color color, float life)
    {
        color.a = 0.7f;
        var go = NewSprite("Shock", Art.Shock, color, OrderParticles);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * (radius * 0.3f);
        var fade = go.AddComponent<Fade>();
        fade.life = life;
        fade.grow = 1f / 0.3f;
    }

    // Короткая вспышка света: мягкое пятно, которое раздувается и гаснет.
    static void Flash(Vector2 pos, float radius, Color color, float life)
    {
        color.a = 0.6f;
        var go = NewSprite("Flash", Art.Soft, color, OrderParticles - 1);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * (radius * 0.55f);
        var fade = go.AddComponent<Fade>();
        fade.life = life;
        fade.grow = 1.9f;
    }

    // Струя сока вдоль линии разреза: капли вылетают в обе стороны по ходу клинка.
    static void Spray(SlimeDef def, Vector2 pos, Vector2 dir, float radius)
    {
        float u = GameManager.Unit;
        for (int i = 0; i < 10; i++)
        {
            Vector2 along = dir * ((i % 2 == 0 ? 1f : -1f) * Random.Range(10f, 22f) * u);
            Emit(drops, pos + Random.insideUnitCircle * (radius * 0.3f), along + Random.insideUnitCircle * (2.5f * u),
                Random.Range(0.14f, 0.32f) * u, Random.Range(0.25f, 0.5f), Color.Lerp(def.body, Color.white, Random.value * 0.5f));
        }
    }

    static void Pulse(Vector2 pos, float radius, Color color, float life)
    {
        color.a = 0.8f;
        var ring = NewSprite("Pulse", Art.Ring, color, OrderParticles);
        ring.transform.position = pos;
        ring.transform.localScale = Vector3.one * (radius * 0.35f);
        var fade = ring.AddComponent<Fade>();
        fade.life = life;
        fade.grow = radius / (radius * 0.35f);
    }

    static void Splat(Vector2 pos, float radius, Color color, bool fizz)
    {
        // Клякса темнее и прозрачнее самого слайма: на её фоне живые цели должны читаться сразу.
        color = new Color(color.r * 0.8f, color.g * 0.8f, color.b * 0.8f, 0.42f);
        var go = NewSprite("Splat", Art.Splats[Random.Range(0, Art.Splats.Length)], color, OrderSplat);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        var splat = go.AddComponent<Splat>();
        splat.size = radius * Random.Range(1.7f, 2.4f);
        splat.fizz = fizz;
    }

    static void Bolt(Vector2 a, Vector2 b)
    {
        var go = new GameObject("Bolt");
        go.transform.SetParent(Root, false);
        var bolt = go.AddComponent<Bolt>();
        bolt.a = a;
        bolt.b = b;
    }

    static Vector2 Fan(Vector2 n, Vector2 dir)
    {
        return n * Random.Range(-1f, 1f) + dir * Random.Range(-0.35f, 0.9f) + Random.insideUnitCircle * 0.35f;
    }

    static Piece NewPiece(string name, Sprite sprite, Color color, Vector2 pos, float scale, int order = OrderParticles)
    {
        var go = NewSprite(name, sprite, color, order);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        go.transform.localScale = Vector3.one * scale;
        return go.AddComponent<Piece>();
    }

    public static GameObject NewSprite(string name, Sprite sprite, Color color, int order, Transform parent = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent != null ? parent : Root, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        sr.sharedMaterial = Art.Mat;
        return go;
    }

    static void Emit(ParticleSystem ps, Vector2 pos, Vector2 vel, float size, float life, Color color, float rotation = 0f)
    {
        if (ps == null) return;
        var p = new ParticleSystem.EmitParams
        {
            position = pos,
            velocity = vel,
            startSize = size,
            startLifetime = life,
            startColor = color,
            rotation = rotation
        };
        ps.Emit(p, 1);
    }

    // Система сама ничего не испускает: частицы выдаются поштучно через Emit.
    static ParticleSystem MakeParticles(string name, Material material, float gravity, AnimationCurve sizeOverLife)
    {
        var go = new GameObject(name);
        go.transform.SetParent(Root, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = gravity * GameManager.Unit;
        main.maxParticles = 1500;
        main.startSpeed = 0f;

        var emission = ps.emission;
        emission.enabled = false;
        var shape = ps.shape;
        shape.enabled = false;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, sizeOverLife);

        var color = ps.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingOrder = OrderParticles;

        ps.Play();
        return ps;
    }
}

// Отлетающий кусок: половинка, осколок, монета, капля ртути. Летит по баллистике и удаляется за краем экрана.
public class Piece : MonoBehaviour
{
    public Vector2 vel;
    public float spin, gravity;
    public float life;      // 0 — живёт, пока не упадёт за экран
    public float flip;      // скорость «кувырка» монеты
    public bool shrink;
    public Color sparkle;   // если задан — кусок оставляет за собой искры

    SpriteRenderer sr;
    Vector3 scale;
    float age, sparkTimer;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        scale = transform.localScale;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        age += dt;
        vel.y -= gravity * dt;
        transform.position += (Vector3)(vel * dt);
        transform.Rotate(0, 0, spin * dt);

        float k = life > 0f ? Mathf.Clamp01(age / life) : 0f;
        float s = shrink ? 1f - k * k : 1f;
        transform.localScale = new Vector3(scale.x * s * (flip > 0f ? Mathf.Cos(age * flip) : 1f), scale.y * s, 1f);
        if (life > 0f && !shrink)
        {
            Color c = sr.color;
            c.a = Mathf.Clamp01((1f - k) * 3f);
            sr.color = c;
        }

        if (sparkle.a > 0f)
        {
            sparkTimer -= dt;
            if (sparkTimer <= 0f)
            {
                sparkTimer = 0.04f;
                Fx.BladeSpark(transform.position, sparkle);
            }
        }

        if (k >= 1f || transform.position.y < GameManager.View.yMin - 3f * GameManager.Unit) Destroy(gameObject);
    }
}

// Плавно гасит спрайт и удаляет объект; умеет сплющивать (след удара) и раздувать (кольцо волны).
public class Fade : MonoBehaviour
{
    public float life = 1f;
    public bool squash;
    public float grow = 1f;
    public float spin;      // градусов в секунду

    SpriteRenderer sr;
    Color color;
    Vector3 scale;
    float age;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        color = sr.color;
        scale = transform.localScale;
    }

    void Update()
    {
        age += Time.deltaTime;
        float k = Mathf.Clamp01(age / life);
        sr.color = new Color(color.r, color.g, color.b, color.a * (1f - k * k));
        if (squash) transform.localScale = new Vector3(scale.x * (1f + 0.3f * k), scale.y * (1f - k), 1f);
        else if (grow != 1f) transform.localScale = scale * Mathf.Lerp(1f, grow, 1f - (1f - k) * (1f - k));
        if (spin != 0f) transform.Rotate(0f, 0f, spin * Time.deltaTime);
        if (k >= 1f) Destroy(gameObject);
    }
}

// Неоновая клякса на стене: шлёпается, пускает потёки вниз и медленно гаснет.
public class Splat : MonoBehaviour
{
    const float Life = 3f;
    const int Crowd = 10;   // когда клякс больше, старые гаснут быстрее — стена не зарастает

    static int alive;

    public float size = 1f;
    public bool fizz;

    SpriteRenderer sr;
    SpriteRenderer[] drips;
    float[] lengths;
    Color color;
    float age, fizzTimer;

    void Awake()
    {
        alive++;
    }

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        color = sr.color;
        transform.localScale = Vector3.one * (size * 0.5f);

        int count = Random.Range(2, 5);
        drips = new SpriteRenderer[count];
        lengths = new float[count];
        for (int i = 0; i < count; i++)
        {
            var go = Fx.NewSprite("Drip", Art.Drip, color, Fx.OrderSplat);
            Vector2 offset = new Vector2(Random.Range(-0.55f, 0.55f), Random.Range(-0.35f, 0.1f)) * size;
            go.transform.position = (Vector2)transform.position + offset;
            go.transform.localScale = new Vector3(size * Random.Range(0.35f, 0.7f), 0.01f, 1f);
            drips[i] = go.GetComponent<SpriteRenderer>();
            lengths[i] = size * Random.Range(0.6f, 1.7f);
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        age += dt * (alive > Crowd && age > 0.6f ? 1f + 0.35f * (alive - Crowd) : 1f);
        float k = Mathf.Clamp01(age / Life);
        Color c = new Color(color.r, color.g, color.b, color.a * (1f - k * k));
        sr.color = c;

        // Шлепок: клякса за мгновение расплывается до полного размера.
        float pop = 1f - Mathf.Exp(-age * 28f);
        transform.localScale = Vector3.one * (size * (0.5f + 0.5f * pop));

        float run = 1f - Mathf.Exp(-age * 1.1f);
        for (int i = 0; i < drips.Length; i++)
        {
            if (drips[i] == null) continue;
            drips[i].color = c;
            Vector3 s = drips[i].transform.localScale;
            drips[i].transform.localScale = new Vector3(s.x, Mathf.Max(0.01f, lengths[i] * run), 1f);
        }

        if (fizz && age < 1.3f)
        {
            fizzTimer -= dt;
            if (fizzTimer <= 0f)
            {
                fizzTimer = 0.05f;
                Fx.Bubble((Vector2)transform.position + Random.insideUnitCircle * (size * 0.6f), color);
            }
        }

        if (k >= 1f) Destroy(gameObject);
    }

    void OnDestroy()
    {
        alive--;
        if (drips == null) return;
        foreach (var d in drips)
            if (d != null) Destroy(d.gameObject);
    }
}

// Молния комбо: яркая ломаная через точки срезов. Слегка дрожит, сужается и гаснет за полсекунды.
public class Link : MonoBehaviour
{
    const float Life = 0.5f;

    public Vector2[] points;
    public Color color;

    LineRenderer glow, core;
    float age;

    void Start()
    {
        glow = Make("Glow", Fx.OrderSlash - 1);
        core = Make("Core", Fx.OrderSlash);
        Draw();
    }

    LineRenderer Make(string name, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var line = go.AddComponent<LineRenderer>();
        line.sharedMaterial = Art.Mat;
        line.useWorldSpace = true;
        line.positionCount = points.Length;
        line.numCornerVertices = 4;
        line.numCapVertices = 4;
        line.sortingOrder = order;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return line;
    }

    void Update()
    {
        age += Time.unscaledDeltaTime;
        if (age >= Life)
        {
            Destroy(gameObject);
            return;
        }
        Draw();
    }

    void Draw()
    {
        float u = GameManager.Unit;
        float k = age / Life, fade = 1f - k * k;
        for (int i = 0; i < points.Length; i++)
        {
            Vector3 p = points[i] + Random.insideUnitCircle * (0.1f * u * (1f - k));
            glow.SetPosition(i, p);
            core.SetPosition(i, p);
        }
        glow.widthMultiplier = 0.9f * u * (1f - 0.6f * k);
        core.widthMultiplier = 0.22f * u * (1f - 0.7f * k);
        glow.startColor = glow.endColor = new Color(color.r, color.g, color.b, 0.5f * fade);
        core.startColor = core.endColor = new Color(1f, 1f, 1f, fade);
    }
}

// Электрический микро-разряд: ломаная линия, которая дрожит и гаснет за доли секунды.
public class Bolt : MonoBehaviour
{
    const float Life = 0.13f;
    const int Points = 7;

    public Vector2 a, b;

    LineRenderer line;
    float age;

    void Start()
    {
        line = gameObject.AddComponent<LineRenderer>();
        line.sharedMaterial = Art.Mat;
        line.useWorldSpace = true;
        line.positionCount = Points;
        line.sortingOrder = Fx.OrderSlash;
        line.widthMultiplier = 0.09f * GameManager.Unit;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Jitter();
    }

    void Update()
    {
        age += Time.unscaledDeltaTime;
        if (age >= Life)
        {
            Destroy(gameObject);
            return;
        }
        Jitter();
        Color c = new Color(0.8f, 0.96f, 1f, 1f - age / Life);
        line.startColor = c;
        line.endColor = c;
    }

    void Jitter()
    {
        Vector2 n = new Vector2(-(b - a).y, (b - a).x) * 0.18f;
        for (int i = 0; i < Points; i++)
        {
            float t = i / (Points - 1f);
            float wobble = i == 0 || i == Points - 1 ? 0f : Random.Range(-1f, 1f);
            line.SetPosition(i, Vector2.Lerp(a, b, t) + n * wobble);
        }
    }
}
