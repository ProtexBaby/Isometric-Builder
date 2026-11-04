using UnityEngine;
using UnityEngine.UI;

public class EditToolbar : MonoBehaviour
{
    public Button rotateButton;
    public Button removeButton;
    public Button cancelButton;
    public Button placeButton;

    [SerializeField] float gap = 20f;

    RectTransform Rect => (RectTransform)transform;

    public void Show(bool canRemove)
    {
        removeButton.gameObject.SetActive(canRemove);
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void FollowObject(Vector3 worldPoint)
    {
        Vector2 screen = Camera.main.WorldToScreenPoint(worldPoint);
        screen.y += gap;

        Vector2 size = Vector2.Scale(Rect.rect.size, Rect.lossyScale);
        screen.x = Mathf.Clamp(screen.x, size.x / 2f, Screen.width - size.x / 2f);
        screen.y = Mathf.Clamp(screen.y, 0f, Screen.height - size.y);

        Rect.position = screen;
    }
}
