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

    private bool started;
    private bool victory;
    private string message;
    private float messageTimer;

    private Texture2D blackTexture;
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
            StartIntro();
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

        if (TryParseLevel(scene.name, out _, out _)) {
            StartIntro();
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
        victory = false;

        LoadLevel(1, 1);
    }

    public void GameOver()
    {
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

        AudioManager.StopMusic();
        AudioManager.Play(Sfx.Victory);
        AddScore(5000);
        ShowMessage("¡EL CASTILLO DE BRASA ES TUYO!\nGracias por jugar", 7f);

        yield return new WaitForSeconds(7.5f);

        NewGame();
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

        float column = Screen.width / 4f;
        float top = size * 0.5f;

        Label(new Rect(0, top, column, size * 3), $"MARIO\n{score:000000}", textStyle);
        Label(new Rect(column, top, column, size * 3), $"MONEDAS\nx{coins:00}", textStyle);
        Label(new Rect(column * 2, top, column, size * 3), $"MUNDO\n{world}-{stage}", textStyle);
        Label(new Rect(column * 3, top, column, size * 3), $"VIDAS\n{lives}", textStyle);

        if (showingIntro) {
            Label(new Rect(0, 0, Screen.width, Screen.height), $"MUNDO {world}-{stage}\n\nMARIO  x  {lives}", bigStyle);
        }

        if (messageTimer > 0f && !string.IsNullOrEmpty(message)) {
            Label(new Rect(0, Screen.height * 0.25f, Screen.width, Screen.height * 0.3f), message, bigStyle);
        }
    }

    private static void Label(Rect rect, string text, GUIStyle style)
    {
        style.normal.textColor = Color.black;
        GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), text, style);

        style.normal.textColor = Color.white;
        GUI.Label(rect, text, style);
    }

}
