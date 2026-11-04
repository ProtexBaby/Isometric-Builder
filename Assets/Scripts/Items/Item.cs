using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Isometric Builder/Item")]
public class Item : ScriptableObject
{
    public string displayName = "Item";
    public ItemCategory category = ItemCategory.Furniture;
    public Vector2Int footprint = Vector2Int.one;
    public Sprite sprite;

    public Vector2Int GetFootprint()
    {
        return footprint;
    }

    public Sprite GetSprite()
    {
        return sprite;
    }
}
