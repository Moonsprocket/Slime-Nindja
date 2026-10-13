using UnityEngine;

// Живая сцена лаборатории. Слои снизу вверх: каменная стена, полки с утварью по бокам, (слаймы), борт котла с зельем.
// Стена, полки и борт — картинки художника в Assets/Resources/Art/Scene. Без полок и борта сцена обходится
// стеной и медной полосой из формул. Поверх слоёв — живые мелочи: огонь фонарей мерцает, зелье бурлит, в воздухе пыль.
public class Backdrop : MonoBehaviour
{
    const float RimHeight = 1.55f;        // высота борта котла над нижним краем экрана
    const float Overscan = 1.08f;         // полки и борт чуть шире экрана: тряска камеры не открывает пустоту
    const float LayerAspect = 16f / 9f;   // слои художника нарисованы под экран 16:9

    static readonly Color Lamp = new Color(1f, 0.6f, 0.2f);
    static readonly Color Brew = new Color(0.35f, 1f, 0.4f);
    static readonly Color Gold = new Color(1f, 0.8f, 0.25f);
    static readonly Color Copper = new Color(0.2f, 0.11f, 0.09f);   // цвет нижней кромки борта: им закрыта щель под ним
    // Где на картинке полок висят фонари: смещение от середины её наружного края, в долях высоты картинки.
    static readonly Vector2 LampLeft = new Vector2(0.278f, 0.324f), LampRight = new Vector2(-0.282f, 0.296f);

    Transform wall, rim, brim, below, shelfLeft, shelfRight;
    SpriteRenderer[] lamps;
    SpriteRenderer brew;
    float moteTimer, bubbleTimer, heat;

    public static Backdrop Create()
    {
        var go = new GameObject("Backdrop");
        go.transform.SetParent(Fx.Root, false);
        return go.AddComponent<Backdrop>();
    }

    void Awake()
    {
        wall = Fx.NewSprite("Wall", Art.Wall, Color.white, Fx.OrderWall, transform).transform;
        // Полки стоят перед стеной и кляксами на ней, но за слаймами.
        if (Art.ShelvesLeft != null)
        {
            shelfLeft = Fx.NewSprite("ShelvesLeft", Art.ShelvesLeft, Color.white, Fx.OrderSplat + 5, transform).transform;
            shelfRight = Fx.NewSprite("ShelvesRight", Art.ShelvesRight, Color.white, Fx.OrderSplat + 5, transform).transform;
        }
        // Борт котла рисуется поверх слаймов: они вылетают из-за него и тонут за ним.
        if (Art.Brim != null)
        {
            brim = Fx.NewSprite("Brim", Art.Brim, Color.white, Fx.OrderRim, transform).transform;
            below = Fx.NewSprite("Below", Art.White, Copper, Fx.OrderRim, transform).transform;
        }
        else rim = Fx.NewSprite("Rim", Art.Rim, Color.white, Fx.OrderRim, transform).transform;

        brew = Fx.NewSprite("Brew", Art.Soft, Color.clear, Fx.OrderAmbient, transform).GetComponent<SpriteRenderer>();
        lamps = new SpriteRenderer[2];
        for (int i = 0; i < lamps.Length; i++)
            lamps[i] = Fx.NewSprite("Lamp", Art.Soft, Color.clear, Fx.OrderSplat + 6, transform).GetComponent<SpriteRenderer>();
        Fit();
    }

