/*
    AudioSettingsUI
    ---------------
    Agrega a la pestana de ajustes del menu de mejoras dos controles de volumen:
    Musica y Efectos. Se arma por codigo (igual que el boton "Como jugar"), asi que no
    hay que tocar la escena: al cargar una escena que tenga UpgradeMenuController,
    construye los sliders dentro del contenido de ajustes.

    - Musica  -> SoundtrackManager.SetVolume
    - Efectos -> SFXManager.SetMasterVolume (al soltar el slider suena un efecto de muestra)

    Los volumenes se guardan en PlayerPrefs desde los propios managers, no en el save del
    juego: son preferencias del dispositivo y no se pierden al reiniciar la partida.
*/

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class AudioSettingsUI
{
    // Textos (cuando se agregue el ingles, cambiar solo aca).
    private const string MusicLabel = "Música";
    private const string SfxLabel = "Efectos";
    private const string SampleSfxId = "Attack";

    private const string RootName = "AudioSettings";

    // Posiciones dentro del contenido de ajustes (origen en el centro, y hacia arriba).
    private const float MusicLabelY = 390f;
    private const float MusicSliderY = 345f;
    private const float SfxLabelY = 300f;
    private const float SfxSliderY = 255f;

    private static readonly Color TrackColor = new Color(0.16f, 0.1f, 0.06f, 1f);
    private static readonly Color FillColor = new Color(0.85f, 0.55f, 0.15f, 1f);
    private static readonly Color HandleColor = new Color(1f, 0.82f, 0.25f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        // Por si el dominio no se recarga entre sesiones del editor: evita suscribir dos veces.
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryBuild();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TryBuild();

    private static void TryBuild()
    {
        var menu = Object.FindFirstObjectByType<UpgradeMenuController>(FindObjectsInactive.Include);
        if (menu == null || menu.SettingsScroll == null || menu.SettingsScroll.content == null) return;

        var content = menu.SettingsScroll.content;
        if (content.Find(RootName) != null) return;

        TMP_FontAsset font = null;
        var existing = content.GetComponentInChildren<TextMeshProUGUI>(true);
        if (existing != null) font = existing.font;
        if (font == null) font = TMP_Settings.defaultFontAsset;

        var root = new GameObject(RootName, typeof(RectTransform));
        root.transform.SetParent(content, false);
        var rootRt = (RectTransform)root.transform;
        rootRt.anchorMin = rootRt.anchorMax = new Vector2(0.5f, 0.5f);
        rootRt.anchoredPosition = Vector2.zero;
        rootRt.sizeDelta = Vector2.zero;

        BuildRow(rootRt, font, MusicLabel, MusicLabelY, MusicSliderY,
            GetMusicVolume(), SetMusicVolume, playSampleOnRelease: false);
        BuildRow(rootRt, font, SfxLabel, SfxLabelY, SfxSliderY,
            GetSfxVolume(), SetSfxVolume, playSampleOnRelease: true);
    }

    // ---- acceso a los managers (si todavia no existen se usa lo guardado) ----

    private static float GetMusicVolume() =>
        SoundtrackManager.Instance != null ? SoundtrackManager.Instance.Volume : SoundtrackManager.LoadSavedVolume(0.5f);

    private static float GetSfxVolume() =>
        SFXManager.Instance != null ? SFXManager.Instance.MasterVolume : SFXManager.LoadSavedVolume(1f);

    private static void SetMusicVolume(float v)
    {
        if (SoundtrackManager.Instance != null) SoundtrackManager.Instance.SetVolume(v);
    }

    private static void SetSfxVolume(float v)
    {
        if (SFXManager.Instance != null) SFXManager.Instance.SetMasterVolume(v);
    }

    // ---- UI ----

    private static void BuildRow(RectTransform parent, TMP_FontAsset font, string label, float labelY, float sliderY,
        float initial, System.Action<float> onChanged, bool playSampleOnRelease)
    {
        var text = CreateLabel(parent, font, labelY);
        var slider = CreateSlider(parent, sliderY);

        void Refresh(float v)
        {
            text.text = TutorialManager.Fit($"{label}: {Mathf.RoundToInt(v * 100f)}%", font);
        }

        slider.SetValueWithoutNotify(initial);
        Refresh(initial);

        slider.onValueChanged.AddListener(v =>
        {
            onChanged(v);
            Refresh(v);
        });

        // Al soltar el slider se guarda (no en cada tick del arrastre) y, en efectos, suena una muestra.
        var relay = slider.gameObject.AddComponent<PointerUpRelay>();
        relay.onPointerUp = () =>
        {
            PlayerPrefs.Save();
            if (playSampleOnRelease) SFXManager.Instance?.Play(SampleSfxId);
        };
    }

    private static TextMeshProUGUI CreateLabel(RectTransform parent, TMP_FontAsset font, float y)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(480f, 50f);
        rt.anchoredPosition = new Vector2(0f, y);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize = 36;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Slider CreateSlider(RectTransform parent, float y)
    {
        var go = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(parent, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(460f, 50f);
        rt.anchoredPosition = new Vector2(0f, y);

        // Pista
        var background = CreateImage(rt, "Background", TrackColor);
        Stretch(background.rectTransform, new Vector2(0f, 0.3f), new Vector2(1f, 0.7f), Vector2.zero, Vector2.zero);

        // Relleno
        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(rt, false);
        Stretch((RectTransform)fillArea.transform, new Vector2(0f, 0.3f), new Vector2(1f, 0.7f),
            new Vector2(10f, 0f), new Vector2(-10f, 0f));

        var fill = CreateImage((RectTransform)fillArea.transform, "Fill", FillColor);
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = new Vector2(0f, 1f);
        fill.rectTransform.sizeDelta = new Vector2(10f, 0f);

        // Manija
        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(rt, false);
        Stretch((RectTransform)handleArea.transform, Vector2.zero, Vector2.one, new Vector2(20f, 0f), new Vector2(-20f, 0f));

        var handle = CreateImage((RectTransform)handleArea.transform, "Handle", HandleColor);
        handle.rectTransform.sizeDelta = new Vector2(40f, 0f);
        handle.rectTransform.anchorMin = new Vector2(0f, 0f);
        handle.rectTransform.anchorMax = new Vector2(0f, 1f);

        var slider = go.GetComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        return slider;
    }

    private static Image CreateImage(RectTransform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static void Stretch(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    /// <summary>Avisa cuando se suelta el slider (para reproducir un sonido de muestra).</summary>
    private class PointerUpRelay : MonoBehaviour, IPointerUpHandler
    {
        public System.Action onPointerUp;
        public void OnPointerUp(PointerEventData eventData) => onPointerUp?.Invoke();
    }
}
