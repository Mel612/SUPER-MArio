using UnityEngine;

public class PowerUp : MonoBehaviour
{
    public enum Type
    {
        Coin,
        ExtraLife,
        MagicMushroom,
        Starpower,
    }

    public Type type;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) {
            Collect(other.gameObject);
        }
    }

    private void Collect(GameObject player)
    {
        switch (type)
        {
            case Type.Coin:
                GameManager.Instance.AddCoin();
                break;

            case Type.ExtraLife:
                GameManager.Instance.AddLife();
                ScorePopup.Show("1UP", transform.position);
                break;

            case Type.MagicMushroom:
                player.GetComponent<Player>().Grow();
                ScorePopup.Show(1000, transform.position); // Grow() ya suma los 1000
                break;

            case Type.Starpower:
                player.GetComponent<Player>().Starpower();
                GameManager.Instance.AddScore(1000, transform.position);
                break;
        }

        Destroy(gameObject);
    }

}
