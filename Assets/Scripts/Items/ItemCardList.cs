using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemCardList : MonoBehaviour
{
    [SerializeField] ItemCatalog catalog;
    [SerializeField] ItemCardView cardPrefab;
    [SerializeField] private TabButtonGroup tabGroup;
    [SerializeField] private PlacementController placement;


    private void OnEnable()
    {
        tabGroup.onTabSelected.AddListener(FilterItems);
    }

    private void OnDisable()
    {
        tabGroup.onTabSelected.RemoveListener(FilterItems);
    }

    private void CreateCard(Item item)
    {
        ItemCardView card = Instantiate(cardPrefab, transform);
        card.InsertData(item);

        Button button = card.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => placement.BeginPlacement(item));
        }
    }

    private void FilterItems(int tabIndex)
    {
        foreach (Transform child in transform) Destroy(child.gameObject);

        if (catalog == null || cardPrefab == null) return;

        switch (tabIndex)
        {
            case 0:
                foreach (Item item in catalog.items)
                {
                    if (item == null) continue;
                    CreateCard(item);
                }
                break;
            case 1:
                foreach (Item item in catalog.items)
                {
                    if (item == null) continue;
                    if (item.category == ItemCategory.Furniture)
                        CreateCard(item);
                }
                break;
            case 2:
                foreach (Item item in catalog.items)
                {
                    if (item == null) continue;
                    if (item.category == ItemCategory.Station)
                        CreateCard(item);
                }
                break;
        }

    }
}
