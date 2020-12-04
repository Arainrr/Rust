using Apex.Serialization;
using UnityEngine;

namespace Rust.Ai
{
	public class SwitchWeaponOperator : BaseAction
	{
		[ApexSerialization]
		private NPCPlayerApex.WeaponTypeEnum WeaponType;

		public override void DoExecute(BaseContext c)
		{
			TrySwitchWeaponTo(c as NPCHumanContext, WeaponType);
		}

		public static bool TrySwitchWeaponTo(NPCHumanContext c, NPCPlayerApex.WeaponTypeEnum WeaponType)
		{
			if (c != null)
			{
				if (Time.realtimeSinceStartup < c.Human.NextWeaponSwitchTime)
				{
					return false;
				}
				uint svActiveItemID = c.Human.svActiveItemID;
				Item item;
				switch (WeaponType)
				{
				default:
					c.Human.UpdateActiveItem(0u);
					if (svActiveItemID != c.Human.svActiveItemID)
					{
						c.Human.NextWeaponSwitchTime = Time.realtimeSinceStartup + c.Human.WeaponSwitchFrequency;
						c.SetFact(NPCPlayerApex.Facts.CurrentWeaponType, (byte)c.Human.GetCurrentWeaponTypeEnum());
						c.SetFact(NPCPlayerApex.Facts.CurrentAmmoState, (byte)c.Human.GetCurrentAmmoStateEnum());
					}
					c.Human.StoppingDistance = 1f;
					return true;
				case NPCPlayerApex.WeaponTypeEnum.CloseRange:
					item = FindBestMelee(c);
					if (item == null)
					{
						item = FindBestProjInRange(NPCPlayerApex.WeaponTypeEnum.None, NPCPlayerApex.WeaponTypeEnum.CloseRange, c);
						if (item == null)
						{
							item = FindBestProjInRange(NPCPlayerApex.WeaponTypeEnum.CloseRange, NPCPlayerApex.WeaponTypeEnum.MediumRange, c);
							if (item == null)
							{
								item = FindBestProjInRange(NPCPlayerApex.WeaponTypeEnum.MediumRange, NPCPlayerApex.WeaponTypeEnum.LongRange, c);
							}
						}
						c.Human.StoppingDistance = 2.5f;
					}
					else
					{
						c.Human.StoppingDistance = 1.5f;
					}
					break;
				case NPCPlayerApex.WeaponTypeEnum.MediumRange:
					item = FindBestProjInRange(NPCPlayerApex.WeaponTypeEnum.CloseRange, NPCPlayerApex.WeaponTypeEnum.MediumRange, c);
					if (item == null)
					{
						item = FindBestProjInRange(NPCPlayerApex.WeaponTypeEnum.MediumRange, NPCPlayerApex.WeaponTypeEnum.LongRange, c);
					}
					c.Human.StoppingDistance = 0.1f;
					break;
				case NPCPlayerApex.WeaponTypeEnum.LongRange:
					item = FindBestProjInRange(NPCPlayerApex.WeaponTypeEnum.MediumRange, NPCPlayerApex.WeaponTypeEnum.LongRange, c);
					if (item == null)
					{
						item = FindBestProjInRange(NPCPlayerApex.WeaponTypeEnum.CloseRange, NPCPlayerApex.WeaponTypeEnum.MediumRange, c);
					}
					c.Human.StoppingDistance = 0.1f;
					break;
				}
				if (item != null)
				{
					c.Human.UpdateActiveItem(item.uid);
					if (svActiveItemID != c.Human.svActiveItemID)
					{
						c.Human.NextWeaponSwitchTime = Time.realtimeSinceStartup + c.Human.WeaponSwitchFrequency;
						c.SetFact(NPCPlayerApex.Facts.CurrentWeaponType, (byte)c.Human.GetCurrentWeaponTypeEnum());
						c.SetFact(NPCPlayerApex.Facts.CurrentAmmoState, (byte)c.Human.GetCurrentAmmoStateEnum());
						c.SetFact(NPCPlayerApex.Facts.CurrentToolType, 0);
					}
					return true;
				}
			}
			return false;
		}

		private static Item FindBestMelee(NPCHumanContext c)
		{
			if (c.Human.GetPathStatus() != 0)
			{
				return null;
			}
			Item item = null;
			BaseMelee baseMelee = null;
			Item[] array = c.Human.inventory.AllItems();
			foreach (Item item2 in array)
			{
				if (item2.info.category != 0 || item2.isBroken)
				{
					continue;
				}
				BaseMelee baseMelee2 = item2.GetHeldEntity() as BaseMelee;
				if ((bool)baseMelee2)
				{
					if (item == null)
					{
						item = item2;
						baseMelee = baseMelee2;
					}
					else if (baseMelee2.hostileScore > baseMelee.hostileScore)
					{
						item = item2;
						baseMelee = baseMelee2;
					}
				}
			}
			return item;
		}

		private static Item FindBestProjInRange(NPCPlayerApex.WeaponTypeEnum from, NPCPlayerApex.WeaponTypeEnum to, NPCHumanContext c)
		{
			Item item = null;
			BaseProjectile baseProjectile = null;
			Item[] array = c.Human.inventory.AllItems();
			foreach (Item item2 in array)
			{
				if (item2.info.category != 0 || item2.isBroken)
				{
					continue;
				}
				BaseProjectile baseProjectile2 = item2.GetHeldEntity() as BaseProjectile;
				if (baseProjectile2 != null && (int)baseProjectile2.effectiveRangeType <= (int)to && (int)baseProjectile2.effectiveRangeType > (int)from)
				{
					if (item == null)
					{
						item = item2;
						baseProjectile = baseProjectile2;
					}
					else if (baseProjectile2.hostileScore > baseProjectile.hostileScore)
					{
						item = item2;
						baseProjectile = baseProjectile2;
					}
				}
			}
			return item;
		}
	}
}
