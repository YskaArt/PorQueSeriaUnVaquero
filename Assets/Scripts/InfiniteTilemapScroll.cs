using UnityEngine;
using UnityEngine.Tilemaps;

public class InfiniteTilemapLoop : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private int mapWidthInTiles;             // Ancho del mapa en tiles 
    [SerializeField] private int visibleHeightInTiles;       // Alto del área visible en tiles

    [SerializeField] private float scrollSpeed = 2f;
    
    private float tileSize;             // Se obtiene desde el Grid
    private float scrolledDistance = 0f;
    private int offsetRows = 0;

    public float ScrollSpeed
    {
        get => scrollSpeed;
        set => scrollSpeed = value;
    }

    void Start()
    {
        tileSize = tilemap.layoutGrid.cellSize.y * tilemap.transform.lossyScale.y;

        Vector3 pos = transform.position;
        pos.y = Mathf.Round(pos.y / tileSize) * tileSize;
        transform.position = pos;
    }

    void Update()
    {
        float delta = scrollSpeed * Time.deltaTime;
        scrolledDistance += delta;

        // Mover visualmente el Tilemap de manera continua
        transform.position += Vector3.down * delta;

        // Cuando se ha desplazado al menos 1 tile en altura
        if (scrolledDistance >= tileSize)
        {
            int steps = Mathf.FloorToInt(scrolledDistance / tileSize);

            for (int i = 0; i < steps; i++)
                ScrollOneRow();

            scrolledDistance -= steps * tileSize;
        }
    }

    void ScrollOneRow()
    {
        int bottomRow = offsetRows;
        int topRow = offsetRows + visibleHeightInTiles;

        for (int x = -mapWidthInTiles / 2; x <= mapWidthInTiles / 2; x++)
        {
            Vector3Int from = new Vector3Int(x, bottomRow, 0);
            Vector3Int to = new Vector3Int(x, topRow, 0);

            TileBase tile = tilemap.GetTile(from);

            // Copiamos el tile inferior y lo colocamos en la parte superior
            tilemap.SetTile(to, tile);

            // Eliminamos el tile inferior
            tilemap.SetTile(from, null);
        }

        offsetRows++;
    }
}
