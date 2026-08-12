/*
 * ProgressionBootstrap
 * --------------------
 * Crea automáticamente el GameObject persistente con los managers de progresión
 * (Maestría, Misiones Diarias y Tienda) al iniciar el juego, en cualquier escena.
 *
 * Gracias a esto NO hace falta agregar estos managers a mano en las escenas:
 * solo la UI (paneles) necesita wiring en el editor.
 */

using UnityEngine;

public static class ProgressionBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        // GameObject compartido: si ya existe (por un Init anterior), lo reusamos.
        // Cada manager se revisa POR SEPARADO (en vez de asumir "todo o nada") para
        // cubrir el caso de una creación parcial en una sesión anterior sin domain reload.
        GameObject go = MasteryManager.Instance != null ? MasteryManager.Instance.gameObject : null;

        if (go == null && DailyMissionManager.Instance != null) go = DailyMissionManager.Instance.gameObject;
        if (go == null && ZoneMissionManager.Instance != null) go = ZoneMissionManager.Instance.gameObject;
        if (go == null && ShopManager.Instance != null) go = ShopManager.Instance.gameObject;
        if (go == null && OfflineEarningsManager.Instance != null) go = OfflineEarningsManager.Instance.gameObject;

        bool isNewObject = go == null;
        if (isNewObject) go = new GameObject("ProgressionSystems");

        if (MasteryManager.Instance == null) go.AddComponent<MasteryManager>();
        if (DailyMissionManager.Instance == null) go.AddComponent<DailyMissionManager>();
        if (ZoneMissionManager.Instance == null) go.AddComponent<ZoneMissionManager>();
        if (ShopManager.Instance == null) go.AddComponent<ShopManager>();
        if (OfflineEarningsManager.Instance == null) go.AddComponent<OfflineEarningsManager>();

        if (isNewObject) Object.DontDestroyOnLoad(go);

        Debug.Log("[ProgressionBootstrap] ProgressionSystems verificado/creado.");
    }
}