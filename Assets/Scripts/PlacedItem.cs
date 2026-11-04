using UnityEngine;

public class PlacedItem : MonoBehaviour
{
    public Item item; 
    public Vector3Int origin;
    public bool rotated;

    public Vector2Int Size()
    {
        Vector2Int size = item.GetFootprint();
        return rotated ? new Vector2Int(size.y, size.x) : size;
    }
}
