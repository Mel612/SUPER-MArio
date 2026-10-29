using System.Collections;
using UnityEngine;

// Bola de fuego que salta desde la lava cada cierto tiempo
public class LavaBubble : MonoBehaviour
{
    public float jumpHeight = 7f;
    public float interval = 2.5f;
    public float startDelay = 0f;
    public float gravity = -22f;

    private SpriteRenderer spriteRenderer;
    private Vector3 restPosition;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        restPosition = transform.position;
    }

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(startDelay);

        while (true)
        {
            float velocity = Mathf.Sqrt(-2f * gravity * jumpHeight);
            Vector3 position = restPosition;

            while (position.y >= restPosition.y || velocity > 0f)
            {
                velocity += gravity * Time.deltaTime;
                position.y += velocity * Time.deltaTime;
                transform.position = position;

                // cuando baja se voltea
                spriteRenderer.flipY = velocity < 0f;

                yield return null;
            }

            transform.position = restPosition;
            yield return new WaitForSeconds(interval);
        }
    }

}
