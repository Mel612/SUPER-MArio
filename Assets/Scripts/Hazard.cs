using UnityEngine;

// Cualquier cosa que lastima a Mario al tocarla (bolas de fuego, llamas...)
[RequireComponent(typeof(Collider2D))]
public class Hazard : MonoBehaviour
{
    [Tooltip("Si es verdadero mata a Mario aunque sea grande")]
    public bool instantKill;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!enabled || !other.CompareTag("Player")) {
            return;
        }

        Player player = other.GetComponent<Player>();

        if (player == null || player.dead) {
            return;
        }

        if (instantKill) {
            player.Death();
        } else {
            player.Hit();
        }
    }

}
