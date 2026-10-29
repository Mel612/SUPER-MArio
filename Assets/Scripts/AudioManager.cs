using System;
using System.Collections.Generic;
using UnityEngine;

public enum Sfx
{
    Jump,
    BigJump,
    Coin,
    Stomp,
    Bump,
    BreakBlock,
    PowerUpAppear,
    PowerUp,
    OneUp,
    Kick,
    Pipe,
    Shrink,
    Death,
    Flagpole,
    StageClear,
    Swim,
    Fire,
    BossHit,
    BossFall,
    BridgeBreak,
    GameOver,
    Victory,
}

public enum MusicTrack
{
    None,
    Overworld,
    Underground,
    Underwater,
    Castle,
    Boss,
}

// Maneja la música y los efectos de sonido del juego.
// Como el repo no trae audios, todos los sonidos se generan por código
// (estilo chiptune / NES). Si quieres usar tus propios .wav/.mp3, arrástralos
// a los arreglos "sfxOverrides" o "musicOverrides" del objeto Audio Manager.
public class AudioManager : MonoBehaviour
{
    [Serializable]
    public struct SfxOverride
    {
        public Sfx sfx;
        public AudioClip clip;
    }

    [Serializable]
    public struct MusicOverride
    {
        public MusicTrack track;
        public AudioClip clip;
    }

    private static AudioManager instance;
    private static bool quitting;

    public static AudioManager Instance
    {
        get
        {
            if (instance == null && !quitting)
            {
                GameObject go = new GameObject("Audio Manager");
                instance = go.AddComponent<AudioManager>();
            }

            return instance;
        }
    }

    public SfxOverride[] sfxOverrides;
    public MusicOverride[] musicOverrides;

    [Range(0f, 1f)] public float musicVolume = 0.3f;
    [Range(0f, 1f)] public float sfxVolume = 0.55f;

    public MusicTrack currentTrack { get; private set; }

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private readonly Dictionary<Sfx, AudioClip> sfxCache = new Dictionary<Sfx, AudioClip>();
    private readonly Dictionary<MusicTrack, AudioClip> musicCache = new Dictionary<MusicTrack, AudioClip>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
    }

    private void OnApplicationQuit()
    {
        quitting = true;
    }

    // ---------- API estática (cómoda de usar desde cualquier script) ----------

    public static void Play(Sfx sfx)
    {
        AudioManager manager = Instance;

        if (manager != null) {
            manager.PlaySfx(sfx);
        }
    }

    public static void PlayMusic(MusicTrack track)
    {
        AudioManager manager = Instance;

        if (manager != null) {
            manager.PlayTrack(track);
        }
    }

    public static void StopMusic()
    {
        AudioManager manager = Instance;

        if (manager != null) {
            manager.StopTrack();
        }
    }

    // ---------- instancia ----------

    public void PlaySfx(Sfx sfx)
    {
        AudioClip clip = GetSfx(sfx);

        if (clip != null) {
            sfxSource.PlayOneShot(clip, sfxVolume);
        }
    }

    public void PlayTrack(MusicTrack track)
    {
        if (track == currentTrack && musicSource.isPlaying) {
            return;
        }

        currentTrack = track;
        musicSource.Stop();

        if (track == MusicTrack.None) {
            return;
        }

        musicSource.clip = GetMusic(track);
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void StopTrack()
    {
        currentTrack = MusicTrack.None;
        musicSource.Stop();
    }

    private AudioClip GetSfx(Sfx sfx)
    {
        if (sfxOverrides != null)
        {
            foreach (SfxOverride o in sfxOverrides) {
                if (o.sfx == sfx && o.clip != null) return o.clip;
            }
        }

        if (!sfxCache.TryGetValue(sfx, out AudioClip clip))
        {
            clip = ChipSynth.CreateSfx(sfx);
            sfxCache[sfx] = clip;
        }

        return clip;
    }

    private AudioClip GetMusic(MusicTrack track)
    {
        if (musicOverrides != null)
        {
            foreach (MusicOverride o in musicOverrides) {
                if (o.track == track && o.clip != null) return o.clip;
            }
        }

        if (!musicCache.TryGetValue(track, out AudioClip clip))
        {
            clip = ChipSynth.CreateMusic(track);
            musicCache[track] = clip;
        }

        return clip;
    }

}

// Sintetizador muy simple de ondas cuadradas, triangulares y ruido.
// Las melodías son composiciones originales hechas para este proyecto.
public static class ChipSynth
{
    public const int Rate = 22050;

    public enum Wave
    {
        Square50,
        Square25,
        Square12,
        Triangle,
        Noise,
    }

