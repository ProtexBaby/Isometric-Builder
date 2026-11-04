using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StationWindow : MonoBehaviour
{
    [SerializeField] Image icon;
    [SerializeField] Text title;
    [SerializeField] Button closeButton;
    [SerializeField] Button dimButton;

    public UnityEvent onOpened;  
    public UnityEvent onClosed;   

    PlacedItem station;           // Station ที่เปิดอยู่

    private void Awake()
    {
        closeButton.onClick.AddListener(Close);
        dimButton.onClick.AddListener(Close);
    }

    public void Open(PlacedItem placed)
    {
        if (placed == null || placed.item.category != ItemCategory.Station) return;

        station = placed;
        icon.sprite = placed.item.GetSprite();
        title.text = placed.item.displayName;
        gameObject.SetActive(true);
        onOpened.Invoke();
    }

    public void Close()
    {
        if (station == null) return;  // ปิดอยู่แล้ว ไม่ต้องยิง event ซ้ำ
        station = null;
        gameObject.SetActive(false);
        onClosed.Invoke();
    }

    void Update()
    {
        // Esc บน PC / ปุ่ม Back บน Android
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
    }
}
