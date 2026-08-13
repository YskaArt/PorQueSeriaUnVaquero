/*
 * UIWarmupList
 * -------------
 * Lista de paneles/GameObjects que arrancan DESACTIVADOS en la escena (menús,
 * paneles de misiones, tienda, ajustes, etc.). Unity difiere Awake()/Start()
 * de un GameObject inactivo hasta que se activa por primera vez -- así que
 * aunque un botón registre su listener en Awake(), si el panel que lo
 * contiene arranca apagado, ese Awake() recién corre cuando el jugador lo
 * abre por primera vez, y ahí puede sentirse como "el primer toque no hizo
 * nada".
 *
 * WarmUpAllRoutine() activa todos los paneles de la lista, espera un par de
 * frames (para que corran Awake/OnEnable/Start de todo lo que contienen), y
 * los vuelve a desactivar. Pensado para llamarse desde GameManager mientras
 * la pantalla de carga sigue tapando todo -- el jugador nunca ve este
 * parpadeo.
 *
 * USO EN LA ESCENA:
 * - Agregar este componente a cualquier GameObject (por ejemplo, al mismo
 *   GameManager, o a uno dedicado).
 * - Arrastrar al array "panelsToWarmUp" TODOS los paneles que arrancan
 *   desactivados: UpgradePanel, MissionsHub (el DialogRoot), ShopPanel,
 *   QuestsPanel, ReincarnationPanel, SettingsPanel, etc.
 * - NO incluir paneles con efectos secundarios reales al abrirse (por
 *   ejemplo, algo que dispare un anuncio o gaste un recurso apenas se activa).
 *   Los paneles de toggle simples (abrir/cerrar UI) son justamente el caso
 *   ideal para esto.
 */

using System.Collections;
using UnityEngine;

public class UIWarmupList : MonoBehaviour
{
    [Tooltip("Todos los paneles que arrancan desactivados y se quieren precalentar durante la carga.")]
    [SerializeField] private GameObject[] panelsToWarmUp;

    [Tooltip("Frames extra a esperar con los paneles abiertos antes de cerrarlos, para asegurar que corran todos los Start().")]
    [SerializeField] private int extraFramesToWait = 2;

    public IEnumerator WarmUpAllRoutine()
    {
        if (panelsToWarmUp == null || panelsToWarmUp.Length == 0)
            yield break;

        foreach (var p in panelsToWarmUp)
            if (p != null) p.SetActive(true);

        // Start() de objetos recién activados corre antes del próximo Update,
        // no de forma sincrónica dentro de SetActive -- esperamos un par de
        // frames por las dudas (paneles con jerarquías más profundas).
        for (int i = 0; i < Mathf.Max(1, extraFramesToWait); i++)
            yield return null;

        foreach (var p in panelsToWarmUp)
            if (p != null) p.SetActive(false);

        Debug.Log($"[UIWarmupList] {panelsToWarmUp.Length} panel(es) precalentado(s).");
    }
}
