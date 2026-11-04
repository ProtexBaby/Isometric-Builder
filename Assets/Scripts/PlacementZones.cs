using UnityEngine;
using UnityEngine.Tilemaps;

public class PlacementZones : MonoBehaviour
{
    [SerializeField] Tilemap zoneMap;
    [SerializeField] TileBase furnitureZone;
    [SerializeField] TileBase mixedZone;

    private void Start()
    {
        zoneMap.GetComponent<TilemapRenderer>().enabled = true;
    }

    public bool Allows(Vector3Int cell, ItemCategory category)
    {
        TileBase tile = zoneMap.GetTile(cell);
        if (tile == mixedZone) return true;
        if (tile == furnitureZone)
        {
            return category == ItemCategory.Furniture;
        }
        return false;
    }
}
