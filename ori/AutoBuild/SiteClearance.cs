using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace AutoBuild
{
	// Token: 0x0200000B RID: 11
	internal sealed class SiteClearance
	{
		// Token: 0x06000043 RID: 67 RVA: 0x00003DE3 File Offset: 0x00001FE3
		internal SiteClearance(Map map)
		{
			this.map = map;
		}

		// Token: 0x06000044 RID: 68 RVA: 0x00003DF4 File Offset: 0x00001FF4
		internal bool CanAdvance(Thing blocker)
		{
			float num;
			return this.TryGetWork(blocker, out num);
		}

		// Token: 0x06000045 RID: 69 RVA: 0x00003E0C File Offset: 0x0000200C
		private bool TryGetWork(Thing blocker, out float workRequired)
		{
			workRequired = 0f;
			if (!blocker.Spawned || FireUtility.IsBurning(blocker) || ForbidUtility.IsForbidden(blocker, Faction.OfPlayer) || this.map.reservationManager.IsReserved(blocker) || this.map.physicalInteractionReservationManager.IsReserved(blocker))
			{
				return false;
			}
			if (blocker.def.category == 2 && blocker.def.EverHaulable)
			{
				return true;
			}
			if (!blocker.def.destroyable)
			{
				return false;
			}
			Plant plant = blocker as Plant;
			if (plant != null && !PlantUtility.TreeMarkedForExtraction(plant))
			{
				workRequired = plant.def.plant.harvestWork;
			}
			else
			{
				Mineable mineable = blocker as Mineable;
				if (mineable != null && mineable.def.mineable)
				{
					workRequired = Mathf.Ceil((float)mineable.HitPoints / (mineable.def.building.isNaturalRock ? 80f : 40f)) * 100f;
				}
				else
				{
					Building building = blocker as Building;
					if (building == null || blocker.def.IsFrame || !building.DeconstructibleBy(Faction.OfPlayer))
					{
						return false;
					}
					workRequired = Deconstruction.WorkRequired(building);
				}
			}
			return true;
		}

		// Token: 0x06000046 RID: 70 RVA: 0x00003F48 File Offset: 0x00002148
		internal bool Advance(Thing blocker, CellRect site, float work, Pawn worker)
		{
			float num;
			if (!this.TryGetWork(blocker, out num))
			{
				return false;
			}
			if (blocker.def.category == 2 && blocker.def.EverHaulable)
			{
				return this.MoveAside(blocker, site);
			}
			if (this.target != blocker)
			{
				this.target = blocker;
				this.workDone = 0f;
			}
			this.workDone += work;
			if (this.workDone < num)
			{
				return true;
			}
			this.target = null;
			this.workDone = 0f;
			Plant plant = blocker as Plant;
			if (plant != null)
			{
				this.Cut(plant, site, worker);
			}
			else
			{
				Mineable mineable = blocker as Mineable;
				if (mineable != null && mineable.def.mineable)
				{
					mineable.Notify_TookMiningDamage(mineable.HitPoints, null);
					mineable.DestroyMined(worker);
				}
				else
				{
					blocker.Destroy(4);
				}
			}
			return true;
		}

		// Token: 0x06000047 RID: 71 RVA: 0x00004018 File Offset: 0x00002218
		private void Cut(Plant plant, CellRect site, Pawn worker)
		{
			ThingDef harvestedThingDef = plant.def.plant.harvestedThingDef;
			int num = ((harvestedThingDef != null) ? plant.YieldNow() : 0);
			IntVec3 position = plant.Position;
			plant.PlantCollected(worker, 3);
			if (!plant.Destroyed)
			{
				plant.Destroy(0);
			}
			if (harvestedThingDef == null || num <= 0)
			{
				return;
			}
			Thing thing = ThingMaker.MakeThing(harvestedThingDef, null);
			thing.stackCount = num;
			if (!GenPlace.TryPlaceThing(thing, position, this.map, 1, null, (IntVec3 cell) => !site.Contains(cell), null, 1))
			{
				GenPlace.TryPlaceThing(thing, position, this.map, 1, null, null, null, 1);
			}
		}

		// Token: 0x06000048 RID: 72 RVA: 0x000040D0 File Offset: 0x000022D0
		private bool MoveAside(Thing item, CellRect site)
		{
			foreach (IntVec3 intVec in GenRadial.RadialCellsAround(item.Position, 12f, false))
			{
				if (GenGrid.InBounds(intVec, this.map) && !site.Contains(intVec) && !GridsUtility.Fogged(intVec, this.map) && GenGrid.Standable(intVec, this.map) && !FireUtility.ContainsStaticFire(intVec, this.map) && GridsUtility.GetItemCount(intVec, this.map) < GridsUtility.GetMaxItemsAllowedInCell(intVec, this.map) && GenPlace.HaulPlaceBlockerIn(item, intVec, this.map, true) == null)
				{
					Rot4 rotation = item.Rotation;
					item.DeSpawn(0);
					GenSpawn.Spawn(item, intVec, this.map, rotation, 0, false, false);
					return true;
				}
			}
			return false;
		}

		// Token: 0x04000032 RID: 50
		private readonly Map map;

		// Token: 0x04000033 RID: 51
		private Thing target;

		// Token: 0x04000034 RID: 52
		private float workDone;
	}
}