    private static readonly System.Random random = new System.Random(1234);

    // ---------- efectos ----------

    public static AudioClip CreateSfx(Sfx sfx)
    {
        float[] buffer;

        switch (sfx)
        {
            case Sfx.Jump:
                buffer = New(0.2f);
                Tone(buffer, 0f, 0.18f, 330f, 760f, Wave.Square25, 0.5f);
                break;

            case Sfx.BigJump:
                buffer = New(0.25f);
                Tone(buffer, 0f, 0.22f, 240f, 600f, Wave.Square25, 0.5f);
                break;

            case Sfx.Coin:
                buffer = New(0.45f);
                Tone(buffer, 0f, 0.07f, 988f, 988f, Wave.Square25, 0.45f, false);
                Tone(buffer, 0.07f, 0.35f, 1319f, 1319f, Wave.Square25, 0.45f);
                break;

            case Sfx.Stomp:
                buffer = New(0.15f);
                Tone(buffer, 0f, 0.12f, 700f, 180f, Wave.Square50, 0.45f);
                Tone(buffer, 0f, 0.05f, 2000f, 2000f, Wave.Noise, 0.25f);
                break;

            case Sfx.Bump:
                buffer = New(0.12f);
                Tone(buffer, 0f, 0.1f, 180f, 90f, Wave.Triangle, 0.8f);
                break;

            case Sfx.BreakBlock:
                buffer = New(0.35f);
                Tone(buffer, 0f, 0.3f, 1800f, 300f, Wave.Noise, 0.55f);
                Tone(buffer, 0f, 0.08f, 220f, 80f, Wave.Triangle, 0.6f);
                break;

            case Sfx.PowerUpAppear:
                buffer = New(0.6f);
                for (int i = 0; i < 6; i++) {
                    Tone(buffer, i * 0.09f, 0.09f, 200f + i * 60f, 500f + i * 90f, Wave.Square25, 0.35f, false);
                }
                break;

            case Sfx.PowerUp:
                buffer = New(0.75f);
                Arpeggio(buffer, 0f, 0.045f, new[] { "C5", "E5", "G5", "C6", "E6", "G6", "D5", "F#5", "A5", "D6", "F#6", "A6", "E5", "G#5", "B5", "E6" }, Wave.Square25, 0.35f);
                break;

            case Sfx.OneUp:
                buffer = New(0.6f);
                Arpeggio(buffer, 0f, 0.09f, new[] { "E6", "G6", "E7", "C7", "D7", "G7" }, Wave.Square25, 0.35f);
                break;

            case Sfx.Kick:
                buffer = New(0.1f);
                Tone(buffer, 0f, 0.08f, 900f, 300f, Wave.Square50, 0.45f);
                break;

            case Sfx.Pipe:
            case Sfx.Shrink:
                buffer = New(0.5f);
                for (int i = 0; i < 3; i++) {
                    Tone(buffer, i * 0.15f, 0.12f, 420f, 140f, Wave.Square50, 0.4f);
                }
                break;

            case Sfx.Death:
                buffer = Score("G5.2 D5.2 B4.2 G4.4 -.2 G3.2 D3.2 G2.8", "-.8 G2.4 -.2 G2.2 D2.2 G1.8", 150f, Wave.Square25, false);
                break;

            case Sfx.Flagpole:
                buffer = New(1.1f);
                Tone(buffer, 0f, 1.0f, 1400f, 260f, Wave.Square25, 0.35f, false);
                break;

            case Sfx.StageClear:
                buffer = Score("C5.2 E5.2 G5.2 C6.4 G5.2 C6.2 D6.2 E6.10", "C3.4 G3.4 C3.4 G3.2 C4.10", 160f, Wave.Square25, false);
                break;

            case Sfx.Swim:
                buffer = New(0.12f);
                Tone(buffer, 0f, 0.1f, 500f, 820f, Wave.Square12, 0.35f);
                break;

            case Sfx.Fire:
                buffer = New(0.45f);
                Tone(buffer, 0f, 0.4f, 900f, 200f, Wave.Noise, 0.5f);
                Tone(buffer, 0f, 0.3f, 120f, 70f, Wave.Square50, 0.3f);
                break;

            case Sfx.BossHit:
                buffer = New(0.3f);
                Tone(buffer, 0f, 0.25f, 260f, 70f, Wave.Square50, 0.55f);
                Tone(buffer, 0f, 0.1f, 2500f, 600f, Wave.Noise, 0.3f);
                break;

            case Sfx.BossFall:
                buffer = New(1.7f);
                Tone(buffer, 0f, 1.6f, 700f, 50f, Wave.Square25, 0.45f);
                break;

            case Sfx.BridgeBreak:
                buffer = New(0.1f);
                Tone(buffer, 0f, 0.08f, 1500f, 500f, Wave.Noise, 0.45f);
                break;

            case Sfx.GameOver:
                buffer = Score("A4.4 G4.4 F4.4 E4.6 -.2 D4.4 E4.4 C4.12", "A2.8 F2.8 G2.8 C2.12", 110f, Wave.Square25, false);
                break;

            case Sfx.Victory:
                buffer = Score("G4.2 C5.2 E5.2 G5.4 E5.2 G5.6 -.2 A5.2 B5.2 C6.12", "C3.4 G3.4 E3.4 G3.4 F3.4 G3.4 C3.12", 150f, Wave.Square25, false);
                break;

            default:
                buffer = New(0.1f);
                break;
        }

        return MakeClip(sfx.ToString(), buffer);
    }

