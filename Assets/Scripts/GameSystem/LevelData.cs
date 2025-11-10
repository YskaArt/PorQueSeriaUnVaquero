using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelData
{
    public string levelName = "Level";
    public GameObject levelRoot; // Contenedor con todos los objetos visuales del nivel
    public TilemapScroller tilemapLoop; // Si el nivel usa Tilemap con scroll
    public List<GameObject> enemyPrefabs = new List<GameObject>(); // Prefabs específicos de este level
    public float scrollSpeed = 2f;

    /// <summary>
    /// Activa/desactiva el root y la tilemap (si existe).
    /// </summary>
    public void SetActive(bool state)
    {
        if (levelRoot != null)
            levelRoot.SetActive(state);

        if (tilemapLoop != null)
            tilemapLoop.enabled = state;
    }
  

}
