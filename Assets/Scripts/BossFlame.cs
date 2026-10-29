using UnityEngine;

// Llamarada que escupe el jefe: avanza en línea recta y se acomoda a la altura de Mario
public class BossFlame : MonoBehaviour
{
    public Vector2 direction = Vector2.left;
    public float speed = 6f;
    public float verticalSpeed = 2f;
    public float targetY;
    public float lifetime = 6f;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        targetY = transform.position.y;
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        Vector3 position = transform.position;
        position.x += direction.x * speed * Time.deltaTime;
        position.y = Mathf.MoveTowards(position.y, targetY, verticalSpeed * Time.deltaTime);
        transform.position = position;

        // el sprite tiene la parte ancha a la derecha
        spriteRenderer.flipX = direction.x < 0f;
    }

}
