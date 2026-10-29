using UnityEngine;

// Plataforma que se mueve (ascensores del 1-2, plataformas sobre la lava del castillo).
// Lleva a Mario encima cuando está parado sobre ella.
[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    public enum Mode
    {
        PingPong, // va y vuelve
        Loop,     // llega al final y reaparece al inicio (como los ascensores del 1-2)
    }

    public Mode mode = Mode.PingPong;
    public Vector2 offset = new Vector2(0f, 4f);
    public float speed = 2f;
    [Range(0f, 1f)] public float startProgress = 0f;

    private new Rigidbody2D rigidbody;
    private Vector2 start;
    private float progress;
    private PlayerMovement rider;

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        rigidbody.bodyType = RigidbodyType2D.Kinematic;
        start = rigidbody.position;
        progress = startProgress;
    }

    private void FixedUpdate()
    {
        float distance = Mathf.Max(0.01f, offset.magnitude);
        progress += speed * Time.fixedDeltaTime / distance;

        float t = mode == Mode.PingPong ? Mathf.PingPong(progress, 1f) : Mathf.Repeat(progress, 1f);
        Vector2 target = start + offset * t;
        Vector2 delta = target - rigidbody.position;

        bool wrapped = mode == Mode.Loop && delta.magnitude > distance * 0.5f;

        if (rider != null && !wrapped)
        {
            if (rider.isActiveAndEnabled) {
                rider.AddExternalMovement(delta);
            } else {
                rider = null;
            }
        }

        if (wrapped)
        {
            rider = null;
            rigidbody.position = target;
        }
        else
        {
            rigidbody.MovePosition(target);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && collision.transform.DotTest(transform, Vector2.down)) {
            rider = collision.gameObject.GetComponent<PlayerMovement>();
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player")) {
            rider = null;
        }
    }

}
