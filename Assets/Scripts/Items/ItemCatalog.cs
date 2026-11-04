using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemCatalog", menuName = "Isometric Builder/ItemCatalog")]
public class ItemCatalog : ScriptableObject
{
    public List<Item> items = new List<Item>();
}
