using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    public int version = 1;
    public List<PlacedData> items = new List<PlacedData>();
}

[System.Serializable]
public class PlacedData
{
    public string id;
    public int x, y;
    public bool rotated;
}
