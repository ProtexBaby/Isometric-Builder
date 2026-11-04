using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class LayoutSaver : MonoBehaviour
{
    const string FileName = "layout.json";

    [SerializeField] PlacementController placement;
    // ใช้หาไอเทมจาก id 
    [SerializeField] ItemCatalog catalog; 

    static string SavePath => Path.Combine(Application.persistentDataPath, FileName);
    static string TempPath => SavePath + ".tmp";

    bool loaded; 

    void Start()
    {
        Load();
        loaded = true;
    }

    void OnApplicationPause(bool paused)
    {
        if (paused) Save();
    }

    public void Save()
    {
        if (!loaded) return;

        SaveData data = new SaveData();
        foreach (PlacedItem placed in placement.AllPlaced())
        {
            data.items.Add(new PlacedData
            {
                id = placed.item.id,
                x = placed.origin.x,
                y = placed.origin.y,
                rotated = placed.rotated,
            });
        }

        try
        {
            // เขียนไฟล์ .tmp ให้เสร็จก่อนแล้วค่อยแทนที่ไฟล์จริง — ถ้าเครื่องดับกลางทาง ไฟล์เก่ายังอยู่ครบ
            File.WriteAllText(TempPath, JsonUtility.ToJson(data, true));
            if (File.Exists(SavePath)) File.Replace(TempPath, SavePath, null);
            else File.Move(TempPath, SavePath);
        }
        catch (IOException e)
        {
            Debug.LogWarning($"Save failed: {e.Message}");
        }
    }

    // อ่านไฟล์แล้ววาง object ตามเดิม
    public void Load()
    {
        // ไม่มีไฟล์จริงแต่มี .tmp = เครื่องดับหลังเขียน .tmp เสร็จ ก่อนแทนที่ → ใช้ .tmp
        string path = File.Exists(SavePath) ? SavePath : TempPath;
        if (!File.Exists(path)) return;

        SaveData data;
        try
        {
            data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Load failed, starting empty: {e.Message}");
            return;
        }
        if (data == null || data.items == null) return;

        // id object (สร้างครั้งเดียว ไม่ต้องไล่หาใน Catalog ทุกชิ้น)
        Dictionary<string, Item> byId = new Dictionary<string, Item>();
        foreach (Item item in catalog.items)
        {
            if (item != null && !string.IsNullOrEmpty(item.id)) byId[item.id] = item;
        }

        int skipped = 0;
        foreach (PlacedData d in data.items)
        {
            if (d.id == null || !byId.TryGetValue(d.id, out Item item)
                || !placement.TrySpawn(item, new Vector3Int(d.x, d.y, 0), d.rotated))
            {
                skipped++;
            }
        }
        if (skipped > 0) Debug.LogWarning($"Load: skipped {skipped} item(s) that can no longer be placed");
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Save/Clear Save")]
    static void ClearSave()
    {
        if (File.Exists(SavePath)) File.Delete(SavePath);
        if (File.Exists(TempPath)) File.Delete(TempPath);
        Debug.Log($"Save cleared: {SavePath}");
    }

    [UnityEditor.MenuItem("Tools/Save/Open Save Folder")]
    static void OpenSaveFolder()
    {
        UnityEditor.EditorUtility.RevealInFinder(Application.persistentDataPath);
    }
#endif
}