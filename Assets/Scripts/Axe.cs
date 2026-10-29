using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Hacha al final del castillo: al tocarla el puente se derrumba y el jefe cae a la lava
public class Axe : MonoBehaviour
{
    public Transform bridge;
    public Boss boss;
    public float tileDelay = 0.07f;

    private bool triggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!triggered && other.CompareTag("Player"))
        {
            triggered = true;
            StartCoroutine(Sequence(other.GetComponent<PlayerMovement>()));
        }
    }

    private IEnumerator Sequence(PlayerMovement player)
    {
        if (player != null) {
            player.enabled = false;
        }

        AudioManager.StopMusic();
        GetComponent<SpriteRenderer>().enabled = false;

        if (boss != null && !boss.defeated) {
            boss.Freeze();
        }

        yield return new WaitForSeconds(0.3f);

        // derrumbar el puente desde el lado del hacha hacia la izquierda
        if (bridge != null)
        {
            List<Transform> tiles = new List<Transform>();

            foreach (Transform tile in bridge) {
                tiles.Add(tile);
            }

            tiles.Sort((a, b) => b.position.x.CompareTo(a.position.x));

            foreach (Transform tile in tiles)
            {
                AudioManager.Play(Sfx.BridgeBreak);
                Destroy(tile.gameObject);
                yield return new WaitForSeconds(tileDelay);
            }
        }

        if (boss != null && !boss.defeated) {
            boss.Defeat();
        }

        yield return new WaitForSeconds(1.5f);

        if (player != null) {
            player.enabled = true;
        }

        GameManager.Instance.Victory();
    }

}
