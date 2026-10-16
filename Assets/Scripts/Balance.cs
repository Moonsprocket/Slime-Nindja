using System;
using UnityEngine;

// Рисунок волны: как именно слаймы вылетают из котла.
public enum Pattern
{
    Scatter,    // россыпь: из случайных мест разом
    Stream,     // очередь: по одному через короткие промежутки, волной вдоль котла
    Cross,      // ножницы: пары летят навстречу и сходятся в одной точке
    Row,        // строй: ровная шеренга
    Column,     // змейка: друг за другом по одной дуге
    Fan,        // веер: из одной точки в стороны
    Arc,        // радуга или чаша: шеренга с вершинами по дуге
    Stairs,     // лесенка: шеренга, взлетающая по очереди
    Figure      // фигура: треугольник, ромб или кольцо, летящие как одно целое
}

// Когда рисунок входит в игру, для скольких слаймов он годится и как часто выпадает.
[Serializable]
public class PatternRule
{
    public Pattern pattern;
    [Tooltip("С какой секунды попытки рисунок доступен")] public float from;
    [Min(1)] public int minCount = 1;
    [Min(1)] public int maxCount = 7;
    [Min(0f)] public float weight = 1f;
}

// Знакомство с новым объектом: он выходит один, в спокойный момент, с подсказкой.
[Serializable]
public class Intro
{
    [Tooltip("Секунда попытки")] public float at;
    public Kind kind;
    [Tooltip("Больше нуля — вылетает шеренга из стольких слаймов")] public int row;
    [Tooltip("Король под конвоем бомб")] public bool escort;
    public string hint;
}

// Звание игрока: открывается, когда рекорд достигает порога. Это цель на следующую попытку.
[Serializable]
public class Rank
{
    public string name;
    [Min(0)] public int score;
    [Tooltip("Цвет звания: надпись и полоса пути в меню. Значок звания — картинка Art/Interface/RankN")]
    public Color color = Color.white;
}

// Все числа баланса в одном ассете (Assets/Resources/Balance): правила, кривые сложности, рисунки волн
// и порядок знакомства с объектами. Меняются в инспекторе, без правки кода.
[CreateAssetMenu(fileName = "Balance", menuName = "Алхимия/Баланс")]
public class Balance : ScriptableObject
{
    [Header("Правила")]
    [Tooltip("Столько слаймов одним взмахом — уже комбо")] public int comboMin = 3;
    [Tooltip("А столько — перегрузка реторты")] public int frenzyCombo = 4;
    [Tooltip("Бонус за каждый слайм в комбо")] public int comboBonus = 10;
    [Tooltip("Пауза между срезами, после которой взмах считается законченным")] public float comboWindow = 0.35f;
    [Tooltip("Длительность перегрузки: она не продлевается")] public float frenzySeconds = 8f;
    [Tooltip("Передышка после перегрузки, пока новая не начнётся")] public float frenzyRest = 10f;
    [Tooltip("Длительность замедления после золотого короля")] public float slowSeconds = 3.2f;
    [Range(0.1f, 1f)] public float slowScale = 0.4f;
    [Tooltip("Слаймы, упавшие в один миг, стоят одной жизни, а не всех трёх")] public float missGrace = 0.6f;

