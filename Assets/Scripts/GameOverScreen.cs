using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Escena de Game Over: se muestra al perder todas las vidas
public class GameOverScreen : MonoBehaviour
{
    public RectTransform title;
    public Text stats;
    public Text prompt;
    public RectTransform marioIcon;
    public float autoRestartDelay = 10f;
    public float inputDelay = 2f;

    private float timer;
    private bool restarting;
    private Vector2 marioStart;

    private void Start()
    {
        AudioManager.StopMusic();
        AudioManager.Play(Sfx.GameOver);

        GameManager gm = GameManager.Instance;

        if (stats != null && gm != null) {
            stats.text = $"PUNTAJE   {gm.score:000000}\nMONEDAS   x{gm.coins:00}\nMUNDO   {gm.world}-{gm.stage}";
        }

        if (title != null) {
            title.localScale = Vector3.zero;
        }

        if (prompt != null) {
            prompt.enabled = false;
        }

        if (marioIcon != null)
        {
            marioStart = marioIcon.anchoredPosition;
            StartCoroutine(MarioAnimation());
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;

        // el título aparece con un "pop" y luego late suavemente
        if (title != null)
        {
            float scale;

            if (timer < 0.6f)
            {
                float t = timer / 0.6f;
                scale = Mathf.Sin(t * Mathf.PI * 0.5f) * 1.15f;
            }
            else
            {
                scale = 1f + Mathf.Sin((timer - 0.6f) * 3f) * 0.04f;
            }

            title.localScale = Vector3.one * scale;
        }

        // texto parpadeante
        if (prompt != null && timer > inputDelay) {
            prompt.enabled = Mathf.Repeat(timer, 1f) < 0.6f;
        }

        bool pressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetButtonDown("Jump");

        if (!restarting && ((timer > inputDelay && pressed) || timer > autoRestartDelay)) {
            Restart();
        }
    }

    // Mario hace su animación de muerte: salta y cae, una y otra vez
    private IEnumerator MarioAnimation()
    {
        while (true)
        {
            float velocity = 900f;
            float gravity = -2200f;
            Vector2 position = marioStart;

            yield return new WaitForSeconds(0.5f);

            while (position.y > marioStart.y - 600f)
            {
                velocity += gravity * Time.deltaTime;
                position.y += velocity * Time.deltaTime;
                marioIcon.anchoredPosition = position;
                yield return null;
            }

            marioIcon.anchoredPosition = marioStart;
            yield return new WaitForSeconds(1.5f);
        }
    }

    private void Restart()
    {
        restarting = true;

        if (GameManager.Instance != null) {
            GameManager.Instance.NewGame();
        } else {
            SceneManager.LoadScene("1-1");
        }
    }

}
