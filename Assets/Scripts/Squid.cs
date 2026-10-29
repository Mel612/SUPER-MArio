using UnityEngine;

// Calamar del nivel acuático: se hunde despacio y cuando queda a la altura
// de Mario da un impulso diagonal hacia él.
[RequireComponent(typeof(Rigidbody2D))]
public class Squid : MonoBehaviour
{
    public Sprite dashSprite;
    public Sprite sinkSprite;

    public float sinkSpeed = 1.5f;
    public float dashSpeed = 6f;
    public float dashDuration = 0.45f;
    public float minY = 2f;

    private enum State { Sink, Dash }

    private new Rigidbody2D rigidbody;
    private SpriteRenderer spriteRenderer;
    private Transform player;
    private State state;
    private float timer;
    private Vector2 dashDirection;
    private bool dead;

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        rigidbody.bodyType = RigidbodyType2D.Kinematic;
        spriteRenderer = GetComponent<SpriteRenderer>();
        enabled = false;
    }

    private void Start()
    {
        GameObject go = GameObject.FindWithTag("Player");

        if (go != null) {
            player = go.transform;
        }
    }

    private void OnBecameVisible()
    {
        if (!dead) {
            enabled = true;
        }
    }

    private void FixedUpdate()
    {
        if (player == null) {
            return;
        }

        timer += Time.fixedDeltaTime;
        Vector2 position = rigidbody.position;
        float maxY = LevelSettings.Current != null ? LevelSettings.Current.waterSurfaceY : 11f;

        if (state == State.Sink)
        {
            spriteRenderer.sprite = sinkSprite;
            position.y -= sinkSpeed * Time.fixedDeltaTime;

            bool levelWithPlayer = position.y <= player.position.y + 0.5f;

            if ((levelWithPlayer && timer > 0.4f) || position.y <= minY || timer > 1.6f)
            {
                state = State.Dash;
                timer = 0f;
                float side = player.position.x < position.x ? -1f : 1f;
                dashDirection = new Vector2(side, 1.2f).normalized;
                AudioManager.Play(Sfx.Swim);
            }
        }
        else
        {
            spriteRenderer.sprite = dashSprite;
            float slowdown = 1f - Mathf.Clamp01(timer / dashDuration);
            position += dashDirection * dashSpeed * slowdown * Time.fixedDeltaTime;

            if (timer >= dashDuration)
            {
                state = State.Sink;
                timer = 0f;
            }
        }

        position.y = Mathf.Clamp(position.y, minY, maxY);
        rigidbody.MovePosition(position);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (dead || !other.CompareTag("Player")) {
            return;
        }

        Player p = other.GetComponent<Player>();

        if (p.starpower) {
            Hit();
        } else {
            p.Hit();
        }
    }

    private void Hit()
    {
        dead = true;
        enabled = false;
        GetComponent<DeathAnimation>().enabled = true;

        AudioManager.Play(Sfx.Kick);
        GameManager.Instance.AddScore(200);

        Destroy(gameObject, 3f);
    }

}