    // ---------- música (composiciones originales) ----------

    public static AudioClip CreateMusic(MusicTrack track)
    {
        float[] buffer;

        switch (track)
        {
            case MusicTrack.Overworld:
                buffer = Score(
                    "E5.2 G5.2 C6.2 G5.2 E5.2 G5.2 A5.4 " +
                    "A5.2 C6.2 E6.2 C6.2 B5.2 A5.2 G5.4 " +
                    "F5.2 A5.2 C6.4 A5.2 F5.2 E5.4 " +
                    "D5.2 G5.2 B5.2 D6.2 C6.2 B5.2 G5.4 " +
                    "C6.3 B5.1 C6.2 G5.2 E5.4 G5.4 " +
                    "A5.3 G5.1 A5.2 E5.2 C5.4 E5.4 " +
                    "F5.2 G5.2 A5.2 C6.2 D6.4 B5.4 " +
                    "C6.4 G5.2 E5.2 C5.8",
                    Repeat("C3.4 G3.4 C3.4 G3.4 A2.4 E3.4 A2.4 E3.4 F2.4 C3.4 F2.4 C3.4 G2.4 D3.4 G2.4 D3.4 ", 2),
                    150f, Wave.Square25, true);
                break;

            case MusicTrack.Underground:
                buffer = Score(
                    "D4.1 -.1 D5.1 -.1 A4.1 -.3 F4.1 -.1 E4.1 -.1 D4.2 -.2 " +
                    "C4.1 -.1 C5.1 -.1 G4.1 -.3 E4.1 -.1 D4.1 -.1 C4.2 -.2 " +
                    "D4.1 -.1 D5.1 -.1 A4.1 -.3 F4.1 -.1 E4.1 -.1 D4.2 -.2 " +
                    "A#3.1 -.1 A#4.1 -.1 A4.1 -.1 G4.1 -.1 F4.1 -.1 E4.1 -.1 D4.4 " +
                    "-.16",
                    "D2.2 -.6 D2.2 -.6 C2.2 -.6 C2.2 -.6 D2.2 -.6 D2.2 -.6 A#1.2 -.6 A1.2 -.6 -.16",
                    120f, Wave.Square50, true);
                break;

            case MusicTrack.Underwater:
                buffer = Score(
                    "A5.6 C6.3 A5.3 F5.6 A5.3 F5.3 D5.6 F5.3 A#5.3 C6.9 -.3 " +
                    "A5.4 G5.2 F5.6 D6.4 C6.2 A5.6 A#5.4 A5.2 G5.3 F5.3 G5.6 E5.6",
                    Repeat("F2.4 A3.4 C4.4 D2.4 F3.4 A3.4 A#1.4 D3.4 F3.4 C2.4 E3.4 G3.4 ", 2),
                    132f, Wave.Square12, true);
                break;

            case MusicTrack.Castle:
            case MusicTrack.Boss:
                string bar1 = "E4.1 F4.1 E4.1 G4.1 E4.1 F4.1 E4.1 B4.1 E4.1 F4.1 E4.1 G4.1 E4.1 F4.1 E4.1 B4.1 ";
                string bar2 = "E4.1 F4.1 E4.1 G4.1 E4.1 A#4.1 B4.1 C5.1 B4.2 A#4.2 G4.2 F4.2 ";
                string bar3 = "E4.2 G4.2 A#4.2 C#5.2 E5.4 -.4 ";
                string bass = Repeat("E2.2 ", 8) + Repeat("E2.2 ", 8) + Repeat("E2.2 ", 8) + "E2.2 G2.2 A#2.2 C#3.2 E3.4 -.4 ";
                buffer = Score(bar1 + bar2 + bar1 + bar3, bass, track == MusicTrack.Boss ? 176f : 138f, Wave.Square25, true);
                break;

            default:
                buffer = New(0.5f);
                break;
        }

        return MakeClip(track.ToString(), buffer);
    }

