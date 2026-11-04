public enum ZoneType
{
    None = 0,
    Furniture = 1,
    Mixed = 2
}

public static class ZoneRules
{
    public static bool Allows(ZoneType zone, ItemCategory category)
    {
        switch (zone)
        {
            case ZoneType.Furniture: return category == ItemCategory.Furniture;
            case ZoneType.Mixed: return true;
            default: return false;
        }
    }
}