using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class InfiniteTilemapLoop : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private int mapWidthInTiles;
    [SerializeField] private int visibleHeightInTiles;
    [SerializeField] private float scrollSpeed = 2f;
    [SerializeField] private float resetThresholdY = -1000f; // ← Límite Y para reiniciar

    private float tileSize;
    private float scrolledDistance = 0f;
    private int offsetRows = 0;

    private Dictionary<Vector3Int, TileBase> initialTiles = new Dictionary<Vector3Int, TileBase>();

    public float ScrollSpeed
    {
        get => scrollSpeed;
        set => scrollSpeed = value;
    }

    private void Start()
    {
        tileSize = tilemap.layoutGrid.cellSize.y * tilemap.transform.lossyScale.y;

        // Alinear con la grilla
        Vector3 pos = transform.position;
        pos.y = Mathf.Round(pos.y / tileSize) * tileSize;
        transform.position = pos;

        SaveInitialLayout();
    }

    private void Update()
    {
        float delta = scrollSpeed * Time.deltaTime;
        scrolledDistance += delta;

        transform.position += Vector3.down * delta;

        if (scrolledDistance >= tileSize)
        {
            int steps = Mathf.FloorToInt(scrolledDistance / tileSize);
            for (int i = 0; i < steps; i++)
                ScrollOneRow();

            scrolledDistance -= steps * tileSize;
        }

        // 🚨 Verifica si debe reiniciarse
        if (transform.position.y <= resetThresholdY)
        {
            ResetTilemap();
        }
    }

    private void ScrollOneRow()
    {
        int bottomRow = offsetRows;
        int topRow = offsetRows + visibleHeightInTiles;

        for (int x = -mapWidthInTiles / 2; x <= mapWidthInTiles / 2; x++)
        {
            Vector3Int from = new Vector3Int(x, bottomRow, 0);
            Vector3Int to = new Vector3Int(x, topRow, 0);

            TileBase tile = tilemap.GetTile(from);

            tilemap.SetTile(to, tile);
            tilemap.SetTile(from, null);
        }

        offsetRows++;
    }

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
