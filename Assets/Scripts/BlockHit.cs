using System.Collections;
using UnityEngine;

public class BlockHit : MonoBehaviour
{
    public GameObject item;
    public Sprite emptyBlock;
    public int maxHits = -1;
    [Tooltip("Mario grande rompe el bloque si no tiene ítem")]
    public bool breakable = true;
    private bool animating;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!animating && maxHits != 0 && collision.gameObject.CompareTag("Player"))
        {
            if (collision.transform.DotTest(transform, Vector2.up))
            {
                Player player = collision.gameObject.GetComponent<Player>();

                if (breakable && item == null && maxHits < 0 && player != null && player.big) {
                    Break();
                } else {
                    Hit();
                }
            }
        }
    }

    private void Hit()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.enabled = true; // show if hidden

        maxHits--;

        if (maxHits == 0) {
            spriteRenderer.sprite = emptyBlock;
        }

        if (item != null)
        {
            Instantiate(item, transform.position, Quaternion.identity);

            // las monedas suenan solas al sumarse; los power-ups tienen su sonido de aparición
            if (item.GetComponent<BlockCoin>() == null) {
                AudioManager.Play(Sfx.PowerUpAppear);
            }
        }
        else
        {
            AudioManager.Play(Sfx.Bump);
        }

        StartCoroutine(Animate());
    }

    // el ladrillo se rompe en 4 pedazos que salen volando
    private void Break()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        Vector2[] directions = {
            new Vector2(-2.5f, 12f), new Vector2(2.5f, 12f),
            new Vector2(-2.5f, 8f), new Vector2(2.5f, 8f),
        };

        for (int i = 0; i < directions.Length; i++)
        {
            Vector3 offset = new Vector3(i % 2 == 0 ? -0.25f : 0.25f, i < 2 ? 0.25f : -0.25f, 0f);
            BrickDebris.Spawn(spriteRenderer.sprite, transform.position + offset, directions[i]);
        }

        AudioManager.Play(Sfx.BreakBlock);
        GameManager.Instance.AddScore(50);

        Destroy(gameObject);
    }

    private IEnumerator Animate()
    {
        animating = true;

        Vector3 restingPosition = transform.localPosition;
        Vector3 animatedPosition = restingPosition + Vector3.up * 0.5f;

        yield return Move(restingPosition, animatedPosition);
        yield return Move(animatedPosition, restingPosition);

        animating = false;
    }

    private IEnumerator Move(Vector3 from, Vector3 to)
    {
        float elapsed = 0f;
        float duration = 0.125f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            transform.localPosition = Vector3.Lerp(from, to, t);
            elapsed += Time.deltaTime;

            yield return null;
        }

        transform.localPosition = to;
    }

}