    [Header("Сложность: по горизонтали секунда попытки")]
    [Tooltip("Сколько секунд слайм в воздухе")]
    public AnimationCurve airTime = Line(0f, 2.7f, 45f, 2.5f, 100f, 2.3f, 170f, 2.05f, 270f, 1.6f);
    [Tooltip("Пауза между волнами, секунды")]
    public AnimationCurve interval = Line(0f, 1.4f, 6f, 1.3f, 45f, 1.2f, 100f, 1.1f, 170f, 0.95f, 270f, 0.7f);
    [Tooltip("Средний размер волны. Дробная часть — шанс на «плюс один»: при 1,3 на семь одиночек приходится три пары")]
    public AnimationCurve load = Line(0f, 1f, 6f, 1.5f, 15f, 2f, 45f, 2.6f, 100f, 3.3f, 170f, 4.4f, 270f, 6f);
    [Tooltip("Шанс бомбы в волне")]
    public AnimationCurve bombChance = Line(10f, 0.15f, 45f, 0.25f, 100f, 0.32f, 170f, 0.42f, 270f, 0.55f);
    [Tooltip("Шанс второй бомбы")]
    public AnimationCurve secondBomb = Line(90f, 0f, 270f, 0.3f);
    [Tooltip("Какую долю полёта бомба может провести вплотную к слайму")]
    public AnimationCurve crowding = Line(10f, 0.1f, 270f, 0.5f);
    [Tooltip("Вес простого кислотного слайма при выборе вида: чем меньше, тем чаще редкие")]
    public AnimationCurve plainWeight = Line(10f, 10f, 270f, 5f);
    [Tooltip("Шанс, что король вылетит под конвоем бомб")]
    public AnimationCurve escortChance = Line(90f, 0.35f, 270f, 0.7f);
    [Tooltip("Золотой король вылетает только в гущу событий: когда в воздухе вместе с новой волной не меньше стольких объектов (бомба считается за двоих). Порог растёт вместе с плотностью волн: король приходит на пике, а не в каждую волну")]
    public AnimationCurve kingCrowd = Line(0f, 5f, 100f, 6f, 170f, 7.5f, 270f, 9f);
    [Tooltip("После этой секунды темп продолжает расти сверх кривых: попытка не должна тянуться бесконечно")]
    public float peak = 270f;

    [Header("Ритм")]
    [Tooltip("Пауза между волнами случайно меняется в этих пределах, чтобы выбросы не шли как по метроному")]
    public Vector2 restSpread = new Vector2(0.65f, 1.25f);
    [Tooltip("Шанс серии: две-три волны почти подряд, затем длинная передышка")]
    [Range(0f, 1f)] public float phraseChance = 0.3f;
    [Tooltip("Шанс золотого короля в тесной волне (порог тесноты — кривая King Crowd в разделе сложности). За каждый объект сверх порога шанс растёт ещё на половину этого числа")]
    [Range(0f, 1f)] public float kingChance = 0.14f;
    [Tooltip("Минимальный перерыв между королями, секунды")]
    public float kingCooldown = 25f;

    [Header("Рисунки волн")]
    public PatternRule[] patterns =
    {
        new PatternRule { pattern = Pattern.Scatter, from = 0f, minCount = 1, maxCount = 7, weight = 0.6f },
        new PatternRule { pattern = Pattern.Stream, from = 4f, minCount = 2, maxCount = 5, weight = 1f },
        new PatternRule { pattern = Pattern.Cross, from = 7f, minCount = 2, maxCount = 4, weight = 0.9f },
        new PatternRule { pattern = Pattern.Row, from = 10f, minCount = 2, maxCount = 6, weight = 1f },
        new PatternRule { pattern = Pattern.Column, from = 13f, minCount = 2, maxCount = 5, weight = 0.7f },
        new PatternRule { pattern = Pattern.Fan, from = 17f, minCount = 3, maxCount = 7, weight = 1f },
        new PatternRule { pattern = Pattern.Arc, from = 22f, minCount = 3, maxCount = 7, weight = 0.9f },
        new PatternRule { pattern = Pattern.Stairs, from = 28f, minCount = 3, maxCount = 6, weight = 0.8f },
        new PatternRule { pattern = Pattern.Figure, from = 34f, minCount = 3, maxCount = 7, weight = 0.9f },
    };

    [Header("Знакомство с объектами")]
    public Intro[] intros =
    {
        new Intro { at = 10f, kind = Kind.Bomb, hint = "Бомбу не трогай!" },
        new Intro { at = 15f, kind = Kind.Acid, row = 3, hint = "Три одним взмахом — комбо!" },
        new Intro { at = 20f, kind = Kind.Magma },
        new Intro { at = 27f, kind = Kind.King, hint = "Режь короля — время замедлится!" },
        new Intro { at = 36f, kind = Kind.Blue, hint = "Делится надвое — бей дважды!" },
        new Intro { at = 48f, kind = Kind.Geode, hint = "Жеода: скорлупа, потом желе" },
        new Intro { at = 60f, kind = Kind.Mercury },
        new Intro { at = 95f, kind = Kind.King, escort = true, hint = "Точнее! Не задень эскорт" },
    };

