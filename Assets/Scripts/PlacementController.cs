using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.Events;
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

    [Header("Zones")]
    [SerializeField] PlacementZones zones;

    [Header("Edit Toolbar")]
    [SerializeField] EditToolbar toolbar;

    [Header("Events")]
    public UnityEvent onPlacementStarted;
    public UnityEvent onPlacementEnded;
    public UnityEvent onBuiltChanged;
    public UnityEvent onLayoutChanged;
    public UnityEvent<PlacedItem> onPlacedTapped;

    [Header("Touch")]
    // นิ้วขยับไม่เกิน 20 พิกเซล ถือว่า "แตะ" ไม่ใช่ "ลาก"
    [SerializeField] float tapDistance = 20f;
    // เวลากดค้าง
    [SerializeField] float holdTime = 0.5f;

    // ไอเทมที่จะวาง
    Item current;
    Vector3Int cell;          
    bool rotated;            
    PlacedItem editing;
    // เก็บตำแหน่ง object ที่วาง
    Dictionary<Vector3Int, GameObject> saveSlot = new Dictionary<Vector3Int, GameObject>();

    HashSet<Item> placedUnique = new HashSet<Item>();

    PlacedItem holdTarget;    
    float holdTimer;          
    Vector2 holdStart;        

    // แตะจอ
    bool pressing;
    // แตะ Ui
    bool pressOnUI;
    // แตะและลาก
    bool dragging;
    // ตำแหน่งที่แตะ
    Vector2 pressPos;
    Vector3Int grabOffset;

    private void Start()
    {
        ghost.gameObject.SetActive(false);

        toolbar.placeButton.onClick.AddListener(Confirm);
        toolbar.cancelButton.onClick.AddListener(Cancel);
        toolbar.rotateButton.onClick.AddListener(Rotate);
        toolbar.removeButton.onClick.AddListener(Remove);
        toolbar.Hide();
    }

    // จุดเริ่มการวาง object
    public void BeginPlacement(Item item)
    {
        if (!CanBuild(item)) return;

        ShowGhost(item, false, false);

        // วาง object กลางจอ
        cell = CenterOn(ScreenToCell(Camera.main.pixelRect.center));
    }

    // จุดเริ่ม edit
    void BeginEdit(PlacedItem placed, Vector2 screen)
    {
        editing = placed;
        Free(placed);                         
        placed.gameObject.SetActive(false);   
        ShowGhost(placed.item, placed.rotated, true);
        cell = placed.origin;

        pressing = true;
        pressOnUI = false;
        pressPos = screen;
        dragging = true;
        grabOffset = cell - ScreenToCell(screen);
    }

    void ShowGhost(Item item, bool isRotated, bool canRemove)
    {
        current = item;
        rotated = isRotated;
        ghost.sprite = item.GetSprite();
        ghost.flipX = isRotated;
        ghost.sortingOrder = short.MaxValue; 
        ghost.gameObject.SetActive(true);
        toolbar.Show(canRemove);
        onPlacementStarted.Invoke();
    }

    public void CancelPlacement()
    {
        if (editing != null)
        {
            // ย้อนกลับตำแหน่งเดิม
            PutDown(editing, editing.origin, editing.rotated);  
            editing = null;
        }

        current = null;
        dragging = false;
        ghost.gameObject.SetActive(false);
        footprintMap.ClearAllTiles();
        toolbar.Hide();
        onPlacementEnded.Invoke();
    }

    // ปุ่ม Place
    public void Confirm()
    {
        if (current == null) return;
        if (!CanPlace(current, cell, Size())) return;  // เงาแดง = วางไม่ได้ กดแล้วไม่เกิดอะไร

        if (editing != null)
        {
            PutDown(editing, cell, rotated);
            editing = null;  // ต้องล้างก่อน ไม่งั้น CancelPlacement จะย้ายกลับที่เดิม
        }
        else
        {
            if (!CanBuild(current)) return;
            Spawn(current, cell, rotated);
        }
        onLayoutChanged.Invoke();
        CancelPlacement();
    }

    // เช็ค object และจัดการกับ station ที่ไม่สามรถวางแบบซ้ำกันได้
    public bool CanBuild(Item item)
    {
        if (!item.IsUnique) return true;              
        if (placedUnique.Contains(item)) return false; 
        return true;                                   
    }

    // ปุ่ม Remove
    public void Remove()
    {
        if (editing == null) return;
        editing.transform.SetParent(null);
        Destroy(editing.gameObject);
        if (editing.item.IsUnique)
        {
            placedUnique.Remove(editing.item);
            onBuiltChanged.Invoke();
        }
        editing = null;
        onLayoutChanged.Invoke();
        CancelPlacement();
    }

    // ปุ่ม Rotate
    public void Rotate()
    {
        if (current == null) return;
        rotated = !rotated;
        ghost.flipX = rotated;  // ขนาด footprint สลับเองใน Size() — Update เฟรมถัดไปวาดเงา/ช่องใหม่ให้
    }

    // ปุ่ม Cancel
    public void Cancel()
    {
        CancelPlacement();
    }

    private void Update()
    {
        if (current == null)
        {
            HandleLongPress();
            return;
        }

        // Esc บน PC / ปุ่ม Back บน Android
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CancelPlacement();
            return;
        }

        HandlePointer();

        Vector2Int size = Size();
        bool canPlace = CanPlace(current, cell, size);

        // object ตามช่องและสีที่จะบอกว่าวางได้ไหม
        FitToCells(ghost, cell, size);
        ghost.color = canPlace ? validColor : invalidColor;
        ShowFootprint(cell, size, canPlace);

        // แถบปุ่มลอยเหนือหัวของ object
        Bounds art = ghost.bounds;
        toolbar.FollowObject(new Vector3(art.center.x, art.max.y, 0f));
    }

    // ย้าย object ตามช่องที่แตะ
    private void HandlePointer()
    {
        Pointer pointer = TouchInput.Primary();  // นิ้วแรกบนจอ หรือ เมาส์
        if (pointer == null) return;
        Vector2 screen = pointer.position.ReadValue();

        if (pointer.press.wasPressedThisFrame)
        {
            pressing = true;
            pressPos = screen;
            pressOnUI = TouchInput.IsOverUI(screen);

            Vector3Int touched = ScreenToCell(screen);
            dragging = !pressOnUI && IsInFootprint(touched);
            grabOffset = cell - touched;
        }
        if (!pressing) return;
        
        // สองนิ้วไม่ลาก object
        if (TouchInput.TouchCount() >= 2) dragging = false;  

        if (dragging && pointer.press.isPressed) cell = ScreenToCell(screen) + grabOffset;

        if (pointer.press.wasReleasedThisFrame)
        {
            bool tap = !pressOnUI && Vector2.Distance(screen, pressPos) < tapDistance;
            if (tap && !dragging) cell = CenterOn(ScreenToCell(screen));
            pressing = false;
            dragging = false;
        }
    }

    // กดค้างเพื่อเข้าโหมด edit
    void HandleLongPress()
    {
        Pointer pointer = TouchInput.Primary();
        if (pointer == null) return;
        Vector2 screen = pointer.position.ReadValue();

        if (pointer.press.wasPressedThisFrame)
        {
            holdTarget = TouchInput.IsOverUI(screen) ? null : PickPlaced(screen);
            holdStart = screen;
            holdTimer = 0f;
        }
        if (holdTarget == null) return;

        if (pointer.press.wasReleasedThisFrame)
        {
            if (Vector2.Distance(screen, holdStart) <= tapDistance)
            {
                toolbar.Hide();
                onPlacedTapped.Invoke(holdTarget);
            }
            holdTarget = null;
            return;
        }

        // ยกนิ้ว / นิ้วเลื่อนออก / สองนิ้ว = ไม่นับเป็นกดค้าง
        if (!pointer.press.isPressed || Vector2.Distance(screen, holdStart) > tapDistance || TouchInput.TouchCount() >= 2)
        {
            holdTarget = null;
            return;
        }

        holdTimer += Time.deltaTime;
        if (holdTimer >= holdTime)
        {
            PlacedItem target = holdTarget;
            holdTarget = null;
            BeginEdit(target, screen);
        }
    }

    PlacedItem PickPlaced(Vector2 screen)
    {
        Vector3 world = Camera.main.ScreenToWorldPoint(screen);
        SpriteRenderer best = null;
        foreach (Transform child in placedParent)
        {
            SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
            if (sr == null || !child.gameObject.activeSelf) continue;
            Bounds b = sr.bounds;
            world.z = b.center.z;
            if (b.Contains(world) && (best == null || sr.sortingOrder > best.sortingOrder)) best = sr;
        }
        return best != null ? best.GetComponent<PlacedItem>() : null;
    }

    // ขนาด footprint ตอนนี้ — หมุนแล้วสลับ x/y เช่น Bench 1×2 → 2×1
    Vector2Int Size()
    {
        Vector2Int size = current.GetFootprint();
        return rotated ? new Vector2Int(size.y, size.x) : size;
    }

    Vector3Int ScreenToCell(Vector2 screen)
    {
        Vector3 world = Camera.main.ScreenToWorldPoint(screen);
        world.z = 0f;
        Vector3Int c = floor.WorldToCell(world);
        c.z = 0;
        return c;
    }

    Vector3Int CenterOn(Vector3Int c)
    {
        Vector2Int size = Size();
        return c - new Vector3Int((size.x - 1) / 2, (size.y - 1) / 2, 0);
    }

    bool IsInFootprint(Vector3Int c)
    {
        Vector2Int size = Size();
        return c.x >= cell.x && c.x < cell.x + size.x && c.y >= cell.y && c.y < cell.y + size.y;
    }

    // เช็คว่าวางได้ไหม
    bool CanPlace(Item item, Vector3Int origin, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector3Int c = origin + new Vector3Int(x, y, 0);
                if (!floor.HasTile(c) || saveSlot.ContainsKey(c)) return false;
                if (zones != null && !zones.Allows(c, item.category)) return false;
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

    /// <summary>ของทุกชิ้นบนแมพ — รวมชิ้นที่ซ่อนอยู่เพราะกำลังถูกย้าย (origin ยังเป็นที่เดิม)</summary>
    public List<PlacedItem> AllPlaced()
    {
        List<PlacedItem> list = new List<PlacedItem>();
        foreach (Transform child in placedParent)
        {
            PlacedItem placed = child.GetComponent<PlacedItem>();
            if (placed != null) list.Add(placed);
        }
        return list;
    }

    /// <summary>วางของจากไฟล์เซฟ — false = วางไม่ได้แล้ว (นอกพื้น/นอกโซน/ทับของอื่น/Station ซ้ำ) ข้ามไป</summary>
    public bool TrySpawn(Item item, Vector3Int origin, bool isRotated)
    {
        Vector2Int size = item.GetFootprint();
        if (isRotated) size = new Vector2Int(size.y, size.x);
        if (!CanBuild(item) || !CanPlace(item, origin, size)) return false;
        Spawn(item, origin, isRotated);
        return true;
    }

    // สร้างของชิ้นใหม่ (Confirm และการโหลดเซฟใช้ทางเดียวกัน)
    void Spawn(Item item, Vector3Int origin, bool isRotated)
    {
        GameObject go = new GameObject(item.displayName);
        go.transform.SetParent(placedParent, false);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = item.GetSprite();

        PlacedItem placed = go.AddComponent<PlacedItem>();  // จำว่าเป็นของอะไร ไว้ใช้ตอนกดค้างแก้ไขและตอนเซฟ
        placed.item = item;
        PutDown(placed, origin, isRotated);

        if (item.IsUnique)
        {
            placedUnique.Add(item);  // วางแล้ว การ์ดใบนี้จะจาง
            onBuiltChanged.Invoke();
        }
    }

    // วาว object ลงที่ตำแหน่ง origin
    void PutDown(PlacedItem placed, Vector3Int origin, bool isRotated)
    {
        placed.origin = origin;
        placed.rotated = isRotated;
        Vector2Int size = placed.Size();

        SpriteRenderer sr = placed.GetComponent<SpriteRenderer>();
        sr.flipX = isRotated;  // ของจริงหันด้านเดียวกับเงา
        FitToCells(sr, origin, size);
        // ชิ้นที่อยู่หน้า (x+y ของจุดกลางน้อยกว่า) ต้องวาดทับชิ้นหลัง
        sr.sortingOrder = 1000 - (origin.x + origin.y) * 2 - (size.x + size.y);

        // จองทุกช่องที่กิน
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                saveSlot[origin + new Vector3Int(x, y, 0)] = placed.gameObject;
            }
        }
        placed.gameObject.SetActive(true);
    }

    // คืนช่องที่ object ชิ้นนี้จองไว้
    void Free(PlacedItem placed)
    {
        Vector2Int size = placed.Size();
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                saveSlot.Remove(placed.origin + new Vector3Int(x, y, 0));
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
