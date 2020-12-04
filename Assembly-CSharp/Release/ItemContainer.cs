#define UNITY_ASSERTIONS
using Facepunch;
using Network;
using Oxide.Core;
using ProtoBuf;
using Rust;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public sealed class ItemContainer
{
	[Flags]
	public enum Flag
	{
		IsPlayer = 0x1,
		Clothing = 0x2,
		Belt = 0x4,
		SingleType = 0x8,
		IsLocked = 0x10,
		ShowSlotsOnIcon = 0x20,
		NoBrokenItems = 0x40,
		NoItemInput = 0x80,
		ContentsHidden = 0x100
	}

	[Flags]
	public enum ContentsType
	{
		Generic = 0x1,
		Liquid = 0x2
	}

	public enum CanAcceptResult
	{
		CanAccept,
		CannotAccept,
		CannotAcceptRightNow
	}

	public Flag flags;

	public ContentsType allowedContents;

	public ItemDefinition onlyAllowedItem;

	public List<ItemSlot> availableSlots = new List<ItemSlot>();

	public int capacity = 2;

	public uint uid;

	public bool dirty;

	public List<Item> itemList = new List<Item>();

	public float temperature = 15f;

	public Item parent;

	public BasePlayer playerOwner;

	public BaseEntity entityOwner;

	public bool isServer;

	public int maxStackSize;

	public Func<Item, int, bool> canAcceptItem;

	public Func<Item, int, bool> slotIsReserved;

	public Action<Item, bool> onItemAddedRemoved;

	public Action<Item> onPreItemRemove;

	public Vector3 dropPosition
	{
		get
		{
			if ((bool)playerOwner)
			{
				return playerOwner.GetDropPosition();
			}
			if ((bool)entityOwner)
			{
				return entityOwner.GetDropPosition();
			}
			if (parent != null)
			{
				BaseEntity worldEntity = parent.GetWorldEntity();
				if (worldEntity != null)
				{
					return worldEntity.GetDropPosition();
				}
			}
			Debug.LogWarning("ItemContainer.dropPosition dropped through");
			return Vector3.zero;
		}
	}

	public Vector3 dropVelocity
	{
		get
		{
			if ((bool)playerOwner)
			{
				return playerOwner.GetDropVelocity();
			}
			if ((bool)entityOwner)
			{
				return entityOwner.GetDropVelocity();
			}
			if (parent != null)
			{
				BaseEntity worldEntity = parent.GetWorldEntity();
				if (worldEntity != null)
				{
					return worldEntity.GetDropVelocity();
				}
			}
			Debug.LogWarning("ItemContainer.dropVelocity dropped through");
			return Vector3.zero;
		}
	}

	public event Action onDirty;

	public bool HasFlag(Flag f)
	{
		return (flags & f) == f;
	}

	public void SetFlag(Flag f, bool b)
	{
		if (b)
		{
			flags |= f;
		}
		else
		{
			flags &= ~f;
		}
	}

	public bool IsLocked()
	{
		return HasFlag(Flag.IsLocked);
	}

	public bool PlayerItemInputBlocked()
	{
		return HasFlag(Flag.NoItemInput);
	}

	public void ServerInitialize(Item parentItem, int iMaxCapacity)
	{
		parent = parentItem;
		capacity = iMaxCapacity;
		uid = 0u;
		isServer = true;
		if (allowedContents == (ContentsType)0)
		{
			allowedContents = ContentsType.Generic;
		}
		MarkDirty();
	}

	public void GiveUID()
	{
		Assert.IsTrue(uid == 0, "Calling GiveUID - but already has a uid!");
		uid = Net.sv.TakeUID();
	}

	public void MarkDirty()
	{
		dirty = true;
		if (parent != null)
		{
			parent.MarkDirty();
		}
		if (this.onDirty != null)
		{
			this.onDirty();
		}
	}

	public DroppedItemContainer Drop(string prefab, Vector3 pos, Quaternion rot)
	{
		if (itemList == null || itemList.Count == 0)
		{
			return null;
		}
		BaseEntity baseEntity = GameManager.server.CreateEntity(prefab, pos, rot);
		if (baseEntity == null)
		{
			return null;
		}
		DroppedItemContainer droppedItemContainer = baseEntity as DroppedItemContainer;
		if (droppedItemContainer != null)
		{
			droppedItemContainer.TakeFrom(this);
		}
		droppedItemContainer.Spawn();
		return droppedItemContainer;
	}

	public static DroppedItemContainer Drop(string prefab, Vector3 pos, Quaternion rot, params ItemContainer[] containers)
	{
		int num = 0;
		foreach (ItemContainer itemContainer in containers)
		{
			num += ((itemContainer.itemList != null) ? itemContainer.itemList.Count : 0);
		}
		if (num == 0)
		{
			return null;
		}
		BaseEntity baseEntity = GameManager.server.CreateEntity(prefab, pos, rot);
		if (baseEntity == null)
		{
			return null;
		}
		DroppedItemContainer droppedItemContainer = baseEntity as DroppedItemContainer;
		if (droppedItemContainer != null)
		{
			droppedItemContainer.TakeFrom(containers);
		}
		droppedItemContainer.Spawn();
		return droppedItemContainer;
	}

	public void OnChanged()
	{
		for (int i = 0; i < itemList.Count; i++)
		{
			itemList[i].OnChanged();
		}
	}

	public Item FindItemByUID(uint iUID)
	{
		for (int i = 0; i < itemList.Count; i++)
		{
			Item item = itemList[i];
			if (item.IsValid())
			{
				Item item2 = item.FindItem(iUID);
				if (item2 != null)
				{
					return item2;
				}
			}
		}
		return null;
	}

	public bool IsFull()
	{
		return itemList.Count >= capacity;
	}

	public bool IsEmpty()
	{
		return itemList.Count == 0;
	}

	public bool CanTake(Item item)
	{
		if (IsFull())
		{
			return false;
		}
		return true;
	}

	public int GetMaxTransferAmount(ItemDefinition def)
	{
		foreach (Item item in itemList)
		{
			if (item.info == def)
			{
				return maxStackSize - item.amount;
			}
		}
		return maxStackSize;
	}

	public bool Insert(Item item)
	{
		if (itemList.Contains(item))
		{
			return false;
		}
		if (IsFull())
		{
			return false;
		}
		itemList.Add(item);
		item.parent = this;
		if (!FindPosition(item))
		{
			return false;
		}
		MarkDirty();
		if (onItemAddedRemoved != null)
		{
			onItemAddedRemoved(item, true);
		}
		Interface.CallHook("OnItemAddedToContainer", this, item);
		return true;
	}

	public bool SlotTaken(Item item, int i)
	{
		if (slotIsReserved != null && slotIsReserved(item, i))
		{
			return true;
		}
		return GetSlot(i) != null;
	}

	public Item GetSlot(int slot)
	{
		for (int i = 0; i < itemList.Count; i++)
		{
			if (itemList[i].position == slot)
			{
				return itemList[i];
			}
		}
		return null;
	}

	public bool FindPosition(Item item)
	{
		int position = item.position;
		item.position = -1;
		if (position >= 0 && !SlotTaken(item, position))
		{
			item.position = position;
			return true;
		}
		for (int i = 0; i < capacity; i++)
		{
			if (!SlotTaken(item, i))
			{
				item.position = i;
				return true;
			}
		}
		return false;
	}

	public void SetLocked(bool isLocked)
	{
		SetFlag(Flag.IsLocked, isLocked);
		MarkDirty();
	}

	public bool Remove(Item item)
	{
		if (!itemList.Contains(item))
		{
			return false;
		}
		if (onPreItemRemove != null)
		{
			onPreItemRemove(item);
		}
		itemList.Remove(item);
		item.parent = null;
		MarkDirty();
		if (onItemAddedRemoved != null)
		{
			onItemAddedRemoved(item, false);
		}
		Interface.CallHook("OnItemRemovedFromContainer", this, item);
		return true;
	}

	public void Clear()
	{
		Item[] array = itemList.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Remove();
		}
	}

	public void Kill()
	{
		this.onDirty = null;
		canAcceptItem = null;
		slotIsReserved = null;
		onItemAddedRemoved = null;
		if (Net.sv != null)
		{
			Net.sv.ReturnUID(uid);
			uid = 0u;
		}
		List<Item> obj = Pool.GetList<Item>();
		foreach (Item item in itemList)
		{
			obj.Add(item);
		}
		foreach (Item item2 in obj)
		{
			item2.Remove();
		}
		Pool.FreeList(ref obj);
		itemList.Clear();
	}

	public int GetAmount(int itemid, bool onlyUsableAmounts)
	{
		int num = 0;
		foreach (Item item in itemList)
		{
			if (item.info.itemid == itemid && (!onlyUsableAmounts || !item.IsBusy()))
			{
				num += item.amount;
			}
		}
		return num;
	}

	public Item FindItemByItemID(int itemid)
	{
		foreach (Item item in itemList)
		{
			if (item.info.itemid == itemid)
			{
				return item;
			}
		}
		return null;
	}

	public Item FindItemsByItemName(string name)
	{
		ItemDefinition itemDefinition = ItemManager.FindItemDefinition(name);
		if (itemDefinition == null)
		{
			return null;
		}
		for (int i = 0; i < itemList.Count; i++)
		{
			if (itemList[i].info == itemDefinition)
			{
				return itemList[i];
			}
		}
		return null;
	}

	public List<Item> FindItemsByItemID(int itemid)
	{
		return itemList.FindAll((Item x) => x.info.itemid == itemid);
	}

	public ProtoBuf.ItemContainer Save()
	{
		ProtoBuf.ItemContainer itemContainer = Pool.Get<ProtoBuf.ItemContainer>();
		itemContainer.contents = Pool.GetList<ProtoBuf.Item>();
		itemContainer.UID = uid;
		itemContainer.slots = capacity;
		itemContainer.temperature = temperature;
		itemContainer.allowedContents = (int)allowedContents;
		itemContainer.allowedItem = ((onlyAllowedItem != null) ? onlyAllowedItem.itemid : 0);
		itemContainer.flags = (int)flags;
		itemContainer.maxStackSize = maxStackSize;
		if (availableSlots != null && availableSlots.Count > 0)
		{
			itemContainer.availableSlots = Pool.GetList<int>();
			for (int i = 0; i < availableSlots.Count; i++)
			{
				itemContainer.availableSlots.Add((int)availableSlots[i]);
			}
		}
		for (int j = 0; j < itemList.Count; j++)
		{
			Item item = itemList[j];
			if (item.IsValid())
			{
				itemContainer.contents.Add(item.Save(true));
			}
		}
		return itemContainer;
	}

	public void Load(ProtoBuf.ItemContainer container)
	{
		using (TimeWarning.New("ItemContainer.Load"))
		{
			uid = container.UID;
			capacity = container.slots;
			List<Item> obj = itemList;
			itemList = Pool.GetList<Item>();
			temperature = container.temperature;
			flags = (Flag)container.flags;
			allowedContents = (ContentsType)((container.allowedContents == 0) ? 1 : container.allowedContents);
			onlyAllowedItem = ((container.allowedItem != 0) ? ItemManager.FindItemDefinition(container.allowedItem) : null);
			maxStackSize = container.maxStackSize;
			availableSlots.Clear();
			for (int i = 0; i < container.availableSlots.Count; i++)
			{
				availableSlots.Add((ItemSlot)container.availableSlots[i]);
			}
			using (TimeWarning.New("container.contents"))
			{
				foreach (ProtoBuf.Item content in container.contents)
				{
					Item created = null;
					foreach (Item item in obj)
					{
						if (item.uid == content.UID)
						{
							created = item;
							break;
						}
					}
					created = ItemManager.Load(content, created, isServer);
					if (created != null)
					{
						created.parent = this;
						created.position = content.slot;
						Insert(created);
					}
				}
			}
			using (TimeWarning.New("Delete old items"))
			{
				foreach (Item item2 in obj)
				{
					if (!itemList.Contains(item2))
					{
						item2.Remove();
					}
				}
			}
			dirty = true;
			Pool.FreeList(ref obj);
		}
	}

	public BasePlayer GetOwnerPlayer()
	{
		return playerOwner;
	}

	public int Take(List<Item> collect, int itemid, int iAmount)
	{
		int num = 0;
		if (iAmount == 0)
		{
			return num;
		}
		List<Item> obj = Pool.GetList<Item>();
		foreach (Item item2 in itemList)
		{
			if (item2.info.itemid == itemid)
			{
				int num2 = iAmount - num;
				if (num2 > 0)
				{
					if (item2.amount > num2)
					{
						item2.MarkDirty();
						item2.amount -= num2;
						num += num2;
						Item item = ItemManager.CreateByItemID(itemid, 1, 0uL);
						item.amount = num2;
						item.CollectedForCrafting(playerOwner);
						collect?.Add(item);
						break;
					}
					if (item2.amount <= num2)
					{
						num += item2.amount;
						obj.Add(item2);
						collect?.Add(item2);
					}
					if (num == iAmount)
					{
						break;
					}
				}
			}
		}
		foreach (Item item3 in obj)
		{
			item3.RemoveFromContainer();
		}
		Pool.FreeList(ref obj);
		return num;
	}

	public void OnCycle(float delta)
	{
		for (int i = 0; i < itemList.Count; i++)
		{
			if (itemList[i].IsValid())
			{
				itemList[i].OnCycle(delta);
			}
		}
	}

	public void FindAmmo(List<Item> list, AmmoTypes ammoType)
	{
		for (int i = 0; i < itemList.Count; i++)
		{
			itemList[i].FindAmmo(list, ammoType);
		}
	}

	public bool HasAmmo(AmmoTypes ammoType)
	{
		for (int i = 0; i < itemList.Count; i++)
		{
			if (itemList[i].HasAmmo(ammoType))
			{
				return true;
			}
		}
		return false;
	}

	public void AddItem(ItemDefinition itemToCreate, int p, ulong skin = 0uL)
	{
		for (int i = 0; i < itemList.Count; i++)
		{
			if (p == 0)
			{
				return;
			}
			if (itemList[i].info != itemToCreate)
			{
				continue;
			}
			int num = itemList[i].MaxStackable();
			if (num <= itemList[i].amount)
			{
				continue;
			}
			MarkDirty();
			itemList[i].amount += p;
			p -= p;
			if (itemList[i].amount > num)
			{
				p = itemList[i].amount - num;
				if (p > 0)
				{
					itemList[i].amount -= p;
				}
			}
		}
		if (p != 0)
		{
			Item item = ItemManager.Create(itemToCreate, p, skin);
			if (!item.MoveToContainer(this))
			{
				item.Remove();
			}
		}
	}

	public void OnMovedToWorld()
	{
		for (int i = 0; i < itemList.Count; i++)
		{
			itemList[i].OnMovedToWorld();
		}
	}

	public void OnRemovedFromWorld()
	{
		for (int i = 0; i < itemList.Count; i++)
		{
			itemList[i].OnRemovedFromWorld();
		}
	}

	public uint ContentsHash()
	{
		uint num = 0u;
		for (int i = 0; i < capacity; i++)
		{
			Item slot = GetSlot(i);
			if (slot != null)
			{
				num = CRC.Compute32(num, slot.info.itemid);
				num = CRC.Compute32(num, slot.skin);
			}
		}
		return num;
	}

	public ItemContainer FindContainer(uint id)
	{
		if (id == uid)
		{
			return this;
		}
		for (int i = 0; i < itemList.Count; i++)
		{
			Item item = itemList[i];
			if (item.contents != null)
			{
				ItemContainer itemContainer = item.contents.FindContainer(id);
				if (itemContainer != null)
				{
					return itemContainer;
				}
			}
		}
		return null;
	}

	public CanAcceptResult CanAcceptItem(Item item, int targetPos)
	{
		if (canAcceptItem != null && !canAcceptItem(item, targetPos))
		{
			return CanAcceptResult.CannotAccept;
		}
		if ((allowedContents & item.info.itemType) != item.info.itemType)
		{
			return CanAcceptResult.CannotAccept;
		}
		if (onlyAllowedItem != null && onlyAllowedItem != item.info)
		{
			return CanAcceptResult.CannotAccept;
		}
		if (availableSlots != null && availableSlots.Count > 0)
		{
			if (item.info.occupySlots == (ItemSlot)0 || item.info.occupySlots == ItemSlot.None)
			{
				return CanAcceptResult.CannotAccept;
			}
			int[] array = new int[32];
			foreach (ItemSlot availableSlot in availableSlots)
			{
				array[(int)Mathf.Log((float)availableSlot, 2f)]++;
			}
			foreach (Item item2 in itemList)
			{
				for (int i = 0; i < 32; i++)
				{
					if (((int)item2.info.occupySlots & (1 << i)) != 0)
					{
						array[i]--;
					}
				}
			}
			for (int j = 0; j < 32; j++)
			{
				if (((int)item.info.occupySlots & (1 << j)) != 0 && array[j] <= 0)
				{
					return CanAcceptResult.CannotAcceptRightNow;
				}
			}
		}
		object obj = Interface.CallHook("CanAcceptItem", this, item, targetPos);
		if (obj is CanAcceptResult)
		{
			return (CanAcceptResult)obj;
		}
		return CanAcceptResult.CanAccept;
	}
}