    // ---------- utilidades ----------

    private static string Repeat(string s, int times)
    {
        string result = "";
        for (int i = 0; i < times; i++) result += s;
        return result;
    }

    private static float[] New(float seconds)
    {
        return new float[Mathf.CeilToInt(seconds * Rate) + 1];
    }

    private static float[] Score(string melody, string bass, float bpm, Wave melodyWave, bool loop)
    {
        float step = 60f / bpm / 4f;
        int steps = Mathf.Max(CountSteps(melody), CountSteps(bass));
        float length = steps * step + (loop ? 0f : 0.3f);
        float[] buffer = New(length);

        RenderVoice(buffer, melody, step, melodyWave, 0.32f);
        RenderVoice(buffer, bass, step, Wave.Triangle, 0.55f);

        return buffer;
    }

    private static int CountSteps(string score)
    {
        int total = 0;

        foreach (string token in score.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            int dot = token.IndexOf('.');
            total += int.Parse(token.Substring(dot + 1));
        }

        return total;
    }

    private static void RenderVoice(float[] buffer, string score, float step, Wave wave, float volume)
    {
        float time = 0f;

        foreach (string token in score.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            int dot = token.IndexOf('.');
            string note = token.Substring(0, dot);
            int duration = int.Parse(token.Substring(dot + 1));

            if (note != "-")
            {
                float frequency = NoteToFrequency(note);
                Tone(buffer, time, duration * step * 0.92f, frequency, frequency, wave, volume);
            }

            time += duration * step;
        }
    }

    private static void Arpeggio(float[] buffer, float start, float noteLength, string[] notes, Wave wave, float volume)
    {
        for (int i = 0; i < notes.Length; i++)
        {
            float f = NoteToFrequency(notes[i]);
            Tone(buffer, start + i * noteLength, noteLength, f, f, wave, volume, false);
        }
    }

    public static float NoteToFrequency(string note)
    {
        int semitone;

        switch (char.ToUpper(note[0]))
        {
            case 'C': semitone = 0; break;
            case 'D': semitone = 2; break;
            case 'E': semitone = 4; break;
            case 'F': semitone = 5; break;
            case 'G': semitone = 7; break;
            case 'A': semitone = 9; break;
            default: semitone = 11; break;
        }

        int index = 1;

        if (note.Length > 1 && note[1] == '#') {
            semitone++;
            index = 2;
        } else if (note.Length > 2 && note[1] == 'b') {
            semitone--;
            index = 2;
        }

        int octave = int.Parse(note.Substring(index));
        int midi = 12 * (octave + 1) + semitone;

        return 440f * Mathf.Pow(2f, (midi - 69) / 12f);
    }

    private static void Tone(float[] buffer, float startTime, float duration, float fromFrequency, float toFrequency, Wave wave, float volume, bool decay = true)
    {
        int start = Mathf.FloorToInt(startTime * Rate);
        int count = Mathf.FloorToInt(duration * Rate);
        double phase = 0.0;
        double previousPhase = 0.0;
        float noise = 0f;
        float fade = 0.004f * Rate;

        for (int i = 0; i < count; i++)
        {
            int index = start + i;

            if (index >= buffer.Length) {
                break;
            }

            float t = (float)i / count;
            float frequency = Mathf.Lerp(fromFrequency, toFrequency, t);

            previousPhase = phase;
            phase += frequency / Rate;
            phase -= Math.Floor(phase);

            float sample;

            switch (wave)
            {
                case Wave.Square50: sample = phase < 0.5 ? 1f : -1f; break;
                case Wave.Square25: sample = phase < 0.25 ? 1f : -1f; break;
                case Wave.Square12: sample = phase < 0.125 ? 1f : -1f; break;
                case Wave.Triangle: sample = 4f * Mathf.Abs((float)phase - 0.5f) - 1f; break;
                default:
                    if (phase < previousPhase || i == 0) noise = (float)(random.NextDouble() * 2.0 - 1.0);
                    sample = noise;
                    break;
            }

            float envelope = decay ? 1f - t * 0.8f : 1f;
            float edge = Mathf.Clamp01(Mathf.Min(i, count - i) / fade);

            buffer[index] += sample * volume * envelope * edge;
        }
    }

    private static AudioClip MakeClip(string name, float[] buffer)
    {
        for (int i = 0; i < buffer.Length; i++) {
            buffer[i] = Mathf.Clamp(buffer[i], -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(name, buffer.Length, 1, Rate, false);
        clip.SetData(buffer, 0);
        return clip;
    }

}
