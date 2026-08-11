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
        // Si alguien ya los puso en la escena, no duplicar
        if (MasteryManager.Instance != null) return;

        var go = new GameObject("ProgressionSystems");
        go.AddComponent<MasteryManager>();
     
        go.AddComponent<ShopManager>();
        go.AddComponent<OfflineEarningsManager>();
        Object.DontDestroyOnLoad(go);

        Debug.Log("[ProgressionBootstrap] ProgressionSystems creado.");
    }
}