    void Update()
    {
        Fit();
        float dt = Time.deltaTime;
        float time = Time.time;
        float u = GameManager.Unit;
        Rect view = GameManager.View;
        var game = GameManager.Instance;

        // В перегрузке реторты зелье раскаляется золотом и бурлит быстрее.
        heat = Mathf.MoveTowards(heat, game.FrenzyLeft > 0f ? 1f : 0f, Time.unscaledDeltaTime * 3f);
        Color liquid = Color.Lerp(Brew, Gold, heat);
        float boil = 0.5f + 0.5f * Mathf.Sin(time * (2f + 5f * heat));
        brew.color = new Color(liquid.r, liquid.g, liquid.b, 0.16f + 0.08f * boil + 0.14f * heat);

        // Огонь в фонарях живой: свет вокруг них неровно мерцает.
        for (int i = 0; i < lamps.Length; i++)
        {
            float flicker = 0.75f + 0.25f * Mathf.PerlinNoise(time * 7f, i * 9.3f);
            lamps[i].color = new Color(Lamp.r, Lamp.g, Lamp.b, 0.2f * flicker);
            lamps[i].transform.localScale = Vector3.one * (view.height * 0.36f * (0.9f + 0.2f * flicker));
        }

        moteTimer -= dt;
        if (moteTimer <= 0f)
        {
            moteTimer = 0.12f;
            Vector2 at = new Vector2(Random.Range(view.xMin, view.xMax), Random.Range(view.yMin, view.yMax));
            Fx.Mote(at, new Color(1f, 0.85f, 0.6f, 0.22f), Random.Range(0.08f, 0.2f) * u, Random.Range(2f, 4f));
        }

        bubbleTimer -= dt;
        if (bubbleTimer <= 0f)
        {
            bubbleTimer = Mathf.Lerp(0.09f, 0.025f, heat);
            Vector2 at = new Vector2(Random.Range(view.xMin, view.xMax), view.yMin + RimHeight * u * Random.Range(0.8f, 1.1f));
            Fx.Bubble(at, liquid);
        }
    }

    void Fit()
    {
        Rect view = GameManager.View;
        float u = GameManager.Unit;
        // Стена заметно больше экрана: её края при тряске камеры не видны никогда.
        const float margin = 1.15f;
        Vector2 picture = Art.Wall.bounds.size;
        float cover = Mathf.Max(view.width / (picture.x / picture.y), view.height) * margin;
        wall.position = new Vector3(view.center.x, view.center.y, 5f);
        wall.localScale = new Vector3(cover, cover, 1f);

        // Каждая половина полок держится своего края экрана, поэтому при любом формате окна утварь стоит по бокам,
        // а середина остаётся свободной для слаймов.
        float layer = view.height * Overscan;
        float pad = 0.5f * (Overscan - 1f) * view.height * LayerAspect;
        Vector3 left = new Vector3(view.xMin - pad, view.center.y, 0f), right = new Vector3(view.xMax + pad, view.center.y, 0f);
        if (shelfLeft != null)
        {
            shelfLeft.position = left;
            shelfRight.position = right;
            shelfLeft.localScale = shelfRight.localScale = Vector3.one * layer;
            lamps[0].transform.position = left + (Vector3)(LampLeft * layer);
            lamps[1].transform.position = right + (Vector3)(LampRight * layer);
        }
        else
        {
            // У стены из формулы факелы нарисованы прямо на ней.
            for (int i = 0; i < lamps.Length; i++)
                lamps[i].transform.position = new Vector3(view.center.x + (i == 0 ? -0.64f : 0.64f) * cover, view.center.y + 0.26f * cover, 0f);
        }

        if (brim != null)
        {
            // Борт растянут по ширине с запасом, а по высоте стоит точно: зелье всегда на одной высоте.
            // Под ним — полоса того же цвета: если камеру тряхнёт вниз, пустоты не будет.
            brim.position = new Vector3(view.center.x, view.yMin, 0f);
            brim.localScale = new Vector3(view.width * Overscan / LayerAspect, view.height, 1f);
            below.position = new Vector3(view.center.x, view.yMin - 2f * u, 0f);
            below.localScale = new Vector3(view.width * 1.3f, 4f * u, 1f);
        }
        else
        {
            float rimWidth = Art.Rim.bounds.size.x;
            rim.position = new Vector3(view.center.x, view.yMin - 0.35f * u, 0f);
            rim.localScale = new Vector3(view.width * 1.15f / rimWidth, (RimHeight + 0.35f) * u, 1f);
        }

        brew.transform.position = new Vector3(view.center.x, view.yMin, 0f);
        brew.transform.localScale = new Vector3(view.width * 0.75f, view.height * 0.3f, 1f);
    }
}
