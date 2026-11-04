using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class ItemCardView : MonoBehaviour
{
    [SerializeField] Image icon;
    [SerializeField] Text nameText;
    [SerializeField] Text footprintText;

    public Item Item { get; private set; }

    CanvasGroup canvasGroup;

    public void SetAvailable(bool available)
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = available ? 1f : 0.4f;
        canvasGroup.interactable = available;
    }

    public void InsertData(Item item)
    {
        Item = item;
        name = item.displayName;
        icon.sprite = item.GetSprite();
        nameText.text = item.displayName;
        Vector2Int size = item.GetFootprint();
        footprintText.text = $"{size.x}x{size.y}";

    }
}
