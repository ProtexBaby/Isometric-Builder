using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class PlacementController : MonoBehaviour
{
    [SerializeField] Tilemap floor;
    [SerializeField] SpriteRenderer ghost;
    [SerializeField] Transform placedParent;
    [SerializeField] Color validColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] Color invalidColor = new Color(1f, 1f, 1f, 1f);

    [Header("Footprint area")]
    [SerializeField] Tilemap footprintMap;
    [SerializeField] TileBase footprintTile;
    [SerializeField] Color validAreaColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] Color invalidAreaColor = new Color(1f, 1f, 1f, 1f);

    [SerializeField, Range(0.5f, 1f)] float fill = 0.9f;

    Item current;
    Dictionary<Vector3Int, GameObject> saveSlot = new Dictionary<Vector3Int, GameObject>();

    private void Start()
    {
        ghost.gameObject.SetActive(false);
    }

    public void BeginPlacement(Item item)
    {
        current = item;
        ghost.sprite = item.GetSprite();
        ghost.sortingOrder = short.MaxValue;
        ghost.gameObject.SetActive(true);
    }

    public void cancelPlacement()
    {
        current = null;
        ghost.gameObject.SetActive(false);
        footprintMap.ClearAllTiles();
    }

    private void Update()
    {
        if (current == null) return;
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.rightButton.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            cancelPlacement();
            return;
        }

        Vector3 world = Camera.main.ScreenToWorldPoint(mouse.position.ReadValue());
        world.z = 0f;
        Vector3Int cell = floor.WorldToCell(world);
        cell.z = 0;
        Vector2Int size = current.GetFootprint();
        bool canPlace = CanPlace(cell, size);

        FitToCells(ghost, cell, size);
        ghost.color = canPlace ? validColor : invalidColor;
        ShowFootprint(cell, size, canPlace);

        //bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        //if (canPlace && !overUI && mouse.leftButton.wasPressedThisFrame)
        //    Place(cell, size);

        if (canPlace && mouse.leftButton.wasPressedThisFrame)
            Place(cell, size);
    }

    // เช็คว่าวางได้ไหม
    bool CanPlace(Vector3Int origin, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector3Int c = origin + new Vector3Int(x, y, 0);
                if (!floor.HasTile(c) || saveSlot.ContainsKey(c)) return false;
            }
        }
        return true;
    }

    // แสดงช่องที่ว่างได้
    void ShowFootprint(Vector3Int origin, Vector2Int size, bool canPlace)
    {
        footprintMap.ClearAllTiles();
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                footprintMap.SetTile(origin + new Vector3Int(x, y, 0), footprintTile);
            }
        }
        footprintMap.color = canPlace ? validAreaColor : invalidAreaColor;
    }

    // วางของ
    void Place(Vector3Int origin, Vector2Int size)
    {
        GameObject go = new GameObject(current.displayName);
        go.transform.SetParent(placedParent, false);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = current.GetSprite();
        FitToCells(sr, origin, size);

        sr.sortingOrder = 1000 - (origin.x + origin.y) * 2 - (size.x + size.y);

        // จองทุกช่องที่กิน
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                saveSlot[origin + new Vector3Int(x, y, 0)] = go;
            }  
        }
    }

    // ปรับรูปในช่อง
    void FitToCells(SpriteRenderer sr, Vector3Int origin, Vector2Int size)
    {
        Vector3 bottom = floor.CellToWorld(origin);
        Vector3 right = floor.CellToWorld(origin + new Vector3Int(size.x, 0, 0));
        Vector3 left = floor.CellToWorld(origin + new Vector3Int(0, size.y, 0));
        Vector3 top = floor.CellToWorld(origin + new Vector3Int(size.x, size.y, 0));

        Bounds art = sr.sprite.bounds;
        float scale = (right.x - left.x) / art.size.x * fill;
        float lift = (top.y - bottom.y) * (1f - fill) / 2f;

        sr.transform.localScale = new Vector3(scale, scale, 1f);
        sr.transform.position = new Vector3((right.x + left.x) / 2f - art.center.x * scale, bottom.y + lift - art.min.y * scale, 0f);
    }
}
