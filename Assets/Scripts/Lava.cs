using UnityEngine;

// Zona de lava: mata a Mario al instante y destruye a los enemigos que caen
[RequireComponent(typeof(BoxCollider2D))]
public class Lava : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();

            if (player != null && !player.dead)
            {
                AudioManager.Play(Sfx.Fire);
                player.Death();
            }
        }
        else if (other.GetComponent<EntityMovement>() != null)
        {
            Destroy(other.gameObject);
        }
    }

}
