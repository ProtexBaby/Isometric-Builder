using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsoBuilder
{
    [Serializable]
    public class ZoneArea
    {
        public string name = "Zone";
        public ZoneType type = ZoneType.Mixed;
        [Tooltip("Cells covered by the zone. x/y is the cell nearest the grid's bottom corner; +x runs up-right, +y runs up-left.")]
        public RectInt rect = new RectInt(0, 0, 5, 5);
    }

    /// <summary>Grid size, zone layout and ground colours. Edits apply live, including in play mode.</summary>
    [CreateAssetMenu(menuName = "Iso Builder/Grid Config", fileName = "GridConfig")]
    public class GridConfig : ScriptableObject
    {
        [Header("Grid")]
        [Min(1)] public int width = 30;
        [Min(1)] public int height = 30;
        [Tooltip("World size of one tile diamond. 2:1 is classic isometric.")]
        public Vector2 tileSize = new Vector2(1f, 0.5f);

        [Header("Zones")]
        [Tooltip("Applied in order; later entries win where zones overlap. Cells not covered by any zone are unbuildable.")]
        public List<ZoneArea> zones = new List<ZoneArea>();

        [Header("Look")]
        public Color outsideColor = new Color(0.616f, 0.757f, 0.514f);
        public Color furnitureZoneColor = new Color(0.941f, 0.827f, 0.627f);
        public Color mixedZoneColor = new Color(0.663f, 0.827f, 0.816f);
        public Color furnitureZoneBorder = new Color(0.769f, 0.498f, 0.227f);
        public Color mixedZoneBorder = new Color(0.239f, 0.549f, 0.541f);
        [Range(0f, 0.2f)] public float gridLineWidth = 0.05f;
        [Min(0f)] public float platformDepth = 0.6f;
        public Color platformLeftColor = new Color(0.482f, 0.357f, 0.247f);
        public Color platformRightColor = new Color(0.369f, 0.267f, 0.188f);

        public event Action Changed;

        public Color GetZoneColor(ZoneType zone)
        {
            switch (zone)
            {
                case ZoneType.Furniture: return furnitureZoneColor;
                case ZoneType.Mixed: return mixedZoneColor;
                default: return outsideColor;
            }
        }

        public Color GetZoneBorder(ZoneType zone)
        {
            switch (zone)
            {
                case ZoneType.Furniture: return furnitureZoneBorder;
                case ZoneType.Mixed: return mixedZoneBorder;
                default: return Color.clear;
            }
        }

        /// <summary>Call after changing fields from code so the grid, renderer and camera pick it up.</summary>
        public void NotifyChanged() => Changed?.Invoke();

        void OnValidate() => Changed?.Invoke();
    }
}