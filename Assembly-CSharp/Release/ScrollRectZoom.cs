using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollRectZoom : MonoBehaviour, IScrollHandler, IEventSystemHandler
{
	public ScrollRectEx scrollRect;

	public float zoom = 1f;

	public bool smooth = true;

	public float max = 1.5f;

	public float min = 0.5f;

	public float scrollAmount = 0.2f;

	public RectTransform rectTransform => scrollRect.transform as RectTransform;

	private void OnEnable()
	{
		SetZoom(zoom);
	}

	public void OnScroll(PointerEventData data)
	{
		SetZoom(zoom + scrollAmount * data.scrollDelta.y);
	}

	private void SetZoom(float z)
	{
		z = Mathf.Clamp(z, min, max);
		zoom = z;
		Vector2 vector = scrollRect.content.rect.size * zoom;
		Vector2 normalizedPosition = scrollRect.normalizedPosition;
		scrollRect.content.localScale = Vector3.one * Mathf.Exp(zoom);
		scrollRect.normalizedPosition = normalizedPosition;
	}
}
