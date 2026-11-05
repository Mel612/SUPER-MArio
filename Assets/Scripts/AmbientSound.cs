using UnityEngine;
using UnityEngine.SceneManagement;

// Mejora: sonido ambiente.
// Agrega una capa de ambiente que suena de fondo, debajo de la música, según el lugar:
//   - Superficie (1-1, salida de la 1-2): viento suave y pajaritos
//   - Subterráneo: eco y gotas de agua (cambia solo al bajar por el tubo)
//   - Acuático (2-2): burbujas y corriente de agua
//   - Castillo (2-4): lava burbujeando y retumbe grave
//   - Menú: música de inicio
// También se puede silenciar todo el juego con la tecla M.
//
// No hace falta ponerlo en ninguna escena: se crea solo al iniciar el juego.
public class AmbientSound : MonoBehaviour
{
    public enum Ambience
    {
        None,
        Overworld,
        Underground,
        Underwater,
        Castle,
    }

    public static AmbientSound Instance { get; private set; }

    [Range(0f, 1f)] public float volume = 0.35f;
    public float fadeSpeed = 1.5f;
    public KeyCode muteKey = KeyCode.M;

    public Ambience current { get; private set; }

    private AudioSource source;
    private AudioClip[] clips;
    private float targetVolume;
    private bool muted;
    private SideScrolling sideScrolling;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Create()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("Ambient Sound");
            go.AddComponent<AmbientSound>();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        source = gameObject.AddComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0f;

        clips = new AudioClip[5];
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        sideScrolling = Camera.main != null ? Camera.main.GetComponent<SideScrolling>() : null;

        if (scene.name == "Menu")
        {
            SetAmbience(Ambience.None);
            AudioManager.PlayMusic(MusicTrack.Overworld);
            return;
        }

        if (scene.name == "GameOver") {
            SetAmbience(Ambience.None);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(muteKey))
        {
            muted = !muted;
            AudioListener.volume = muted ? 0f : 1f;

            if (GameManager.Instance != null) {
                GameManager.Instance.ShowMessage(muted ? "SONIDO: APAGADO" : "SONIDO: ENCENDIDO", 1.2f);
            }
        }

        string sceneName = SceneManager.GetActiveScene().name;

        if (GameManager.TryParseLevel(sceneName, out _, out _)) {
            SetAmbience(DetectAmbience());
        }

        // baja el ambiente durante la pantalla "MUNDO X-X" y cuando Mario muere
        bool quiet = Time.timeScale == 0f || (GameManager.Instance != null && GameManager.Instance.showingIntro) || PlayerIsDead();
        targetVolume = current == Ambience.None || quiet ? 0f : volume;

        source.volume = Mathf.MoveTowards(source.volume, targetVolume, fadeSpeed * Time.unscaledDeltaTime);

        if (source.volume <= 0f && targetVolume <= 0f && source.isPlaying && current == Ambience.None) {
            source.Stop();
        }
    }

    private Ambience DetectAmbience()
    {
        LevelSettings settings = LevelSettings.Current;

        if (settings != null)
        {
            if (settings.underwater) return Ambience.Underwater;
            if (settings.music == MusicTrack.Castle || settings.music == MusicTrack.Boss) return Ambience.Castle;
        }

        // en la 1-1 y 1-2 depende de si la cámara está bajo tierra
        if (sideScrolling == null && Camera.main != null) {
            sideScrolling = Camera.main.GetComponent<SideScrolling>();
        }

        if (sideScrolling != null && sideScrolling.transform.position.y < sideScrolling.undergroundThreshold) {
            return Ambience.Underground;
        }

        return Ambience.Overworld;
    }

    private bool PlayerIsDead()
    {
        GameObject go = GameObject.FindWithTag("Player");

        if (go == null) {
            return false;
        }

        Player player = go.GetComponent<Player>();
        return !go.activeInHierarchy || (player != null && player.dead);
    }

    public void SetAmbience(Ambience ambience)
    {
        if (ambience == current) {
            return;
        }

        current = ambience;

        if (ambience == Ambience.None) {
            return;
        }

        int index = (int)ambience;

        if (clips[index] == null) {
            clips[index] = AmbientSynth.Create(ambience);
        }

        source.clip = clips[index];
        source.volume = 0f;
        source.Play();
    }

}

// Genera los sonidos de ambiente por código (no hacen falta archivos de audio).
// Cada clip dura varios segundos y hace loop sin cortes.
public static class AmbientSynth
{
    private const int Rate = 22050;

