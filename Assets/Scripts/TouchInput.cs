using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public static class TouchInput
{
    static readonly List<RaycastResult> uiHits = new List<RaycastResult>();

    public static Pointer Primary()
    {
        Touchscreen screen = Touchscreen.current;
        if (screen != null && (screen.press.isPressed || screen.press.wasReleasedThisFrame)) return screen;
        if (Mouse.current != null) return Mouse.current;
        return screen;
    }

    public static int TouchCount()
    {
        Touchscreen screen = Touchscreen.current;
        if (screen == null) return 0;
        int count = 0;
        foreach (var touch in screen.touches)
        {
            if (touch.press.isPressed) count++;
        }
        return count;
    }

    public static bool IsOverUI(Vector2 screen)
    {
        if (EventSystem.current == null) return false;
        PointerEventData data = new PointerEventData(EventSystem.current) { position = screen };
        EventSystem.current.RaycastAll(data, uiHits);
        return uiHits.Count > 0;
    }
}
