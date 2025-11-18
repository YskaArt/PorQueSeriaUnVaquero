using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Sistema de scroll infinito para Tilemaps.
/// Utiliza dos Tilemaps idénticos posicionados uno encima del otro. 
/// A medida que ambos se desplazan, cuando uno sale completamente del área visible 
/// se reposiciona arriba/abajo para simular un fondo continuo.
/// También permite modificar y guardar/restaurar la velocidad del scroll.
/// </summary>
[RequireComponent(typeof(TilemapRenderer))]
public class TilemapScroller : MonoBehaviour
{
    [Header("Tilemaps")]
    [SerializeField] private Tilemap tilemapA;
    [SerializeField] private Tilemap tilemapB;
    [SerializeField] private Grid grid;

    [Header("Configuración del mapa (en celdas)")]
    [SerializeField] private int mapWidthInTiles;
    [SerializeField] private int mapHeightInTiles = 15;

    [Header("Velocidad del desplazamiento")]
    [SerializeField] private float scrollSpeed = 2f; // tiles/segundo

    [Header("Parámetros opcionales")]
    [SerializeField] private bool scrollDown = true;

    private float tileHeight;
    private float scrollOffset = 0f;

    private Tilemap activeMap;
    private Tilemap hiddenMap;

    private void Start()
    {
        if (grid == null)
            grid = GetComponentInParent<Grid>();

        tileHeight = grid.cellSize.y * tilemapA.transform.lossyScale.y;

        // Coloca ambos tilemaps uno encima del otro
        tilemapA.transform.localPosition = Vector3.zero;
        tilemapB.transform.localPosition = new Vector3(0, mapHeightInTiles * tileHeight, 0);

        activeMap = tilemapA;
        hiddenMap = tilemapB;
    }

    private void Update()
    {
        float delta = scrollSpeed * tileHeight * Time.deltaTime;
        scrollOffset += delta * (scrollDown ? -1f : 1f);

        // Mueve ambos tilemaps según la dirección configurada
        tilemapA.transform.localPosition += Vector3.down * (scrollDown ? delta : -delta);
        tilemapB.transform.localPosition += Vector3.down * (scrollDown ? delta : -delta);

        // Reposiciona tilemaps cuando salen del área visible
        if (scrollDown)
        {
            if (tilemapA.transform.localPosition.y <= -mapHeightInTiles * tileHeight)
                RecycleTilemap(tilemapA, tilemapB);
            else if (tilemapB.transform.localPosition.y <= -mapHeightInTiles * tileHeight)
                RecycleTilemap(tilemapB, tilemapA);
        }
        else
        {
            if (tilemapA.transform.localPosition.y >= mapHeightInTiles * tileHeight)
                RecycleTilemap(tilemapA, tilemapB);
            else if (tilemapB.transform.localPosition.y >= mapHeightInTiles * tileHeight)
                RecycleTilemap(tilemapB, tilemapA);
        }
    }

    /// <summary>
    /// Reposiciona el Tilemap que salió de pantalla por encima del otro 
    /// para mantener el bucle infinito.
    ///</summary>
    private void RecycleTilemap(Tilemap toMove, Tilemap reference)
    {
        float direction = scrollDown ? 1f : -1f;
        float newY = reference.transform.localPosition.y + direction * mapHeightInTiles * tileHeight;
        toMove.transform.localPosition = new Vector3(0, newY, 0);
    }

    // === Getters / Setters de velocidad ===
    public float GetScrollSpeed() => scrollSpeed;
    public void SetScrollSpeed(float newSpeed) => scrollSpeed = newSpeed;

    // === Guardar / Restaurar velocidad original ===
    private float savedScrollSpeed = float.NaN;

    public void SaveOriginalSpeed()
    {
        if (float.IsNaN(savedScrollSpeed))
            savedScrollSpeed = scrollSpeed;
    }

    public void RestoreOriginalSpeed()
    {
        if (!float.IsNaN(savedScrollSpeed))
        {
            scrollSpeed = savedScrollSpeed;
            savedScrollSpeed = float.NaN;
        }
        else
        {
            Debug.LogWarning("[TilemapScroller] RestoreOriginalSpeed() llamado sin velocidad guardada.");
        }
    }
}
