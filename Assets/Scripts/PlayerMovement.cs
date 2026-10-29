using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    private new Camera camera;
    private new Rigidbody2D rigidbody;
    private new Collider2D collider;
    private Player player;

    private Vector2 velocity;
    private float inputAxis;

    public float moveSpeed = 8f;
    public float maxJumpHeight = 5f;
    public float maxJumpTime = 1f;
    public float jumpForce => (2f * maxJumpHeight) / (maxJumpTime / 2f);
    public float gravity => (-2f * maxJumpHeight) / Mathf.Pow(maxJumpTime / 2f, 2f);

    [Header("Nado (nivel acuático)")]
    public float swimSpeed = 4.5f;
    public float swimStroke = 6f;
    public float swimGravity = -10f;
    public float swimMaxFallSpeed = -3.5f;

    public bool grounded { get; private set; }
    public bool jumping { get; private set; }
    public bool swimming { get; private set; }
    public bool stroking => strokeTimer > 0f;
    public bool running => Mathf.Abs(velocity.x) > 0.25f || Mathf.Abs(inputAxis) > 0.25f;
    public bool sliding => !swimming && ((inputAxis > 0f && velocity.x < 0f) || (inputAxis < 0f && velocity.x > 0f));
    public bool falling => velocity.y < 0f && !grounded;

    private float strokeTimer;
    private Vector2 externalMovement;

    private void Awake()
    {
        camera = Camera.main;
        rigidbody = GetComponent<Rigidbody2D>();
        collider = GetComponent<Collider2D>();
        player = GetComponent<Player>();
    }

    private void Start()
    {
        swimming = LevelSettings.Current != null && LevelSettings.Current.underwater;
    }

    private void OnEnable()
    {
        rigidbody.bodyType = RigidbodyType2D.Dynamic;
        collider.enabled = true;
        velocity = Vector2.zero;
        jumping = false;
    }

    private void OnDisable()
    {
        rigidbody.bodyType = RigidbodyType2D.Kinematic;
        rigidbody.linearVelocity = Vector2.zero;
        collider.enabled = false;
        velocity = Vector2.zero;
        jumping = false;
    }

    public void SetSwimming(bool value)
    {
        swimming = value;
        velocity.y = 0f;
    }

    // Usado por las plataformas móviles para llevar a Mario encima
    public void AddExternalMovement(Vector2 delta)
    {
        externalMovement += delta;
    }

    private void Update()
    {
        // pausa durante la pantalla de inicio de nivel
        if (Time.timeScale == 0f) {
            return;
        }

        HorizontalMovement();

        grounded = rigidbody.Raycast(Vector2.down);

        if (swimming)
        {
            SwimMovement();
        }
        else
        {
            if (grounded) {
                GroundedMovement();
            }

            ApplyGravity();
        }

        strokeTimer -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        // move mario based on his velocity
        Vector2 position = rigidbody.position;
        position += velocity * Time.fixedDeltaTime + externalMovement;
        externalMovement = Vector2.zero;

        // clamp within the screen bounds
        Vector2 leftEdge = camera.ScreenToWorldPoint(Vector2.zero);
        Vector2 rightEdge = camera.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));
        position.x = Mathf.Clamp(position.x, leftEdge.x + 0.5f, rightEdge.x - 0.5f);

        // en el agua no se puede salir por encima de la superficie
        if (swimming && LevelSettings.Current != null && position.y > LevelSettings.Current.waterSurfaceY)
        {
            position.y = LevelSettings.Current.waterSurfaceY;
            velocity.y = Mathf.Min(velocity.y, 0f);
        }

        rigidbody.MovePosition(position);
    }

    private void HorizontalMovement()
    {
        float speed = swimming ? swimSpeed : moveSpeed;

        // accelerate / decelerate
        inputAxis = Input.GetAxis("Horizontal");
        velocity.x = Mathf.MoveTowards(velocity.x, inputAxis * speed, speed * Time.deltaTime);

        // check if running into a wall
        if (rigidbody.Raycast(Vector2.right * velocity.x)) {
            velocity.x = 0f;
        }

        // flip sprite to face direction
        if (velocity.x > 0f) {
            transform.eulerAngles = Vector3.zero;
        } else if (velocity.x < 0f) {
            transform.eulerAngles = new Vector3(0f, 180f, 0f);
        }
    }

    private void GroundedMovement()
    {
        // prevent gravity from infinitly building up
        velocity.y = Mathf.Max(velocity.y, 0f);
        jumping = velocity.y > 0f;

        // perform jump
        if (Input.GetButtonDown("Jump"))
        {
            velocity.y = jumpForce;
            jumping = true;
            AudioManager.Play(player != null && player.big ? Sfx.BigJump : Sfx.Jump);
        }
    }

    private void SwimMovement()
    {
        jumping = false;

        // cada vez que se presiona salto, Mario da una brazada hacia arriba
        if (Input.GetButtonDown("Jump"))
        {
            velocity.y = swimStroke;
            strokeTimer = 0.3f;
            AudioManager.Play(Sfx.Swim);
        }

        // se hunde lento por el agua
        velocity.y += swimGravity * Time.deltaTime;
        velocity.y = Mathf.Max(velocity.y, swimMaxFallSpeed);

        if (grounded) {
            velocity.y = Mathf.Max(velocity.y, 0f);
        }
    }

    private void ApplyGravity()
    {
        // check if falling
        bool falling = velocity.y < 0f || !Input.GetButton("Jump");
        float multiplier = falling ? 2f : 1f;

        // apply gravity and terminal velocity
        velocity.y += gravity * multiplier * Time.deltaTime;
        velocity.y = Mathf.Max(velocity.y, gravity / 2f);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            // bounce off enemy head
            if (transform.DotTest(collision.transform, Vector2.down))
            {
                velocity.y = jumpForce / 2f;
                jumping = true;
            }
        }
        else if (collision.gameObject.layer != LayerMask.NameToLayer("PowerUp"))
        {
            // stop vertical movement if mario bonks his head
            if (transform.DotTest(collision.transform, Vector2.up)) {
                velocity.y = 0f;
            }
        }
    }

}
