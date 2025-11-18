using UnityEngine;
using UnityEngine.Audio;

public class SoundtrackManager : MonoBehaviour
{
    public static SoundtrackManager Instance;

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

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.loop = true;
        audioSource.volume = musicVolume;

        // Reproducir automático si hay música asignada
        if (mainMusic != null)
            Play(mainMusic);
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
    }

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
