using UnityEngine;

// Pedazo de ladrillo que sale volando cuando Mario grande rompe un bloque
public class BrickDebris : MonoBehaviour
{
    public Vector2 velocity;
    public float gravity = -40f;
    public float spinSpeed = 720f;
    public float lifetime = 1.5f;

    public static void Spawn(Sprite sprite, Vector3 position, Vector2 velocity)
    {
        GameObject go = new GameObject("Brick Debris");
        go.transform.position = position;
        go.transform.localScale = Vector3.one * 0.5f;

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 10;

        BrickDebris debris = go.AddComponent<BrickDebris>();
        debris.velocity = velocity;
        debris.spinSpeed *= velocity.x < 0f ? 1f : -1f;
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        velocity.y += gravity * Time.deltaTime;
        transform.position += (Vector3)(velocity * Time.deltaTime);
        transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
    }

}
