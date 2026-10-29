using UnityEngine;

// Barra de fuego que gira alrededor de un bloque del castillo
public class Firebar : MonoBehaviour
{
    public GameObject fireballPrefab;
    public int length = 6;
    public float spacing = 0.5f;
    public float degreesPerSecond = 100f;
    public float startAngle = 0f;

    private float angle;

    private void Start()
    {
        angle = startAngle;

        for (int i = 0; i < length; i++)
        {
            GameObject ball = Instantiate(fireballPrefab, transform);
            ball.transform.localPosition = new Vector3(i * spacing, 0f, 0f);
        }

        transform.localEulerAngles = new Vector3(0f, 0f, angle);
    }

    private void Update()
    {
        angle += degreesPerSecond * Time.deltaTime;
        transform.localEulerAngles = new Vector3(0f, 0f, angle);
    }

}
