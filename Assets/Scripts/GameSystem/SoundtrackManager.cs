using UnityEngine;
using UnityEngine.Audio;

public class SoundtrackManager : MonoBehaviour
{
    public static SoundtrackManager Instance;

    private const string VolumePrefKey = "MusicVolume";

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Tracks")]
    [SerializeField] private AudioClip mainMusic;

    [Header("Volumen")]
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.5f;

    private void Awake()
    {
        // Singleton seguro
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Volumen elegido por el jugador (si existe); si no, el del inspector.
        musicVolume = LoadSavedVolume(musicVolume);

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.loop = true;
        audioSource.volume = musicVolume;

        // Reproducir automático si hay música asignada
        if (mainMusic != null)
            Play(mainMusic);

        Debug.Log($"[SoundtrackManager] volumen={musicVolume} (guardado: {(PlayerPrefs.HasKey(VolumePrefKey) ? PlayerPrefs.GetFloat(VolumePrefKey).ToString() : "ninguno")}) | clip={(mainMusic != null ? mainMusic.name : "NINGUNO")} | sonando={audioSource.isPlaying}");
    }

    /// <summary>
    /// Reproduce una pista. Si ya está sonando la misma, no reinicia.
    /// </summary>
    public void Play(AudioClip clip)
    {
        if (clip == null) return;

        if (audioSource.clip == clip && audioSource.isPlaying)
            return; // Prevent restart

        audioSource.clip = clip;
        audioSource.Play();
    }

    /// <summary>
    /// Cambia el volumen de la música global.
    /// </summary>
    public void SetVolume(float v)
    {
        musicVolume = Mathf.Clamp01(v);

        if (audioSource != null)
            audioSource.volume = musicVolume;

        PlayerPrefs.SetFloat(VolumePrefKey, musicVolume);
    }

    public float Volume => musicVolume;

    public static float LoadSavedVolume(float fallback) =>
        Mathf.Clamp01(PlayerPrefs.GetFloat(VolumePrefKey, fallback));

    /// <summary>
    /// Detiene la música completamente.
    /// </summary>
    public void Stop()
    {
        audioSource.Stop();
    }

    /// <summary>
    /// Retoma la música si estaba pausada.
    /// </summary>
    public void Resume()
    {
        if (!audioSource.isPlaying)
            audioSource.Play();
    }

    /// <summary>
    /// Pausa momentáneamente la música (sin reiniciar).
    /// </summary>
    public void Pause()
    {
        audioSource.Pause();
    }
}
