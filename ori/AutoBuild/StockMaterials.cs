using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AutoBuild
{
	// Token: 0x0200000C RID: 12
	internal sealed class StockMaterials
	{
		// Token: 0x06000049 RID: 73 RVA: 0x000041C0 File Offset: 0x000023C0
		internal StockMaterials(Map map)
		{
			this.map = map;
		}

		// Token: 0x0600004A RID: 74 RVA: 0x000041FC File Offset: 0x000023FC
		internal bool Plan(IConstructible project, AutoBuildSettings settings, out MaterialShortage shortage)
		{
			this.plan.Clear();
			this.remainingByDef.Clear();
			shortage = default(MaterialShortage);
			foreach (ThingDefCountClass thingDefCountClass in project.TotalMaterialCost())
			{
				int num;
				this.remainingByDef.TryGetValue(thingDefCountClass.thingDef, out num);
				this.remainingByDef[thingDefCountClass.thingDef] = num + thingDefCountClass.count;
			}
			Frame frame = project as Frame;
			foreach (KeyValuePair<ThingDef, int> keyValuePair in this.remainingByDef)
			{
				int num2 = keyValuePair.Value - ((frame != null) ? frame.resourceContainer.TotalStackCountOfDef(keyValuePair.Key) : 0);
				if (num2 > 0)
				{
					List<Thing> list = this.AvailableStacks(keyValuePair.Key, settings.allowMapMaterials);
					int num3 = settings.StockReserve(keyValuePair.Key);
					int num4 = 0;
					int num5 = int.MaxValue;
					if (num3 > 0)
					{
						long num6 = 0L;
						foreach (Thing thing in list)
						{
							bool flag;
							if (this.CanWithdraw(thing, false, out flag))
							{
								num6 += (long)thing.stackCount;
							}
						}
						num4 = (int)Math.Min((long)num3, num6);
						num5 = (int)Math.Min(2147483647L, num6 - (long)num4);
					}
					foreach (Thing thing2 in list)
					{
						bool flag2;
						if (this.CanWithdraw(thing2, settings.allowMapMaterials, out flag2))
						{
							int num7 = Math.Min(thing2.stackCount, num2);
							if (flag2 && num3 > 0)
							{
								num7 = Math.Min(num7, num5);
								num5 -= num7;
							}
							if (num7 != 0)
							{
								this.plan.Add(new StockMaterials.Transfer(thing2, num7));
								num2 -= num7;
								if (num2 == 0)
								{
									break;
								}
							}
						}
					}
					if (num2 > 0)
					{
						shortage = new MaterialShortage(keyValuePair.Key, num2, num4);
						return false;
					}
				}
			}
			return true;
		}

		// Token: 0x0600004B RID: 75 RVA: 0x000044A0 File Offset: 0x000026A0
		private List<Thing> AvailableStacks(ThingDef def, bool allowMapMaterials)
		{
			List<Thing> list;
			if (this.stacksByDef.TryGetValue(def, out list))
			{
				return list;
			}
			list = new List<Thing>();
			foreach (Thing thing in this.map.listerThings.ThingsOfDef(def))
			{
				bool flag;
				if (this.CanWithdraw(thing, allowMapMaterials, out flag))
				{
					if (flag)
					{
						list.Add(thing);
					}
					else
					{
						this.looseStacks.Add(thing);
					}
				}
			}
			list.AddRange(this.looseStacks);
			this.looseStacks.Clear();
			this.stacksByDef.Add(def, list);
			return list;
		}

		// Token: 0x0600004C RID: 76 RVA: 0x00004558 File Offset: 0x00002758
		private bool CanWithdraw(Thing thing, bool allowMapMaterials, out bool stored)
		{
			stored = false;
			if (!thing.Spawned || thing.def.category != 2 || GridsUtility.Fogged(thing.Position, this.map) || ForbidUtility.IsForbidden(thing, Faction.OfPlayer) || this.map.reservationManager.IsReserved(thing) || this.map.physicalInteractionReservationManager.IsReserved(thing))
			{
				return false;
			}
			ISlotGroupParent slotGroupParent = StoreUtility.CurrentHaulDestinationOf(thing) as ISlotGroupParent;
			Thing thing2 = slotGroupParent as Thing;
			if (thing2 != null && thing2.Faction != Faction.OfPlayer)
			{
				return false;
			}
			stored = slotGroupParent != null && slotGroupParent.Accepts(thing);
			return stored || allowMapMaterials;
		}

		// Token: 0x0600004D RID: 77 RVA: 0x00004608 File Offset: 0x00002808
		internal void DeliverTo(Frame frame, bool allowMapMaterials)
		{
			foreach (StockMaterials.Transfer transfer in this.plan)
			{
				bool flag;
				if (transfer.Stack.stackCount < transfer.Count || !this.CanWithdraw(transfer.Stack, allowMapMaterials, out flag))
				{
					return;
				}
			}
			foreach (StockMaterials.Transfer transfer2 in this.plan)
			{
				IntVec3 position = transfer2.Stack.Position;
				Thing thing = transfer2.Stack.SplitOff(transfer2.Count);
				if (!frame.resourceContainer.TryAdd(thing, true))
				{
					if (!GenPlace.TryPlaceThing(thing, position, this.map, 1, null, null, null, 1))
					{
						GenSpawn.Spawn(thing, position, this.map, 0);
					}
					break;
				}
			}
		}

		// Token: 0x0600004E RID: 78 RVA: 0x0000471C File Offset: 0x0000291C
		internal void Clear()
		{
			this.stacksByDef.Clear();
			this.looseStacks.Clear();
			this.plan.Clear();
			this.remainingByDef.Clear();
		}

		// Token: 0x04000035 RID: 53
		private readonly Map map;

		// Token: 0x04000036 RID: 54
		private readonly Dictionary<ThingDef, List<Thing>> stacksByDef = new Dictionary<ThingDef, List<Thing>>();

		// Token: 0x04000037 RID: 55
		private readonly List<Thing> looseStacks = new List<Thing>();

		// Token: 0x04000038 RID: 56
		private readonly List<StockMaterials.Transfer> plan = new List<StockMaterials.Transfer>();

		// Token: 0x04000039 RID: 57
		private readonly Dictionary<ThingDef, int> remainingByDef = new Dictionary<ThingDef, int>();

		// Token: 0x0200001C RID: 28
		private readonly struct Transfer
		{
			// Token: 0x0600006E RID: 110 RVA: 0x00005562 File Offset: 0x00003762
			internal Transfer(Thing stack, int count)
			{
				this.Stack = stack;
				this.Count = count;
			}

			// Token: 0x04000056 RID: 86
			internal readonly Thing Stack;

			// Token: 0x04000057 RID: 87
			internal readonly int Count;
		}
	}
}
