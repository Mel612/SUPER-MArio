using System.Collections;
using UnityEngine;

public class Pipe : MonoBehaviour
{
    public Transform connection;
    public KeyCode enterKeyCode = KeyCode.S;
    public Vector3 enterDirection = Vector3.down;
    public Vector3 exitDirection = Vector3.zero;

    [Header("Mejoras")]
    [Tooltip("Música que suena al salir por el otro lado (None = no cambia)")]
    public MusicTrack musicOnExit = MusicTrack.None;
    [Tooltip("Si está activo, la cámara se coloca en esta X al salir")]
    public bool overrideCameraX;
    public float cameraX;

    [Header("Tubo de salida del nivel")]
    public bool exitsLevel;
    public int nextWorld = 1;
    public int nextStage = 1;

    private bool entering;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!entering && (connection != null || exitsLevel) && other.CompareTag("Player"))
        {
            if (IsPressingEnterKey()) {
                StartCoroutine(Enter(other.transform));
            }
        }
    }

    // acepta tanto WASD como las flechas
    private bool IsPressingEnterKey()
    {
        if (Input.GetKey(enterKeyCode)) {
            return true;
        }

        switch (enterKeyCode)
        {
            case KeyCode.S: return Input.GetKey(KeyCode.DownArrow);
            case KeyCode.D: return Input.GetKey(KeyCode.RightArrow);
            case KeyCode.A: return Input.GetKey(KeyCode.LeftArrow);
            case KeyCode.W: return Input.GetKey(KeyCode.UpArrow);
            default: return false;
        }
    }

    private IEnumerator Enter(Transform player)
    {
        entering = true;
        player.GetComponent<PlayerMovement>().enabled = false;

        AudioManager.Play(Sfx.Pipe);

        Vector3 enteredPosition = transform.position + enterDirection;
        Vector3 enteredScale = Vector3.one * 0.5f;

        yield return Move(player, enteredPosition, enteredScale);
        yield return new WaitForSeconds(1f);

        if (exitsLevel)
        {
            AudioManager.StopMusic();
            GameManager.Instance.AddScore(500);
            GameManager.Instance.LoadLevel(nextWorld, nextStage);
            yield break;
        }

        var sideSrolling = Camera.main.GetComponent<SideScrolling>();
        sideSrolling.SetUnderground(connection.position.y < sideSrolling.undergroundThreshold);
        sideSrolling.SnapTo(overrideCameraX ? cameraX : connection.position.x);

        if (musicOnExit != MusicTrack.None) {
            AudioManager.PlayMusic(musicOnExit);
        }

        if (exitDirection != Vector3.zero)
        {
            player.position = connection.position - exitDirection;
            AudioManager.Play(Sfx.Pipe);
            yield return Move(player, connection.position + exitDirection, Vector3.one);
        }
        else
        {
            player.position = connection.position;
            player.localScale = Vector3.one;
        }

        player.GetComponent<PlayerMovement>().enabled = true;
        entering = false;
    }

    private IEnumerator Move(Transform player, Vector3 endPosition, Vector3 endScale)
    {
        float elapsed = 0f;
        float duration = 1f;

        Vector3 startPosition = player.position;
        Vector3 startScale = player.localScale;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            player.position = Vector3.Lerp(startPosition, endPosition, t);
            player.localScale = Vector3.Lerp(startScale, endScale, t);
            elapsed += Time.deltaTime;

            yield return null;
        }

        player.position = endPosition;
        player.localScale = endScale;
    }

}
