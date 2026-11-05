using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Escena inicial de menú: se elige una opción con el teclado o el mouse
// y cualquiera de las dos opciones inicia el juego.
public class TitleMenu : MonoBehaviour
{
    public SpriteRenderer[] options;
    public Transform cursor;
    public float cursorGap = 0.8f;
    public string firstLevel = "1-1";
    public float topScoreY = -3.4f;

    private const string TopScoreKey = "TopScore";

    private int selected;
    private bool starting;
    private int topScore;
    private Vector3 cursorBase;
    private new Camera camera;
    private GUIStyle style;

    private void Awake()
    {
        camera = Camera.main;

        // si venimos de una partida, cortar la música del nivel anterior
        AudioManager.StopMusic();
        Time.timeScale = 1f;
    }

    private void Start()
    {
        topScore = LoadTopScore();

        // guardar el récord de la partida que acaba de terminar
        if (GameManager.Instance != null && GameManager.Instance.score > topScore)
        {
            topScore = GameManager.Instance.score;
            SaveTopScore(topScore);
        }

        Select(0, false);
    }

    private void Update()
    {
        if (starting) {
            return;
        }

        // teclado
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) {
            Select(selected + 1, true);
        } else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) {
            Select(selected - 1, true);
        }

        // mouse: pasar por encima selecciona, clic inicia
        int hovered = OptionUnderMouse();

        if (hovered >= 0 && hovered != selected) {
            Select(hovered, true);
        }

        bool confirm = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
                       Input.GetKeyDown(KeyCode.Space) || (Input.GetMouseButtonDown(0) && hovered >= 0);

        if (confirm) {
            StartCoroutine(StartGame());
        }

        // el hongo "flota" un poquito
        if (cursor != null) {
            cursor.position = cursorBase + Vector3.up * Mathf.Sin(Time.unscaledTime * 6f) * 0.06f;
        }
    }

    private void Select(int index, bool playSound)
    {
        if (options == null || options.Length == 0) {
            return;
        }

        selected = (index % options.Length + options.Length) % options.Length;

        for (int i = 0; i < options.Length; i++) {
            options[i].color = i == selected ? Color.white : new Color(1f, 1f, 1f, 0.75f);
        }

        if (cursor != null)
        {
            Bounds b = options[selected].bounds;
            cursorBase = new Vector3(b.min.x - cursorGap, b.center.y, cursor.position.z);
            cursor.position = cursorBase;
        }

        if (playSound) {
            AudioManager.Play(Sfx.Kick);
        }
    }

    private int OptionUnderMouse()
    {
        if (camera == null || options == null) {
            return -1;
        }

        Vector2 mouse = camera.ScreenToWorldPoint(Input.mousePosition);

        for (int i = 0; i < options.Length; i++)
        {
            Bounds b = options[i].bounds;
            b.Expand(new Vector3(cursorGap * 2f, 0.3f, 1f));

            if (b.Contains(new Vector3(mouse.x, mouse.y, b.center.z))) {
                return i;
            }
        }

        return -1;
    }

    private IEnumerator StartGame()
    {
        starting = true;
        PlayerPrefs.SetInt("Players", selected + 1);

        AudioManager.StopMusic();
        AudioManager.Play(Sfx.Coin);

        // la opción elegida parpadea antes de empezar
        float elapsed = 0f;

        while (elapsed < 0.8f)
        {
            elapsed += Time.unscaledDeltaTime;
            options[selected].enabled = Mathf.Repeat(elapsed, 0.16f) < 0.1f;
            yield return null;
        }

        options[selected].enabled = true;

        if (GameManager.Instance != null) {
            GameManager.Instance.NewGame(); // reinicia vidas, monedas y puntaje
        } else {
            SceneManager.LoadScene(firstLevel);
        }
    }

    // ---------- récord ----------

    public static int LoadTopScore()
    {
        return PlayerPrefs.GetInt(TopScoreKey, 0);
    }

    public static void SaveTopScore(int value)
    {
        PlayerPrefs.SetInt(TopScoreKey, value);
        PlayerPrefs.Save();
    }

    // ---------- textos ----------

    private void OnGUI()
    {
        int size = Mathf.Max(12, Screen.height / 28);

        if (style == null) {
            style = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        }

        style.fontSize = size;

        // récord debajo de las opciones
        if (camera != null)
        {
            Vector3 p = camera.WorldToScreenPoint(new Vector3(0.6f, topScoreY, 0f));
            Label(new Rect(0, Screen.height - p.y - size, Screen.width, size * 2), $"TOP - {topScore:000000}");
        }

        // barra superior (cuando el GameManager todavía no existe, la dibuja el menú)
        if (GameManager.Instance == null)
        {
            float column = Screen.width / 4f;
            float top = size * 0.5f;

            Label(new Rect(0, top, column, size * 3), "MARIO\n000000");
            Label(new Rect(column, top, column, size * 3), "MONEDAS\nx00");
            Label(new Rect(column * 2, top, column, size * 3), "MUNDO\n1-1");
            Label(new Rect(column * 3, top, column, size * 3), "TIEMPO\n");
        }

        Label(new Rect(0, Screen.height - size * 2.2f, Screen.width, size * 2),
              "FLECHAS para elegir  -  ENTER, ESPACIO o CLIC para jugar");
    }

    private void Label(Rect rect, string text)
    {
        style.normal.textColor = Color.black;
        GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), text, style);

        style.normal.textColor = Color.white;
        GUI.Label(rect, text, style);
    }

}
