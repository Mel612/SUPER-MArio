using UnityEngine;

// Pez del nivel acuático: nada en una dirección haciendo una onda.
// Bajo el agua no se le puede pisar, solo esquivar (o matarlo con la estrella).
[RequireComponent(typeof(Rigidbody2D))]
public class Fish : MonoBehaviour
{
    public float speed = 2f;
    public Vector2 direction = Vector2.left;
    public float waveAmplitude = 0.5f;
    public float waveFrequency = 0.5f;

    private new Rigidbody2D rigidbody;
    private Vector2 origin;
    private float time;
    private bool dead;

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        rigidbody.bodyType = RigidbodyType2D.Kinematic;
        origin = rigidbody.position;
        time = Random.value * 10f;
        enabled = false;
    }

    private void OnBecameVisible()
    {
        if (!dead) {
            enabled = true;
        }
    }

    private void FixedUpdate()
    {
        time += Time.fixedDeltaTime;
        origin += direction.normalized * speed * Time.fixedDeltaTime;

        float wave = Mathf.Sin(time * waveFrequency * Mathf.PI * 2f) * waveAmplitude;
        rigidbody.MovePosition(origin + Vector2.up * wave);

        transform.localEulerAngles = direction.x > 0f ? new Vector3(0f, 180f, 0f) : Vector3.zero;

        // se destruye cuando sale por la izquierda de la pantalla
        Camera cam = Camera.main;

        if (cam != null && rigidbody.position.x < cam.transform.position.x - cam.orthographicSize * cam.aspect - 3f) {
            Destroy(gameObject);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (dead || !other.CompareTag("Player")) {
            return;
        }

        Player player = other.GetComponent<Player>();

        if (player.starpower) {
            Hit();
        } else {
            player.Hit();
        }
    }

    private void Hit()
    {
        dead = true;
        enabled = false;

        AnimatedSprite animated = GetComponent<AnimatedSprite>();

        if (animated != null) {
            animated.enabled = false;
        }

        GetComponent<DeathAnimation>().enabled = true;

        AudioManager.Play(Sfx.Kick);
        GameManager.Instance.AddScore(200);

        Destroy(gameObject, 3f);
    }

}
