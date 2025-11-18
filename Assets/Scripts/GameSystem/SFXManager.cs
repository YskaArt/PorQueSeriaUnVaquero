/*
 * SFXManager
 * ----------------------------------------------------------
 * Este script administra todos los efectos de sonido del juego
 * desde un único punto centralizado.
 *
 * Permite:
 *  - Reproducir efectos de sonido por nombre o por referencia.
 *  - Evitar duplicación de AudioSources en la escena.
 *  - Manejar volumen global de SFX.
 *  - Ser llamado fácilmente desde otros scripts con:
 *          SFXManager.Instance.Play("Jump");
 *
 * NOTA:
 * Este script está diseñado para escalar y agregar más sonidos
 * sin modificar código, simplemente añadiéndolos desde el Inspector.
 * ----------------------------------------------------------
 */

using UnityEngine;
using System.Collections.Generic;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    [System.Serializable]
    private class SFXData
    {
        public string id;                  // Nombre que se usa para llamar el sonido
        public AudioClip clip;             // Clip asociado
        [Range(0f, 1f)] public float volume = 1f; // Volumen individual del clip
    }

    [SerializeField] private List<SFXData> sfxList = new List<SFXData>();
    [SerializeField] private AudioSource sfxSource;   // Fuente por la cual se reproducen los SFX
    [SerializeField] private float masterVolume = 1f; // Volumen global de SFX

    private Dictionary<string, SFXData> sfxDictionary;

    private void Awake()
    {
        // Implementación Singleton sencilla
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Convertimos la lista en diccionario para accesos rápidos
        sfxDictionary = new Dictionary<string, SFXData>();
        foreach (var sfx in sfxList)
        {
            if (!sfxDictionary.ContainsKey(sfx.id))
                sfxDictionary.Add(sfx.id, sfx);
        }

        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();
    }

    // ----------------------------------------------
    // Reproducir un sonido por ID (string)
    // ----------------------------------------------
    public void Play(string id)
    {
        if (sfxDictionary.TryGetValue(id, out var sfx))
        {
            sfxSource.PlayOneShot(sfx.clip, sfx.volume * masterVolume);
        }
        else
        {
            Debug.LogWarning($"[SFXManager] No se encontró el SFX con ID: {id}");
        }
    }

    // ----------------------------------------------
    // Ajustar volumen global
    // ----------------------------------------------
    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
    }
}