    public static AudioClip Create(AmbientSound.Ambience ambience)
    {
        System.Random random = new System.Random(42 + (int)ambience);
        float seconds = 8f;
        float[] buffer = new float[(int)(Rate * seconds)];

        switch (ambience)
        {
            case AmbientSound.Ambience.Overworld:
                Wind(buffer, random, 0.10f, 0.15f);
                for (int i = 0; i < 5; i++) {
                    Chirp(buffer, random, (float)random.NextDouble() * (seconds - 1f));
                }
                break;

            case AmbientSound.Ambience.Underground:
                Wind(buffer, random, 0.05f, 0.04f);
                Rumble(buffer, 55f, 0.08f);
                for (int i = 0; i < 6; i++) {
                    Drip(buffer, (float)random.NextDouble() * (seconds - 1f), 900f + (float)random.NextDouble() * 700f);
                }
                break;

            case AmbientSound.Ambience.Underwater:
                Wind(buffer, random, 0.12f, 0.02f);
                Rumble(buffer, 70f, 0.06f);
                for (int i = 0; i < 18; i++) {
                    Bubble(buffer, random, (float)random.NextDouble() * (seconds - 0.5f));
                }
                break;

            case AmbientSound.Ambience.Castle:
                Rumble(buffer, 42f, 0.2f);
                Wind(buffer, random, 0.06f, 0.03f);
                for (int i = 0; i < 26; i++) {
                    Pop(buffer, random, (float)random.NextDouble() * (seconds - 0.3f));
                }
                break;
        }

        // fundido en los extremos para que el loop no haga "click"
        int fade = Rate / 10;
        for (int i = 0; i < fade; i++)
        {
            float t = (float)i / fade;
            buffer[i] *= t;
            buffer[buffer.Length - 1 - i] *= t;
        }

        for (int i = 0; i < buffer.Length; i++) {
            buffer[i] = Mathf.Clamp(buffer[i], -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Ambient_" + ambience, buffer.Length, 1, Rate, false);
        clip.SetData(buffer, 0);
        return clip;
    }

    // ruido filtrado que sube y baja (viento / corriente)
    private static void Wind(float[] buffer, System.Random random, float volume, float smoothing)
    {
        float value = 0f;

        for (int i = 0; i < buffer.Length; i++)
        {
            float noise = (float)(random.NextDouble() * 2.0 - 1.0);
            value += (noise - value) * smoothing;
            float swell = 0.6f + 0.4f * Mathf.Sin(i / (float)Rate * Mathf.PI * 2f / 4f);
            buffer[i] += value * volume * swell * 3f;
        }
    }

    // tono grave continuo (retumbe)
    private static void Rumble(float[] buffer, float frequency, float volume)
    {
        for (int i = 0; i < buffer.Length; i++)
        {
            float t = i / (float)Rate;
            float wobble = 0.7f + 0.3f * Mathf.Sin(t * Mathf.PI * 2f / 2f);
            buffer[i] += Mathf.Sin(t * frequency * Mathf.PI * 2f) * volume * wobble;
        }
    }

    // gota de agua: tono que sube rápido y se apaga
    private static void Drip(float[] buffer, float start, float frequency)
    {
        int s = (int)(start * Rate);
        int length = Rate / 6;
        float phase = 0f;

        for (int i = 0; i < length && s + i < buffer.Length; i++)
        {
            float t = (float)i / length;
            phase += frequency * (1f + t * 1.5f) / Rate;
            buffer[s + i] += Mathf.Sin(phase * Mathf.PI * 2f) * 0.25f * Mathf.Pow(1f - t, 3f);
        }
    }

    // burbuja: tono que sube un poco
    private static void Bubble(float[] buffer, System.Random random, float start)
    {
        int s = (int)(start * Rate);
        int length = Rate / 12 + random.Next(Rate / 12);
        float frequency = 300f + (float)random.NextDouble() * 500f;
        float phase = 0f;

        for (int i = 0; i < length && s + i < buffer.Length; i++)
        {
            float t = (float)i / length;
            phase += frequency * (1f + t * 0.8f) / Rate;
            buffer[s + i] += Mathf.Sin(phase * Mathf.PI * 2f) * 0.16f * Mathf.Sin(t * Mathf.PI);
        }
    }

    // chasquido corto (lava burbujeando)
    private static void Pop(float[] buffer, System.Random random, float start)
    {
        int s = (int)(start * Rate);
        int length = Rate / 30 + random.Next(Rate / 25);
        float frequency = 120f + (float)random.NextDouble() * 160f;

        for (int i = 0; i < length && s + i < buffer.Length; i++)
        {
            float t = (float)i / length;
            float noise = (float)(random.NextDouble() * 2.0 - 1.0) * 0.4f;
            float tone = Mathf.Sin(i * frequency / Rate * Mathf.PI * 2f);
            buffer[s + i] += (tone + noise) * 0.2f * (1f - t);
        }
    }

    // pajarito: dos notas agudas rápidas
    private static void Chirp(float[] buffer, System.Random random, float start)
    {
        int notes = 2 + random.Next(2);
        float baseFrequency = 2200f + (float)random.NextDouble() * 900f;

        for (int n = 0; n < notes; n++)
        {
            int s = (int)((start + n * 0.11f) * Rate);
            int length = Rate / 14;
            float phase = 0f;

            for (int i = 0; i < length && s + i < buffer.Length; i++)
            {
                float t = (float)i / length;
                phase += baseFrequency * (1f + 0.25f * Mathf.Sin(t * Mathf.PI)) / Rate;
                buffer[s + i] += Mathf.Sin(phase * Mathf.PI * 2f) * 0.05f * Mathf.Sin(t * Mathf.PI);
            }
        }
    }

}
