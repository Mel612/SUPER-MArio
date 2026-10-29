using System.Collections;
using UnityEngine;

public class FlagPole : MonoBehaviour
{
    public Transform flag;
    public Transform poleBottom;
    public Transform castle;
    public float speed = 6f;
    public int nextWorld = 1;
    public int nextStage = 1;

    private bool reached;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!reached && other.CompareTag("Player"))
        {
            reached = true;

            // más puntos mientras más alto se agarre la bandera
            float height = other.transform.position.y - poleBottom.position.y;
            GameManager.Instance.AddScore(Mathf.Clamp(Mathf.RoundToInt(height) * 500, 100, 5000));

            AudioManager.StopMusic();
            AudioManager.Play(Sfx.Flagpole);

            StartCoroutine(MoveTo(flag, poleBottom.position));
            StartCoroutine(LevelCompleteSequence(other.transform));
        }
    }

    private IEnumerator LevelCompleteSequence(Transform player)
    {
        player.GetComponent<PlayerMovement>().enabled = false;

        yield return MoveTo(player, poleBottom.position);

        AudioManager.Play(Sfx.StageClear);

        yield return MoveTo(player, player.position + Vector3.right);
        yield return MoveTo(player, player.position + Vector3.right + Vector3.down);
        yield return MoveTo(player, castle.position);

        player.gameObject.SetActive(false);

        yield return new WaitForSeconds(2f);

        GameManager.Instance.LoadLevel(nextWorld, nextStage);
    }

    private IEnumerator MoveTo(Transform subject, Vector3 position)
    {
        while (Vector3.Distance(subject.position, position) > 0.125f)
        {
            subject.position = Vector3.MoveTowards(subject.position, position, speed * Time.deltaTime);
            yield return null;
        }

        subject.position = position;
    }

}
