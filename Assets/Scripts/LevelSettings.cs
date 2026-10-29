using System.Collections;
using UnityEngine;

// Configuración propia de cada nivel: música, si es acuático, etc.
public class LevelSettings : MonoBehaviour
{
    public static LevelSettings Current { get; private set; }

    public MusicTrack music = MusicTrack.Overworld;

    [Header("Nivel acuático")]
    public bool underwater;
    [Tooltip("Mario no puede nadar por encima de esta altura")]
    public float waterSurfaceY = 11f;

    [Header("Cámara")]
    public bool startUnderground;

    private void Awake()
    {
        Current = this;
    }

    private void OnDestroy()
    {
        if (Current == this) {
            Current = null;
        }
    }

    private IEnumerator Start()
    {
        if (startUnderground)
        {
            SideScrolling sideScrolling = Camera.main.GetComponent<SideScrolling>();

            if (sideScrolling != null) {
                sideScrolling.SetUnderground(true);
            }
        }

        // espera a que termine la pantalla "MUNDO X-X" antes de empezar la música
        while (GameManager.Instance != null && GameManager.Instance.showingIntro) {
            yield return null;
        }

        AudioManager.PlayMusic(music);
    }

}
