using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SideScrolling : MonoBehaviour
{
    private new Camera camera;
    private Transform player;

    public float height = 6.5f;
    public float undergroundHeight = -9.5f;
    public float undergroundThreshold = 0f;

    [Header("Mejoras de cámara")]
    [Tooltip("La cámara no avanza más a la derecha de esta X (fin del nivel)")]
    public float maxX = 100000f;
    [Tooltip("Suavizado del seguimiento (0 = instantáneo como el original)")]
    public float smoothTime = 0.08f;
    [Tooltip("Cambiar el color de fondo al entrar/salir de zonas subterráneas")]
    public bool switchBackgroundColor;
    public Color overworldColor = new Color(0.36f, 0.58f, 0.99f);
    public Color undergroundColor = Color.black;

    private Vector3 basePosition;
    private float followVelocity;
    private float shakeTime;
    private float shakeMagnitude;
    private bool locked;
    private float lockX;

    private void Awake()
    {
        camera = GetComponent<Camera>();
        player = GameObject.FindWithTag("Player").transform;
        basePosition = transform.position;
    }

    private void LateUpdate()
    {
        // track the player moving to the right
        if (player != null && player.gameObject.activeInHierarchy)
        {
            float target = locked ? lockX : Mathf.Max(basePosition.x, player.position.x);
            target = Mathf.Min(target, maxX);

            if (smoothTime > 0f) {
                float smoothed = Mathf.SmoothDamp(basePosition.x, target, ref followVelocity, smoothTime);
                basePosition.x = locked ? smoothed : Mathf.Max(basePosition.x, smoothed);
            } else {
                basePosition.x = target;
            }
        }

        // temblor de cámara (jefe, puente que cae...)
        Vector3 offset = Vector3.zero;

        if (shakeTime > 0f)
        {
            shakeTime -= Time.deltaTime;
            offset = (Vector3)(Random.insideUnitCircle * shakeMagnitude);
        }

        transform.position = basePosition + offset;
    }

    public void SetUnderground(bool underground)
    {
        // set underground height offset
        basePosition.y = underground ? undergroundHeight : height;
        transform.position = basePosition;

        if (switchBackgroundColor) {
            camera.backgroundColor = underground ? undergroundColor : overworldColor;
        }
    }

    // mueve la cámara de golpe a una X (al salir de un tubo)
    public void SnapTo(float x)
    {
        basePosition.x = Mathf.Min(Mathf.Max(basePosition.x, x), maxX);
        followVelocity = 0f;
        transform.position = basePosition;
    }

    public void Shake(float duration, float magnitude)
    {
        shakeTime = duration;
        shakeMagnitude = magnitude;
    }

    public void LockAt(float x)
    {
        locked = true;
        lockX = x;
    }

    public void Unlock()
    {
        locked = false;
    }

}
