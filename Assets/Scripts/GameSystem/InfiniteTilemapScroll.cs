using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class InfiniteTilemapLoop : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap; // Tilemap que se va a desplazar.
    [SerializeField] private int mapWidthInTiles; // Ancho del mapa en cantidad de tiles.
    [SerializeField] private int visibleHeightInTiles; // Altura visible en cantidad de tiles.
    [SerializeField] private float scrollSpeed = 2f; // Velocidad de desplazamiento vertical.
    [SerializeField] private float resetThresholdY = -1000f; // Límite en Y para reiniciar el tilemap.

    private float tileSize; // Tamaño de un tile en unidades.
    private float scrolledDistance = 0f; // Distancia acumulada desde el último "scroll" de fila completa.
    private int offsetRows = 0; // Fila inferior actual del tilemap (para scroll).

    // Diccionario que guarda la disposición inicial de los tiles (para reiniciar).
    private Dictionary<Vector3Int, TileBase> initialTiles = new Dictionary<Vector3Int, TileBase>();

    // Propiedad pública para ajustar la velocidad desde otros scripts.
    public float ScrollSpeed
    {
        get => scrollSpeed;
        set => scrollSpeed = value;
    }

    // MÉTODO: Start()
    // Calcula tamaño de tile, alinea el tilemap con la grilla y guarda el layout inicial.
    private void Start()
    {
        tileSize = tilemap.layoutGrid.cellSize.y * tilemap.transform.lossyScale.y;

        // Alinear tilemap con la grilla
        Vector3 pos = transform.position;
        pos.y = Mathf.Round(pos.y / tileSize) * tileSize;
        transform.position = pos;

        SaveInitialLayout();
    }

    // MÉTODO: Update()
    // Desplaza el tilemap hacia abajo y hace scroll de filas completas.
    // Reinicia el tilemap si alcanza el límite de Y.
    private void Update()
    {
        float delta = scrollSpeed * Time.deltaTime;
        scrolledDistance += delta;

        // Mueve el tilemap hacia abajo
        transform.position += Vector3.down * delta;

        // Scroll de filas completas
        if (scrolledDistance >= tileSize)
        {
            int steps = Mathf.FloorToInt(scrolledDistance / tileSize);
            for (int i = 0; i < steps; i++)
                ScrollOneRow();

            scrolledDistance -= steps * tileSize;
        }

        // Verifica si hay que reiniciar el tilemap
        if (transform.position.y <= resetThresholdY)
        {
            ResetTilemap();
        }
    }

    // MÉTODO: ScrollOneRow()
    // Mueve una fila de tiles del fondo al tope del tilemap, simulando scroll infinito.
    private void ScrollOneRow()
    {
        int bottomRow = offsetRows;
        int topRow = offsetRows + visibleHeightInTiles;

        for (int x = -mapWidthInTiles / 2; x <= mapWidthInTiles / 2; x++)
        {
            Vector3Int from = new Vector3Int(x, bottomRow, 0);
            Vector3Int to = new Vector3Int(x, topRow, 0);

            TileBase tile = tilemap.GetTile(from);

            tilemap.SetTile(to, tile); // Copia la fila al tope
            tilemap.SetTile(from, null); // Borra la fila inferior
        }

        offsetRows++;
    }

    // MÉTODO: SaveInitialLayout()
    // Guarda la disposición inicial de todos los tiles del tilemap.
    private void SaveInitialLayout()
    {
        initialTiles.Clear();
        BoundsInt bounds = tilemap.cellBounds;

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            TileBase tile = tilemap.GetTile(pos);
            if (tile != null)
                initialTiles[pos] = tile;
        }
    }

    // MÉTODO: ResetTilemap()
    // Reinicia el tilemap a su disposición inicial y resetea variables de scroll.
    public void ResetTilemap()
    {
        tilemap.ClearAllTiles();

        foreach (var pair in initialTiles)
        {
            tilemap.SetTile(pair.Key, pair.Value);
        }

        scrolledDistance = 0f;
        offsetRows = 0;
        transform.position = Vector3.zero;
    }
}
