using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TabButtonGroup : MonoBehaviour
{
    [SerializeField] private List<Button> buttons = new List<Button>();
    [SerializeField] private Color SelectedColor = Color.green;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private int defaultIndex = 0;

    public UnityEvent<int> onTabSelected;
    public int SelectedIndex { get; private set; } = -1;

    private void Awake()
    {
        if (buttons.Count == 0)
        {
            GetComponentsInChildren(true, buttons);
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            int index = i;
            buttons[i].onClick.AddListener(() => Select(index));
        }
    }

    private void Start()
    {
        Select(defaultIndex);
    }

    public void Select(int index)
    {
        if (index < 0 || index >= buttons.Count || index == SelectedIndex) return;

        SelectedIndex = index;

        for (int i = 0; i < buttons.Count; i++)
        {
            Graphic graphic = buttons[i].targetGraphic;
            if (graphic != null)
            {
                graphic.color = i == index ? SelectedColor : normalColor;
            }
        }

        onTabSelected?.Invoke(index);
    }
}
