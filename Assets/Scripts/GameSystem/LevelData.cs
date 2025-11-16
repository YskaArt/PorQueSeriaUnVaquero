using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contiene la configuración de un nivel individual: su nombre, el contenedor visual,
/// la tilemap con scroll (si aplica), los enemigos específicos y la velocidad de scroll.
/// Permite activar o desactivar todo el nivel de manera centralizada.
/// </summary>
[Serializable]
public class LevelData
{
    public string levelName = "Level";
    public GameObject levelRoot; // Contenedor visual del nivel
    public TilemapScroller tilemapLoop; // Scroll del tilemap (opcional)
    public List<GameObject> enemyPrefabs = new List<GameObject>(); // Enemigos propios de este nivel
    public float scrollSpeed = 2f;

    public void SetActive(bool state)
    {
        if (levelRoot != null)
            levelRoot.SetActive(state);

        if (tilemapLoop != null)
            tilemapLoop.enabled = state;
    }
}
