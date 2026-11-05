using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int world { get; private set; }
    public int stage { get; private set; }
    public int lives { get; private set; }
    public int coins { get; private set; }
    public int score { get; private set; }

    public bool showingIntro { get; private set; }

    [Header("Mejoras")]
    public string gameOverScene = "GameOver";
    public float introDuration = 2f;
    public bool showHud = true;

    [Header("Contadores")]
    [Tooltip("Tiempo inicial de cada nivel")]
    public int levelTime = 400;
    [Tooltip("Tiempo inicial de los castillos (nivel X-4)")]
    public int castleTime = 300;
    [Tooltip("Segundos reales que dura una unidad del reloj (en el original ~0.4)")]
    public float secondsPerTick = 0.4f;
    [Tooltip("Puntos por cada unidad de tiempo que sobra al terminar el nivel")]
    public int pointsPerTimeLeft = 50;
    public int hurryTime = 100;

    // estadísticas de la partida (puntaje en base a monedas y enemigos)
    public int time { get; private set; }
    public bool timerRunning { get; private set; }
    public int enemiesDefeated { get; private set; }
    public int coinsCollected { get; private set; }

    private float tickTimer;
    private bool hurryWarned;
    private bool tallying;

    private bool started;
    private bool victory;
    private string message;
    private float messageTimer;

    private Texture2D blackTexture;
    private Texture2D coinTexture;
    private GUIStyle textStyle;
    private GUIStyle bigStyle;

    private void Awake()
    {
        if (Instance != null) {
            DestroyImmediate(gameObject);
        } else {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void Start()
    {
        Application.targetFrameRate = 60;

        // Permite darle Play directamente a cualquier escena de nivel (1-2, 2-2, 2-4...)
        // sin que siempre te mande a la 1-1.
        string sceneName = SceneManager.GetActiveScene().name;

        if (TryParseLevel(sceneName, out int w, out int s))
        {
            world = w;
            stage = s;
            lives = 3;
            coins = 0;
            score = 0;
            enemiesDefeated = 0;
            coinsCollected = 0;
            StartIntro();
            StartLevelTimer();
        }
        else if (sceneName != gameOverScene)
        {
            NewGame();
        }

        started = true;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!started) {
            return;
        }

        Time.timeScale = 1f;

        if (TryParseLevel(scene.name, out _, out _))
        {
            StartIntro();
            StartLevelTimer();
        }
        else
        {
            timerRunning = false;
        }
    }

    public static bool TryParseLevel(string sceneName, out int world, out int stage)
    {
        world = 0;
        stage = 0;

        string[] parts = sceneName.Split('-');

        return parts.Length == 2 && int.TryParse(parts[0], out world) && int.TryParse(parts[1], out stage);
    }

    public void NewGame()
    {
        lives = 3;
        coins = 0;
        score = 0;
        enemiesDefeated = 0;
        coinsCollected = 0;
        victory = false;

        LoadLevel(1, 1);
    }

    public void GameOver()
    {
        timerRunning = false;
        AudioManager.StopMusic();
        SceneManager.LoadScene(gameOverScene);
    }

    public void LoadLevel(int world, int stage)
    {
        this.world = world;
        this.stage = stage;

        SceneManager.LoadScene($"{world}-{stage}");
    }

    public void NextLevel()
    {
        LoadLevel(world, stage + 1);
    }

    public void ResetLevel(float delay)
    {
        // Mario murió: el reloj se detiene
        timerRunning = false;

        CancelInvoke(nameof(ResetLevel));
        Invoke(nameof(ResetLevel), delay);
    }

    public void ResetLevel()
    {
        lives--;

        if (lives > 0) {
            LoadLevel(world, stage);
        } else {
            GameOver();
        }
    }

    public void AddCoin()
    {
        coins++;
        coinsCollected++;
        score += 200;
        AudioManager.Play(Sfx.Coin);

        if (coins == 100)
        {
            coins = 0;
            AddLife();
        }
    }

    public void AddLife()
    {
        lives++;
        AudioManager.Play(Sfx.OneUp);
    }

    public void AddScore(int amount)
    {
        score += amount;
    }

    // suma puntos y muestra el número flotando donde pasó (ej. al pisar un enemigo)
    public void AddScore(int amount, Vector3 worldPosition)
    {
        score += amount;
        ScorePopup.Show(amount, worldPosition);
    }

    // puntos por enemigo eliminado (pisado, con caparazón o con estrella)
    public void AddEnemyScore(int amount, Vector3 worldPosition)
    {
        enemiesDefeated++;
        AddScore(amount, worldPosition);
    }

    public void ShowMessage(string text, float duration)
    {
        message = text;
        messageTimer = duration;
    }

    // Se llama al tocar el hacha del castillo
    public void Victory()
    {
        if (!victory) {
            StartCoroutine(VictorySequence());
        }
    }

    private IEnumerator VictorySequence()
    {
        victory = true;
        timerRunning = false;

        AudioManager.StopMusic();
        AudioManager.Play(Sfx.Victory);

        yield return TallyTime();

        AddScore(5000);
        ShowMessage("¡EL CASTILLO DE BRASA ES TUYO!\nGracias por jugar", 7f);

        yield return new WaitForSeconds(7.5f);

        NewGame();
    }

    // ---------- reloj del nivel ----------

    private void StartLevelTimer()
    {
        time = stage == 4 ? castleTime : levelTime;
        tickTimer = 0f;
        hurryWarned = false;
        tallying = false;
        timerRunning = true;
    }

    public void StopTimer()
    {
        timerRunning = false;
    }

    // al terminar el nivel, el tiempo que sobra se convierte en puntos
    public IEnumerator TallyTime()
    {
        timerRunning = false;
        tallying = true;

        int step = 0;

        while (time > 0)
        {
            int amount = Mathf.Min(time, 3);
            time -= amount;
            score += amount * pointsPerTimeLeft;

            if (step++ % 3 == 0) {
                AudioManager.Play(Sfx.Bump);
            }

            yield return new WaitForSeconds(0.016f);
        }

        tallying = false;
    }

    private void UpdateTimer()
    {
        if (!timerRunning || showingIntro || Time.timeScale == 0f) {
            return;
        }

        tickTimer += Time.deltaTime;

        while (tickTimer >= secondsPerTick && time > 0)
        {
            tickTimer -= secondsPerTick;
            time--;

            if (time == hurryTime && !hurryWarned)
            {
                hurryWarned = true;
                AudioManager.Play(Sfx.PowerUpAppear);
                ShowMessage("¡APÚRATE!", 2f);
            }
        }

        if (time <= 0) {
            TimeUp();
        }
    }

    private void TimeUp()
    {
        timerRunning = false;
        ShowMessage("¡SE ACABÓ EL TIEMPO!", 3f);

        GameObject go = GameObject.FindWithTag("Player");
        Player player = go != null ? go.GetComponent<Player>() : null;

        if (player != null && !player.dead) {
            player.Death();
        } else {
            ResetLevel(3f);
        }
    }

    // ---------- pantalla de inicio de nivel ----------

    private void StartIntro()
    {
        if (introDuration <= 0f) {
            return;
        }

        StopCoroutine(nameof(Intro));
        StartCoroutine(nameof(Intro));
    }

    private IEnumerator Intro()
    {
        showingIntro = true;
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(introDuration);

        Time.timeScale = 1f;
        showingIntro = false;
    }

    // ---------- HUD ----------

    private void Update()
    {
        if (messageTimer > 0f) {
            messageTimer -= Time.unscaledDeltaTime;
        }

        UpdateTimer();
    }

    private void OnGUI()
    {
        if (!showHud || SceneManager.GetActiveScene().name == gameOverScene) {
            return;
        }

        if (blackTexture == null)
        {
            blackTexture = new Texture2D(1, 1);
            blackTexture.SetPixel(0, 0, Color.black);
            blackTexture.Apply();
        }

        if (coinTexture == null) {
            coinTexture = CreateCoinTexture();
        }

        int size = Mathf.Max(12, Screen.height / 28);

        if (textStyle == null)
        {
            textStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter };
            bigStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        }

        textStyle.fontSize = size;
        bigStyle.fontSize = size * 2;

        if (showingIntro) {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), blackTexture);
        }

        bool inLevel = TryParseLevel(SceneManager.GetActiveScene().name, out _, out _);
        float column = Screen.width / 5f;
        float top = size * 0.5f;

        // 1. puntaje
        Label(new Rect(0, top, column, size * 3), $"MARIO\n{score:000000}", textStyle, Color.white);

        // 2. monedas (con ícono de moneda que parpadea)
        Label(new Rect(column, top, column, size * 3), $"MONEDAS\n   x{coins:00}", textStyle, Color.white);
        float blink = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 6f);
        Color old = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, blink);
        GUI.DrawTexture(new Rect(column + column / 2f - size * 1.6f, top + size * 1.15f, size * 0.9f, size * 0.9f), coinTexture);
        GUI.color = old;

        // 3. mundo
        Label(new Rect(column * 2, top, column, size * 3), $"MUNDO\n{world}-{stage}", textStyle, Color.white);

        // 4. tiempo restante (se pone rojo y parpadea cuando queda poco)
        string timeText = inLevel && !showingIntro ? $"{time:000}" : "";
        bool hurry = inLevel && time <= hurryTime && !tallying && timerRunning;
        Color timeColor = hurry && Mathf.Repeat(Time.unscaledTime, 0.5f) < 0.25f ? new Color(1f, 0.3f, 0.25f) : Color.white;
        Label(new Rect(column * 3, top, column, size * 3), "TIEMPO\n" + timeText, textStyle, timeColor);

        // 5. vidas
        Label(new Rect(column * 4, top, column, size * 3), $"VIDAS\n{lives}", textStyle, Color.white);

        if (showingIntro) {
            Label(new Rect(0, 0, Screen.width, Screen.height), $"MUNDO {world}-{stage}\n\nMARIO  x  {lives}", bigStyle, Color.white);
        }

        if (messageTimer > 0f && !string.IsNullOrEmpty(message)) {
            Label(new Rect(0, Screen.height * 0.25f, Screen.width, Screen.height * 0.3f), message, bigStyle, Color.white);
        }
    }

    private static void Label(Rect rect, string text, GUIStyle style, Color color)
    {
        style.normal.textColor = Color.black;
        GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), text, style);

        style.normal.textColor = color;
        GUI.Label(rect, text, style);
    }

    // ícono de moneda dibujado por código (8x8 píxeles)
    private static Texture2D CreateCoinTexture()
    {
        string[] rows = {
            "..YYYY..",
            ".YWYYYY.",
            "YWYYYYOY",
            "YWYYYYOY",
            "YWYYYYOY",
            "YWYYYYOY",
            ".YYYYOY.",
            "..YYYY..",
        };

        Texture2D texture = new Texture2D(8, 8) { filterMode = FilterMode.Point };

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                char c = rows[7 - y][x];
                Color color = c == 'Y' ? new Color(0.99f, 0.73f, 0.27f)
                            : c == 'W' ? new Color(1f, 0.95f, 0.8f)
                            : c == 'O' ? new Color(0.78f, 0.3f, 0.05f)
                            : new Color(0f, 0f, 0f, 0f);
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return texture;
    }

}
