using UnityEngine;

public class NoticeArea : SingletonComponent<NoticeArea>
{
	public GameObject itemPickupPrefab;

	public GameObject itemDroppedPrefab;

	private IVitalNotice[] notices;

	protected override void Awake()
	{
		base.Awake();
		notices = GetComponentsInChildren<IVitalNotice>(true);
	}

	public static void ItemPickUp(ItemDefinition def, int amount, string nameOverride)
	{
		if (SingletonComponent<NoticeArea>.Instance == null)
		{
			return;
		}
		GameObject gameObject = Object.Instantiate((amount > 0) ? SingletonComponent<NoticeArea>.Instance.itemPickupPrefab : SingletonComponent<NoticeArea>.Instance.itemDroppedPrefab);
		if (gameObject == null)
		{
			return;
		}
		gameObject.transform.SetParent(SingletonComponent<NoticeArea>.Instance.transform, false);
		ItemPickupNotice component = gameObject.GetComponent<ItemPickupNotice>();
		if (!(component == null))
		{
			component.itemInfo = def;
			component.amount = amount;
			if (!string.IsNullOrEmpty(nameOverride))
			{
				component.Text.text = nameOverride;
			}
		}
	}
}