    [Header("Звания: цель на следующую попытку")]
    public Rank[] ranks =
    {
        new Rank { name = "Ученик", score = 0, color = new Color(0.7f, 0.86f, 0.8f) },
        new Rank { name = "Подмастерье", score = 1000, color = new Color(0.95f, 0.64f, 0.44f) },
        new Rank { name = "Зельевар", score = 3000, color = new Color(0.5f, 0.92f, 0.45f) },
        new Rank { name = "Алхимик", score = 7000, color = new Color(0.4f, 0.78f, 1f) },
        new Rank { name = "Магистр", score = 15000, color = new Color(0.74f, 0.58f, 1f) },
        new Rank { name = "Архимаг", score = 30000, color = new Color(1f, 0.47f, 0.64f) },
        new Rank { name = "Легенда лаборатории", score = 60000, color = new Color(0.84f, 0.95f, 1f) },
    };

    [Header("Комбо: ступени в главном меню")]
    [Tooltip("Комбо такого размера открывает следующую ступень — это цель на будущие попытки")]
    public int[] comboGoals = { 5, 10, 15, 20, 25 };

    [Header("Серия без промаха")]
    [Tooltip("Вехи серии: на каждой игрок получает объявление и бонус, а полоса серии переходит на следующий уровень")]
    public int[] streakGoals = { 10, 25, 50, 100, 200, 350, 500 };
    [Tooltip("После последней вехи бонус идёт за каждые столько слаймов")]
    [Min(1)] public int streakEvery = 50;
    [Tooltip("Бонус за веху: столько очков за каждый слайм серии")]
    public int streakBonus = 10;

    void OnEnable()
    {
        if (streakGoals == null || streakGoals.Length <= 4)
        {
            streakGoals = new[] { 10, 25, 50, 100, 200, 350, 500 };
        }
    }

    // Уровень серии: сколько вех уже взято.
    public int StreakTier(int streak)
    {
        int tier = 0;
        foreach (int goal in streakGoals)
            if (streak >= goal) tier++;
        return tier;
    }

    // Отрезок, по которому сейчас идёт серия: от взятой вехи до следующей. После последней вехи — круги по streakEvery.
    public void StreakSpan(int streak, out int from, out int to)
    {
        from = 0;
        foreach (int goal in streakGoals)
        {
            if (goal > streak)
            {
                to = goal;
                return;
            }
            from = goal;
        }
        from += (streak - from) / streakEvery * streakEvery;
        to = from + streakEvery;
    }

    // Ближайшая веха серии, которая больше этого числа.
    public int NextStreakGoal(int streak)
    {
        int from, to;
        StreakSpan(streak, out from, out to);
        return to;
    }

    // Взята ли веха ровно на этом слайме.
    public bool IsStreakGoal(int streak)
    {
        int last = streakGoals[streakGoals.Length - 1];
        return System.Array.IndexOf(streakGoals, streak) >= 0 || streak > last && (streak - last) % streakEvery == 0;
    }

    // Сколько ступеней комбо открыто с таким лучшим комбо.
    public int ComboStep(int combo)
    {
        int step = 0;
        foreach (int goal in comboGoals)
            if (combo >= goal) step++;
        return step;
    }

    // Номер высшего звания, доступного с таким счётом.
    public int RankOf(int score)
    {
        int rank = 0;
        for (int i = 0; i < ranks.Length; i++)
            if (score >= ranks[i].score) rank = i;
        return rank;
    }

    static Balance current;

    // Ассет из Resources; если его нет, работают значения по умолчанию, записанные выше.
    public static Balance Current
    {
        get
        {
            if (current == null) current = Resources.Load<Balance>("Balance");
            if (current == null) current = CreateInstance<Balance>();
            return current;
        }
    }

    // Ломаная по парам (секунда, значение): между точками значение меняется равномерно.
    static AnimationCurve Line(params float[] points)
    {
        int n = points.Length / 2;
        var keys = new Keyframe[n];
        for (int i = 0; i < n; i++)
        {
            float before = i > 0 ? (points[2 * i + 1] - points[2 * i - 1]) / (points[2 * i] - points[2 * i - 2]) : 0f;
            float after = i < n - 1 ? (points[2 * i + 3] - points[2 * i + 1]) / (points[2 * i + 2] - points[2 * i]) : 0f;
            keys[i] = new Keyframe(points[2 * i], points[2 * i + 1], before, after);
        }
        return new AnimationCurve(keys);
    }
}
