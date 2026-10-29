using System.Collections;
using UnityEngine;

// "Rey Brasa": jefe original del castillo (propuesta propia).
// Camina sobre el puente, salta y escupe llamaradas hacia Mario.
// Se le puede pisar 3 veces o derrotarlo tocando el hacha (el puente se cae).
[RequireComponent(typeof(Rigidbody2D))]
public class Boss : MonoBehaviour
{
    public Sprite[] walkSprites;
    public Sprite hurtSprite;
    public GameObject flamePrefab;
    public Transform mouth;

    public int health = 3;
    public float walkSpeed = 1.5f;
    public float patrolDistance = 3f;
    public float jumpVelocity = 9f;
    public float gravity = -25f;
    public float jumpInterval = 3f;
    public float fireInterval = 2.2f;
    public float activationDistance = 13f;

    public bool defeated { get; private set; }

    private new Rigidbody2D rigidbody;
    private SpriteRenderer spriteRenderer;
    private Transform player;
    private Vector2 home;
    private float direction = -1f;
    private float verticalVelocity;
    private float jumpTimer;
    private float fireTimer;
    private float animationTimer;
    private float hurtTimer;
    private int frame;
    private bool active;
    private bool frozen;

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        rigidbody.bodyType = RigidbodyType2D.Kinematic;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        home = rigidbody.position;
        jumpTimer = jumpInterval;
        fireTimer = 1f;

        GameObject go = GameObject.FindWithTag("Player");

        if (go != null) {
            player = go.transform;
        }
    }

    private void FixedUpdate()
    {
        if (defeated || frozen || player == null) {
            return;
        }

        if (!active)
        {
            if (Mathf.Abs(player.position.x - home.x) < activationDistance)
            {
                active = true;
                AudioManager.PlayMusic(MusicTrack.Boss);
                ShakeCamera(0.4f, 0.15f);
            }

            return;
        }

        float dt = Time.fixedDeltaTime;
        Vector2 position = rigidbody.position;

        // caminar de un lado a otro
        position.x += direction * walkSpeed * dt;

        if (position.x < home.x - patrolDistance) direction = 1f;
        if (position.x > home.x + patrolDistance) direction = -1f;

        // saltar de vez en cuando
        bool onGround = position.y <= home.y + 0.001f;
        jumpTimer -= dt;

        if (jumpTimer <= 0f && onGround)
        {
            verticalVelocity = jumpVelocity;
            jumpTimer = jumpInterval + Random.Range(-0.5f, 1f);
        }

        verticalVelocity += gravity * dt;
        position.y += verticalVelocity * dt;

        if (position.y <= home.y)
        {
            if (!onGround && verticalVelocity < -5f) {
                ShakeCamera(0.15f, 0.1f);
            }

            position.y = home.y;
            verticalVelocity = 0f;
        }

        rigidbody.MovePosition(position);

        // siempre mira hacia Mario (el sprite original mira a la izquierda)
        bool facingRight = player.position.x > position.x;
        spriteRenderer.flipX = facingRight;

        // escupir fuego
        fireTimer -= dt;

        if (fireTimer <= 0f)
        {
            fireTimer = fireInterval + Random.Range(-0.4f, 0.6f);
            Breathe(facingRight);
        }

        Animate(dt);
    }

    private void Animate(float dt)
    {
        if (hurtTimer > 0f)
        {
            hurtTimer -= dt;
            spriteRenderer.sprite = hurtSprite;
            return;
        }

        animationTimer += dt;

        if (animationTimer >= 0.25f && walkSprites.Length > 0)
        {
            animationTimer = 0f;
            frame = (frame + 1) % walkSprites.Length;
        }

        if (walkSprites.Length > 0) {
            spriteRenderer.sprite = walkSprites[frame];
        }
    }

    private void Breathe(bool facingRight)
    {
        if (flamePrefab == null) {
            return;
        }

        Vector3 spawn = mouth != null ? mouth.localPosition : new Vector3(-1f, 0.4f, 0f);

        if (facingRight) {
            spawn.x = -spawn.x;
        }

        GameObject flame = Instantiate(flamePrefab, transform.position + spawn, Quaternion.identity);
        BossFlame bossFlame = flame.GetComponent<BossFlame>();

        if (bossFlame != null)
        {
            bossFlame.direction = facingRight ? Vector2.right : Vector2.left;
            bossFlame.targetY = player.position.y + 0.25f;
        }

        AudioManager.Play(Sfx.Fire);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (defeated || !collision.gameObject.CompareTag("Player")) {
            return;
        }

        Player p = collision.gameObject.GetComponent<Player>();

        if (p.starpower || (hurtTimer <= 0f && collision.transform.DotTest(transform, Vector2.down))) {
            TakeDamage();
        } else if (hurtTimer <= 0f) {
            p.Hit();
        }
    }

    private void TakeDamage()
    {
        if (hurtTimer > 0f) {
            return;
        }

        health--;
        hurtTimer = 1f;
        AudioManager.Play(Sfx.BossHit);
        ShakeCamera(0.3f, 0.2f);
        GameManager.Instance.AddScore(1000);

        if (health <= 0) {
            Defeat();
        }
    }

    // el hacha congela al jefe mientras el puente se cae
    public void Freeze()
    {
        frozen = true;
    }

    public void Defeat()
    {
        if (defeated) {
            return;
        }

        defeated = true;
        StartCoroutine(FallIntoLava());
    }

    private IEnumerator FallIntoLava()
    {
        foreach (Collider2D c in GetComponents<Collider2D>()) {
            c.enabled = false;
        }

        spriteRenderer.sprite = hurtSprite;
        yield return new WaitForSeconds(0.6f);

        AudioManager.Play(Sfx.BossFall);
        GameManager.Instance.AddScore(5000);

        float velocity = 2f;
        float elapsed = 0f;

        while (elapsed < 3f)
        {
            velocity += gravity * Time.deltaTime;
            transform.position += Vector3.up * velocity * Time.deltaTime;
            transform.Rotate(0f, 0f, 90f * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        gameObject.SetActive(false);
    }

    private void ShakeCamera(float duration, float magnitude)
    {
        if (Camera.main == null) {
            return;
        }

        SideScrolling sideScrolling = Camera.main.GetComponent<SideScrolling>();

        if (sideScrolling != null) {
            sideScrolling.Shake(duration, magnitude);
        }
    }

}
