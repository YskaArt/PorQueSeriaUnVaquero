using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(TilemapRenderer))]
public class TilemapScroller : MonoBehaviour
{
    [Header("Tilemaps")]
    [SerializeField] private Tilemap tilemapA;  // Primer Tilemap
    [SerializeField] private Tilemap tilemapB;  // Segundo Tilemap (duplicado del primero)
    [SerializeField] private Grid grid;          // Referencia al Grid padre (opcional)

    [Header("Configuración del mapa (en celdas)")]
    [SerializeField] private int mapWidthInTiles = 10;
    [SerializeField] private int mapHeightInTiles = 15;

    [Header("Velocidad del desplazamiento")]
    [SerializeField] private float scrollSpeed = 2f; // tiles por segundo

    [Header("Parámetros opcionales")]
    [SerializeField] private bool scrollDown = true; // true = jugador sube

    private float tileHeight;
    private float scrollOffset = 0f;
    private Tilemap activeMap;
    private Tilemap hiddenMap;

    private void Start()
    {
        if (grid == null)
            grid = GetComponentInParent<Grid>();

        tileHeight = grid.cellSize.y * tilemapA.transform.lossyScale.y;

        // Posicionamos los tilemaps uno encima del otro
        tilemapA.transform.localPosition = Vector3.zero;
        tilemapB.transform.localPosition = new Vector3(0, mapHeightInTiles * tileHeight, 0);

        activeMap = tilemapA;
        hiddenMap = tilemapB;
    }

    private void Update()
    {
        float delta = scrollSpeed * tileHeight * Time.deltaTime;
        scrollOffset += delta * (scrollDown ? -1f : 1f);

        // Mueve ambos tilemaps
        tilemapA.transform.localPosition += Vector3.down * (scrollDown ? delta : -delta);
        tilemapB.transform.localPosition += Vector3.down * (scrollDown ? delta : -delta);

        // Cuando uno se sale completamente de pantalla, lo reposicionamos arriba
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

    private void RecycleTilemap(Tilemap toMove, Tilemap reference)
    {
        float direction = scrollDown ? 1f : -1f;
        float newY = reference.transform.localPosition.y + direction * mapHeightInTiles * tileHeight;
        toMove.transform.localPosition = new Vector3(0, newY, 0);
        // Podés agregar aquí un método para cambiar los tiles si querés variación visual
    }

    // === MÉTODOS EXISTENTES ===
    public float GetScrollSpeed() => scrollSpeed;
    public void SetScrollSpeed(float newSpeed) => scrollSpeed = newSpeed;

    // === Guardar / Restaurar velocidad ===
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
            Debug.LogWarning("[TilemapScroller] RestoreOriginalSpeed() llamado sin una velocidad guardada.");
        }
    }
}
